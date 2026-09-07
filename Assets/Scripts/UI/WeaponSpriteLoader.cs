using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Threading;
using VContainer.Unity;
using System;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class WeaponSpriteLoader : IAsyncStartable, IDisposable
    {
        private SpriteAtlas _cachedAtlas;
        private AsyncOperationHandle<SpriteAtlas> _atlasHandler;
        private readonly UniTaskCompletionSource<SpriteAtlas> _atlasLoadTcs = new();
        private readonly CompositeDisposable _disposables = new();
        private readonly CancellationTokenSource _cts = new();

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            using var linkedCTs = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, cancellation);
            try
            {
                _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("SwordAtlas");
                _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: linkedCTs.Token);
                _atlasLoadTcs.TrySetResult(_cachedAtlas);
                Debug.Log("[WeaponSpriteLoader] SwordAtlas 로드 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[WeaponSpriteLoader] SwordAtlas 로드 실패: {ex.Message}");
                _atlasLoadTcs.TrySetException(ex);
            }
        }

        public async UniTask<Sprite> GetWeaponSprite(string spriteId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(spriteId)) return null;

            if (_cachedAtlas == null)
            {
                await _atlasLoadTcs.Task.AttachExternalCancellation(cancellationToken);
            }

            if (_cachedAtlas == null)
            {
                Debug.LogError("[WeaponSpriteLoader] 아틀라스를 불러올 수 없습니다.");
                return null;
            }

            Sprite sprite = _cachedAtlas.GetSprite(spriteId);
            if (sprite == null)
            {
                Debug.LogWarning($"[WeaponSpriteLoader] '{spriteId}' 스프라이트를 찾을 수 없습니다.");
            }

            return sprite;
        }
        public void Dispose()
        {
            _disposables.Dispose();
            if (_atlasHandler.IsValid())
            {
                Addressables.Release(_atlasHandler);
            }
        }
    }
}