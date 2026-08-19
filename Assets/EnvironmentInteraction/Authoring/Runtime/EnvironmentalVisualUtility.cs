using UnityEngine;

namespace EnvironmentInteraction.Authoring
{
    public static class EnvironmentalVisualUtility
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public static void ApplyColor(Renderer renderer, Color color)
        {
            if (renderer == null)
                return;

            MaterialPropertyBlock properties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(properties);
            properties.SetColor(BaseColorId, color);
            properties.SetColor(ColorId, color);
            renderer.SetPropertyBlock(properties);
        }

        public static void ApplyColor(Renderer[] renderers, Color color)
        {
            if (renderers == null)
                return;

            foreach (Renderer renderer in renderers)
                ApplyColor(renderer, color);
        }
    }
}
