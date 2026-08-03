using System;
using System.Diagnostics;
using MessagePipe;
using UnityEngine;
using ElementalBlacksmithStory.Events;
using VContainer.Unity;
using UnityEngine.AddressableAssets;
using ElementalBlacksmithStory.Data;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ElementalBlacksmithStory.Core
{
    public class EnhancementService : IDisposable, IStartable
    {
        private readonly IPublisher<ChangeWeaponEvent> weaponChangePublisher;
        private readonly IDisposable subscription;
        private SO_WeaponDatabase weaponDatabase;
        public void Start()
        {
        }

        public EnhancementService(IAsyncSubscriber<EnhanceRequestEvent> requestSubscriber,
                                    IPublisher<ChangeWeaponEvent> weaponChangePublisher,
                                    SO_WeaponDatabase weaponDatabase)
        {
            this.weaponDatabase = weaponDatabase;
            this.weaponChangePublisher = weaponChangePublisher;
            this.subscription = requestSubscriber.Subscribe(OnEnhanceRequested);
        }

        private async UniTask OnEnhanceRequested(EnhanceRequestEvent request, CancellationToken cancellationToken)
        {
            SO_WeaponData _currentWeaponData = weaponDatabase.GetWeapon(request.id);
            if(_currentWeaponData.recipes.Count == 0)
            {
                UnityEngine.Debug.LogWarning("더 이상 강화할 수 없는 무기입니다.");
                return;
            }
            SO_CraftRecipe recipe = _currentWeaponData.recipes[0];
            RecipeOutcome result = recipe.recipeOutcome;

            bool success = UnityEngine.Random.value <= result.chance;
            if (success)
            {
                var handle = Addressables.LoadAssetAsync<SO_WeaponData>(result.resultWeapon.id.ToString());
                _currentWeaponData = await handle.ToUniTask(cancellationToken: cancellationToken);
                
                UnityEngine.Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 성공, 결과물: {_currentWeaponData.weaponName}");
            }
            else
            {
                _currentWeaponData = result.defaultFailWeapon;
                UnityEngine.Debug.Log($"[EnhancementService] 강화 요청 처리됨 -> 실패, 결과물: {_currentWeaponData.weaponName}");
            }
            if(_currentWeaponData != null)
            {
                
            }
            weaponChangePublisher.Publish(new ChangeWeaponEvent(_currentWeaponData.id));
        }
        public void Dispose()
        {
            subscription?.Dispose();
        }
    }
}