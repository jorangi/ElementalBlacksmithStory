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

namespace ElementalBlacksmithStory.Core
{
    public class EnhancementService : IDisposable, IStartable
    {
        private readonly IPublisher<ChangeWeaponEvent> weaponChangePublisher;
        private readonly IPublisher<ChangeMoneyEvent> moneyChangePublisher;
        IPublisher<PlaySoundEvent> _soundPublisher;
        private readonly IDisposable subscription;
        private SO_WeaponDatabase weaponDatabase;
        private ulong cumulativeCost;
        private ulong cumulativePrice;
        private ulong money = 5000; //임시로 이곳에 두겠음
        private CompositeDisposable _disposables = new();
        public EnhancementService(IAsyncSubscriber<EnhanceRequestEvent> enhaceRequestSubscriber,
                                    IPublisher<ChangeWeaponEvent> weaponChangePublisher,
                                    IPublisher<ChangeMoneyEvent> moneyChangePublisher,
                                    IPublisher<PlaySoundEvent> soundPublisher,
                                    SO_WeaponDatabase weaponDatabase,
                                    ISubscriber<SellEvent> sellEventSubscriber
                                    )
        {
            //오디오 재생은 이벤트로 변경할 것임 일단 테스트
            this.weaponDatabase = weaponDatabase;
            this.weaponChangePublisher = weaponChangePublisher;
            this.moneyChangePublisher = moneyChangePublisher;
            this._soundPublisher = soundPublisher;
            this.moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId, money));
            this.subscription = enhaceRequestSubscriber.Subscribe(OnEnhanceRequested).AddTo(_disposables);
            sellEventSubscriber.Subscribe(e=>
            {
                money += e.Price;
                moneyChangePublisher.Publish(new ChangeMoneyEvent(out uint tempId2, money));
                weaponChangePublisher.Publish(new ChangeWeaponEvent(new Weapon(weaponDatabase.GetWeapon(10001)), 10001, 0));
                totalCost = 0;
                cumulativeCost = 0;
                cumulativePrice = 0;
                soundPublisher.Publish(new PlaySoundEvent(40103));
            }).AddTo(_disposables);
        }
        public void Start()
        {
            weaponChangePublisher.Publish(new ChangeWeaponEvent(new Weapon(weaponDatabase.GetWeapon(10001)), 10001, 0));
        }
        private ulong totalCost;
        private async UniTask OnEnhanceRequested(EnhanceRequestEvent request, CancellationToken cancellationToken)
        {
            Weapon weapon = request.Weapon;
            SO_WeaponData _currentWeaponData = weaponDatabase.GetWeapon(weapon.WeaponId);

            if(_currentWeaponData == null)
            {
                Debug.LogError($"[EnhancementService] 강화 요청 처리 무효됨 -> 현재 무기 데이터를 찾을 수 없음");
                return;
            }
            if(_currentWeaponData.recipes.Count == 0)
            {
                Debug.LogWarning("[EnhancementService]더 이상 강화할 수 없는 무기입니다.");
                return;
            }

            SO_CraftRecipe recipe = _currentWeaponData.recipes[0];
            RecipeOutcome result = recipe.recipeOutcome;
            
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
            
            bool success = UnityEngine.Random.value <= result.chance;

            if (success)
            {
                _soundPublisher.Publish(new PlaySoundEvent(40102));
                var handle = Addressables.LoadAssetAsync<SO_WeaponData>(result.resultWeapon.Id.ToString());
                _currentWeaponData = await handle.ToUniTask(cancellationToken: cancellationToken);

                weapon.PushCost(_currentWeaponData.cost);
                weapon.PushPrice(result.resultWeapon.basePrice);
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 성공, 결과물: {_currentWeaponData.weaponName}");
            }
            else
            {
                _soundPublisher.Publish(new PlaySoundEvent(40101));
                weapon.PopCost();
                weapon.PopPrice();
                _currentWeaponData = result.defaultFailWeapon;
                weapon.SetData(_currentWeaponData);
                Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 실패, 결과물: {_currentWeaponData.weaponName}");
            }
            float margin = _currentWeaponData.margin > 1f ? UnityEngine.Random.Range(1.0f, _currentWeaponData.margin) : _currentWeaponData.margin;
            weapon.SetMargin(margin);
            Debug.Log($"[EnhancementService] 현재 무기: {_currentWeaponData.weaponName}, 가격: {weapon.Price}({weapon.BasePrice}x{margin}) / 총 소모비용: {totalCost}");
            weaponChangePublisher.Publish(new ChangeWeaponEvent(weapon:weapon, nextWeaponId:_currentWeaponData.Id, totalCost:totalCost));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}