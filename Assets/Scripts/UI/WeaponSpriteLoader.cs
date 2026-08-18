using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using MessagePipe;
using VContainer;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Data;
using System.Threading;

namespace ElementalBlacksmithStory.UI
{
    public class WeaponSpriteLoader : MonoBehaviour
    {
        private SpriteAtlas _cachedAtlas;
        private AsyncOperationHandle<SpriteAtlas> _atlasHandler;
        private UniTaskCompletionSource<SpriteAtlas> _atlasLoadTcs;
        private void Awake()
        {
            LoadAtlasAsync().Forget();
        }
        private async UniTask LoadAtlasAsync()
        {
            _atlasLoadTcs = new UniTaskCompletionSource<SpriteAtlas>();
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("SwordAtlas");
            _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: cancellationToken);

            _atlasLoadTcs.TrySetResult(_cachedAtlas);
            Debug.Log("[WeaponSpriteLoader] SwordAtlas 로드 완료");
        }
        private async UniTaskVoid Start()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("SwordAtlas");
            _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: cancellationToken);
        }
        public async UniTask<Sprite> GetWeaponSprite(string spriteId, CancellationToken cancellationToken = default)
        {
            if (_cachedAtlas == null && _atlasLoadTcs != null)
            {
                Debug.Log("[WeaponSpriteLoader] 아틀라스 로딩 대기 중...");
                await _atlasLoadTcs.Task.AttachExternalCancellation(cancellationToken);
            }
            if(_cachedAtlas == null)
            {
                Debug.LogError("[WeaponSpriteLoader] 아틀라스를 불러올 수 없습니다.");
                return null;  
            }
            Sprite newSprite = _cachedAtlas.GetSprite(spriteId);
            if (newSprite == null)
            {
                Debug.LogWarning($"[WeaponSpriteLoader] '{spriteId}' 스프라이트를 찾을 수 없습니다.");
            }
            return newSprite;
        }
        private void OnDestroy()
        {
            if(_atlasHandler.IsValid())
            {
                Addressables.Release(_atlasHandler);
            }
        }
    }
}