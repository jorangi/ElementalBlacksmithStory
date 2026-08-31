using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

namespace ElementalBlacksmithStory.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class WeaponTreeBranchView : MaskableGraphic
    {
        [Header("상태별 파이프 색상")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color routeColor = Color.orange;
        [Header("선 스프라이트/텍스처")]
        [SerializeField] private Sprite branchSprite;
        
        [Header("연결 대상")]
        [SerializeField] private RectTransform startNode;
        [SerializeField] private RectTransform endNode;

        [Header("선 설정")]
        [SerializeField] private float thickness = 6f;
        [Range(2, 30)]
        [SerializeField] private int segments = 15;
        [Header("UV / 타일링 설정")]
        [Tooltip("세로로 긴 텍스처(32x256 등)를 사용하는 경우 체크")]
        [SerializeField] private bool isVerticalTexture = true; 
        [Tooltip("수치를 낮출수록 텍스처가 길게 늘어나고, 높일수록 촘촘하게 반복됩니다.")]
        [SerializeField] private float uvTilingFactor = 0.003f;

        [Header("곡선 제어")]
        [Tooltip("트리 형태(가로/세로)에 맞게 핸들 방향 조정")]
        [SerializeField] private Vector2 curveDirection = new(1f, 0);
        private Vector3 lastStartPos;
        private Vector3 lastEndPos;
        public override Texture mainTexture
        {
            get
            {
                if (branchSprite != null)
                    return branchSprite.texture;
                if (material != null && material.mainTexture != null)
                    return material.mainTexture;
                return s_WhiteTexture;
            }
        }
        public void SetHighlight(bool isHighlight)
        {
            color = isHighlight ? routeColor : normalColor;
            SetVerticesDirty();
        }
        public void SetNodes(RectTransform start, RectTransform end)
        {
            startNode = start;
            endNode = end;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if(startNode == null || endNode == null) return;

            Vector2 startPos = transform.InverseTransformPoint(startNode.position);
            Vector2 endPos = transform.InverseTransformPoint(endNode.position);

            float distance = Vector2.Distance(startPos, endPos);
            float handleLength = distance * 0.4f;
            
            Vector3 p0 = startPos;
            Vector3 p1 = startPos + (curveDirection.normalized * handleLength);
            Vector3 p2 = endPos - (curveDirection.normalized * handleLength);
            Vector3 p3 = endPos;

            List<Vector2> points = new();
            for(int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                points.Add(EvaluateCubicBezier(p0, p1, p2, p3, t));
            }

            float totalLength = 0f;
            for(int i = 0; i < points.Count; i++)
            {
                if(i > 0)
                    totalLength += Vector2.Distance(points[i - 1], points[i]);
                Vector2 normal;
                if(i < points.Count - 1)
                {
                    Vector2 dir = (points[i+1] - points[i]).normalized;
                    normal = new Vector2(-dir.y, dir.x);
                }
                else
                {
                    Vector2 dir = (points[i] - points[i-1]).normalized;
                    normal = new Vector2(-dir.y, dir.x);
                }
                Vector2 v1 = points[i] + normal * (thickness * 0.5f);
                Vector2 v2 = points[i] - normal * (thickness * 0.5f);
                
                float progress = totalLength * uvTilingFactor;
                Vector2 uv1, uv2;

                if (isVerticalTexture)
                {
                    uv1 = new Vector2(1f, progress);
                    uv2 = new Vector2(0f, progress);
                }
                else
                {
                    uv1 = new Vector2(progress, 1f);
                    uv2 = new Vector2(progress, 0f);
                }
                vh.AddVert(v1, color, uv1);
                vh.AddVert(v2, color, uv2);
                if(i > 0)
                {
                    int baseIndex = (i-1)*2;
                    vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                    vh.AddTriangle(baseIndex + 1, baseIndex + 3, baseIndex + 2);
                }
            }
        }

        private Vector2 EvaluateCubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float u = 1f - t;
            return (u*u*u*p0) + (3f*u*u*t*p1) + (3f*u*t*t*p2) + (t*t*t*p3);
        }
        private void Update()
        {
            if(startNode != null && endNode != null)
            {
                if(startNode.position != lastStartPos || endNode.position != lastEndPos)
                {
                    lastStartPos = startNode.position;
                    lastEndPos = endNode.position;
                    SetVerticesDirty();
                }
            }
        }
        #if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            SetVerticesDirty();
        }
        #endif
    }
}