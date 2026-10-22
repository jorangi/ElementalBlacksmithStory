using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    /// <summary>
    /// 화면에 메시를 렌더링하지 않고(드로우콜 0), Raycast 타깃 역할만 수행하는 빈 그래픽 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EmptyGraphic : Graphic
    {
        protected override void Awake()
        {
            base.Awake();
            useGUILayout = false; // 불필요한 GUI 레이아웃 처리 방지
        }

        public override void SetMaterialDirty()
        {
            // 렌더링 재질이 없으므로 더티 플래그 무시
        }

        public override void SetVerticesDirty()
        {
            // 정점이 없으므로 더티 플래그 무시
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            // 정점을 생성하지 않고 비워둠 -> 드로우콜 및 오버드로우 제로
            vh.Clear();
        }
    }
}
