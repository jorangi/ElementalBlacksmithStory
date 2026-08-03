using R3;
using UnityEngine;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    public class EnhanceButtonView : MonoBehaviour
    {
        [SerializeField] private Button enhanceButton;
        public Observable<Unit> OnClickAsObservable()
        {
            if(enhanceButton == null)
            {
                Debug.LogError("[EnhanceButtonView] enhanceButton이 연결되지 않았습니다.");
                return Observable.Empty<Unit>();
            }
            return enhanceButton.OnClickAsObservable();
        }
    }
}