using System;
using MessagePipe;
using UnityEngine;
using ElementalBlacksmithStory.Events;
using VContainer.Unity;
using UnityEngine.AddressableAssets;
using ElementalBlacksmithStory.Data;
using Cysharp.Threading.Tasks;
using System.Threading;
using R3;
using System.Collections.Generic;
using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.Core
{
    public class EnhancementService : IDisposable, IStartable
    {
        private readonly IPublisher<ChangeWeaponEvent> weaponChangePublisher;
        private readonly IPublisher<ChangeMoneyEvent> moneyChangePublisher;
        private readonly IPublisher<NextRecipeChangedEvent> _nextRecipePublisher;
        IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly IDisposable subscription;
        private SO_WeaponDatabase weaponDatabase;
        private readonly MaterialInventory _materialInventory;
        private ulong money = 5000;
        List<uint> cachedPinnedWeaponEnhancePath = null;
        uint pinnedRecipeId;
        private CompositeDisposable _disposables = new();
        private Weapon _currentWeapon;
        private SO_CraftRecipe _currentRecipe;
        private readonly Dictionary<uint, uint> _selectedMaterials = new();

        public EnhancementService(IAsyncSubscriber<EnhanceRequestEvent> enhaceRequestSubscriber,
                                    IPublisher<ChangeWeaponEvent> weaponChangePublisher,
                                    IPublisher<ChangeMoneyEvent> moneyChangePublisher,
                                    IPublisher<PlaySoundEvent> soundPublisher,
                                    IPublisher<NextRecipeChangedEvent> nextRecipePublisher,
                                    SO_WeaponDatabase weaponDatabase,
                                    MaterialInventory materialInventory,
                                    ISubscriber<SellEvent> sellEventSubscriber,
                                    ISubscriber<ChangeRecipeFlagEvent> changeRecipeFlagSubscriber,
                                    ISubscriber<SubmitMaterialEvent> submitMaterialSubscriber
                                    )
        {
            this.weaponDatabase = weaponDatabase;
            this._materialInventory = materialInventory;
            this.weaponChangePublisher = weaponChangePublisher;
            this.moneyChangePublisher = moneyChangePublisher;
            this._soundPublisher = soundPublisher;
            this._nextRecipePublisher = nextRecipePublisher;
            this.moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
            this.subscription = enhaceRequestSubscriber.Subscribe(OnEnhanceRequested).AddTo(_disposables);
            
            submitMaterialSubscriber.Subscribe(e =>
            {
                if (e.Amount == 0)
                {
                    _selectedMaterials.Remove(e.MaterialId);
                }
                else
                {
                    _selectedMaterials[e.MaterialId] = e.Amount;
                }
            }).AddTo(_disposables);

            sellEventSubscriber.Subscribe(e=>
            {
                money += e.Price;
                moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId2, money));
                _currentWeapon = new Weapon(weaponDatabase.GetWeapon(10001));
                weaponChangePublisher.Publish(new ChangeWeaponEvent(_currentWeapon, 10001, 0));
                totalCost = 0;
                soundPublisher.Publish(new PlaySoundEvent(40103));
                cachedPinnedWeaponEnhancePath = null;
                _selectedMaterials.Clear();
                UpdateNextRecipe();
            }).AddTo(_disposables);

            changeRecipeFlagSubscriber.Subscribe(e=>
            {
                pinnedRecipeId = e._recipeId;
                cachedPinnedWeaponEnhancePath = null;
                UpdateNextRecipe();
            }).AddTo(_disposables);
        }

        public void Start()
        {
            _currentWeapon = new Weapon(weaponDatabase.GetWeapon(10001));
            weaponChangePublisher.Publish(new ChangeWeaponEvent(_currentWeapon, 10001, 0));
            cachedWeaponId = 10001;
            UpdateNextRecipe();
        }

        private void UpdateNextRecipe()
        {
            if (_currentWeapon == null) return;
            SO_WeaponData currentWeaponData = weaponDatabase.GetWeapon(_currentWeapon.WeaponId);
            if (currentWeaponData == null || currentWeaponData.recipes == null || currentWeaponData.recipes.Count == 0)
            {
                _currentRecipe = null;
                _nextRecipePublisher.Publish(new NextRecipeChangedEvent(null));
                return;
            }

            if (pinnedRecipeId != 0)
            {
                if (cachedPinnedWeaponEnhancePath == null || cachedWeaponId != _currentWeapon.WeaponId)
                {
                    cachedWeaponId = _currentWeapon.WeaponId;
                    var path = WeaponTreePathFinder.FindPath(currentWeaponData.Id, pinnedRecipeId, weaponDatabase);
                    if (path != null && path.Count > 1)
                    {
                        path.RemoveAt(0);
                        cachedPinnedWeaponEnhancePath = path;
                    }
                    else
                    {
                        cachedPinnedWeaponEnhancePath = null;
                    }
                }
            }
            else
            {
                cachedPinnedWeaponEnhancePath = null;
            }

            _currentRecipe = null;
            if (cachedPinnedWeaponEnhancePath == null || cachedPinnedWeaponEnhancePath.Count == 0)
            {
                _currentRecipe = currentWeaponData.recipes[0];
            }
            else
            {
                foreach (var r in currentWeaponData.recipes)
                {
                    if (r.recipeOutcome.resultWeapon.Id == cachedPinnedWeaponEnhancePath[0])
                    {
                        _currentRecipe = r;
                        break;
                    }
                }
            }

            _nextRecipePublisher.Publish(new NextRecipeChangedEvent(_currentRecipe));
        }

        private ulong totalCost;
        private uint cachedWeaponId = 0;
        private async UniTask OnEnhanceRequested(EnhanceRequestEvent request, CancellationToken cancellationToken)
        {
            Weapon weapon = request.Weapon;
            _currentWeapon = weapon;
            if (_currentRecipe == null)
            {
                Debug.LogWarning("[EnhancementService] 진행 가능한 강화 레시피가 없습니다.");
                return;
            }

            RecipeOutcome result = _currentRecipe.recipeOutcome;
            
            if (_currentRecipe.recipeMaterials != null)
            {
                foreach (var rm in _currentRecipe.recipeMaterials)
                {
                    if (rm.material == null) continue;
                    if (_materialInventory.GetCount(rm.material) < rm.count)
                    {
                        Debug.LogWarning($"[EnhancementService] 재료가 부족합니다: {rm.material.materialName} ({_materialInventory.GetCount(rm.material)}/{rm.count})");
                        return;
                    }
                }
            }

            ulong cost = weapon.Cost;
            if(money < cost)
            {
                Debug.LogWarning($"[EnhancementService]강화 비용이 부족합니다. 요구비용: {weapon.Cost}, 현재보유금액: {money}");
                return;
            }
            Debug.Log(money + " - " + cost + " = " + (money - cost));
            money -= cost;
            moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
            totalCost += cost;

            if (_currentRecipe.recipeMaterials != null)
            {
                foreach (var rm in _currentRecipe.recipeMaterials)
                {
                    if (rm.material == null) continue;
                    _materialInventory.GetMaterial(rm.material, rm.count);
                }
            }
            
            bool success = UnityEngine.Random.value <= result.chance;

            SO_WeaponData currentWeaponData;
            if (success)
            {
                _soundPublisher.Publish(new PlaySoundEvent(40104));
                var handle = Addressables.LoadAssetAsync<SO_WeaponData>(result.resultWeapon.Id.ToString());
                currentWeaponData = await handle.ToUniTask(cancellationToken: cancellationToken);

                weapon.PushEnhanceStep(currentWeaponData.Id, result.resultWeapon.basePrice, currentWeaponData.cost);
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 성공, 결과물: {currentWeaponData.weaponName}");
                if(cachedPinnedWeaponEnhancePath != null && cachedPinnedWeaponEnhancePath.Count > 0)
                {
                    cachedPinnedWeaponEnhancePath.RemoveAt(0);
                }
                cachedWeaponId = currentWeaponData.Id;
            }
            else
            {
                _soundPublisher.Publish(new PlaySoundEvent(40101));
                if(_currentWeapon.WeaponId != result.defaultFailWeapon.Id)
                {
                    weapon.RollbackTo(result.defaultFailWeapon.Id, result.defaultFailWeapon.cost);
                }
                currentWeaponData = result.defaultFailWeapon;
                weapon.SetData(currentWeaponData);
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 실패, 결과물: {currentWeaponData.weaponName}");
                cachedPinnedWeaponEnhancePath = null;
                cachedWeaponId = currentWeaponData.Id;
            }
            float margin = currentWeaponData.margin > 1f ? UnityEngine.Random.Range(1.0f, currentWeaponData.margin) : currentWeaponData.margin;
            weapon.SetMargin(margin);
            Debug.Log($"[EnhancementService] 현재 무기: {currentWeaponData.weaponName}, 가격: {weapon.Price}({weapon.BasePrice}x{margin}) / 총 소모비용: {totalCost}");
            weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon:weapon, nextWeaponId:currentWeaponData.Id, totalCost:totalCost));
            
            UpdateNextRecipe();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}