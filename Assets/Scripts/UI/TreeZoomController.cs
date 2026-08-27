using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ElementalBlacksmithStory.UI
{
    [RequireComponent(typeof(ScrollRect))]
    public class TreeZoomController : MonoBehaviour, IScrollHandler
    {
        [Header("참조")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform content;

        [Header("줌 배율 설정")]
        [SerializeField] private float minZoom = 0.4f; // 최소 축소 배율
        [SerializeField] private float maxZoom = 2.0f; // 최대 확대 배율
        [SerializeField] private float mouseZoomSpeed = 0.1f;
        [SerializeField] private float pinchZoomSpeed = 0.005f;

        private float currentZoom = 1f;
        private Canvas rootCanvas;

        private void Awake()
        {
            if (scrollRect == null) 
                scrollRect = GetComponent<ScrollRect>();
            if (content == null && scrollRect != null) 
                content = scrollRect.content;

            rootCanvas = GetComponentInParent<Canvas>();
            
            // 휠 굴림 시 기본 ScrollRect 상하 스크롤 동작 방지 (드래그 이동만 유지)
            if (scrollRect != null)
                scrollRect.scrollSensitivity = 0f;
        }

        // 마우스 휠 스크롤 감지 (PC)
        public void OnScroll(PointerEventData eventData)
        {
            float scrollDelta = eventData.scrollDelta.y;
            if (Mathf.Abs(scrollDelta) < 0.01f) return;

            ApplyZoom(scrollDelta * mouseZoomSpeed, eventData.position);
        }

        // 터치 핀치 감지 (모바일)
        private void Update()
        {
            if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
                float currentMagnitude = (touch0.position - touch1.position).magnitude;

                float delta = currentMagnitude - prevMagnitude;
                Vector2 midPoint = (touch0.position + touch1.position) * 0.5f;

                ApplyZoom(delta * pinchZoomSpeed, midPoint);
            }
        }

        private void ApplyZoom(float deltaZoom, Vector2 screenPoint)
        {
            float newZoom = Mathf.Clamp(currentZoom + deltaZoom, minZoom, maxZoom);
            if (Mathf.Approximately(newZoom, currentZoom)) return;

            Camera cam = (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay) 
                ? rootCanvas.worldCamera 
                : null;

            // 줌 중심점 보정 (커서가 가리키는 노드가 그 자리에 유지되도록 좌표 보정)
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, cam, out Vector2 localPointBefore);

            currentZoom = newZoom;
            content.localScale = Vector3.one * currentZoom;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, cam, out Vector2 localPointAfter);

            Vector2 offset = localPointAfter - localPointBefore;
            content.anchoredPosition += offset * currentZoom;
        }

        // 줌 배율 외부 리셋용 함수 (트리 새로고침 시 호출 가능)
        public void ResetZoom()
        {
            currentZoom = 1f;
            if (content != null)
            {
                content.localScale = Vector3.one;
            }
        }
    }
}