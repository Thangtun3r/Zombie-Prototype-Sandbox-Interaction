using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EnvironmentInteraction.Authoring.Editor
{
    public sealed class EnvironmentalInteractionGlobalTuning : ScriptableObject
    {
        [Serializable]
        public sealed class DropSettings
        {
            public bool interactionEnabled = true;
            public bool isOneUse = true;
            [Min(0f)] public float dropDelay;
            [Min(0.05f)] public float fallDuration = 0.45f;
            public EnvironmentalAreaShape impactShape = EnvironmentalAreaShape.Box;
            [Min(0.05f)] public float impactRadius = 2f;
            public Vector3 impactBoxSize = new Vector3(4f, 2f, 2.5f);
            public bool becomesNavMeshObstacle = true;
            [Min(0f)] public float impactDamage = 500f;
            [Min(0f)] public float impactForce = 8f;
            public LayerMask affectedLayers = ~0;
            public Color objectColor = new Color(0.95f, 0.55f, 0.08f, 1f);
            public Color triggerColor = new Color(0.92f, 0.035f, 0.02f, 1f);
            public bool spawnImpactParticles = true;
            public Color impactParticleColor = new Color(0.62f, 0.54f, 0.42f, 0.9f);
            [Min(0.1f)] public float impactParticleAmount = 1f;
            public Color impactPulseColor = new Color(1f, 0.5f, 0.08f, 0.24f);
        }

        [Serializable]
        public sealed class PushSettings
        {
            public bool interactionEnabled = true;
            public bool isOneUse = true;
            [Min(0f)] public float duration = 1.25f;
            [Min(0.05f)] public float pushRange = 8f;
            [Min(0.05f)] public float pushWidth = 4f;
            [Min(0.05f)] public float pushHeight = 2.5f;
            [Min(0f)] public float forceValue = 18f;
            public LayerMask affectedLayers = ~0;
            public Color objectColor = new Color(0.78f, 0.04f, 0.025f, 1f);
            public Color triggerColor = new Color(1f, 0.72f, 0.05f, 1f);
            public bool spawnWaterParticles = true;
            public Color waterColor = new Color(0.08f, 0.48f, 1f, 0.9f);
            [Min(0.1f)] public float waterParticleAmount = 1f;
        }

        [Serializable]
        public sealed class ShockSettings
        {
            public bool interactionEnabled = true;
            public bool isOneUse = true;
            [Min(0f)] public float delay;
            [Min(0f)] public float duration = 2f;
            [Min(0.05f)] public float pulseInterval = 0.35f;
            public EnvironmentalAreaShape shockAreaShape = EnvironmentalAreaShape.Box;
            [Min(0.05f)] public float radius = 3f;
            public Vector3 boxSize = new Vector3(5f, 0.5f, 4f);
            [Min(0f)] public float damagePerPulse = 8f;
            [Range(0f, 1f)] public float slowMultiplier;
            public LayerMask affectedLayers = ~0;
            public Color objectColor = new Color(0.88f, 0.62f, 0.08f, 1f);
            public Color triggerColor = new Color(0.88f, 0.62f, 0.08f, 1f);
            public Color conductiveSurfaceColor = new Color(0.05f, 0.35f, 0.9f, 1f);
            public Color wireColor = new Color(0.85f, 0.04f, 0.02f, 1f);
            public Color shockEffectColor = new Color(0.15f, 0.65f, 1f, 0.22f);
            public bool spawnActiveZoneParticles = true;
            public Color activeZoneParticleColor = new Color(0.2f, 0.7f, 1f, 1f);
            [Min(0.1f)] public float activeZoneParticleAmount = 1f;
            public bool spawnTazeParticles = true;
            public Color tazeParticleColor = new Color(0.25f, 0.75f, 1f, 1f);
            [Min(0.1f)] public float tazeParticleAmount = 1f;
            [Min(0f)] public float tazeShakeStrength = 0.045f;
            [Min(0f)] public float tazeShakeSpeed = 28f;
        }

        [Serializable]
        public sealed class ExplodeSettings
        {
            public bool interactionEnabled = true;
            public bool isOneUse = true;
            [Min(0f)] public float delay;
            [Min(0.05f)] public float outerRadius = 5f;
            [Min(0f)] public float innerRadius = 1.5f;
            [Min(0f)] public float damage = 125f;
            [Min(0f)] public float forceRepresentation = 8f;
            public bool ragdollEnemies = true;
            [Min(0f)] public float ragdollUpwardForce = 6f;
            [Min(0f)] public float ragdollTumbleTorque = 12f;
            [Min(0f)] public float ragdollDisappearDelay = 3f;
            public LayerMask affectedLayers = ~0;
            public Color objectColor = new Color(0.86f, 0.12f, 0.035f, 1f);
            public Color triggerColor = new Color(0.86f, 0.12f, 0.035f, 1f);
            public Color effectColor = new Color(1f, 0.18f, 0.02f, 0.28f);
            public bool hideExplosiveObjectOnActivation = true;
        }

        [SerializeField] private DropSettings drop = new DropSettings();
        [SerializeField] private PushSettings push = new PushSettings();
        [SerializeField] private ShockSettings shock = new ShockSettings();
        [SerializeField] private ExplodeSettings explode = new ExplodeSettings();
    }

    internal static class EnvironmentalInteractionGlobalTuningUtility
    {
        public const string AssetPath =
            "Assets/EnvironmentInteraction/Authoring/Settings/EnvironmentalInteractionGlobalTuning.asset";

        private static readonly string[] DropProperties =
        {
            "interactionEnabled", "isOneUse", "dropDelay", "fallDuration", "impactShape",
            "impactRadius", "impactBoxSize", "becomesNavMeshObstacle", "impactDamage", "impactForce", "affectedLayers",
            "objectColor", "triggerColor", "spawnImpactParticles", "impactParticleColor",
            "impactParticleAmount", "impactPulseColor"
        };

        private static readonly string[] PushProperties =
        {
            "interactionEnabled", "isOneUse", "duration", "pushRange", "pushWidth", "pushHeight",
            "forceValue", "affectedLayers", "objectColor", "triggerColor", "spawnWaterParticles",
            "waterColor", "waterParticleAmount"
        };

        private static readonly string[] ShockProperties =
        {
            "interactionEnabled", "isOneUse", "delay", "duration", "pulseInterval",
            "shockAreaShape", "radius", "boxSize", "damagePerPulse", "slowMultiplier",
            "affectedLayers", "objectColor", "triggerColor", "conductiveSurfaceColor",
            "wireColor", "shockEffectColor", "spawnActiveZoneParticles", "activeZoneParticleColor",
            "activeZoneParticleAmount", "spawnTazeParticles", "tazeParticleColor",
            "tazeParticleAmount", "tazeShakeStrength", "tazeShakeSpeed"
        };

        private static readonly string[] ExplodeProperties =
        {
            "interactionEnabled", "isOneUse", "delay", "outerRadius", "innerRadius", "damage",
            "forceRepresentation", "ragdollEnemies", "ragdollUpwardForce", "ragdollTumbleTorque",
            "ragdollDisappearDelay", "affectedLayers", "objectColor", "triggerColor", "effectColor",
            "hideExplosiveObjectOnActivation"
        };

        public static EnvironmentalInteractionGlobalTuning GetOrCreate()
        {
            EnvironmentalInteractionGlobalTuning tuning =
                AssetDatabase.LoadAssetAtPath<EnvironmentalInteractionGlobalTuning>(AssetPath);
            if (tuning != null)
                return tuning;

            EnsureFolder("Assets/EnvironmentInteraction/Authoring/Settings");
            tuning = ScriptableObject.CreateInstance<EnvironmentalInteractionGlobalTuning>();
            AssetDatabase.CreateAsset(tuning, AssetPath);
            AssetDatabase.SaveAssets();
            return tuning;
        }

        public static int ApplyToAll(
            EnvironmentalInteractionGlobalTuning tuning,
            EnvironmentalInteractionType? type = null,
            bool recordUndo = true)
        {
            if (tuning == null)
                return 0;

            List<EnvironmentalInteractionBase> current =
                Resources.FindObjectsOfTypeAll<EnvironmentalInteractionBase>()
                    .Where(interaction =>
                        interaction != null &&
                        interaction.gameObject.scene.IsValid() &&
                        interaction.gameObject.scene.isLoaded &&
                        !EditorUtility.IsPersistent(interaction) &&
                        (!type.HasValue || interaction.Type == type.Value))
                    .ToList();

            if (recordUndo && current.Count > 0)
                Undo.SetCurrentGroupName("Apply Global Environmental Tuning");

            foreach (EnvironmentalInteractionBase interaction in current)
                ApplyToInteraction(tuning, interaction, recordUndo);

            return current.Count;
        }

        public static bool ApplyToInteraction(
            EnvironmentalInteractionGlobalTuning tuning,
            EnvironmentalInteractionBase interaction,
            bool recordUndo)
        {
            if (tuning == null || interaction == null)
                return false;

            SerializedObject sourceObject = new SerializedObject(tuning);
            SerializedObject targetObject = new SerializedObject(interaction);
            sourceObject.UpdateIfRequiredOrScript();
            targetObject.UpdateIfRequiredOrScript();

            string groupName;
            string[] propertyNames;
            switch (interaction.Type)
            {
                case EnvironmentalInteractionType.Drop:
                    groupName = "drop";
                    propertyNames = DropProperties;
                    break;
                case EnvironmentalInteractionType.Push:
                    groupName = "push";
                    propertyNames = PushProperties;
                    break;
                case EnvironmentalInteractionType.Shock:
                    groupName = "shock";
                    propertyNames = ShockProperties;
                    break;
                default:
                    groupName = "explode";
                    propertyNames = ExplodeProperties;
                    break;
            }

            SerializedProperty sourceGroup = sourceObject.FindProperty(groupName);
            if (sourceGroup == null)
                return false;

            if (recordUndo)
                Undo.RecordObject(interaction, "Apply Global " + interaction.Type + " Tuning");

            foreach (string propertyName in propertyNames)
            {
                SerializedProperty source = sourceGroup.FindPropertyRelative(propertyName);
                SerializedProperty target = targetObject.FindProperty(propertyName);
                if (source != null && target != null)
                    CopyValue(source, target);
            }

            targetObject.ApplyModifiedPropertiesWithoutUndo();
            Synchronize(interaction);
            interaction.ApplyVisualColors();
            EditorUtility.SetDirty(interaction);
            PrefabUtility.RecordPrefabInstancePropertyModifications(interaction);
            return true;
        }

        private static void Synchronize(EnvironmentalInteractionBase interaction)
        {
            if (interaction is DropInteraction drop)
                drop.SynchronizeImpactZone();
            else if (interaction is PushInteraction push)
                push.SynchronizeEffectZone();
            else if (interaction is ShockInteraction shock)
                shock.SynchronizeAreaVisual();
            else if (interaction is ExplodeInteraction explode)
                explode.SynchronizeAreaVisual();
        }

        private static void CopyValue(SerializedProperty source, SerializedProperty target)
        {
            switch (source.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    target.boolValue = source.boolValue;
                    break;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                    target.intValue = source.intValue;
                    break;
                case SerializedPropertyType.Float:
                    target.floatValue = source.floatValue;
                    break;
                case SerializedPropertyType.Enum:
                    target.enumValueIndex = source.enumValueIndex;
                    break;
                case SerializedPropertyType.Color:
                    target.colorValue = source.colorValue;
                    break;
                case SerializedPropertyType.Vector3:
                    target.vector3Value = source.vector3Value;
                    break;
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[index]);
                current = next;
            }
        }
    }
}
