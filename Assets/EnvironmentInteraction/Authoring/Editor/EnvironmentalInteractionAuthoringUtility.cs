using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace EnvironmentInteraction.Authoring.Editor
{
    internal static class EnvironmentalInteractionAuthoringUtility
    {
        private const string HighlightMaterialPath =
            "Assets/EnvironmentInteraction/Authoring/Materials/EnvironmentalInteractableHighlight.mat";
        private const string HighlightShaderName =
            "Zombie Prototype/Environmental Interactable Pulse";
        private const string AreaPreviewMaterialPath =
            "Assets/EnvironmentInteraction/Authoring/Materials/EnvironmentalAreaPreview.mat";
        private const string AreaPreviewShaderName =
            "Zombie Prototype/Environmental Area Preview";
        private static readonly Color DropContainerColor = new Color(0.95f, 0.55f, 0.08f, 1f);
        private static readonly Color HydrantColor = new Color(0.78f, 0.04f, 0.025f, 1f);
        private static readonly Color PowerBoxColor = new Color(0.88f, 0.62f, 0.08f, 1f);
        private static readonly Color ExplosiveColor = new Color(0.86f, 0.12f, 0.035f, 1f);
        private static readonly Color RedWireColor = new Color(0.88f, 0.025f, 0.015f, 1f);
        private static readonly Color WeakPointRed = new Color(0.92f, 0.035f, 0.02f, 1f);
        private static readonly Color WeakPointYellow = new Color(1f, 0.72f, 0.05f, 1f);

        public static EnvironmentalInteractionBase CreateInteraction(
            EnvironmentalInteractionType type,
            Transform parent = null)
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create " + type + " Environmental Interaction");

            GameObject root = new GameObject(GetUniqueName(type));
            Undo.RegisterCreatedObjectUndo(root, "Create " + type + " Environmental Interaction");
            root.transform.position = GetScenePlacementPosition();
            if (parent != null)
                Undo.SetTransformParent(root.transform, parent, "Parent Environmental Interaction");

            root.AddComponent<global::EnvironmentInteraction.EnvironmentInteractionMarker>();
            EnvironmentalInteractionBase interaction;

            switch (type)
            {
                case EnvironmentalInteractionType.Drop:
                    interaction = CreateDrop(root);
                    break;
                case EnvironmentalInteractionType.Push:
                    interaction = CreatePush(root);
                    break;
                case EnvironmentalInteractionType.Shock:
                    interaction = CreateShock(root);
                    break;
                default:
                    interaction = CreateExplode(root);
                    break;
            }

            EnvironmentalInteractionGlobalTuningUtility.ApplyToInteraction(
                EnvironmentalInteractionGlobalTuningUtility.GetOrCreate(),
                interaction,
                false);
            EnsureGeneratedAreaVisual(interaction);

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorUtility.SetDirty(root);
            Undo.CollapseUndoOperations(undoGroup);
            SceneView.RepaintAll();
            return interaction;
        }

        public static EnvironmentalTrigger AssignExistingTrigger(
            EnvironmentalInteractionBase interaction,
            GameObject triggerObject)
        {
            if (interaction == null || triggerObject == null)
                return null;

            Undo.RecordObject(interaction, "Assign Environmental Trigger");
            EnvironmentalTrigger trigger = EnsureTriggerSetup(triggerObject);
            interaction.SetTrigger(trigger);
            EditorUtility.SetDirty(interaction);
            return trigger;
        }

        public static EnvironmentalTrigger CreatePlaceholderTrigger(
            EnvironmentalInteractionBase interaction)
        {
            if (interaction == null)
                return null;

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Create Environmental Trigger");

            GameObject triggerObject = CreatePrimitive(
                "Trigger",
                PrimitiveType.Cube,
                interaction.transform,
                Vector3.up,
                new Vector3(0.35f, 0.35f, 0.18f));
            Undo.RegisterCreatedObjectUndo(triggerObject, "Create Environmental Trigger");
            EnvironmentalTrigger trigger = AssignExistingTrigger(interaction, triggerObject);

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = interaction.gameObject;
            return trigger;
        }

        public static EnvironmentalTrigger EnsureTriggerSetup(GameObject triggerObject)
        {
            EnvironmentalTrigger trigger = triggerObject.GetComponent<EnvironmentalTrigger>();
            if (trigger == null)
                trigger = Undo.AddComponent<EnvironmentalTrigger>(triggerObject);

            Collider triggerCollider = triggerObject.GetComponent<Collider>();
            if (triggerCollider == null)
                triggerCollider = triggerObject.GetComponentInChildren<Collider>();
            if (triggerCollider == null)
                triggerCollider = Undo.AddComponent<BoxCollider>(triggerObject);
            if (triggerCollider.isTrigger)
            {
                Undo.RecordObject(triggerCollider, "Configure Shootable Trigger Collider");
                triggerCollider.isTrigger = false;
            }

            Renderer visual = triggerObject.GetComponent<Renderer>();
            if (visual == null)
                visual = triggerObject.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(renderer => renderer.gameObject.name != "InteractableHighlightOverlay");

            EnvironmentalInteractableHighlight highlight =
                triggerObject.GetComponent<EnvironmentalInteractableHighlight>();
            if (highlight == null)
                highlight = Undo.AddComponent<EnvironmentalInteractableHighlight>(triggerObject);

            Renderer overlay = EnsureHighlightOverlay(visual);
            Material material = GetOrCreateHighlightMaterial();
            highlight.Configure(overlay != null ? new[] { overlay } : new Renderer[0], material);
            trigger.Configure(triggerObject.transform, triggerCollider, visual, highlight);

            EditorUtility.SetDirty(trigger);
            EditorUtility.SetDirty(highlight);
            return trigger;
        }

        private static EnvironmentalInteractionBase CreateDrop(GameObject root)
        {
            GameObject dropObject = CreatePrimitive(
                "DropObject",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(0f, 3.375f, 0f),
                new Vector3(4f, 0.75f, 2.5f));
            GameObject triggerObject = CreatePrimitive(
                "Trigger",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(0f, 4.5f, 0f),
                new Vector3(0.08f, 0.75f, 0.08f));
            GameObject effectZone = CreateEmpty("EffectZone", root.transform, Vector3.zero);
            GameObject impactZone = CreateEmpty("ImpactZone", effectZone.transform, Vector3.zero);
            NavMeshObstacle landedObstacle = dropObject.AddComponent<NavMeshObstacle>();
            landedObstacle.enabled = false;

            EnvironmentalTrigger trigger = EnsureTriggerSetup(triggerObject);
            DropInteraction interaction = root.AddComponent<DropInteraction>();
            interaction.ConfigureCommon("Drop Interaction", trigger);
            interaction.ConfigureVisuals(
                new[] { dropObject.GetComponent<Renderer>() },
                DropContainerColor,
                WeakPointRed);
            interaction.Configure(dropObject.transform, impactZone.transform, landedObstacle);
            return interaction;
        }

        private static EnvironmentalInteractionBase CreatePush(GameObject root)
        {
            EnvironmentalTrigger trigger = BuildFireHydrant(
                root.transform,
                out Transform waterOrigin,
                out Renderer[] hydrantRenderers);
            GameObject effectZone = CreateEmpty("EffectZone", root.transform, Vector3.forward * 3f);

            PushInteraction interaction = root.AddComponent<PushInteraction>();
            interaction.ConfigureCommon("Push Interaction", trigger);
            interaction.ConfigureVisuals(hydrantRenderers, HydrantColor, WeakPointYellow);
            interaction.Configure(waterOrigin, effectZone.transform);
            return interaction;
        }

        private static EnvironmentalInteractionBase CreateShock(GameObject root)
        {
            GameObject source = CreatePrimitive(
                "ElectricalSource",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(0f, 1f, 0f),
                new Vector3(0.8f, 1.4f, 0.35f));
            GameObject wire = CreateVisualPrimitive(
                "Wire",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(-0.75f, 0.85f, 0f),
                new Vector3(0.08f, 0.85f, 0.08f),
                Quaternion.identity);
            GameObject conductiveArea = CreatePrimitive(
                "ConductiveArea",
                PrimitiveType.Cube,
                root.transform,
                new Vector3(0f, 0.025f, 2.25f),
                new Vector3(5f, 0.05f, 4f));
            Collider areaCollider = conductiveArea.GetComponent<Collider>();
            if (areaCollider != null)
                Object.DestroyImmediate(areaCollider);

            EnvironmentalTrigger trigger = EnsureTriggerSetup(source);
            ShockInteraction interaction = root.AddComponent<ShockInteraction>();
            interaction.ConfigureCommon("Shock Interaction", trigger);
            interaction.ConfigureVisuals(
                new[] { source.GetComponent<Renderer>() },
                PowerBoxColor,
                PowerBoxColor);
            interaction.Configure(
                source.transform,
                conductiveArea.transform,
                conductiveArea.transform,
                conductiveArea.GetComponent<Renderer>(),
                wire.GetComponent<Renderer>());
            return interaction;
        }

        private static EnvironmentalInteractionBase CreateExplode(GameObject root)
        {
            GameObject explosiveObject = CreatePrimitive(
                "ExplosiveObject",
                PrimitiveType.Cylinder,
                root.transform,
                new Vector3(0f, 0.75f, 0f),
                new Vector3(0.55f, 0.75f, 0.55f));
            GameObject explosionZone = CreateEmpty(
                "ExplosionZone",
                root.transform,
                explosiveObject.transform.localPosition);

            EnvironmentalTrigger trigger = EnsureTriggerSetup(explosiveObject);
            ExplodeInteraction interaction = root.AddComponent<ExplodeInteraction>();
            interaction.ConfigureCommon("Explode Interaction", trigger);
            interaction.ConfigureVisuals(
                new[] { explosiveObject.GetComponent<Renderer>() },
                ExplosiveColor,
                ExplosiveColor);
            interaction.Configure(explosiveObject.transform, explosionZone.transform);
            return interaction;
        }

        public static bool ApplyGeneratedVisualDefaults(EnvironmentalInteractionBase interaction)
        {
            if (interaction == null)
                return false;

            bool areaVisualChanged = EnsureGeneratedAreaVisual(interaction);

            if (interaction is DropInteraction drop &&
                drop.DropObject != null &&
                drop.DropObject.name == "DropObject")
            {
                interaction.ConfigureVisuals(
                    new[] { drop.DropObject.GetComponent<Renderer>() },
                    DropContainerColor,
                    WeakPointRed);
                EditorUtility.SetDirty(interaction);
                return true;
            }

            if (interaction is PushInteraction push &&
                push.PushOrigin != null &&
                push.PushOrigin.name == "Source")
            {
                GameObject oldSource = push.PushOrigin.gameObject;
                EnvironmentalTrigger newTrigger = BuildFireHydrant(
                    interaction.transform,
                    out Transform waterOrigin,
                    out Renderer[] renderers);
                Undo.RegisterCreatedObjectUndo(
                    waterOrigin.parent.gameObject,
                    "Upgrade PUSH Fire Hydrant");
                interaction.SetTrigger(newTrigger);
                interaction.ConfigureVisuals(renderers, HydrantColor, WeakPointYellow);
                push.Configure(waterOrigin, push.EffectZone);
                Undo.DestroyObjectImmediate(oldSource);
                EditorUtility.SetDirty(interaction);
                return true;
            }

            if (interaction is ShockInteraction shock &&
                shock.ElectricalSource != null &&
                shock.ElectricalSource.name == "ElectricalSource")
            {
                EnvironmentalTrigger oldTrigger = interaction.Trigger;
                EnvironmentalTrigger newTrigger = EnsureTriggerSetup(shock.ElectricalSource.gameObject);
                interaction.SetTrigger(newTrigger);

                Transform existingWire = interaction.transform.Find("Wire");
                GameObject wire = existingWire != null
                    ? existingWire.gameObject
                    : CreateVisualPrimitive(
                        "Wire",
                        PrimitiveType.Cylinder,
                        interaction.transform,
                        new Vector3(-0.75f, 0.85f, 0f),
                        new Vector3(0.08f, 0.85f, 0.08f),
                        Quaternion.identity);
                if (existingWire == null)
                    Undo.RegisterCreatedObjectUndo(wire, "Add SHOCK Wire");

                Renderer conductiveRenderer = shock.ConductiveSurface != null
                    ? shock.ConductiveSurface.GetComponent<Renderer>()
                    : null;
                interaction.ConfigureVisuals(
                    new[] { shock.ElectricalSource.GetComponent<Renderer>() },
                    PowerBoxColor,
                    PowerBoxColor);
                shock.Configure(
                    shock.ElectricalSource,
                    shock.ShockArea,
                    shock.ConductiveSurface,
                    conductiveRenderer,
                    wire.GetComponent<Renderer>());
                if (oldTrigger != null && oldTrigger != newTrigger)
                    Undo.DestroyObjectImmediate(oldTrigger.gameObject);
                EditorUtility.SetDirty(interaction);
                return true;
            }

            if (interaction is ExplodeInteraction explode && explode.ExplosiveObject != null)
            {
                interaction.ConfigureVisuals(
                    new[] { explode.ExplosiveObject.GetComponent<Renderer>() },
                    ExplosiveColor,
                    ExplosiveColor);
                EditorUtility.SetDirty(interaction);
                return true;
            }

            return areaVisualChanged;
        }

        public static bool EnsureGeneratedAreaVisual(EnvironmentalInteractionBase interaction)
        {
            if (interaction == null)
                return false;

            Transform areaTransform = null;
            Color color = new Color(0.2f, 0.7f, 1f, 0.14f);
            if (interaction is DropInteraction drop)
            {
                areaTransform = drop.ImpactZone;
                color = drop.ImpactPulseColor;
            }
            else if (interaction is PushInteraction push)
            {
                areaTransform = push.EffectZone;
                color = push.WaterColor;
            }
            else if (interaction is ShockInteraction shock)
            {
                areaTransform = shock.ShockArea;
                color = shock.ShockEffectColor;
            }
            else if (interaction is ExplodeInteraction explode)
            {
                areaTransform = explode.ExplosionOrigin;
                color = explode.EffectColor;
            }

            if (areaTransform == null)
                return false;

            bool changed = false;
            EnvironmentalAreaVisual areaVisual = areaTransform.GetComponent<EnvironmentalAreaVisual>();
            if (areaVisual == null)
            {
                areaVisual = Undo.AddComponent<EnvironmentalAreaVisual>(areaTransform.gameObject);
                changed = true;
            }

            Transform boxVisual = areaTransform.Find("AffectedAreaPreview_Box");
            if (boxVisual == null)
            {
                GameObject box = CreateVisualPrimitive(
                    "AffectedAreaPreview_Box",
                    PrimitiveType.Cube,
                    areaTransform,
                    Vector3.zero,
                    Vector3.one,
                    Quaternion.identity);
                Undo.RegisterCreatedObjectUndo(box, "Create Box Area Preview");
                boxVisual = box.transform;
                changed = true;
            }

            Transform sphereVisual = areaTransform.Find("AffectedAreaPreview_Sphere");
            if (sphereVisual == null)
            {
                GameObject sphere = CreateVisualPrimitive(
                    "AffectedAreaPreview_Sphere",
                    PrimitiveType.Sphere,
                    areaTransform,
                    Vector3.zero,
                    Vector3.one,
                    Quaternion.identity);
                Undo.RegisterCreatedObjectUndo(sphere, "Create Sphere Area Preview");
                sphereVisual = sphere.transform;
                changed = true;
            }

            Material material = GetOrCreateAreaPreviewMaterial();
            ConfigureAreaPreviewRenderer(boxVisual.GetComponent<Renderer>(), material);
            ConfigureAreaPreviewRenderer(sphereVisual.GetComponent<Renderer>(), material);
            areaVisual.Configure(boxVisual, sphereVisual);
            SynchronizeAreaVisual(interaction);
            color.a = Mathf.Min(color.a, interaction is PushInteraction || interaction is ExplodeInteraction
                ? 0.14f
                : 0.16f);
            EnvironmentalVisualUtility.ApplyColor(boxVisual.GetComponent<Renderer>(), color);
            EnvironmentalVisualUtility.ApplyColor(sphereVisual.GetComponent<Renderer>(), color);
            EditorUtility.SetDirty(areaVisual);
            return changed;
        }

        private static void SynchronizeAreaVisual(EnvironmentalInteractionBase interaction)
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

        private static void ConfigureAreaPreviewRenderer(Renderer renderer, Material material)
        {
            if (renderer == null)
                return;

            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static EnvironmentalTrigger BuildFireHydrant(
            Transform parent,
            out Transform waterOrigin,
            out Renderer[] coloredRenderers)
        {
            GameObject hydrant = CreateEmpty("FireHydrant", parent, Vector3.zero);
            GameObject body = CreateVisualPrimitive(
                "Body",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(0f, 0.75f, 0f),
                new Vector3(0.46f, 0.72f, 0.46f),
                Quaternion.identity);
            GameObject baseRing = CreateVisualPrimitive(
                "BaseRing",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(0f, 0.12f, 0f),
                new Vector3(0.62f, 0.12f, 0.62f),
                Quaternion.identity);
            GameObject collar = CreateVisualPrimitive(
                "Collar",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(0f, 1.34f, 0f),
                new Vector3(0.6f, 0.12f, 0.6f),
                Quaternion.identity);
            GameObject dome = CreateVisualPrimitive(
                "Dome",
                PrimitiveType.Sphere,
                hydrant.transform,
                new Vector3(0f, 1.55f, 0f),
                new Vector3(0.53f, 0.34f, 0.53f),
                Quaternion.identity);
            GameObject topCap = CreateVisualPrimitive(
                "TopCap",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(0f, 1.83f, 0f),
                new Vector3(0.2f, 0.12f, 0.2f),
                Quaternion.identity);
            GameObject leftCap = CreateVisualPrimitive(
                "LeftCap",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(-0.55f, 0.92f, 0f),
                new Vector3(0.28f, 0.16f, 0.28f),
                Quaternion.Euler(0f, 0f, 90f));
            GameObject rightCap = CreateVisualPrimitive(
                "RightCap",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(0.55f, 0.92f, 0f),
                new Vector3(0.28f, 0.16f, 0.28f),
                Quaternion.Euler(0f, 0f, 90f));
            GameObject triggerValve = CreatePrimitive(
                "TriggerValve",
                PrimitiveType.Cylinder,
                hydrant.transform,
                new Vector3(0f, 0.92f, 0.55f),
                new Vector3(0.28f, 0.16f, 0.28f));
            triggerValve.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject nozzle = CreateEmpty(
                "WaterNozzle",
                hydrant.transform,
                new Vector3(0f, 0.92f, 0.78f));
            waterOrigin = nozzle.transform;
            coloredRenderers = new[]
            {
                body.GetComponent<Renderer>(),
                baseRing.GetComponent<Renderer>(),
                collar.GetComponent<Renderer>(),
                dome.GetComponent<Renderer>(),
                topCap.GetComponent<Renderer>(),
                leftCap.GetComponent<Renderer>(),
                rightCap.GetComponent<Renderer>()
            };
            EnvironmentalVisualUtility.ApplyColor(coloredRenderers, HydrantColor);
            EnvironmentalTrigger trigger = EnsureTriggerSetup(triggerValve);
            trigger.ApplyVisualColor(WeakPointYellow);
            return trigger;
        }

        private static GameObject CreatePrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject instance = GameObject.CreatePrimitive(primitiveType);
            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = localScale;
            return instance;
        }

        private static GameObject CreateVisualPrimitive(
            string name,
            PrimitiveType primitiveType,
            Transform parent,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation)
        {
            GameObject instance = CreatePrimitive(
                name,
                primitiveType,
                parent,
                localPosition,
                localScale);
            instance.transform.localRotation = localRotation;
            Collider collider = instance.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);
            return instance;
        }

        private static GameObject CreateEmpty(string name, Transform parent, Vector3 localPosition)
        {
            GameObject instance = new GameObject(name);
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            return instance;
        }

        private static Renderer EnsureHighlightOverlay(Renderer sourceRenderer)
        {
            if (sourceRenderer == null)
                return null;

            Transform existing = sourceRenderer.transform.Find("InteractableHighlightOverlay");
            if (existing != null)
                return existing.GetComponent<Renderer>();

            MeshFilter sourceMeshFilter = sourceRenderer.GetComponent<MeshFilter>();
            SkinnedMeshRenderer sourceSkinnedRenderer = sourceRenderer as SkinnedMeshRenderer;
            if (sourceMeshFilter == null && sourceSkinnedRenderer == null)
                return null;

            GameObject overlayObject = new GameObject("InteractableHighlightOverlay");
            Undo.RegisterCreatedObjectUndo(overlayObject, "Create Interactable Highlight Overlay");
            overlayObject.layer = sourceRenderer.gameObject.layer;
            overlayObject.transform.SetParent(sourceRenderer.transform, false);
            overlayObject.transform.localPosition = Vector3.zero;
            overlayObject.transform.localRotation = Quaternion.identity;
            overlayObject.transform.localScale = Vector3.one * 1.015f;

            Renderer overlayRenderer;
            if (sourceSkinnedRenderer != null)
            {
                SkinnedMeshRenderer overlaySkinnedRenderer = overlayObject.AddComponent<SkinnedMeshRenderer>();
                overlaySkinnedRenderer.sharedMesh = sourceSkinnedRenderer.sharedMesh;
                overlaySkinnedRenderer.bones = sourceSkinnedRenderer.bones;
                overlaySkinnedRenderer.rootBone = sourceSkinnedRenderer.rootBone;
                overlaySkinnedRenderer.localBounds = sourceSkinnedRenderer.localBounds;
                overlayRenderer = overlaySkinnedRenderer;
            }
            else
            {
                MeshFilter overlayFilter = overlayObject.AddComponent<MeshFilter>();
                overlayFilter.sharedMesh = sourceMeshFilter.sharedMesh;
                overlayRenderer = overlayObject.AddComponent<MeshRenderer>();
            }

            overlayRenderer.shadowCastingMode = ShadowCastingMode.Off;
            overlayRenderer.receiveShadows = false;
            overlayRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            overlayRenderer.sharedMaterial = GetOrCreateHighlightMaterial();
            return overlayRenderer;
        }

        private static Material GetOrCreateHighlightMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);
            Shader pulseShader = Shader.Find(HighlightShaderName);
            if (material != null)
            {
                if (pulseShader != null && material.shader != pulseShader)
                {
                    material.shader = pulseShader;
                    ConfigureHighlightMaterial(material);
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                }
                return material;
            }

            EnsureAssetFolder("Assets/EnvironmentInteraction/Authoring/Materials");
            Shader shader = pulseShader != null
                ? pulseShader
                : Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            material = new Material(shader)
            {
                name = "Environmental Interactable Highlight",
                renderQueue = (int)RenderQueue.Transparent
            };
            ConfigureHighlightMaterial(material);

            AssetDatabase.CreateAsset(material, HighlightMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Material GetOrCreateAreaPreviewMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(AreaPreviewMaterialPath);
            Shader previewShader = Shader.Find(AreaPreviewShaderName);
            if (material != null)
            {
                if (previewShader != null && material.shader != previewShader)
                {
                    material.shader = previewShader;
                    ConfigureAreaPreviewMaterial(material);
                    EditorUtility.SetDirty(material);
                    AssetDatabase.SaveAssets();
                }
                return material;
            }

            EnsureAssetFolder("Assets/EnvironmentInteraction/Authoring/Materials");
            Shader shader = previewShader != null
                ? previewShader
                : Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            material = new Material(shader)
            {
                name = "Environmental Area Preview",
                renderQueue = (int)RenderQueue.Transparent
            };
            ConfigureAreaPreviewMaterial(material);
            AssetDatabase.CreateAsset(material, AreaPreviewMaterialPath);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void ConfigureAreaPreviewMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            Color color = new Color(0.2f, 0.7f, 1f, 0.14f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
        }

        private static void ConfigureHighlightMaterial(Material material)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend"))
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend"))
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            Color color = new Color(0.55f, 0.82f, 1f, 0.08f);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_FlashColor"))
                material.SetColor("_FlashColor", new Color(1f, 0.48f, 0.08f, 1f));
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_BaseOpacity"))
                material.SetFloat("_BaseOpacity", 0.1f);
            if (material.HasProperty("_PulseAmount"))
                material.SetFloat("_PulseAmount", 0.08f);
            if (material.HasProperty("_PulseSpeed"))
                material.SetFloat("_PulseSpeed", 0.8f);
            if (material.HasProperty("_EmissionAmount"))
                material.SetFloat("_EmissionAmount", 0.2f);
        }

        private static void EnsureAssetFolder(string path)
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

        private static Vector3 GetScenePlacementPosition()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                return sceneView.pivot;
            if (Selection.activeTransform != null)
                return Selection.activeTransform.position;
            return Vector3.zero;
        }

        private static string GetUniqueName(EnvironmentalInteractionType type)
        {
            string prefix = "ENV_" + type + "_";
            HashSet<string> sceneNames = Resources.FindObjectsOfTypeAll<EnvironmentalInteractionBase>()
                .Where(interaction => interaction != null && interaction.gameObject.scene.IsValid())
                .Select(interaction => interaction.gameObject.name)
                .ToHashSet();

            for (int index = 1; index < 10000; index++)
            {
                string candidate = prefix + index.ToString("000");
                if (!sceneNames.Contains(candidate))
                    return candidate;
            }

            return prefix + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        }
    }
}
