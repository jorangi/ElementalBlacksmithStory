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

            try
            {
                _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("MaterialAtlas");
                _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: cancellationToken);
                _atlasLoadTcs.TrySetResult(_cachedAtlas);
                Debug.Log("[MaterialSpriteLoader] MaterialAtlas 로드 완료");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MaterialSpriteLoader] MaterialAtlas 로드 실패: {ex.Message}");
                _atlasLoadTcs.TrySetException(ex);
            }
        }

        public async UniTask<Sprite> GetMaterialSprite(string materialId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(materialId)) return null;

            if (_cachedAtlas == null && _atlasLoadTcs != null)
            {
                await _atlasLoadTcs.Task.AttachExternalCancellation(cancellationToken);
            }

            if (_cachedAtlas == null)
            {
                Debug.LogError("[MaterialSpriteLoader] 아틀라스를 불러올 수 없습니다.");
                return null;
            }

            Sprite sprite = _cachedAtlas.GetSprite(materialId);
            if (sprite == null)
            {
                sprite = _cachedAtlas.GetSprite($"{materialId}_0");
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[MaterialSpriteLoader] '{materialId}' 스프라이트를 찾을 수 없습니다.");
            }
            return sprite;
        }

        private void OnDestroy()
        {
            if (_atlasHandler.IsValid())
            {
                Addressables.Release(_atlasHandler);
            }
        }
    }
}