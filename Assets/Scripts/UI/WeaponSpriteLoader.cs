using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
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

            try
            {
                _atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("SwordAtlas");
                _cachedAtlas = await _atlasHandler.ToUniTask(cancellationToken: cancellationToken);
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

            if (_cachedAtlas == null && _atlasLoadTcs != null)
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
                sprite = _cachedAtlas.GetSprite($"{spriteId}_0");
            }

            if (sprite == null)
            {
                Debug.LogWarning($"[WeaponSpriteLoader] '{spriteId}' 스프라이트를 찾을 수 없습니다.");
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