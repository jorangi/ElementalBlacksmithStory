using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class EnhancementService : IDisposable, IStartable
    {
        private readonly IPublisher<ChangeWeaponEvent> weaponChangePublisher;
        private readonly IPublisher<ChangeMoneyEvent> moneyChangePublisher;
        private readonly IPublisher<ChangeSelectedMaterialsEvent> _changedSelectedMaterialPublisher;
        private readonly IPublisher<UpdateEnhanceChanceEvent> _updateEnhanceChancePublisher;
        IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly IDisposable subscription;
        private SO_WeaponDatabase weaponDatabase;
        private SO_MaterialDatabase materialDatabase;
        private readonly MaterialInventory _materialInventory;
        private readonly EquipmentInventory _equipmentInventory;
        private readonly RecipeKeyHelper _recipeKeyHelper;
        private readonly RecipeUnlockService _recipeUnlockService;
        private readonly WeaponRollbackService _weaponRollbackService;
        private ulong money = 5000;
        List<uint> cachedPinnedWeaponEnhancePath = null;
        uint pinnedRecipeId;
        private CompositeDisposable _disposables = new();
        private Weapon _currentWeapon;
        private SO_CraftRecipe _currentRecipe;
        private readonly Dictionary<BaseMaterialData, uint> _selectedMaterials = new();
        private bool _fixedRecipe;
        public bool FixedRecipe
        {
            get => _fixedRecipe;
            set
            {
                if (_fixedRecipe != value)
                {
                    _fixedRecipe = value;
                    Debug.Log($"[EnhancementService] 레시피 고정 상태 변경됨: {_fixedRecipe} (현재 목표 ID: {pinnedRecipeId})");
                    if (_fixedRecipe && pinnedRecipeId != 0)
                    {
                        cachedPinnedWeaponEnhancePath = null;
                        UpdateNextRecipe();
                    }
                }
            }
        }
        private bool _isHardMode = true;
        public bool IsHardMode
        {
            get => _isHardMode;
            set => _isHardMode = value;
        }

        public EnhancementService(IPublisher<ChangeWeaponEvent> weaponChangePublisher,
                                    IPublisher<ChangeMoneyEvent> moneyChangePublisher,
                                    ISubscriber<ChangeMoneyEvent> moneyChangeSubscriber,
                                    IPublisher<PlaySoundEvent> soundPublisher,
                                    IPublisher<ChangeSelectedMaterialsEvent> changedSelectedMaterialPublisher,
                                    IPublisher<UpdateEnhanceChanceEvent> updateEnhanceChancePublisher,
                                    SO_WeaponDatabase weaponDatabase,
                                    SO_MaterialDatabase materialDatabase,
                                    MaterialInventory materialInventory,
                                    EquipmentInventory equipmentInventory,
                                    RecipeKeyHelper recipeKeyHelper,
                                    RecipeUnlockService recipeUnlockService,
                                    WeaponRollbackService weaponRollbackService,
                                    ISubscriber<SellEvent> sellEventSubscriber,
                                    ISubscriber<ChangeRecipeFlagEvent> changeRecipeFlagSubscriber,
                                    ISubscriber<SubmitMaterialEvent> submitMaterialSubscriber
                                    )
        {
            this.weaponDatabase = weaponDatabase;
            this.materialDatabase = materialDatabase;
            this._materialInventory = materialInventory;
            this._equipmentInventory = equipmentInventory;
            this._recipeKeyHelper = recipeKeyHelper;
            this._recipeUnlockService = recipeUnlockService;
            this._weaponRollbackService = weaponRollbackService;
            this.weaponChangePublisher = weaponChangePublisher;
            this.moneyChangePublisher = moneyChangePublisher;
            this._soundPublisher = soundPublisher;
            this._changedSelectedMaterialPublisher = changedSelectedMaterialPublisher;
            this._updateEnhanceChancePublisher = updateEnhanceChancePublisher;

            moneyChangeSubscriber.Subscribe(e =>
            {
                money = e.MoneyChange;
            }).AddTo(_disposables);

            submitMaterialSubscriber.Subscribe(e =>
            {
                BaseMaterialData mat = RecipeKeyHelper.IsWeaponId(e.MaterialId)
                    ? (BaseMaterialData)weaponDatabase.Get(e.MaterialId)
                    : (BaseMaterialData)materialDatabase.Get(e.MaterialId);

                if (mat == null) return;

                if (e.Amount == 0)
                {
                    _selectedMaterials.Remove(mat);
                }
                else
                {
                    _selectedMaterials[mat] = e.Amount;
                }
                // 유저가 재료를 수동 선택/조정할 때는 기존 선택을 덮어쓰거나 지우지 않음
                UpdateNextRecipe(autoFillMaterials: false);
            }).AddTo(_disposables);

            sellEventSubscriber.Subscribe(e =>
            {
                money += e.Price;
                moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
                totalCost = 0;
                soundPublisher.Publish(new PlaySoundEvent(40103));

                if (!_fixedRecipe)
                {
                    pinnedRecipeId = 0;
                }
                cachedPinnedWeaponEnhancePath = null;
                _selectedMaterials.Clear();

                _currentWeapon = new Weapon(weaponDatabase.Get(10001));
                cachedWeaponId = 10001;

                Debug.Log($"[EnhancementService] 판매됨 -> 레시피 고정: {_fixedRecipe}, 목표 무기: {pinnedRecipeId}");
                UpdateNextRecipe(autoFillMaterials: true);

                weaponChangePublisher.Publish(new ChangeWeaponEvent(_currentWeapon, 10001, 0));
            }).AddTo(_disposables);

            changeRecipeFlagSubscriber.Subscribe(e =>
            {
                pinnedRecipeId = e._recipeId;
                cachedPinnedWeaponEnhancePath = null;
                UpdateNextRecipe(autoFillMaterials: true);
            }).AddTo(_disposables);
        }

        public void Start()
        {
            _currentWeapon = new Weapon(weaponDatabase.Get(10001));
            weaponChangePublisher.Publish(new ChangeWeaponEvent(_currentWeapon, 10001, 0));
            cachedWeaponId = 10001;
            UpdateNextRecipe(autoFillMaterials: true, force: true);
            this.moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
        }
        /// <summary>
        /// 다음 레시피 업데이트
        /// </summary>
        /// <param name="autoFillMaterials">핀 레시피의 재료를 자동으로 채울지 여부 (유저가 수동 투입할 때는 false)</param>
        /// <param name="force">강제성</param>
        private void UpdateNextRecipe(bool autoFillMaterials = true, bool force = false)
        {
            if (_currentWeapon == null) return;
            SO_WeaponData currentWeaponData = _currentWeapon.Data;

            // 현재 무기 데이터가 없거나, 현재 무기가 레시피가 없을시
            if (currentWeaponData == null || currentWeaponData.recipes == null || currentWeaponData.recipes.Count == 0)
            {
                if (_currentRecipe != null || force)
                {
                    _currentRecipe = null;
                    _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(0, 0));
                }
                return;
            }

            // 핀으로 지정된 목표 무기가 있고, 현재 무기와 다른 경우 경로 탐색
            if (pinnedRecipeId != 0 && pinnedRecipeId != currentWeaponData.Id)
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
                        if (!_fixedRecipe)
                        {
                            pinnedRecipeId = 0;
                        }
                        cachedPinnedWeaponEnhancePath = null;
                    }
                }
            }
            else
            {
                // 현재 무기가 목표 무기와 같거나(현재 노드 선택 또는 도달), 핀이 없는 경우
                if (pinnedRecipeId == currentWeaponData.Id && !_fixedRecipe)
                {
                    pinnedRecipeId = 0;
                }
                cachedPinnedWeaponEnhancePath = null;
            }

            SO_CraftRecipe newRecipe = null;

            // 1. 유저가 직접 재료를 선택/수정 중이거나, 핀 경로가 없는 경우 (또는 현재 단계 노드일 때):
            // -> 현재 _selectedMaterials(유저가 선택한 재료)를 기반으로 완전 일치 레시피 탐색!
            if (!autoFillMaterials || cachedPinnedWeaponEnhancePath == null || cachedPinnedWeaponEnhancePath.Count == 0)
            {
                newRecipe = _recipeKeyHelper.FindMatchingRecipe(_selectedMaterials, currentWeaponData.recipes);
                _currentRecipe = newRecipe;

                if (newRecipe != null)
                {
                    // 수동 조작 시 ClearMaterials를 false로 하여 유저가 선택한 슬롯을 유지
                    _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(false, _selectedMaterials));
                    if (!_recipeUnlockService.IsUnlocked(newRecipe.Id))
                    {
                        ulong cost = _currentWeapon.BasePrice;
                        float chance = -1f;
                        foreach (var kv in _selectedMaterials)
                        {
                            cost += kv.Key.Value * kv.Value;
                        }
                        _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(chance, cost));
                        return;
                    }
                    _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(newRecipe.recipeOutcome.chance, _currentWeapon.Cost));
                    return;
                }
                else
                {
                    // 일치하는 레시피가 없는 경우 (없는 레시피 조합 또는 재료 미투입)
                    _currentRecipe = null;
                    ulong cost = _currentWeapon.BasePrice;
                    if (_selectedMaterials != null)
                    {
                        foreach (var kv in _selectedMaterials)
                        {
                            cost += kv.Key.Value * kv.Value;
                        }
                    }
                    _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(-1f, cost));
                    _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(false, _selectedMaterials));
                    return;
                }
            }

            // 2. autoFillMaterials가 true이고 핀 경로가 있는 경우:
            // -> 핀으로 지정된 다음 단계 레시피를 찾고 재료를 자동 선택!
            foreach (var r in currentWeaponData.recipes)
            {
                if (r.recipeOutcome.resultWeapon.Id == cachedPinnedWeaponEnhancePath[0])
                {
                    newRecipe = r;
                    break;
                }
            }

            if (newRecipe != null)
            {
                _currentRecipe = newRecipe;

                if (_recipeUnlockService.IsUnlocked(newRecipe.Id))
                {
                    _selectedMaterials.Clear();
                    if (newRecipe.recipeMaterials != null)
                    {
                        foreach (var rm in newRecipe.recipeMaterials)
                        {
                            if (rm.material != null)
                            {
                                _selectedMaterials[rm.material] = rm.count;
                            }
                        }
                    }
                    _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(true, _selectedMaterials));
                    _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(newRecipe.recipeOutcome.chance, _currentWeapon.Cost));
                }
                else
                {
                    _selectedMaterials.Clear();
                    _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(true));
                    _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(-1f, _currentWeapon.BasePrice));
                }
            }
            else
            {
                _currentRecipe = null;
                _selectedMaterials.Clear();
                _updateEnhanceChancePublisher.Publish(new UpdateEnhanceChanceEvent(-1f, _currentWeapon.BasePrice));
                _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(true));
            }
        }
        private ulong totalCost;
        private uint cachedWeaponId = 0;
        /// <summary>
        /// 강제 시도
        /// </summary>
        /// <param name="weapon"></param>
        /// <param name="materials"></param>
        /// <returns></returns>
        public EnhanceResult TryEnhance(Weapon weapon, IReadOnlyDictionary<BaseMaterialData, uint> materials = null)
        {
            _currentWeapon = weapon;
            materials ??= _selectedMaterials;
            ulong cost;

            // 더 이상 강화가 불가능한 무기면 시도 차단
            if (_currentWeapon.Data.recipes == null || _currentWeapon.Data.recipes.Count == 0)
            {
                Debug.LogWarning("[EnhancementService] 진행 가능한 강화 레시피가 없습니다.");
                return EnhanceResult.NOMOREENHANCEMENT;
            }

            // 핀이 꽂혀있지 않다면, 현재 투입된 재료 기반으로 레시피 탐색 (이전 레시피 잔류 방지)
            if (pinnedRecipeId == 0 || _currentRecipe == null)
            {
                _currentRecipe = _recipeKeyHelper.FindMatchingRecipe(materials, _currentWeapon.Data.recipes);
            }

            // [미해금 / 미지의 레시피 조합 처리]
            // 1. 레시피가 아예 없거나(일치하지 않는 재료 조합), 2. 아직 해금되지 않은 레시피인 경우
            if (_currentRecipe == null || !_recipeUnlockService.IsUnlocked(_currentRecipe.Id))
            {
                // 1) 조합 결과가 아예 없는 경우 -> 확률 0% (무조건 실패 처리, 비용 및 재료 소모)
                if (_currentRecipe == null)
                {
                    cost = _currentWeapon.BasePrice;
                    foreach (var kv in materials)
                    {
                        //실패처리 코스트: 재료값
                        cost += kv.Value * kv.Key.Value;
                    }

                    if (money < cost)
                    {
                        Debug.LogWarning($"[EnhancementService] 강화 비용이 부족합니다. 요구비용: {cost}, 현재보유금액: {money}");
                        return EnhanceResult.NEEDSMOREMONEY;
                    }

                    _soundPublisher.Publish(new PlaySoundEvent(40101));
                    money -= cost;
                    totalCost += cost;
                    moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint failDiscoverRecipe, money));

                    ConsumeMaterials(materials);

                    if (_isHardMode)
                    {
                        // 하드모드: 강화 1단계 강등 처리 (기본 무기인 경우 제외)
                        if (_weaponRollbackService.TryRollbackOneStep(weapon))
                        {
                            cachedPinnedWeaponEnhancePath = null;
                            cachedWeaponId = weapon.WeaponId;
                            Debug.LogWarning($"[EnhancementService][하드모드] 없는 레시피로 강화 실패 -> 1단계 강등됨: {weapon.Data?.weaponName ?? weapon.WeaponId.ToString()}");
                        }
                        else
                        {
                            Debug.LogWarning("[EnhancementService][하드모드] 없는 레시피로 강화 실패 -> 기본 무기 상태이므로 강등되지 않음");
                        }
                    }
                    else
                    {
                        // 이지모드: 무기 강등 없이 유지, 재료 및 비용만 소모
                        Debug.LogWarning($"[EnhancementService][이지모드] 없는 레시피로 강화 실패 -> 무기 유지: {weapon.Data?.weaponName}");
                    }

                    weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon: weapon, nextWeaponId: weapon.WeaponId, totalCost: totalCost));
                    UpdateNextRecipe();
                    Debug.LogWarning("[EnhancementService] 재료가 일치하는 레시피가 없습니다.");
                    return EnhanceResult.INVAILDRECIPE;
                }

                // 2) 조합 결과는 맞지만 아직 해금되지 않은 레시피인 경우
                cost = _currentWeapon.BasePrice;
                foreach (var kv in materials)
                {
                    //실패처리 코스트: 재료값
                    cost += kv.Value * kv.Key.Value;
                }

                if (money < cost)
                {
                    Debug.LogWarning($"[EnhancementService] 강화 비용이 부족합니다. 요구비용: {cost}, 현재보유금액: {money}");
                    return EnhanceResult.NEEDSMOREMONEY;
                }

                money -= cost;
                totalCost += cost;
                moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint discoverRecipe, money));

                ConsumeMaterials(materials);

                // 조합 결과가 존재하므로 정해진 본래 확률로 성공 판정
                if (UnityEngine.Random.value <= _currentRecipe.recipeOutcome.chance)
                {
                    _soundPublisher.Publish(new PlaySoundEvent(40104));

                    weapon.PushEnhanceStep(_currentRecipe.recipeOutcome.resultWeapon.Id, _currentRecipe.recipeOutcome.resultWeapon.basePrice, _currentRecipe.recipeOutcome.resultWeapon.cost);
                    Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 성공, 결과물: {_currentRecipe.recipeOutcome.resultWeapon.weaponName}");

                    cachedWeaponId = _currentRecipe.recipeOutcome.resultWeapon.Id;

                    float margin = _currentRecipe.recipeOutcome.resultWeapon.margin > 1f ? UnityEngine.Random.Range(1.0f, _currentRecipe.recipeOutcome.resultWeapon.margin) : _currentRecipe.recipeOutcome.resultWeapon.margin;
                    weapon.SetMargin(margin);
                    Debug.Log($"[EnhancementService] 현재 무기: {_currentRecipe.recipeOutcome.resultWeapon.weaponName}, 가격: {weapon.Price}({weapon.BasePrice}x{margin}) / 총 소모비용: {totalCost}");
                    weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon: weapon, nextWeaponId: _currentRecipe.recipeOutcome.resultWeapon.Id, totalCost: totalCost));

                    // 새로운 레시피 해금
                    _recipeUnlockService.Unlock(_currentRecipe.Id);
                    Debug.Log($"[EnhancementService] 새로운 레시피{_currentRecipe.recipeName}을(를) 발견했습니다.");
                    UpdateNextRecipe();
                    return EnhanceResult.SUCESS;
                }
                else // 조합 결과는 맞지만 확률로 실패 판정
                {
                    _soundPublisher.Publish(new PlaySoundEvent(40101));
                    _weaponRollbackService.RollbackTo(weapon, _currentRecipe.recipeOutcome.defaultFailWeapon);
                    Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 실패, 결과물: {_currentRecipe.recipeOutcome.defaultFailWeapon.weaponName}");
                    cachedPinnedWeaponEnhancePath = null;
                    cachedWeaponId = _currentRecipe.recipeOutcome.defaultFailWeapon.Id;

                    Debug.Log($"[EnhancementService] 현재 무기: {weapon.Data?.weaponName}, 가격: {weapon.Price} / 총 소모비용: {totalCost}");
                    weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon: weapon, nextWeaponId: _currentRecipe.recipeOutcome.defaultFailWeapon.Id, totalCost: totalCost));

                    UpdateNextRecipe();
                    return EnhanceResult.FAIL;
                }
            }

            RecipeOutcome result = _currentRecipe.recipeOutcome;

            // 레시피 선택됐을 때, 재료 판별
            if (_currentRecipe.recipeMaterials != null)
            {
                foreach (var rm in _currentRecipe.recipeMaterials)
                {
                    if (rm.material == null) continue;
                    uint id = rm.material.Id;
                    if (RecipeKeyHelper.IsWeaponId(id))
                    {
                        if (_equipmentInventory.GetCountByWeaponId(id) < rm.count)//무기로된 재료 부족한지 판단
                        {
                            Debug.LogWarning($"[EnhancementService] 무기 재료가 부족합니다: {rm.material.materialName} ({_equipmentInventory.GetCountByWeaponId(id)}/{rm.count})");
                            return EnhanceResult.LACKOFMATERIALS_WEAPONS;
                        }
                    }
                    else
                    {
                        if (_materialInventory.GetCount(rm.material) < rm.count) //일반 재료가 부족한건지
                        {
                            Debug.LogWarning($"[EnhancementService] 재료가 부족합니다: {rm.material.materialName} ({_materialInventory.GetCount(rm.material)}/{rm.count})");
                            return EnhanceResult.LACKOFMATERIALS_PUREMATERIALS;
                        }
                    }
                }
            }

            cost = weapon.Cost;
            if (money < cost)
            {
                Debug.LogWarning($"[EnhancementService]강화 비용이 부족합니다. 요구비용: {weapon.Cost}, 현재보유금액: {money}");
                return EnhanceResult.NEEDSMOREMONEY;
            }
            money -= cost;
            moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
            totalCost += cost;

            if (_currentRecipe.recipeMaterials != null)
            {
                foreach (var rm in _currentRecipe.recipeMaterials)
                {
                    if (rm.material == null) continue;
                    uint id = rm.material.Id;
                    if (RecipeKeyHelper.IsWeaponId(id))
                    {
                        _equipmentInventory.RemoveByWeaponId(id, (int)rm.count);
                    }
                    else
                    {
                        _materialInventory.Get(rm.material, rm.count);
                    }
                }
            }

            _selectedMaterials.Clear();
            _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(true));

            bool success = UnityEngine.Random.value <= result.chance;

            SO_WeaponData currentWeaponData;
            if (success)
            {
                _soundPublisher.Publish(new PlaySoundEvent(40104));
                currentWeaponData = weaponDatabase.Get(result.resultWeapon.Id) ?? result.resultWeapon;

                weapon.PushEnhanceStep(currentWeaponData.Id, result.resultWeapon.basePrice, currentWeaponData.cost);
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 성공, 결과물: {currentWeaponData.weaponName}");
                if (cachedPinnedWeaponEnhancePath != null && cachedPinnedWeaponEnhancePath.Count > 0)
                {
                    cachedPinnedWeaponEnhancePath.RemoveAt(0);
                }
                cachedWeaponId = currentWeaponData.Id;

                float margin = currentWeaponData.margin > 1f ? UnityEngine.Random.Range(1.0f, currentWeaponData.margin) : currentWeaponData.margin;
                weapon.SetMargin(margin);
                Debug.Log($"[EnhancementService] 현재 무기: {currentWeaponData.weaponName}, 가격: {weapon.Price}({weapon.BasePrice}x{margin}) / 총 소모비용: {totalCost}");
                weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon: weapon, nextWeaponId: currentWeaponData.Id, totalCost: totalCost));

                UpdateNextRecipe();
                return EnhanceResult.SUCESS;
            }
            else
            {
                _soundPublisher.Publish(new PlaySoundEvent(40101));
                _weaponRollbackService.RollbackTo(weapon, result.defaultFailWeapon);
                currentWeaponData = weapon.Data ?? result.defaultFailWeapon;
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 실패, 결과물: {currentWeaponData.weaponName}");
                cachedPinnedWeaponEnhancePath = null;
                cachedWeaponId = currentWeaponData.Id;

                Debug.Log($"[EnhancementService] 현재 무기: {currentWeaponData.weaponName}, 가격: {weapon.Price} / 총 소모비용: {totalCost}");
                weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon: weapon, nextWeaponId: currentWeaponData.Id, totalCost: totalCost));

                UpdateNextRecipe();
                return EnhanceResult.FAIL;
            }

        }

        /// <summary>
        /// 투입된 재료를 인벤토리에서 차감하고 선택 상태를 초기화
        /// </summary>
        private void ConsumeMaterials(IReadOnlyDictionary<BaseMaterialData, uint> mats)
        {
            if (mats == null) return;

            foreach (var kv in mats)
            {
                if (kv.Key == null || kv.Value == 0) continue;
                uint id = kv.Key.Id;
                if (RecipeKeyHelper.IsWeaponId(id))
                {
                    _equipmentInventory.RemoveByWeaponId(id, (int)kv.Value);
                }
                else if (kv.Key is SO_MaterialData materialData)
                {
                    _materialInventory.Get(materialData, kv.Value);
                }
            }

            _selectedMaterials.Clear();
            _changedSelectedMaterialPublisher.Publish(new ChangeSelectedMaterialsEvent(true));
        }

        [Obsolete("강화는 이벤트 기반보다 직접 실행하는 게 디버깅추적 등 흐름 제어에 용이할듯")]
        private void OnEnhanceRequested(EnhanceRequestEvent request)
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
                    uint id = rm.material.Id;
                    if (RecipeKeyHelper.IsWeaponId(id))
                    {
                        if (_equipmentInventory.GetCountByWeaponId(id) < rm.count)
                        {
                            Debug.LogWarning($"[EnhancementService] 무기 재료가 부족합니다: {rm.material.materialName} ({_equipmentInventory.GetCountByWeaponId(id)}/{rm.count})");
                            return;
                        }
                    }
                    else
                    {
                        if (_materialInventory.GetCount(rm.material) < rm.count)
                        {
                            Debug.LogWarning($"[EnhancementService] 재료가 부족합니다: {rm.material.materialName} ({_materialInventory.GetCount(rm.material)}/{rm.count})");
                            return;
                        }
                    }
                }
            }

            ulong cost = weapon.Cost;
            if (money < cost)
            {
                Debug.LogWarning($"[EnhancementService]강화 비용이 부족합니다. 요구비용: {weapon.Cost}, 현재보유금액: {money}");
                return;
            }
            money -= cost;
            moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
            totalCost += cost;

            if (_currentRecipe.recipeMaterials != null)
            {
                foreach (var rm in _currentRecipe.recipeMaterials)
                {
                    if (rm.material == null) continue;
                    uint id = rm.material.Id;
                    if (RecipeKeyHelper.IsWeaponId(id))
                    {
                        _equipmentInventory.RemoveByWeaponId(id, (int)rm.count);
                    }
                    else
                    {
                        _materialInventory.Get(rm.material, rm.count);
                    }
                }
            }

            bool success = UnityEngine.Random.value <= result.chance;

            SO_WeaponData currentWeaponData;
            if (success)
            {
                _soundPublisher.Publish(new PlaySoundEvent(40104));
                currentWeaponData = weaponDatabase.Get(result.resultWeapon.Id) ?? result.resultWeapon;

                weapon.PushEnhanceStep(currentWeaponData.Id, result.resultWeapon.basePrice, currentWeaponData.cost);
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 성공, 결과물: {currentWeaponData.weaponName}");
                if (cachedPinnedWeaponEnhancePath != null && cachedPinnedWeaponEnhancePath.Count > 0)
                {
                    cachedPinnedWeaponEnhancePath.RemoveAt(0);
                }
                cachedWeaponId = currentWeaponData.Id;
            }
            else
            {
                _soundPublisher.Publish(new PlaySoundEvent(40101));
                _weaponRollbackService.RollbackTo(weapon, result.defaultFailWeapon);
                currentWeaponData = weapon.Data ?? result.defaultFailWeapon;
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 실패, 결과물: {currentWeaponData.weaponName}");
                cachedPinnedWeaponEnhancePath = null;
                cachedWeaponId = currentWeaponData.Id;
            }
            Debug.Log($"[EnhancementService] 현재 무기: {currentWeaponData.weaponName}, 가격: {weapon.Price} / 총 소모비용: {totalCost}");
            weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon: weapon, nextWeaponId: currentWeaponData.Id, totalCost: totalCost));

            UpdateNextRecipe();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}