using System.Collections;
using UnityEngine;

namespace EnvironmentInteraction.Authoring
{
    public sealed class ShockInteraction : EnvironmentalInteractionBase
    {
        [SerializeField] private Transform electricalSource;
        [SerializeField] private Transform shockArea;
        [SerializeField] private EnvironmentalAreaShape shockAreaShape = EnvironmentalAreaShape.Box;
        [SerializeField, Min(0.05f)] private float radius = 3f;
        [SerializeField] private Vector3 boxSize = new Vector3(5f, 0.5f, 4f);
        [SerializeField, Min(0f)] private float duration = 2f;
        [SerializeField, Min(0f)] private float delay;
        [SerializeField] private Transform conductiveSurface;
        [SerializeField] private Renderer conductiveSurfaceRenderer;
        [SerializeField] private Color conductiveSurfaceColor = new Color(0.05f, 0.35f, 0.9f, 1f);
        [SerializeField] private Renderer wireRenderer;
        [SerializeField] private Color wireColor = new Color(0.85f, 0.04f, 0.02f, 1f);
        [SerializeField] private Color shockEffectColor = new Color(0.15f, 0.65f, 1f, 0.22f);
        [SerializeField] private bool spawnActiveZoneParticles = true;
        [SerializeField] private Color activeZoneParticleColor = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField, Min(0.1f)] private float activeZoneParticleAmount = 1f;
        [SerializeField] private bool spawnTazeParticles = true;
        [SerializeField] private Color tazeParticleColor = new Color(0.25f, 0.75f, 1f, 1f);
        [SerializeField, Min(0.1f)] private float tazeParticleAmount = 1f;
        [SerializeField, Min(0f)] private float tazeShakeStrength = 0.045f;
        [SerializeField, Min(0f)] private float tazeShakeSpeed = 28f;
        [SerializeField, Min(0f)] private float damagePerPulse = 8f;
        [SerializeField, Min(0.05f)] private float pulseInterval = 0.35f;
        [SerializeField, Range(0f, 1f)] private float slowMultiplier;
        [SerializeField] private LayerMask affectedLayers = ~0;

        public override EnvironmentalInteractionType Type => EnvironmentalInteractionType.Shock;
        public Transform ElectricalSource => electricalSource;
        public Transform ShockArea => shockArea;
        public EnvironmentalAreaShape ShockAreaShape => shockAreaShape;
        public float Radius => radius;
        public Vector3 BoxSize => boxSize;
        public float Duration => duration;
        public float Delay => delay;
        public Transform ConductiveSurface => conductiveSurface;
        public Renderer ConductiveSurfaceRenderer => conductiveSurfaceRenderer;
        public Color ConductiveSurfaceColor => conductiveSurfaceColor;
        public Renderer WireRenderer => wireRenderer;
        public Color WireColor => wireColor;
        public Color ShockEffectColor => shockEffectColor;
        public bool SpawnActiveZoneParticles => spawnActiveZoneParticles;
        public Color ActiveZoneParticleColor => activeZoneParticleColor;
        public float ActiveZoneParticleAmount => activeZoneParticleAmount;
        public bool SpawnTazeParticles => spawnTazeParticles;
        public Color TazeParticleColor => tazeParticleColor;
        public float TazeParticleAmount => tazeParticleAmount;
        public float TazeShakeStrength => tazeShakeStrength;
        public float TazeShakeSpeed => tazeShakeSpeed;
        public float DamagePerPulse => damagePerPulse;
        public float PulseInterval => pulseInterval;
        public float SlowMultiplier => slowMultiplier;
        public Vector3 AreaPosition => shockArea != null ? shockArea.position : transform.position;
        public Quaternion AreaRotation => shockArea != null ? shockArea.rotation : transform.rotation;

        protected override void Awake()
        {
            base.Awake();
            ApplyShockVisualColors();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            radius = Mathf.Max(0.05f, radius);
            boxSize = MaxVector(boxSize, 0.05f);
            duration = Mathf.Max(0f, duration);
            delay = Mathf.Max(0f, delay);
            damagePerPulse = Mathf.Max(0f, damagePerPulse);
            pulseInterval = Mathf.Max(0.05f, pulseInterval);
            slowMultiplier = Mathf.Clamp01(slowMultiplier);
            activeZoneParticleAmount = Mathf.Max(0.1f, activeZoneParticleAmount);
            tazeParticleAmount = Mathf.Max(0.1f, tazeParticleAmount);
            tazeShakeStrength = Mathf.Max(0f, tazeShakeStrength);
            tazeShakeSpeed = Mathf.Max(0f, tazeShakeSpeed);
            ApplyShockVisualColors();
            SynchronizeAreaVisual();
        }

        public void Configure(
            Transform configuredElectricalSource,
            Transform configuredShockArea,
            Transform configuredConductiveSurface,
            Renderer configuredConductiveSurfaceRenderer,
            Renderer configuredWireRenderer)
        {
            electricalSource = configuredElectricalSource;
            shockArea = configuredShockArea;
            conductiveSurface = configuredConductiveSurface;
            conductiveSurfaceRenderer = configuredConductiveSurfaceRenderer;
            wireRenderer = configuredWireRenderer;
            ApplyShockVisualColors();
            SynchronizeAreaVisual();
        }

        public void SetRadius(float configuredRadius)
        {
            radius = Mathf.Max(0.05f, configuredRadius);
            SynchronizeAreaVisual();
        }

        public void SetBoxSize(Vector3 configuredSize)
        {
            boxSize = MaxVector(configuredSize, 0.05f);
            SynchronizeAreaVisual();
        }

        public void SynchronizeAreaVisual()
        {
            SynchronizeConductiveSurfaceFootprint();
            if (shockArea == null)
                return;

            EnvironmentalAreaVisual areaVisual = shockArea.GetComponent<EnvironmentalAreaVisual>();
            if (areaVisual != null)
                areaVisual.Synchronize(
                    shockAreaShape,
                    boxSize,
                    radius,
                    PreviewColor(shockEffectColor));
        }

        protected override void ActivateEffect()
        {
            StartCoroutine(ShockRoutine());
        }

        private IEnumerator ShockRoutine()
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            float effectDuration = Mathf.Max(0f, duration);
            float endTime = Time.time + effectDuration;
            if (spawnActiveZoneParticles)
            {
                EnvironmentalRuntimeEffects.SpawnShockField(
                    AreaPosition,
                    AreaRotation,
                    shockAreaShape,
                    radius,
                    boxSize,
                    Mathf.Max(0.15f, effectDuration),
                    activeZoneParticleColor,
                    activeZoneParticleAmount);
            }
            do
            {
                float remainingDuration = effectDuration > 0f
                    ? Mathf.Max(0.08f, endTime - Time.time)
                    : 0.15f;
                EnvironmentalRuntimeEffects.ApplyShock(
                    AreaPosition,
                    AreaRotation,
                    shockAreaShape,
                    radius,
                    boxSize,
                    damagePerPulse,
                    slowMultiplier,
                    remainingDuration,
                    spawnTazeParticles,
                    tazeParticleColor,
                    tazeParticleAmount,
                    tazeShakeStrength,
                    tazeShakeSpeed,
                    affectedLayers);
                if (effectDuration <= 0f)
                    break;
                yield return new WaitForSeconds(pulseInterval);
            }
            while (Time.time < endTime);

            EnvironmentalRuntimeEffects.SpawnSpherePulse(
                AreaPosition,
                shockAreaShape == EnvironmentalAreaShape.Sphere
                    ? radius
                    : Mathf.Max(boxSize.x, boxSize.z) * 0.5f,
                shockEffectColor);
            NotifyEffectCompleted();
        }

        private void ApplyShockVisualColors()
        {
            EnvironmentalVisualUtility.ApplyColor(conductiveSurfaceRenderer, conductiveSurfaceColor);
            EnvironmentalVisualUtility.ApplyColor(wireRenderer, wireColor);
        }

        private void SynchronizeConductiveSurfaceFootprint()
        {
            if (conductiveSurface == null)
                return;

            float footprint = radius * 2f;
            Vector3 desiredWorldSize = shockAreaShape == EnvironmentalAreaShape.Sphere
                ? new Vector3(footprint, 0f, footprint)
                : new Vector3(boxSize.x, 0f, boxSize.z);
            Vector3 parentScale = conductiveSurface.parent != null
                ? conductiveSurface.parent.lossyScale
                : Vector3.one;
            Vector3 localScale = conductiveSurface.localScale;
            localScale.x = DivideScale(desiredWorldSize.x, parentScale.x);
            localScale.z = DivideScale(desiredWorldSize.z, parentScale.z);
            conductiveSurface.localScale = localScale;
        }

        private static float DivideScale(float desired, float parentScale)
        {
            return desired / Mathf.Max(0.0001f, Mathf.Abs(parentScale));
        }

        private static Color PreviewColor(Color color)
        {
            color.a = Mathf.Min(color.a, 0.16f);
            return color;
        }

        private static Vector3 MaxVector(Vector3 value, float minimum)
        {
            return new Vector3(
                Mathf.Max(minimum, Mathf.Abs(value.x)),
                Mathf.Max(minimum, Mathf.Abs(value.y)),
                Mathf.Max(minimum, Mathf.Abs(value.z)));
        }
    }
}
