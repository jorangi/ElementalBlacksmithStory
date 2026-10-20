using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public abstract class BaseSpriteLoader : IAsyncStartable, IDisposable
    {
        protected virtual string AtlasAddress => null;
        protected virtual string[] AtlasAddresses => !string.IsNullOrEmpty(AtlasAddress) ? new[] { AtlasAddress } : Array.Empty<string>();
        private readonly List<SpriteAtlas> _cachedAtlases = new();
        private readonly List<AsyncOperationHandle<SpriteAtlas>> _atlasHandlers = new();
        private readonly CompositeDisposable _disposable = new();
        private CancellationTokenSource _cts = new();
        private UniTaskCompletionSource<bool> _tcs = new();
        protected readonly Sprite DefaultSprite;

        protected BaseSpriteLoader(Sprite defaultSprite = null)
        {
            DefaultSprite = defaultSprite;
        }
        /// <summary>
        /// 스프라이트 아틀라스를 비동기적으로 로드
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                var addresses = AtlasAddresses;
                for (int i = 0; i < addresses.Length; i++)
                {
                    var handle = Addressables.LoadAssetAsync<SpriteAtlas>(addresses[i]);
                    _atlasHandlers.Add(handle);
                    var atlas = await handle.ToUniTask(cancellationToken: cancellation);
                    if (atlas != null) _cachedAtlases.Add(atlas);
                }
                _tcs.TrySetResult(true);
            }
            catch (Exception e)
            {
                _tcs.TrySetException(e);
                Debug.LogError($"[SpriteLoader] 아틀라스 로드 실패: {e}");
            }
        }
        /// <summary>
        /// id를 사용하여 비동기적으로 스프라이트를 로드, 아직 덜 됐다면 대기
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async UniTask<Sprite> GetSprite(uint id, CancellationToken ct = default)
        {
            if (_cachedAtlases.Count == 0) await _tcs.Task.AttachExternalCancellation(ct);
            if (_cachedAtlases.Count == 0) return DefaultSprite;
            string idStr = id.ToString();
            for (int i = 0; i < _cachedAtlases.Count; i++)
            {
                var sprite = _cachedAtlases[i].GetSprite(idStr);
                if (sprite != null) return sprite;
            }
            Debug.LogWarning($"[SpriteLoader] {id}를 찾을 수 없음");
            return DefaultSprite;
        }
        public void Dispose()
        {
            for (int i = 0; i < _atlasHandlers.Count; i++)
            {
                var handle = _atlasHandlers[i];
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            _atlasHandlers.Clear();
            _cachedAtlases.Clear();
            _disposable.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}