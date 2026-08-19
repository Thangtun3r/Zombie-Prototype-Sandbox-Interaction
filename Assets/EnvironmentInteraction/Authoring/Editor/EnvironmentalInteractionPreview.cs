using UnityEditor;
using UnityEngine;

namespace EnvironmentInteraction.Authoring.Editor
{
    internal static class EnvironmentalInteractionPreview
    {
        private const double PreviewDuration = 1.25d;

        private static int previewInstanceId;
        private static double previewStartTime;
        private static double previewEndTime;
        private static bool subscribed;

        public static void Start(EnvironmentalInteractionBase interaction)
        {
            if (interaction == null)
                return;

            previewInstanceId = interaction.GetInstanceID();
            previewStartTime = EditorApplication.timeSinceStartup;
            previewEndTime = previewStartTime + PreviewDuration;
            if (!subscribed)
            {
                EditorApplication.update += Tick;
                subscribed = true;
            }
            SceneView.RepaintAll();
        }

        public static bool TryGetProgress(EnvironmentalInteractionBase interaction, out float progress)
        {
            double now = EditorApplication.timeSinceStartup;
            if (interaction == null || interaction.GetInstanceID() != previewInstanceId || now >= previewEndTime)
            {
                progress = 0f;
                return false;
            }

            progress = Mathf.Clamp01((float)((now - previewStartTime) / PreviewDuration));
            return true;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < previewEndTime)
            {
                SceneView.RepaintAll();
                return;
            }

            previewInstanceId = 0;
            EditorApplication.update -= Tick;
            subscribed = false;
            SceneView.RepaintAll();
        }
    }
}
