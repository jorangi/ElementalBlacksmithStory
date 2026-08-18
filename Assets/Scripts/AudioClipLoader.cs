using VContainer;
using VContainer.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Cysharp.Threading.Tasks;
using R3;
using MessagePipe;
using ElementalBlacksmithStory.Events;

namespace ElementalBlacksmithStory.Core
{
    public class AudioClipLoader : MonoBehaviour
    {
        private UniTaskCompletionSource<AudioClip> _tcs;
        private readonly Dictionary<uint, AudioClip> _audioClips = new();
        private readonly List<AsyncOperationHandle<AudioClip>> _loadedHandles = new();
        private readonly CompositeDisposable _disposables = new();
        private ISubscriber<PlaySoundEvent> _subscriber;
        [SerializeField]private AudioSource _audioSource;

        private void Awake()
        {
            LoadAudioClipsAsync().Forget();
        }
        [Inject]
        public void Construct(ISubscriber<PlaySoundEvent> subscriber)
        {
            _subscriber = subscriber;
            _subscriber.Subscribe(e =>
            {
                var audioClip = GetAudioClip(e.Id);
                if(_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                    _audioSource.playOnAwake = false;
                }
                if(audioClip != null)
                {
                    _audioSource.PlayOneShot(audioClip);
                }
            }).AddTo(_disposables);
        }
        private async UniTask LoadAudioClipsAsync()
        {
            _tcs = new();
            var cancellationToken = this.GetCancellationTokenOnDestroy();
            try
            {
                var _handle = Addressables.LoadResourceLocationsAsync("Sounds", typeof(AudioClip));
                IList<IResourceLocation> locations = await _handle.ToUniTask(cancellationToken: cancellationToken);
                var loadTasks = locations.Select(async loc =>
                {
                    if(uint.TryParse(loc.PrimaryKey, out uint id))
                    {
                        var handle = Addressables.LoadAssetAsync<AudioClip>(loc);
                        _loadedHandles.Add(handle);
                        AudioClip clip = await handle.ToUniTask(cancellationToken: cancellationToken);
                        _audioClips[id] = clip;
                    }
                    else
                    {
                        Debug.LogWarning($"[AudioClipLoader] 주소(Address)를 숫자로 변환할 수 없습니다: '{loc.PrimaryKey}'");
                    }
                });
                await UniTask.WhenAll(loadTasks);
                Debug.Log($"[AudioClipLoader] AudioClip 로드 완료: 총 {_audioClips.Count}개");

            }
            catch (OperationCanceledException)
            {
                Debug.Log("[AudioClipLoader] 로딩 취소됨");
            }
            catch(Exception ex)
            {
                Debug.LogError($"[AudioClipLoader] AudioClip 로드 실패: {ex.Message}");
            }
        }
        public AudioClip GetAudioClip(uint id)
        {
            if(_audioClips.TryGetValue(id, out AudioClip audioClip))
            {
                return audioClip;
            }
            Debug.LogError($"[AudioClipLoader] {id} 해당하는 오디오 클립이 없습니다.");
            return null;
        }
        private void OnDestroy()
        {
            foreach (var handle in _loadedHandles)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
            _audioClips.Clear();
            _disposables.Dispose();
        }
    }
}