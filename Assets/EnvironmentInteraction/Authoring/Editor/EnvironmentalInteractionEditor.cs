using UnityEditor;
using UnityEngine;

namespace EnvironmentInteraction.Authoring.Editor
{
    [CustomEditor(typeof(EnvironmentalInteractionBase), true)]
    public sealed class EnvironmentalInteractionEditor : UnityEditor.Editor
    {
        private SerializedProperty interactionId;
        private SerializedProperty displayName;
        private SerializedProperty interactionEnabled;
        private SerializedProperty isOneUse;
        private SerializedProperty trigger;
        private SerializedProperty objectRenderers;
        private SerializedProperty objectColor;
        private SerializedProperty triggerColor;
        private SerializedProperty showSceneGizmos;
        private SerializedProperty showLabels;
        private SerializedProperty onActivated;
        private SerializedProperty onEffectCompleted;
        private SerializedProperty designerNotes;

        private GameObject existingTriggerObject;

        private EnvironmentalInteractionBase Interaction => target as EnvironmentalInteractionBase;

        private void OnEnable()
        {
            interactionId = serializedObject.FindProperty("interactionId");
            displayName = serializedObject.FindProperty("displayName");
            interactionEnabled = serializedObject.FindProperty("interactionEnabled");
            isOneUse = serializedObject.FindProperty("isOneUse");
            trigger = serializedObject.FindProperty("trigger");
            objectRenderers = serializedObject.FindProperty("objectRenderers");
            objectColor = serializedObject.FindProperty("objectColor");
            triggerColor = serializedObject.FindProperty("triggerColor");
            showSceneGizmos = serializedObject.FindProperty("showSceneGizmos");
            showLabels = serializedObject.FindProperty("showLabels");
            onActivated = serializedObject.FindProperty("onActivated");
            onEffectCompleted = serializedObject.FindProperty("onEffectCompleted");
            designerNotes = serializedObject.FindProperty("designerNotes");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawGeneralSection();
            EditorGUILayout.Space(7f);
            DrawTriggerSection();
            EditorGUILayout.Space(7f);
            DrawAppearanceSection();
            EditorGUILayout.Space(7f);
            DrawEffectSection();
            EditorGUILayout.Space(7f);
            DrawVisualizationSection();
            EditorGUILayout.Space(7f);
            DrawNotesSection();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            EnvironmentalInteractionBase interaction = Interaction;
            if (interaction != null && interaction.ShowSceneGizmos)
                EnvironmentalInteractionSceneHandles.DrawSelected(interaction);
        }

        private void DrawGeneralSection()
        {
            EditorGUILayout.LabelField("GENERAL", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.EnumPopup("Interaction Type", Interaction.Type);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(interactionId, new GUIContent("Interaction ID"));
            if (GUILayout.Button("Regenerate", GUILayout.Width(86f)))
            {
                Undo.RecordObject(Interaction, "Regenerate Interaction ID");
                Interaction.RegenerateInteractionId();
                EditorUtility.SetDirty(Interaction);
                serializedObject.Update();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(interactionEnabled, new GUIContent("Enabled"));
            EditorGUILayout.PropertyField(isOneUse, new GUIContent("One Use"));
            if (Application.isPlaying)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.Toggle("Has Activated", Interaction.HasActivated);
            }
        }

        private void DrawTriggerSection()
        {
            EditorGUILayout.LabelField("TRIGGER — WHAT THE PLAYER SHOOTS", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(trigger, new GUIContent("Trigger"));

            EnvironmentalTrigger currentTrigger = Interaction.Trigger;
            if (currentTrigger == null)
            {
                EditorGUILayout.HelpBox(
                    "Assign an EnvironmentalTrigger, use an existing scene object, or create a placeholder weak point.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(GetTriggerGuidance(), MessageType.Info);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.ObjectField(
                        "Trigger Collider",
                        currentTrigger.TriggerCollider,
                        typeof(Collider),
                        true);
                    EditorGUILayout.ObjectField(
                        "Highlight",
                        currentTrigger.InteractableHighlight,
                        typeof(EnvironmentalInteractableHighlight),
                        true);
                }

                if (GUILayout.Button("Select Trigger"))
                    Selection.activeGameObject = currentTrigger.gameObject;
            }

            existingTriggerObject = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Existing Scene Object", "Drag any scene object here, then configure it as the physical trigger."),
                existingTriggerObject,
                typeof(GameObject),
                true);

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(existingTriggerObject == null))
            {
                if (GUILayout.Button("Use Existing Object"))
                {
                    serializedObject.ApplyModifiedProperties();
                    EnvironmentalInteractionAuthoringUtility.AssignExistingTrigger(
                        Interaction,
                        existingTriggerObject);
                    serializedObject.Update();
                    SceneView.RepaintAll();
                }
            }

            if (GUILayout.Button("Create Placeholder Trigger"))
            {
                serializedObject.ApplyModifiedProperties();
                EnvironmentalInteractionAuthoringUtility.CreatePlaceholderTrigger(Interaction);
                serializedObject.Update();
                SceneView.RepaintAll();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAppearanceSection()
        {
            EditorGUILayout.LabelField("APPEARANCE — COLOR CODING", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(objectRenderers, new GUIContent("Colored Renderers"), true);
            EditorGUILayout.PropertyField(objectColor, new GUIContent("Object Color"));
            EditorGUILayout.PropertyField(triggerColor, new GUIContent("Shootable Part Color"));
        }

        private void DrawEffectSection()
        {
            EditorGUILayout.LabelField("EFFECT — WHAT HAPPENS AND WHERE", EditorStyles.boldLabel);

            if (Interaction is DropInteraction)
            {
                DrawProperty("dropObject", "Drop Object");
                EditorGUILayout.HelpBox(
                    "DROP leads forward by the authored distance, then searches straight down without a distance limit for the first solid floor in Floor Layers.",
                    MessageType.Info);
                DrawProperty("floorLayers", "Floor Layers");
                DrawProperty("surfaceClearance", "Ground Clearance");
                DrawProperty("dropDelay", "Drop Delay");
                DrawProperty("forwardTravelDistance", "Forward Lead Distance");
                DrawProperty("impactZone", "Impact Zone");
                DrawProperty("impactShape", "Impact Shape");
                SerializedProperty shape = serializedObject.FindProperty("impactShape");
                if ((EnvironmentalAreaShape)shape.enumValueIndex == EnvironmentalAreaShape.Sphere)
                    DrawProperty("impactRadius", "Impact Radius");
                else
                    DrawProperty("impactBoxSize", "Smash / Container Size");
                DrawProperty("becomesNavMeshObstacle", "Blocks Navigation After Landing");
                DrawProperty("fallDuration", "Fall Duration");
                DrawProperty("impactDamage", "Smash Damage");
                DrawProperty("impactForce", "Smash Force");
                DrawProperty("spawnImpactParticles", "Impact Particles");
                DrawProperty("impactParticleColor", "Dust / Debris Color");
                DrawProperty("impactParticleAmount", "Particle Amount");
                DrawProperty("impactPulseColor", "Impact Pulse Color");
                DrawProperty("affectedLayers", "Affected Layers");
            }
            else if (Interaction is PushInteraction)
            {
                DrawProperty("pushOrigin", "Water Nozzle / Push Origin");
                DrawProperty("effectZone", "Effect Zone");
                DrawProperty("pushDirection", "Push Direction");
                DrawProperty("pushRange", "Water Reach");
                DrawProperty("pushWidth", "Push Area Width");
                DrawProperty("pushHeight", "Push Area Height");
                DrawProperty("forceValue", "Pushback Power");
                DrawProperty("duration", "Sustained Push Duration");
                DrawProperty("spawnWaterParticles", "Water Particles");
                DrawProperty("waterColor", "Water Color");
                DrawProperty("waterParticleAmount", "Water Particle Amount");
                DrawProperty("affectedLayers", "Affected Layers");
            }
            else if (Interaction is ShockInteraction)
            {
                DrawProperty("electricalSource", "Electrical Source");
                DrawProperty("shockArea", "Shock Area");
                DrawProperty("shockAreaShape", "Shock Area Shape");
                SerializedProperty shape = serializedObject.FindProperty("shockAreaShape");
                if ((EnvironmentalAreaShape)shape.enumValueIndex == EnvironmentalAreaShape.Sphere)
                    DrawProperty("radius", "Shock Radius");
                else
                    DrawProperty("boxSize", "Shock Area Size");
                DrawProperty("duration", "Shock Duration");
                DrawProperty("delay", "Activation Delay");
                DrawProperty("conductiveSurface", "Conductive Surface");
                DrawProperty("conductiveSurfaceRenderer", "Water Renderer");
                DrawProperty("conductiveSurfaceColor", "Water Color");
                DrawProperty("wireRenderer", "Wire Renderer");
                DrawProperty("wireColor", "Wire Color");
                DrawProperty("shockEffectColor", "Shock Effect Color");
                DrawProperty("spawnActiveZoneParticles", "Active Zone Electricity");
                DrawProperty("activeZoneParticleColor", "Active Zone Particle Color");
                DrawProperty("activeZoneParticleAmount", "Active Zone Particle Amount");
                DrawProperty("spawnTazeParticles", "Enemy Taze Particles");
                DrawProperty("tazeParticleColor", "Taze Particle Color");
                DrawProperty("tazeParticleAmount", "Taze Particle Amount");
                DrawProperty("tazeShakeStrength", "Enemy Shake Strength");
                DrawProperty("tazeShakeSpeed", "Enemy Shake Speed");
                DrawProperty("damagePerPulse", "Damage Per Pulse");
                DrawProperty("pulseInterval", "Seconds Between Pulses");
                DrawProperty("slowMultiplier", "Zombie Speed Multiplier (0 = Stopped)");
                DrawProperty("affectedLayers", "Affected Layers");
            }
            else if (Interaction is ExplodeInteraction)
            {
                DrawProperty("explosiveObject", "Explosive Object");
                DrawProperty("explosionOrigin", "Explosion Origin");
                DrawProperty("outerRadius", "Blast Radius");
                DrawProperty("innerRadius", "Full-Damage Radius");
                DrawProperty("delay", "Explosion Delay");
                DrawProperty("forceRepresentation", "Explosion Knockback Power");
                DrawProperty("damage", "Explosion Damage");
                DrawProperty("ragdollEnemies", "Ragdoll Enemies");
                DrawProperty("ragdollUpwardForce", "Ragdoll Upward Force");
                DrawProperty("ragdollTumbleTorque", "Ragdoll Tumble Torque");
                DrawProperty("ragdollDisappearDelay", "Corpse Disappear Delay");
                DrawProperty("effectColor", "Explosion Effect Color");
                DrawProperty("affectedLayers", "Affected Layers");
                DrawProperty("hideExplosiveObjectOnActivation", "Hide Explosive Object");
            }
        }

        private string GetTriggerGuidance()
        {
            switch (Interaction.Type)
            {
                case EnvironmentalInteractionType.Drop:
                    return "Shoot the flashing red string above the container.";
                case EnvironmentalInteractionType.Push:
                    return "Shoot any flashing part of the fire hydrant.";
                case EnvironmentalInteractionType.Shock:
                    return "Shoot the flashing power box. The red wire is visual guidance, not the trigger.";
                default:
                    return "Shoot the flashing explosive body.";
            }
        }

        private void DrawVisualizationSection()
        {
            EditorGUILayout.LabelField("VISUALIZATION", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(showSceneGizmos, new GUIContent("Show Scene Gizmos"));
            EditorGUILayout.PropertyField(showLabels, new GUIContent("Show Labels"));
            EditorGUILayout.PropertyField(onActivated, new GUIContent("On Activated"));
            EditorGUILayout.PropertyField(onEffectCompleted, new GUIContent("On Effect Completed"));
            using (new EditorGUI.DisabledScope(!showSceneGizmos.boolValue))
            {
                if (GUILayout.Button("Preview Effect In Scene View", GUILayout.Height(26f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    EnvironmentalInteractionPreview.Start(Interaction);
                    serializedObject.Update();
                }
            }
        }

        private void DrawNotesSection()
        {
            EditorGUILayout.LabelField("NOTES", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(designerNotes, GUIContent.none);
        }

        private void DrawProperty(string propertyName, string label)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
                EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }
    }
}
