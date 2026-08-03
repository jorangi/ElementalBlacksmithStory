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
        [Header("무기 이미지-Image")]
        [SerializeField]private Image weaponImage;
        private SpriteAtlas cachedAtlas;
        private AsyncOperationHandle<SpriteAtlas> atlasHandler;
        private UniTaskCompletionSource<SpriteAtlas> atlasLoadTcs;
        private SO_WeaponDatabase weaponDatabase;
        [Inject]
        public void Construct(SO_WeaponDatabase weaponDatabase)
        {
            this.weaponDatabase = weaponDatabase;
        }
        private void Awake()
        {
            LoadAtlasAsync().Forget();
        }
        private async UniTask LoadAtlasAsync()
        {
            atlasLoadTcs = new UniTaskCompletionSource<SpriteAtlas>();
            var cancellationToken = this.GetCancellationTokenOnDestroy();

            atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("SwordAtlas");
            cachedAtlas = await atlasHandler.ToUniTask(cancellationToken: cancellationToken);

            atlasLoadTcs.TrySetResult(cachedAtlas);
            Debug.Log("[WeaponSpriteLoader] SwordAtlas 로드 완료");
        }
        private async UniTaskVoid Start()
        {
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            atlasHandler = Addressables.LoadAssetAsync<SpriteAtlas>("SwordAtlas");
            cachedAtlas = await atlasHandler.ToUniTask(cancellationToken: cancellationToken);
        }
        public async UniTask<Sprite> GetWeaponSprite(string spriteId, CancellationToken cancellationToken = default)
        {
            if (cachedAtlas == null && atlasLoadTcs != null)
            {
                Debug.Log("[WeaponSpriteLoader] 아틀라스 로딩 대기 중...");
                await atlasLoadTcs.Task.AttachExternalCancellation(cancellationToken);
            }
            if(cachedAtlas == null)
            {
                Debug.LogError("[WeaponSpriteLoader] 아틀라스를 불러올 수 없습니다.");
                return null;  
            }
            Sprite newSprite = cachedAtlas.GetSprite(spriteId);
            if (newSprite == null)
            {
                Debug.LogWarning($"[WeaponSpriteLoader] '{spriteId}' 스프라이트를 찾을 수 없습니다.");
            }
            return newSprite;
        }
        private void OnDestroy()
        {
            if(atlasHandler.IsValid())
            {
                Addressables.Release(atlasHandler);
            }
        }
    }
}