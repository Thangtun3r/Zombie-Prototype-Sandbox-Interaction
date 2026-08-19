using System.Collections;
using UnityEngine;

namespace EnvironmentInteraction.Authoring
{
    public sealed class PushInteraction : EnvironmentalInteractionBase
    {
        [SerializeField] private Transform pushOrigin;
        [SerializeField] private Transform effectZone;
        [SerializeField] private Vector3 pushDirection = Vector3.forward;
        [SerializeField, Min(0.05f)] private float pushRange = 6f;
        [SerializeField, Min(0.05f)] private float pushWidth = 3f;
        [SerializeField, Min(0.05f)] private float pushHeight = 2f;
        [SerializeField, Min(0f)] private float forceValue = 18f;
        [SerializeField, Min(0f)] private float duration = 1.25f;
        [SerializeField] private bool spawnWaterParticles = true;
        [SerializeField] private Color waterColor = new Color(0.08f, 0.48f, 1f, 0.9f);
        [SerializeField, Min(0.1f)] private float waterParticleAmount = 1f;
        [SerializeField] private LayerMask affectedLayers = ~0;

        public override EnvironmentalInteractionType Type => EnvironmentalInteractionType.Push;
        public Transform PushOrigin => pushOrigin;
        public Transform EffectZone => effectZone;
        public Vector3 PushDirection => pushDirection;
        public float PushRange => pushRange;
        public float PushWidth => pushWidth;
        public float PushHeight => pushHeight;
        public float ForceValue => forceValue;
        public float Duration => duration;
        public bool SpawnWaterParticles => spawnWaterParticles;
        public Color WaterColor => waterColor;
        public float WaterParticleAmount => waterParticleAmount;
        public Vector3 OriginPosition => pushOrigin != null ? pushOrigin.position : transform.position;
        public Vector3 WorldPushDirection => DirectionSpace.TransformDirection(NormalizedPushDirection);

        private Transform DirectionSpace => pushOrigin != null ? pushOrigin : transform;
        private Vector3 NormalizedPushDirection => pushDirection.sqrMagnitude > 0.0001f
            ? pushDirection.normalized
            : Vector3.forward;

        protected override void OnValidate()
        {
            base.OnValidate();
            pushRange = Mathf.Max(0.05f, pushRange);
            pushWidth = Mathf.Max(0.05f, pushWidth);
            pushHeight = Mathf.Max(0.05f, pushHeight);
            forceValue = Mathf.Max(0f, forceValue);
            duration = Mathf.Max(0f, duration);
            waterParticleAmount = Mathf.Max(0.1f, waterParticleAmount);
            if (pushDirection.sqrMagnitude < 0.0001f)
                pushDirection = Vector3.forward;
            SynchronizeEffectZone();
        }

        public void Configure(Transform configuredOrigin, Transform configuredEffectZone)
        {
            pushOrigin = configuredOrigin;
            effectZone = configuredEffectZone;
            SynchronizeEffectZone();
        }

        public void SetWorldPushDirection(Vector3 worldDirection)
        {
            if (worldDirection.sqrMagnitude < 0.0001f)
                return;
            pushDirection = DirectionSpace.InverseTransformDirection(worldDirection.normalized);
            SynchronizeEffectZone();
        }

        public void SetPushRange(float range)
        {
            pushRange = Mathf.Max(0.05f, range);
            SynchronizeEffectZone();
        }

        public void SetPushWidth(float width)
        {
            pushWidth = Mathf.Max(0.05f, width);
            SynchronizeEffectZone();
        }

        public void SynchronizeEffectZone()
        {
            if (effectZone == null)
                return;

            Vector3 direction = WorldPushDirection;
            effectZone.position = OriginPosition + direction * (pushRange * 0.5f);
            effectZone.rotation = Quaternion.LookRotation(direction, transform.up);

            EnvironmentalAreaVisual areaVisual = effectZone.GetComponent<EnvironmentalAreaVisual>();
            if (areaVisual != null)
                areaVisual.Synchronize(
                    EnvironmentalAreaShape.Box,
                    new Vector3(pushWidth, pushHeight, pushRange),
                    pushRange * 0.5f,
                    PreviewColor(waterColor));
        }

        private static Color PreviewColor(Color color)
        {
            color.a = Mathf.Min(color.a, 0.14f);
            return color;
        }

        protected override void ActivateEffect()
        {
            StartCoroutine(PushRoutine());
        }

        private IEnumerator PushRoutine()
        {
            Vector3 direction = WorldPushDirection.normalized;
            Quaternion rotation = Quaternion.LookRotation(direction, transform.up);
            Vector3 center = OriginPosition + direction * (pushRange * 0.5f);
            if (spawnWaterParticles)
            {
                EnvironmentalRuntimeEffects.SpawnWaterJet(
                    OriginPosition,
                    direction,
                    pushRange,
                    pushWidth,
                    Mathf.Max(0.1f, duration),
                    waterColor,
                    waterParticleAmount);
            }

            float pushDuration = Mathf.Max(0.02f, duration);
            float endTime = Time.time + pushDuration;
            do
            {
                EnvironmentalRuntimeEffects.ApplyDirectionalPush(
                    center,
                    rotation,
                    new Vector3(pushWidth, pushHeight, pushRange),
                    direction,
                    forceValue,
                    affectedLayers);
                yield return new WaitForFixedUpdate();
            }
            while (Time.time < endTime);

            NotifyEffectCompleted();
        }
    }
}
