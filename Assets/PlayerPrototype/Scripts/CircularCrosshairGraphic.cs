using UnityEngine;
using UnityEngine.UI;

namespace PlayerPrototype
{
    [DisallowMultipleComponent]
    public sealed class CircularCrosshairGraphic : MaskableGraphic
    {
        [SerializeField, Min(1f)] private float radius = 22f;
        [SerializeField, Min(0.5f)] private float thickness = 3f;
        [SerializeField, Range(12, 128)] private int segments = 64;

        public float Radius
        {
            get => radius;
            set
            {
                float validated = Mathf.Max(1f, value);
                if (Mathf.Approximately(radius, validated))
                    return;
                radius = validated;
                SetVerticesDirty();
            }
        }

        public float Thickness
        {
            get => thickness;
            set
            {
                float validated = Mathf.Max(0.5f, value);
                if (Mathf.Approximately(thickness, validated))
                    return;
                thickness = validated;
                SetVerticesDirty();
            }
        }

        public int Segments
        {
            get => segments;
            set
            {
                segments = Mathf.Clamp(value, 12, 128);
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();

            float outerRadius = radius + thickness * 0.5f;
            float innerRadius = Mathf.Max(0f, radius - thickness * 0.5f);
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < segments; i++)
            {
                float startAngle = i * Mathf.PI * 2f / segments;
                float endAngle = (i + 1) * Mathf.PI * 2f / segments;
                Vector2 startDirection = new Vector2(Mathf.Cos(startAngle), Mathf.Sin(startAngle));
                Vector2 endDirection = new Vector2(Mathf.Cos(endAngle), Mathf.Sin(endAngle));
                int firstVertex = vertexHelper.currentVertCount;

                vertex.position = startDirection * innerRadius;
                vertexHelper.AddVert(vertex);
                vertex.position = startDirection * outerRadius;
                vertexHelper.AddVert(vertex);
                vertex.position = endDirection * outerRadius;
                vertexHelper.AddVert(vertex);
                vertex.position = endDirection * innerRadius;
                vertexHelper.AddVert(vertex);

                vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            radius = Mathf.Max(1f, radius);
            thickness = Mathf.Max(0.5f, thickness);
            segments = Mathf.Clamp(segments, 12, 128);
            raycastTarget = false;
            SetVerticesDirty();
        }
    }
}
