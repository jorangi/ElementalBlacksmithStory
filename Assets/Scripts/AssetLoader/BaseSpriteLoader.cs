using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;
using VContainer.Unity;
using UnityEngine;

namespace ElementalBlacksmithStory.Core
{
    public abstract class BaseSpriteLoader : IAsyncStartable, IDisposable
    {
        protected abstract string AtlasAddress {get;}
        private SpriteAtlas _cachedAtlas;
        private AsyncOperationHandle<SpriteAtlas> _atlasHandler;
        private readonly CompositeDisposable _disposable = new();
        private CancellationTokenSource _cts = new();
        private UniTaskCompletionSource<SpriteAtlas> _tcs = new();
        /// <summary>
        /// 스프라이트 아틀라스를 비동기적으로 로드
        /// </summary>
        /// <param name="cancellation"></param>
        /// <returns></returns>
        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            try
            {
                _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>(AtlasAddress);
                _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken:cancellation);
                _tcs.TrySetResult(_cachedAtlas);
            }
            catch(Exception e)
            {
                _tcs.TrySetException(e);
                Debug.LogError($"[SpriteLoader] {AtlasAddress}가 로드되지 않음: {e}");
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
            if(_cachedAtlas == null) await _tcs.Task.AttachExternalCancellation(ct);
            if(_cachedAtlas == null) return null;
            var sprite = _cachedAtlas.GetSprite(id.ToString());
            if(sprite == null) Debug.LogWarning($"[SpriteLoader] {id}를 찾을 수 없음");
            return sprite;
        }
        public void Dispose()
        {
            if(_atlasHandler.IsValid()) Addressables.Release(_atlasHandler);
            _disposable.Dispose();
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}