using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace EnvironmentInteraction.Authoring.Editor
{
    internal static class EnvironmentalInteractionSceneHandles
    {
        private static readonly Color DropColor = new Color(0.95f, 0.6f, 0.18f, 0.95f);
        private static readonly Color PushColor = new Color(0.2f, 0.8f, 0.68f, 0.95f);
        private static readonly Color ShockColor = new Color(0.2f, 0.65f, 1f, 0.95f);
        private static readonly Color ExplodeColor = new Color(1f, 0.32f, 0.16f, 0.95f);

        public static void DrawSelected(EnvironmentalInteractionBase interaction)
        {
            Color color = GetColor(interaction.Type);
            Handles.zTest = CompareFunction.LessEqual;
            DrawTrigger(interaction, color);

            if (interaction is DropInteraction drop)
                DrawDrop(drop, color);
            else if (interaction is PushInteraction push)
                DrawPush(push, color);
            else if (interaction is ShockInteraction shock)
                DrawShock(shock, color);
            else if (interaction is ExplodeInteraction explode)
                DrawExplode(explode, color);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected | GizmoType.Pickable)]
        private static void DrawInteractionGizmo(
            EnvironmentalInteractionBase interaction,
            GizmoType gizmoType)
        {
            if (interaction == null || !interaction.ShowSceneGizmos)
                return;

            Color color = GetColor(interaction.Type);
            color.a = 0.65f;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(interaction.transform.position, 0.22f);

            EnvironmentalTrigger trigger = interaction.Trigger;
            if ((gizmoType & GizmoType.Selected) == 0 && trigger != null)
                Gizmos.DrawLine(interaction.transform.position, trigger.TriggerTransform.position);
        }

        private static void DrawTrigger(EnvironmentalInteractionBase interaction, Color color)
        {
            EnvironmentalTrigger trigger = interaction.Trigger;
            if (trigger == null)
                return;

            Vector3 position = trigger.TriggerTransform.position;
            float size = HandleUtility.GetHandleSize(position) * 0.08f;
            Handles.color = Color.Lerp(color, Color.white, 0.35f);
            Handles.SphereHandleCap(0, position, Quaternion.identity, size, EventType.Repaint);
            if (interaction.ShowLabels)
                Handles.Label(position + Vector3.up * size * 1.5f, "TRIGGER — SHOOT HERE", EditorStyles.boldLabel);
        }

        private static void DrawDrop(DropInteraction interaction, Color color)
        {
            Vector3 start = interaction.DropStartPosition;
            bool hasFloor = interaction.TryResolveImpactPosition(out Vector3 impact);
            if (!hasFloor)
                impact = start;
            DrawArrow(start, impact, color);
            DrawConnection(interaction, interaction.DropObject, color);
            DrawArea(
                impact,
                interaction.transform.rotation,
                interaction.ImpactShape,
                interaction.ImpactRadius,
                interaction.ImpactBoxSize,
                color,
                GetPreviewPulse(interaction));

            if (interaction.ShowLabels)
                Handles.Label(
                    impact + Vector3.up * 0.25f,
                    hasFloor ? "DROP — AUTO FLOOR SMASH" : "DROP — NO FLOOR BELOW",
                    EditorStyles.boldLabel);

            if (interaction.ImpactShape == EnvironmentalAreaShape.Sphere)
            {
                EditorGUI.BeginChangeCheck();
                float radius = Handles.RadiusHandle(
                    interaction.transform.rotation,
                    impact,
                    interaction.ImpactRadius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(interaction, "Resize Drop Impact Radius");
                    interaction.SetImpactRadius(radius);
                    EditorUtility.SetDirty(interaction);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Vector3 size = Handles.ScaleHandle(
                    interaction.ImpactBoxSize,
                    impact,
                    interaction.transform.rotation,
                    HandleUtility.GetHandleSize(impact));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(interaction, "Resize Drop Impact Box");
                    interaction.SetImpactBoxSize(size);
                    EditorUtility.SetDirty(interaction);
                }
            }

            if (EnvironmentalInteractionPreview.TryGetProgress(interaction, out float progress))
            {
                float travel = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.72f));
                Vector3 previewPosition = Vector3.Lerp(start, impact, travel);
                Handles.color = new Color(color.r, color.g, color.b, 0.85f);
                Handles.CubeHandleCap(
                    0,
                    previewPosition,
                    interaction.transform.rotation,
                    HandleUtility.GetHandleSize(previewPosition) * 0.24f,
                    EventType.Repaint);
            }
        }

        private static void DrawPush(PushInteraction interaction, Color color)
        {
            Vector3 origin = interaction.OriginPosition;
            Vector3 direction = interaction.WorldPushDirection.normalized;
            Vector3 up = interaction.transform.up;
            Quaternion rotation = Quaternion.LookRotation(direction, up);
            Vector3 center = origin + direction * (interaction.PushRange * 0.5f);
            Vector3 size = new Vector3(
                interaction.PushWidth,
                interaction.PushHeight,
                interaction.PushRange);

            DrawWireBox(center, rotation, size, color);
            DrawArrow(origin, origin + direction * interaction.PushRange, color);
            DrawConnection(interaction, interaction.PushOrigin, color);
            if (interaction.ShowLabels)
                Handles.Label(center + up * (interaction.PushHeight * 0.55f), "PUSH — AFFECTED VOLUME", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Quaternion newRotation = Handles.RotationHandle(rotation, origin);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(interaction, "Rotate Push Direction");
                interaction.SetWorldPushDirection(newRotation * Vector3.forward);
                EditorUtility.SetDirty(interaction);
            }

            float rangeHandleSize = HandleUtility.GetHandleSize(origin + direction * interaction.PushRange) * 0.1f;
            EditorGUI.BeginChangeCheck();
            Vector3 newRangePoint = Handles.Slider(
                origin + direction * interaction.PushRange,
                direction,
                rangeHandleSize,
                Handles.CubeHandleCap,
                0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(interaction, "Resize Push Range");
                interaction.SetPushRange(Vector3.Dot(newRangePoint - origin, direction));
                EditorUtility.SetDirty(interaction);
            }

            Vector3 right = Vector3.Cross(up, direction).normalized;
            if (right.sqrMagnitude < 0.0001f)
                right = interaction.transform.right;
            Vector3 widthPoint = center + right * (interaction.PushWidth * 0.5f);
            EditorGUI.BeginChangeCheck();
            Vector3 newWidthPoint = Handles.Slider(
                widthPoint,
                right,
                HandleUtility.GetHandleSize(widthPoint) * 0.1f,
                Handles.CubeHandleCap,
                0f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(interaction, "Resize Push Width");
                interaction.SetPushWidth(Vector3.Dot(newWidthPoint - center, right) * 2f);
                EditorUtility.SetDirty(interaction);
            }

            if (EnvironmentalInteractionPreview.TryGetProgress(interaction, out float progress))
            {
                Handles.color = new Color(color.r, color.g, color.b, 0.9f);
                for (int index = 0; index < 5; index++)
                {
                    float lane = Mathf.Lerp(-0.4f, 0.4f, index / 4f);
                    float distance = Mathf.Repeat(progress * 1.8f + index * 0.17f, 1f) * interaction.PushRange;
                    Vector3 point = origin + direction * distance + right * lane * interaction.PushWidth;
                    Handles.SphereHandleCap(
                        0,
                        point,
                        Quaternion.identity,
                        HandleUtility.GetHandleSize(point) * 0.06f,
                        EventType.Repaint);
                }
            }
        }

        private static void DrawShock(ShockInteraction interaction, Color color)
        {
            Vector3 source = interaction.ElectricalSource != null
                ? interaction.ElectricalSource.position
                : interaction.transform.position;
            Vector3 center = interaction.AreaPosition;
            DrawArrow(source, center, color);
            DrawConnection(interaction, interaction.ElectricalSource, color);
            DrawArea(
                center,
                interaction.AreaRotation,
                interaction.ShockAreaShape,
                interaction.Radius,
                interaction.BoxSize,
                color,
                GetPreviewPulse(interaction));
            DrawShockBolts(center, interaction, color);

            if (interaction.ShowLabels)
                Handles.Label(center + Vector3.up * 0.25f, "SHOCK — CONDUCTIVE AREA", EditorStyles.boldLabel);

            if (interaction.ShockAreaShape == EnvironmentalAreaShape.Sphere)
            {
                EditorGUI.BeginChangeCheck();
                float radius = Handles.RadiusHandle(interaction.AreaRotation, center, interaction.Radius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(interaction, "Resize Shock Radius");
                    interaction.SetRadius(radius);
                    EditorUtility.SetDirty(interaction);
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                Vector3 size = Handles.ScaleHandle(
                    interaction.BoxSize,
                    center,
                    interaction.AreaRotation,
                    HandleUtility.GetHandleSize(center));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(interaction, "Resize Shock Area");
                    interaction.SetBoxSize(size);
                    EditorUtility.SetDirty(interaction);
                }
            }
        }

        private static void DrawExplode(ExplodeInteraction interaction, Color color)
        {
            Vector3 center = interaction.OriginPosition;
            DrawConnection(interaction, interaction.ExplosionOrigin, color);
            DrawSphere(center, interaction.OuterRadius, color, GetPreviewPulse(interaction));
            if (interaction.InnerRadius > 0.001f)
            {
                Color innerColor = Color.Lerp(color, Color.white, 0.35f);
                DrawSphere(center, interaction.InnerRadius, innerColor, 0f);
            }

            if (interaction.ShowLabels)
                Handles.Label(center + Vector3.up * (interaction.OuterRadius + 0.2f), "EXPLODE — OUTER RADIUS", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float outerRadius = Handles.RadiusHandle(Quaternion.identity, center, interaction.OuterRadius);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(interaction, "Resize Explosion Radius");
                interaction.SetOuterRadius(outerRadius);
                EditorUtility.SetDirty(interaction);
            }

            if (interaction.InnerRadius > 0.001f)
            {
                EditorGUI.BeginChangeCheck();
                float innerRadius = Handles.RadiusHandle(Quaternion.identity, center, interaction.InnerRadius);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(interaction, "Resize Inner Explosion Radius");
                    interaction.SetInnerRadius(innerRadius);
                    EditorUtility.SetDirty(interaction);
                }
            }
        }

        private static void DrawConnection(
            EnvironmentalInteractionBase interaction,
            Transform destination,
            Color color)
        {
            if (interaction.Trigger == null || destination == null)
                return;
            Handles.color = new Color(color.r, color.g, color.b, 0.8f);
            Handles.DrawDottedLine(
                interaction.Trigger.TriggerTransform.position,
                destination.position,
                5f);
        }

        private static void DrawArrow(Vector3 start, Vector3 end, Color color)
        {
            Vector3 direction = end - start;
            float distance = direction.magnitude;
            if (distance < 0.001f)
                return;

            direction /= distance;
            Handles.color = color;
            Handles.DrawAAPolyLine(3f, start, end);
            Handles.ConeHandleCap(
                0,
                end,
                Quaternion.LookRotation(direction),
                Mathf.Min(0.35f, distance * 0.18f),
                EventType.Repaint);
        }

        private static void DrawArea(
            Vector3 center,
            Quaternion rotation,
            EnvironmentalAreaShape shape,
            float radius,
            Vector3 boxSize,
            Color color,
            float previewPulse)
        {
            if (shape == EnvironmentalAreaShape.Sphere)
                DrawSphere(center, radius, color, previewPulse);
            else
            {
                DrawWireBox(center, rotation, boxSize, color);
                if (previewPulse > 0f)
                {
                    Color fill = color;
                    fill.a = 0.03f + previewPulse * 0.12f;
                    using (new Handles.DrawingScope(fill, Matrix4x4.TRS(center, rotation, Vector3.one)))
                        Handles.DrawSolidRectangleWithOutline(GetBoxFace(boxSize), fill, color);
                }
            }
        }

        private static void DrawSphere(Vector3 center, float radius, Color color, float previewPulse)
        {
            Handles.color = color;
            Handles.DrawWireDisc(center, Vector3.up, radius);
            Handles.DrawWireDisc(center, Vector3.right, radius);
            Handles.DrawWireDisc(center, Vector3.forward, radius);
            if (previewPulse <= 0f)
                return;

            Color fill = color;
            fill.a = 0.03f + previewPulse * 0.14f;
            Handles.color = fill;
            Handles.DrawSolidDisc(center, Vector3.up, radius);
        }

        private static void DrawWireBox(Vector3 center, Quaternion rotation, Vector3 size, Color color)
        {
            using (new Handles.DrawingScope(color, Matrix4x4.TRS(center, rotation, Vector3.one)))
                Handles.DrawWireCube(Vector3.zero, size);
        }

        private static Vector3[] GetBoxFace(Vector3 size)
        {
            Vector3 half = size * 0.5f;
            return new[]
            {
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, half.z),
                new Vector3(-half.x, -half.y, half.z)
            };
        }

        private static void DrawShockBolts(Vector3 center, ShockInteraction interaction, Color color)
        {
            float extent = interaction.ShockAreaShape == EnvironmentalAreaShape.Sphere
                ? interaction.Radius
                : Mathf.Max(interaction.BoxSize.x, interaction.BoxSize.z) * 0.5f;
            float tick = (float)EditorApplication.timeSinceStartup;
            Handles.color = Color.Lerp(color, Color.white, 0.35f);
            for (int index = 0; index < 3; index++)
            {
                float phase = tick * 0.7f + index * 2.1f;
                Vector3 offset = new Vector3(Mathf.Sin(phase), 0.08f, Mathf.Cos(phase)) * extent * 0.45f;
                Vector3 start = center + offset + Vector3.up * 0.35f;
                Vector3 middle = center + offset * 0.65f + Vector3.up * 0.15f;
                Vector3 end = center + offset * 0.85f;
                Handles.DrawAAPolyLine(2f, start, middle, end);
            }
        }

        private static float GetPreviewPulse(EnvironmentalInteractionBase interaction)
        {
            if (!EnvironmentalInteractionPreview.TryGetProgress(interaction, out float progress))
                return 0f;
            return Mathf.Sin(progress * Mathf.PI);
        }

        private static Color GetColor(EnvironmentalInteractionType type)
        {
            switch (type)
            {
                case EnvironmentalInteractionType.Drop:
                    return DropColor;
                case EnvironmentalInteractionType.Push:
                    return PushColor;
                case EnvironmentalInteractionType.Shock:
                    return ShockColor;
                default:
                    return ExplodeColor;
            }
        }
    }
}
