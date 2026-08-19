using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ZombiePrototype.Editor
{
    public sealed class ZombiePlacementWindow : EditorWindow
    {
        private enum BrushMode
        {
            Paint,
            Erase
        }

        private const string ArchetypeFolder = "Assets/ZombiePrototype/Archetypes";
        private const float TileHeight = 70f;

        [SerializeField] private List<ZombieArchetype> archetypes = new List<ZombieArchetype>();
        [SerializeField] private int selectedArchetypeIndex;
        [SerializeField] private Transform parent;
        [SerializeField] private LayerMask placementLayers = ~0;
        [SerializeField] private BrushMode brushMode;
        [SerializeField, Min(0.1f)] private float brushRadius = 2f;
        [SerializeField, Min(0.05f)] private float strokeSpacing = 1.25f;
        [SerializeField, Range(1, 20)] private int zombiesPerStamp = 1;
        [SerializeField, Min(0f)] private float minimumSeparation = 1f;
        [SerializeField] private float heightOffset;
        [SerializeField] private bool randomYaw = true;
        [SerializeField] private float yaw;

        private Vector2 scrollPosition;
        private bool painting;
        private bool hasLastStamp;
        private Vector3 lastStampPosition;
        private int undoGroup = -1;

        private ZombieArchetype SelectedArchetype
        {
            get
            {
                if (archetypes == null || archetypes.Count == 0)
                    return null;
                selectedArchetypeIndex = Mathf.Clamp(selectedArchetypeIndex, 0, archetypes.Count - 1);
                return archetypes[selectedArchetypeIndex];
            }
        }

        [MenuItem("Tools/Zombie Prototype/Zombie Painter")]
        [MenuItem("Tools/Zombie Prototype/Zombie Placer")]
        private static void OpenWindow()
        {
            ZombiePlacementWindow window = GetWindow<ZombiePlacementWindow>("Zombie Painter");
            window.minSize = new Vector2(820f, 650f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshArchetypes();
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            EndStroke();
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.Space(8f);
            DrawArchetypeSelector();
            EditorGUILayout.Space(10f);
            DrawBalanceTable();
            EditorGUILayout.Space(10f);
            DrawBrushSettings();
            EditorGUILayout.EndScrollView();
        }

        private void DrawArchetypeSelector()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Zombie Type", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Types", GUILayout.Width(110f)))
                RefreshArchetypes();
            EditorGUILayout.EndHorizontal();

            if (archetypes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No ZombieArchetype assets were found. They are expected in " + ArchetypeFolder + ".",
                    MessageType.Warning);
                return;
            }

            const int columns = 3;
            for (int start = 0; start < archetypes.Count; start += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int column = 0; column < columns; column++)
                {
                    int index = start + column;
                    if (index >= archetypes.Count)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    ZombieArchetype profile = archetypes[index];
                    Color previousBackground = GUI.backgroundColor;
                    GUI.backgroundColor = index == selectedArchetypeIndex
                        ? Color.Lerp(profile.EditorColor, Color.white, 0.25f)
                        : Color.Lerp(profile.EditorColor, Color.gray, 0.45f);

                    string label = profile.DisplayName + "\n" +
                                   "HP " + profile.Health.ToString("0") +
                                   "   Speed " + profile.MoveSpeed.ToString("0.0") +
                                   "   DMG " + profile.AttackDamage.ToString("0");
                    if (GUILayout.Button(label, GUILayout.Height(TileHeight)))
                    {
                        selectedArchetypeIndex = index;
                        Repaint();
                        SceneView.RepaintAll();
                    }

                    GUI.backgroundColor = previousBackground;
                }
                EditorGUILayout.EndHorizontal();
            }

            ZombieArchetype selected = SelectedArchetype;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Selected Prefab", selected != null ? selected.Prefab : null, typeof(GameObject), false);
        }

        private void DrawBalanceTable()
        {
            EditorGUILayout.LabelField("Global Balance", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These are shared archetype values. Changes apply to every instance of that zombie type.",
                MessageType.Info);

            if (archetypes.Count == 0)
                return;

            const float typeWidth = 90f;
            const float numberWidth = 66f;
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Type", GUILayout.Width(typeWidth));
            GUILayout.Label("Health", GUILayout.Width(numberWidth));
            GUILayout.Label("Speed", GUILayout.Width(numberWidth));
            GUILayout.Label("Damage", GUILayout.Width(numberWidth));
            GUILayout.Label("Cooldown", GUILayout.Width(numberWidth));
            GUILayout.Label("Mass", GUILayout.Width(numberWidth));
            GUILayout.Label("KB Time", GUILayout.Width(numberWidth));
            GUILayout.Label("Explodes", GUILayout.Width(numberWidth));
            EditorGUILayout.EndHorizontal();

            foreach (ZombieArchetype profile in archetypes)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(profile.DisplayName, GUILayout.Width(typeWidth));
                float health = EditorGUILayout.FloatField(profile.Health, GUILayout.Width(numberWidth));
                float speed = EditorGUILayout.FloatField(profile.MoveSpeed, GUILayout.Width(numberWidth));
                float damage = EditorGUILayout.FloatField(profile.AttackDamage, GUILayout.Width(numberWidth));
                float cooldown = EditorGUILayout.FloatField(profile.AttackCooldown, GUILayout.Width(numberWidth));
                float mass = EditorGUILayout.FloatField(profile.BodyMass, GUILayout.Width(numberWidth));
                float knockbackTime = EditorGUILayout.FloatField(profile.KnockbackDuration, GUILayout.Width(numberWidth));
                bool explodes = EditorGUILayout.Toggle(profile.ExplodesOnDeath, GUILayout.Width(numberWidth));
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(profile, "Balance " + profile.DisplayName);
                    profile.Health = health;
                    profile.MoveSpeed = speed;
                    profile.AttackDamage = damage;
                    profile.AttackCooldown = cooldown;
                    profile.BodyMass = mass;
                    profile.KnockbackDuration = knockbackTime;
                    profile.ExplodesOnDeath = explodes;
                    EditorUtility.SetDirty(profile);
                }
            }

            ZombieArchetype selected = SelectedArchetype;
            if (selected == null || !selected.ExplodesOnDeath)
                return;

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField(selected.DisplayName + " Death Explosion", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            float radius = EditorGUILayout.FloatField("Radius", selected.ExplosionRadius);
            float explosionDamage = EditorGUILayout.FloatField("Damage", selected.ExplosionDamage);
            float force = EditorGUILayout.FloatField("Knockback Force", selected.ExplosionForce);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(selected, "Balance " + selected.DisplayName + " Explosion");
                selected.ExplosionRadius = radius;
                selected.ExplosionDamage = explosionDamage;
                selected.ExplosionForce = force;
                EditorUtility.SetDirty(selected);
            }
        }

        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            brushMode = (BrushMode)GUILayout.Toolbar((int)brushMode, new[] { "Paint", "Erase" });
            parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
            placementLayers = LayerMaskField("Placement Layers", placementLayers);
            brushRadius = EditorGUILayout.Slider("Radius", brushRadius, 0.1f, 20f);
            strokeSpacing = EditorGUILayout.Slider("Stroke Spacing", strokeSpacing, 0.05f, 10f);
            zombiesPerStamp = EditorGUILayout.IntSlider("Zombies Per Stamp", zombiesPerStamp, 1, 20);
            minimumSeparation = EditorGUILayout.Slider("Minimum Separation", minimumSeparation, 0f, 10f);
            heightOffset = EditorGUILayout.FloatField("Height Offset", heightOffset);
            randomYaw = EditorGUILayout.Toggle("Random Yaw", randomYaw);
            if (!randomYaw)
                yaw = EditorGUILayout.Slider("Yaw", yaw, 0f, 360f);

            EditorGUILayout.HelpBox(
                "In the Scene view, hold and drag the left mouse button to paint. Select Erase, or hold Ctrl/Cmd while painting, to remove zombies quickly.",
                MessageType.None);
        }

        private void RefreshArchetypes()
        {
            archetypes = AssetDatabase.FindAssets("t:ZombieArchetype", new[] { ArchetypeFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ZombieArchetype>)
                .Where(profile => profile != null)
                .OrderBy(profile => profile.SortOrder)
                .ThenBy(profile => profile.DisplayName)
                .ToList();
            selectedArchetypeIndex = Mathf.Clamp(selectedArchetypeIndex, 0, Mathf.Max(0, archetypes.Count - 1));
            Repaint();
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            Event current = Event.current;
            if (!TryGetPlacementPoint(current.mousePosition, out Vector3 point, out Vector3 normal))
                return;

            bool temporaryErase = current.control || current.command;
            bool erase = brushMode == BrushMode.Erase || temporaryErase;
            DrawBrushPreview(point, normal, erase);

            if (current.alt)
                return;

            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (current.type == EventType.Layout)
                HandleUtility.AddDefaultControl(controlId);

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                BeginStroke(erase);
                Stamp(point, normal, erase);
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && current.button == 0 && painting)
            {
                if (!hasLastStamp || Vector3.Distance(lastStampPosition, point) >= strokeSpacing)
                    Stamp(point, normal, erase);
                current.Use();
            }
            else if ((current.type == EventType.MouseUp || current.rawType == EventType.MouseUp) && current.button == 0)
            {
                EndStroke();
                current.Use();
            }
        }

        private void BeginStroke(bool erase)
        {
            painting = true;
            hasLastStamp = false;
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(erase ? "Erase Zombies" : "Paint Zombies");
        }

        private void EndStroke()
        {
            if (!painting)
                return;
            painting = false;
            hasLastStamp = false;
            if (undoGroup >= 0)
                Undo.CollapseUndoOperations(undoGroup);
            undoGroup = -1;
        }

        private void Stamp(Vector3 center, Vector3 surfaceNormal, bool erase)
        {
            if (erase)
                EraseStamp(center);
            else
                PaintStamp(center, surfaceNormal);

            lastStampPosition = center;
            hasLastStamp = true;
            SceneView.RepaintAll();
        }

        private void PaintStamp(Vector3 center, Vector3 surfaceNormal)
        {
            ZombieArchetype selected = SelectedArchetype;
            if (selected == null || selected.Prefab == null)
                return;

            for (int i = 0; i < zombiesPerStamp; i++)
            {
                Vector2 offset2D = UnityEngine.Random.insideUnitCircle * brushRadius;
                Vector3 candidate = center + new Vector3(offset2D.x, 0f, offset2D.y);
                if (!TryProjectToSurface(candidate, surfaceNormal, out Vector3 position, out Vector3 normal))
                    continue;
                position += normal * heightOffset;

                if (!HasMinimumSeparation(position))
                    continue;

                PlaceZombie(selected.Prefab, position, normal);
            }
        }

        private void PlaceZombie(GameObject prefab, Vector3 position, Vector3 normal)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Paint Zombie");
            Transform targetParent = ResolveParent();
            if (targetParent != null)
                Undo.SetTransformParent(instance.transform, targetParent, "Parent Painted Zombie");

            float selectedYaw = randomYaw ? UnityEngine.Random.Range(0f, 360f) : yaw;
            instance.transform.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, normal) * Quaternion.Euler(0f, selectedYaw, 0f));
        }

        private Transform ResolveParent()
        {
            if (parent != null)
                return parent;

            GameObject existing = GameObject.Find("Painted Zombies");
            if (existing == null)
            {
                existing = new GameObject("Painted Zombies");
                Undo.RegisterCreatedObjectUndo(existing, "Create Painted Zombies Parent");
            }
            parent = existing.transform;
            return parent;
        }

        private void EraseStamp(Vector3 center)
        {
            Collider[] overlaps = Physics.OverlapSphere(center, brushRadius, ~0, QueryTriggerInteraction.Collide);
            HashSet<GameObject> roots = new HashSet<GameObject>();
            foreach (Collider overlap in overlaps)
            {
                ZombieHealth zombie = overlap.GetComponentInParent<ZombieHealth>();
                if (zombie != null)
                    roots.Add(zombie.gameObject);
            }

            foreach (GameObject zombie in roots)
                Undo.DestroyObjectImmediate(zombie);
        }

        private bool HasMinimumSeparation(Vector3 position)
        {
            if (minimumSeparation <= 0f)
                return true;

            Collider[] overlaps = Physics.OverlapSphere(position, minimumSeparation, ~0, QueryTriggerInteraction.Collide);
            return overlaps.All(overlap => overlap.GetComponentInParent<ZombieHealth>() == null);
        }

        private bool TryGetPlacementPoint(Vector2 mousePosition, out Vector3 point, out Vector3 normal)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            return TryRaycastIgnoringZombies(ray, Mathf.Infinity, out point, out normal);
        }

        private bool TryProjectToSurface(Vector3 candidate, Vector3 fallbackNormal, out Vector3 point, out Vector3 normal)
        {
            Ray ray = new Ray(candidate + Vector3.up * 100f, Vector3.down);
            if (TryRaycastIgnoringZombies(ray, 200f, out point, out normal))
                return true;

            point = candidate;
            normal = fallbackNormal;
            return false;
        }

        private bool TryRaycastIgnoringZombies(Ray ray, float distance, out Vector3 point, out Vector3 normal)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, placementLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<ZombieHealth>() != null)
                    continue;
                point = hit.point;
                normal = hit.normal;
                return true;
            }

            point = default;
            normal = Vector3.up;
            return false;
        }

        private void DrawBrushPreview(Vector3 point, Vector3 normal, bool erase)
        {
            Color color = erase ? new Color(1f, 0.2f, 0.15f, 0.9f) : new Color(0.2f, 1f, 0.35f, 0.9f);
            Handles.color = color;
            Handles.DrawWireDisc(point, normal, brushRadius);
            Handles.color = new Color(color.r, color.g, color.b, 0.08f);
            Handles.DrawSolidDisc(point, normal, brushRadius);
        }

        private static LayerMask LayerMaskField(string label, LayerMask selected)
        {
            string[] layerNames = UnityEditorInternal.InternalEditorUtility.layers;
            int maskWithoutEmpty = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                int layer = LayerMask.NameToLayer(layerNames[i]);
                if ((selected.value & (1 << layer)) != 0)
                    maskWithoutEmpty |= 1 << i;
            }

            maskWithoutEmpty = EditorGUILayout.MaskField(label, maskWithoutEmpty, layerNames);
            int mask = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                if ((maskWithoutEmpty & (1 << i)) != 0)
                    mask |= 1 << LayerMask.NameToLayer(layerNames[i]);
            }
            selected.value = mask;
            return selected;
        }
    }
}
