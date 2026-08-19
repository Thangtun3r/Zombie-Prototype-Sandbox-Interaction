using UnityEditor;
using UnityEngine;

namespace EnvironmentInteraction.Authoring.Editor
{
    public sealed class EnvironmentalInteractionWindow : EditorWindow
    {
        private static readonly Color DropColor = new Color(0.95f, 0.6f, 0.18f);
        private static readonly Color PushColor = new Color(0.2f, 0.8f, 0.68f);
        private static readonly Color ShockColor = new Color(0.2f, 0.65f, 1f);
        private static readonly Color ExplodeColor = new Color(1f, 0.32f, 0.16f);

        [SerializeField] private Transform parent;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Level Design/Environmental Interaction Tool")]
        private static void OpenWindow()
        {
            EnvironmentalInteractionWindow window =
                GetWindow<EnvironmentalInteractionWindow>("Environmental Interactions");
            window.minSize = new Vector2(480f, 390f);
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Environmental Interaction Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Create one-use environmental combat opportunities. Each setup keeps the shootable trigger separate from the effect and affected area.",
                MessageType.Info);

            if (GUILayout.Button("Open Scene-Wide Tuning", GUILayout.Height(28f)))
                EnvironmentalInteractionTuningWindow.OpenWindow();

            parent = (Transform)EditorGUILayout.ObjectField(
                new GUIContent("Optional Parent", "New interaction roots are parented here when assigned."),
                parent,
                typeof(Transform),
                true);

            EditorGUILayout.Space(12f);
            DrawTypeRow(
                EnvironmentalInteractionType.Drop,
                "DROP",
                "Shoot a support; preview a falling object and impact zone.",
                DropColor,
                EnvironmentalInteractionType.Push,
                "PUSH",
                "Shoot a source; author a directional push volume.",
                PushColor);
            EditorGUILayout.Space(8f);
            DrawTypeRow(
                EnvironmentalInteractionType.Shock,
                "SHOCK",
                "Shoot an electrical weak point; author a conductive zone.",
                ShockColor,
                EnvironmentalInteractionType.Explode,
                "EXPLODE",
                "Shoot an explosive object; author inner and outer radii.",
                ExplodeColor);

            EditorGUILayout.Space(14f);
            EditorGUILayout.HelpBox(
                "Placement uses the current Scene View pivot. Generated placeholders are blockout geometry and can be replaced with scene art. Select the root to edit geometry with Scene handles.",
                MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawTypeRow(
            EnvironmentalInteractionType leftType,
            string leftLabel,
            string leftDescription,
            Color leftColor,
            EnvironmentalInteractionType rightType,
            string rightLabel,
            string rightDescription,
            Color rightColor)
        {
            EditorGUILayout.BeginHorizontal();
            DrawTypeCard(leftType, leftLabel, leftDescription, leftColor);
            GUILayout.Space(8f);
            DrawTypeCard(rightType, rightLabel, rightDescription, rightColor);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTypeCard(
            EnvironmentalInteractionType type,
            string label,
            string description,
            Color color)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinHeight(112f));
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = color;
            if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.Height(44f)))
                EnvironmentalInteractionAuthoringUtility.CreateInteraction(type, parent);
            GUI.backgroundColor = previous;
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel, GUILayout.MinHeight(42f));
            EditorGUILayout.EndVertical();
        }
    }
}
