using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EnvironmentInteraction.Editor
{
    public sealed class EnvironmentInteractionPainterWindow : EditorWindow
    {
        private enum BrushMode
        {
            Paint,
            Erase
        }

        private const string CatalogFolder = "Assets/EnvironmentInteraction/Catalog";
        private const int Columns = 3;
        private const float TileHeight = 64f;

        [SerializeField] private List<EnvironmentInteractionCatalogItem> catalog = new List<EnvironmentInteractionCatalogItem>();
        [SerializeField] private int selectedIndex;
        [SerializeField] private Transform parent;
        [SerializeField] private LayerMask placementLayers = ~0;
        [SerializeField] private BrushMode brushMode;
        [SerializeField, Min(0.1f)] private float brushRadius = 1.5f;
        [SerializeField, Min(0.05f)] private float strokeSpacing = 1.25f;
        [SerializeField, Min(0f)] private float minimumSeparation = 1f;
        [SerializeField] private float heightOffset;
        [SerializeField] private bool randomYaw = true;
        [SerializeField, Range(0f, 360f)] private float yaw;

        private bool painting;
        private bool hasLastStamp;
        private Vector3 lastStampPosition;
        private int undoGroup = -1;
        private Vector2 scrollPosition;

        private EnvironmentInteractionCatalogItem SelectedItem
        {
            get
            {
                if (catalog == null || catalog.Count == 0)
                    return null;
                selectedIndex = Mathf.Clamp(selectedIndex, 0, catalog.Count - 1);
                return catalog[selectedIndex];
            }
        }

        [MenuItem("Tools/Zombie Prototype/Environment Interaction Painter")]
        [MenuItem("Tools/Environment Interaction/Painter")]
        private static void OpenWindow()
        {
            EnvironmentInteractionPainterWindow window = GetWindow<EnvironmentInteractionPainterWindow>("Environment Interaction");
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshCatalog();
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
            DrawCatalog();
            EditorGUILayout.Space(12f);
            DrawBrushSettings();
            EditorGUILayout.EndScrollView();
        }

        private void DrawCatalog()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Environment Interaction", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh Catalog", GUILayout.Width(120f)))
                RefreshCatalog();
            EditorGUILayout.EndHorizontal();

            if (catalog.Count == 0)
            {
                EditorGUILayout.HelpBox("No catalog items found in " + CatalogFolder + ".", MessageType.Warning);
                return;
            }

            for (int start = 0; start < catalog.Count; start += Columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int column = 0; column < Columns; column++)
                {
                    int index = start + column;
                    if (index >= catalog.Count)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }

                    EnvironmentInteractionCatalogItem item = catalog[index];
                    Color oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = index == selectedIndex
                        ? Color.Lerp(item.EditorColor, Color.white, 0.25f)
                        : Color.Lerp(item.EditorColor, Color.gray, 0.45f);

                    if (GUILayout.Button(item.DisplayName, GUILayout.Height(TileHeight)))
                    {
                        selectedIndex = index;
                        Repaint();
                        SceneView.RepaintAll();
                    }
                    GUI.backgroundColor = oldColor;
                }
                EditorGUILayout.EndHorizontal();
            }

            EnvironmentInteractionCatalogItem selected = SelectedItem;
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Selected Prefab", selected != null ? selected.Prefab : null, typeof(GameObject), false);
        }

        private void DrawBrushSettings()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            brushMode = (BrushMode)GUILayout.Toolbar((int)brushMode, new[] { "Paint", "Erase" });
            parent = (Transform)EditorGUILayout.ObjectField("Parent", parent, typeof(Transform), true);
            placementLayers = LayerMaskField("Placement Layers", placementLayers);
            brushRadius = EditorGUILayout.Slider("Radius", brushRadius, 0.1f, 20f);
            strokeSpacing = EditorGUILayout.Slider("Stroke Spacing", strokeSpacing, 0.05f, 10f);
            minimumSeparation = EditorGUILayout.Slider("Minimum Separation", minimumSeparation, 0f, 10f);
            heightOffset = EditorGUILayout.FloatField("Height Offset", heightOffset);
            randomYaw = EditorGUILayout.Toggle("Random Yaw", randomYaw);
            if (!randomYaw)
                yaw = EditorGUILayout.Slider("Yaw", yaw, 0f, 360f);

            EditorGUILayout.HelpBox(
                "Paint in the Scene view with left click/drag. Use Erase, or hold Ctrl/Cmd, to remove marked environment interactions. Undo is grouped per stroke.",
                MessageType.None);
        }

        private void RefreshCatalog()
        {
            catalog = AssetDatabase.FindAssets("t:EnvironmentInteractionCatalogItem", new[] { CatalogFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<EnvironmentInteractionCatalogItem>)
                .Where(item => item != null)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.DisplayName)
                .ToList();
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, catalog.Count - 1));
            Repaint();
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            Event current = Event.current;
            if (!TryGetPlacementPoint(current.mousePosition, out Vector3 point, out Vector3 normal))
                return;

            bool erase = brushMode == BrushMode.Erase || current.control || current.command;
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
            Undo.SetCurrentGroupName(erase ? "Erase Environment Interaction" : "Paint Environment Interaction");
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

        private void Stamp(Vector3 point, Vector3 normal, bool erase)
        {
            if (erase)
                Erase(point);
            else
                Paint(point, normal);
            lastStampPosition = point;
            hasLastStamp = true;
            SceneView.RepaintAll();
        }

        private void Paint(Vector3 point, Vector3 normal)
        {
            EnvironmentInteractionCatalogItem item = SelectedItem;
            if (item == null || item.Prefab == null)
                return;

            Vector2 offset = UnityEngine.Random.insideUnitCircle * brushRadius;
            Vector3 candidate = point + new Vector3(offset.x, 0f, offset.y);
            if (!TryProjectToSurface(candidate, normal, out Vector3 position, out Vector3 surfaceNormal))
                return;
            position += surfaceNormal * heightOffset;

            if (!HasMinimumSeparation(position))
                return;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(item.Prefab);
            if (instance == null)
                return;

            Undo.RegisterCreatedObjectUndo(instance, "Paint Environment Interaction");
            Transform targetParent = ResolveParent();
            if (targetParent != null)
                Undo.SetTransformParent(instance.transform, targetParent, "Parent Environment Interaction");

            float selectedYaw = randomYaw ? UnityEngine.Random.Range(0f, 360f) : yaw;
            instance.transform.SetPositionAndRotation(
                position,
                Quaternion.FromToRotation(Vector3.up, surfaceNormal) * Quaternion.Euler(0f, selectedYaw, 0f));
        }

        private void Erase(Vector3 point)
        {
            Collider[] overlaps = Physics.OverlapSphere(point, brushRadius, ~0, QueryTriggerInteraction.Collide);
            HashSet<GameObject> roots = new HashSet<GameObject>();
            foreach (Collider overlap in overlaps)
            {
                EnvironmentInteractionMarker marker = overlap.GetComponentInParent<EnvironmentInteractionMarker>();
                if (marker != null)
                    roots.Add(marker.gameObject);
            }

            foreach (GameObject root in roots)
                Undo.DestroyObjectImmediate(root);
        }

        private bool HasMinimumSeparation(Vector3 position)
        {
            if (minimumSeparation <= 0f)
                return true;

            Collider[] overlaps = Physics.OverlapSphere(position, minimumSeparation, ~0, QueryTriggerInteraction.Collide);
            return overlaps.All(overlap => overlap.GetComponentInParent<EnvironmentInteractionMarker>() == null);
        }

        private Transform ResolveParent()
        {
            if (parent != null)
                return parent;

            GameObject existing = GameObject.Find("Painted Environment Interactions");
            if (existing == null)
            {
                existing = new GameObject("Painted Environment Interactions");
                Undo.RegisterCreatedObjectUndo(existing, "Create Environment Interaction Parent");
            }
            parent = existing.transform;
            return parent;
        }

        private bool TryGetPlacementPoint(Vector2 mousePosition, out Vector3 point, out Vector3 normal)
        {
            return TryRaycastIgnoringInteractions(
                HandleUtility.GUIPointToWorldRay(mousePosition),
                Mathf.Infinity,
                out point,
                out normal);
        }

        private bool TryProjectToSurface(Vector3 candidate, Vector3 fallbackNormal, out Vector3 point, out Vector3 normal)
        {
            if (TryRaycastIgnoringInteractions(
                    new Ray(candidate + Vector3.up * 100f, Vector3.down),
                    200f,
                    out point,
                    out normal))
                return true;

            point = candidate;
            normal = fallbackNormal;
            return false;
        }

        private bool TryRaycastIgnoringInteractions(Ray ray, float distance, out Vector3 point, out Vector3 normal)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, distance, placementLayers, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.GetComponentInParent<EnvironmentInteractionMarker>() != null)
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
            Color color = erase ? new Color(1f, 0.2f, 0.15f, 0.9f) : new Color(1f, 0.55f, 0.08f, 0.9f);
            Handles.color = color;
            Handles.DrawWireDisc(point, normal, brushRadius);
            Handles.color = new Color(color.r, color.g, color.b, 0.08f);
            Handles.DrawSolidDisc(point, normal, brushRadius);
        }

        private static LayerMask LayerMaskField(string label, LayerMask selected)
        {
            string[] layerNames = UnityEditorInternal.InternalEditorUtility.layers;
            int compactMask = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                int layer = LayerMask.NameToLayer(layerNames[i]);
                if ((selected.value & (1 << layer)) != 0)
                    compactMask |= 1 << i;
            }

            compactMask = EditorGUILayout.MaskField(label, compactMask, layerNames);
            int mask = 0;
            for (int i = 0; i < layerNames.Length; i++)
            {
                if ((compactMask & (1 << i)) != 0)
                    mask |= 1 << LayerMask.NameToLayer(layerNames[i]);
            }
            selected.value = mask;
            return selected;
        }
    }
}
