using System;
using System.Threading;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using R3;

namespace ElementalBlacksmithStory.UI
{
    public class MaterialSpriteLoader : IAsyncStartable, IDisposable
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
                _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("MaterialAtlas");
                _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: linkedCTs.Token);
                _atlasLoadTcs.TrySetResult(_cachedAtlas);
                Debug.Log("[MaterialSpriteLoader] MaterialAtlas 로드 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MaterialSpriteLoader] MaterialAtlas 로드 실패: {ex.Message}");
                _atlasLoadTcs.TrySetException(ex);
            }
        }

        public async UniTask<Sprite> GetMaterialSprite(string spriteId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(spriteId)) return null;

            if (_cachedAtlas == null)
            {
                await _atlasLoadTcs.Task.AttachExternalCancellation(cancellationToken);
            }

            if (_cachedAtlas == null)
            {
                Debug.LogError("[MaterialSpriteLoader] 아틀라스를 불러올 수 없습니다.");
                return null;
            }

            Sprite sprite = _cachedAtlas.GetSprite(spriteId);
            if (sprite == null)
            {
                sprite = _cachedAtlas.GetSprite($"{spriteId}_0");
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[MaterialSpriteLoader] '{spriteId}' 스프라이트를 찾을 수 없습니다.");
            }

            return sprite;
        }

        public void Dispose()
        {
            _disposables.Dispose();
            _cts.Cancel();
            _cts.Dispose();
            if (_atlasHandler.IsValid())
            {
                Addressables.Release(_atlasHandler);
            }
        }
    }
}