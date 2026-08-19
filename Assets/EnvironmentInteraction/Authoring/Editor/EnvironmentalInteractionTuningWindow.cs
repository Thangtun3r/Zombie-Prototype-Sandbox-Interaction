using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EnvironmentInteraction.Authoring.Editor
{
    public sealed class EnvironmentalInteractionTuningWindow : EditorWindow
    {
        private readonly Dictionary<EnvironmentalInteractionType, bool> foldouts =
            new Dictionary<EnvironmentalInteractionType, bool>();
        private EnvironmentalInteractionGlobalTuning tuning;
        private SerializedObject tuningData;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Level Design/Environmental Interaction Tuning")]
        public static void OpenWindow()
        {
            EnvironmentalInteractionTuningWindow window =
                GetWindow<EnvironmentalInteractionTuningWindow>("Environment Tuning");
            window.minSize = new Vector2(520f, 430f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadTuning();
            EditorApplication.hierarchyChanged += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.hierarchyChanged -= Repaint;
        }

        private void OnGUI()
        {
            if (tuning == null || tuningData == null)
                LoadTuning();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Global Environmental Interaction Tuning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These project-wide values immediately update every matching interaction in all open scenes. Newly created interactions inherit the same global tuning. Use a selected object's Inspector only when you intentionally need a local override.",
                MessageType.Info);

            int total = FindCurrentInteractions().Count;
            EditorGUILayout.LabelField("Current open-scene objects", total.ToString());
            if (GUILayout.Button("Apply All Global Tuning to Current Objects", GUILayout.Height(28f)))
                ApplyAll();

            tuningData.UpdateIfRequiredOrScript();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUI.BeginChangeCheck();
            DrawTypeGroup(EnvironmentalInteractionType.Drop, "drop");
            DrawTypeGroup(EnvironmentalInteractionType.Push, "push");
            DrawTypeGroup(EnvironmentalInteractionType.Shock, "shock");
            DrawTypeGroup(EnvironmentalInteractionType.Explode, "explode");
            bool changed = EditorGUI.EndChangeCheck();
            tuningData.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();

            if (changed)
            {
                EditorUtility.SetDirty(tuning);
                EnvironmentalInteractionGlobalTuningUtility.ApplyToAll(tuning);
                AssetDatabase.SaveAssetIfDirty(tuning);
                SceneView.RepaintAll();
            }
        }

        private void DrawTypeGroup(EnvironmentalInteractionType type, string propertyName)
        {
            if (!foldouts.TryGetValue(type, out bool expanded))
                expanded = true;

            Color previousBackground = GUI.backgroundColor;
            GUI.backgroundColor = GetTypeColor(type);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUI.backgroundColor = previousBackground;

            int count = FindCurrentInteractions().Count(interaction => interaction.Type == type);
            expanded = EditorGUILayout.Foldout(
                expanded,
                type.ToString().ToUpperInvariant() + "  (" + count + " current)",
                true,
                EditorStyles.foldoutHeader);
            foldouts[type] = expanded;

            if (expanded)
            {
                SerializedProperty group = tuningData.FindProperty(propertyName);
                DrawProperty(group, "interactionEnabled", "Enabled");
                DrawProperty(group, "isOneUse", "One Use");
                DrawGameplay(type, group);
                EditorGUILayout.Space(3f);
                EditorGUILayout.LabelField("Visual Feedback", EditorStyles.boldLabel);
                DrawProperty(group, "objectColor", "Object Color");
                DrawProperty(group, "triggerColor", "Shootable Part Color");
                DrawVisuals(type, group);

                if (GUILayout.Button("Apply " + type.ToString().ToUpperInvariant() + " to All Current Objects"))
                {
                    tuningData.ApplyModifiedProperties();
                    EnvironmentalInteractionGlobalTuningUtility.ApplyToAll(tuning, type);
                    SceneView.RepaintAll();
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }

        private static void DrawGameplay(EnvironmentalInteractionType type, SerializedProperty group)
        {
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Gameplay", EditorStyles.boldLabel);

            if (type == EnvironmentalInteractionType.Drop)
            {
                DrawProperty(group, "dropDelay", "Trigger Delay (seconds)");
                DrawProperty(group, "fallDuration", "Fall Time (seconds)");
                DrawAreaShape(group, "impactShape", "impactRadius", "impactBoxSize", "Smash");
                if ((EnvironmentalAreaShape)group.FindPropertyRelative("impactShape").enumValueIndex == EnvironmentalAreaShape.Box)
                {
                    EditorGUILayout.HelpBox(
                        "For generated DROP blockouts, Smash Area Size also resizes the hanging container and keeps the shootable string directly above it.",
                        MessageType.None);
                }
                DrawProperty(group, "becomesNavMeshObstacle", "Blocks Navigation After Landing");
                DrawProperty(group, "impactDamage", "Smash Damage");
                DrawProperty(group, "impactForce", "Impact Knockback Power");
                DrawProperty(group, "affectedLayers", "Affected Layers");
            }
            else if (type == EnvironmentalInteractionType.Push)
            {
                DrawProperty(group, "duration", "Sustained Push Duration (seconds)");
                DrawProperty(group, "pushRange", "Water Reach");
                DrawProperty(group, "pushWidth", "Push Area Width");
                DrawProperty(group, "pushHeight", "Push Area Height");
                DrawProperty(group, "forceValue", "Pushback Power");
                DrawProperty(group, "affectedLayers", "Affected Layers");
                EditorGUILayout.HelpBox(
                    "Water applies normal directional knockback for the full Sustained Push Duration. Zombies stay upright, alive, and NavMesh-controlled while zombie mass provides resistance.",
                    MessageType.None);
            }
            else if (type == EnvironmentalInteractionType.Shock)
            {
                DrawProperty(group, "delay", "Activation Delay (seconds)");
                DrawProperty(group, "duration", "Shock Duration (seconds)");
                DrawProperty(group, "pulseInterval", "Seconds Between Pulses");
                DrawAreaShape(group, "shockAreaShape", "radius", "boxSize", "Shock");
                DrawProperty(group, "damagePerPulse", "Damage Per Pulse");
                DrawProperty(group, "slowMultiplier", "Zombie Speed Multiplier (0 = Stopped)");
                DrawProperty(group, "affectedLayers", "Affected Layers");
            }
            else
            {
                DrawProperty(group, "delay", "Explosion Delay (seconds)");
                DrawProperty(group, "outerRadius", "Blast Radius");
                DrawProperty(group, "innerRadius", "Full-Damage Radius");
                DrawProperty(group, "damage", "Explosion Damage");
                DrawProperty(group, "forceRepresentation", "Explosion Knockback Power");
                DrawProperty(group, "ragdollEnemies", "Ragdoll Enemies");
                DrawProperty(group, "ragdollUpwardForce", "Ragdoll Upward Force");
                DrawProperty(group, "ragdollTumbleTorque", "Ragdoll Tumble Torque");
                DrawProperty(group, "ragdollDisappearDelay", "Corpse Disappear Delay");
                DrawProperty(group, "affectedLayers", "Affected Layers");
            }
        }

        private static void DrawVisuals(EnvironmentalInteractionType type, SerializedProperty group)
        {
            if (type == EnvironmentalInteractionType.Drop)
            {
                DrawProperty(group, "spawnImpactParticles", "Impact Particles");
                DrawProperty(group, "impactParticleColor", "Dust / Debris Color");
                DrawProperty(group, "impactParticleAmount", "Particle Amount");
                DrawProperty(group, "impactPulseColor", "Impact Pulse Color");
            }
            else if (type == EnvironmentalInteractionType.Push)
            {
                DrawProperty(group, "spawnWaterParticles", "Water Particles");
                DrawProperty(group, "waterColor", "Water Color");
                DrawProperty(group, "waterParticleAmount", "Water Particle Amount");
            }
            else if (type == EnvironmentalInteractionType.Shock)
            {
                DrawProperty(group, "conductiveSurfaceColor", "Water Color");
                DrawProperty(group, "wireColor", "Wire Color");
                DrawProperty(group, "shockEffectColor", "Shock Effect Color");
                DrawProperty(group, "spawnActiveZoneParticles", "Active Zone Electricity");
                DrawProperty(group, "activeZoneParticleColor", "Active Zone Particle Color");
                DrawProperty(group, "activeZoneParticleAmount", "Active Zone Particle Amount");
                DrawProperty(group, "spawnTazeParticles", "Enemy Taze Particles");
                DrawProperty(group, "tazeParticleColor", "Taze Particle Color");
                DrawProperty(group, "tazeParticleAmount", "Taze Particle Amount");
                DrawProperty(group, "tazeShakeStrength", "Enemy Shake Strength");
                DrawProperty(group, "tazeShakeSpeed", "Enemy Shake Speed");
            }
            else
            {
                DrawProperty(group, "effectColor", "Explosion Effect Color");
                DrawProperty(group, "hideExplosiveObjectOnActivation", "Hide After Explosion");
            }
        }

        private static void DrawAreaShape(
            SerializedProperty group,
            string shapeName,
            string radiusName,
            string boxName,
            string labelPrefix)
        {
            SerializedProperty shape = group.FindPropertyRelative(shapeName);
            if (shape == null)
                return;

            EditorGUILayout.PropertyField(shape, new GUIContent(labelPrefix + " Area Shape"));
            if ((EnvironmentalAreaShape)shape.enumValueIndex == EnvironmentalAreaShape.Sphere)
                DrawProperty(group, radiusName, labelPrefix + " Radius");
            else
                DrawProperty(group, boxName, labelPrefix + " Area Size");
        }

        private static void DrawProperty(SerializedProperty group, string propertyName, string label)
        {
            SerializedProperty property = group?.FindPropertyRelative(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private void ApplyAll()
        {
            tuningData.ApplyModifiedProperties();
            EnvironmentalInteractionGlobalTuningUtility.ApplyToAll(tuning);
            SceneView.RepaintAll();
        }

        private void LoadTuning()
        {
            tuning = EnvironmentalInteractionGlobalTuningUtility.GetOrCreate();
            tuningData = tuning != null ? new SerializedObject(tuning) : null;
        }

        private static List<EnvironmentalInteractionBase> FindCurrentInteractions()
        {
            return Resources.FindObjectsOfTypeAll<EnvironmentalInteractionBase>()
                .Where(interaction =>
                    interaction != null &&
                    interaction.gameObject.scene.IsValid() &&
                    interaction.gameObject.scene.isLoaded &&
                    !EditorUtility.IsPersistent(interaction))
                .ToList();
        }

        private static Color GetTypeColor(EnvironmentalInteractionType type)
        {
            switch (type)
            {
                case EnvironmentalInteractionType.Drop:
                    return new Color(0.95f, 0.6f, 0.18f);
                case EnvironmentalInteractionType.Push:
                    return new Color(0.2f, 0.8f, 0.68f);
                case EnvironmentalInteractionType.Shock:
                    return new Color(0.2f, 0.65f, 1f);
                default:
                    return new Color(1f, 0.32f, 0.16f);
            }
        }
    }
}
