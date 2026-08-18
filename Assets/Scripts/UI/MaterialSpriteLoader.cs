using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Threading;

namespace ElementalBlacksmithStory.UI
{
    public class MaterialSpriteLoader : MonoBehaviour
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
            _atlasLoadTcs = new();
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("MaterialAtlas");
            _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: cancellationToken);

            _atlasLoadTcs.TrySetResult(_cachedAtlas);
            Debug.Log("[MaterialSpriteLoader] MaterialAtlas 로드 완료");
        }
        public async UniTask<Sprite> GetMaterialSprite(string materialId, CancellationToken cancellationToken = default)
        {
            if(_cachedAtlas == null && _atlasLoadTcs != null)
            {
                Debug.Log("[MaterialSpriteLoader] 아틀라스 로딩 대기 중...");
                await _atlasLoadTcs.Task.AttachExternalCancellation(cancellationToken);
            }
            if(_cachedAtlas == null)
            {
                Debug.LogError("[MaterialSpriteLoader] 아틀라스를 불러올 수 없습니다.");
                return null;
            }
            Sprite newSprite = _cachedAtlas.GetSprite(materialId);
            if(newSprite == null)
            {
                Debug.LogWarning($"[MaterialSpriteLoader] '{materialId}' 스프라이트를 찾을 수 없습니다.");
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