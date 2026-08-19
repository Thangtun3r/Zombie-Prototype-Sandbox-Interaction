using System.Collections;
using UnityEngine;

namespace EnvironmentInteraction.Authoring
{
    public sealed class ExplodeInteraction : EnvironmentalInteractionBase
    {
        [SerializeField] private Transform explosiveObject;
        [SerializeField] private Transform explosionOrigin;
        [SerializeField, Min(0.05f)] private float outerRadius = 5f;
        [SerializeField, Min(0f)] private float innerRadius = 1.5f;
        [SerializeField, Min(0f)] private float delay;
        [SerializeField, Min(0f)] private float forceRepresentation = 8f;
        [SerializeField, Min(0f)] private float damage = 125f;
        [SerializeField] private bool ragdollEnemies = true;
        [SerializeField, Min(0f)] private float ragdollUpwardForce = 6f;
        [SerializeField, Min(0f)] private float ragdollTumbleTorque = 12f;
        [SerializeField, Min(0f)] private float ragdollDisappearDelay = 3f;
        [SerializeField] private Color effectColor = new Color(1f, 0.18f, 0.02f, 0.28f);
        [SerializeField] private LayerMask affectedLayers = ~0;
        [SerializeField] private bool hideExplosiveObjectOnActivation = true;

        public override EnvironmentalInteractionType Type => EnvironmentalInteractionType.Explode;
        public Transform ExplosiveObject => explosiveObject;
        public Transform ExplosionOrigin => explosionOrigin;
        public float OuterRadius => outerRadius;
        public float InnerRadius => innerRadius;
        public float Delay => delay;
        public float ForceRepresentation => forceRepresentation;
        public float Damage => damage;
        public bool RagdollEnemies => ragdollEnemies;
        public float RagdollUpwardForce => ragdollUpwardForce;
        public float RagdollTumbleTorque => ragdollTumbleTorque;
        public float RagdollDisappearDelay => ragdollDisappearDelay;
        public Color EffectColor => effectColor;
        public Vector3 OriginPosition => explosionOrigin != null ? explosionOrigin.position : transform.position;

        protected override void OnValidate()
        {
            base.OnValidate();
            outerRadius = Mathf.Max(0.05f, outerRadius);
            innerRadius = Mathf.Clamp(innerRadius, 0f, outerRadius);
            delay = Mathf.Max(0f, delay);
            forceRepresentation = Mathf.Max(0f, forceRepresentation);
            damage = Mathf.Max(0f, damage);
            ragdollUpwardForce = Mathf.Max(0f, ragdollUpwardForce);
            ragdollTumbleTorque = Mathf.Max(0f, ragdollTumbleTorque);
            ragdollDisappearDelay = Mathf.Max(0f, ragdollDisappearDelay);
            SynchronizeAreaVisual();
        }

        public void Configure(Transform configuredExplosiveObject, Transform configuredOrigin)
        {
            explosiveObject = configuredExplosiveObject;
            explosionOrigin = configuredOrigin;
            SynchronizeAreaVisual();
        }

        public void SetOuterRadius(float radius)
        {
            outerRadius = Mathf.Max(0.05f, radius);
            innerRadius = Mathf.Min(innerRadius, outerRadius);
            SynchronizeAreaVisual();
        }

        public void SetInnerRadius(float radius)
        {
            innerRadius = Mathf.Clamp(radius, 0f, outerRadius);
            SynchronizeAreaVisual();
        }

        public void SynchronizeAreaVisual()
        {
            if (explosionOrigin == null)
                return;

            EnvironmentalAreaVisual areaVisual = explosionOrigin.GetComponent<EnvironmentalAreaVisual>();
            if (areaVisual != null)
                areaVisual.Synchronize(
                    EnvironmentalAreaShape.Sphere,
                    Vector3.one * (outerRadius * 2f),
                    outerRadius,
                    PreviewColor(effectColor));
        }

        private static Color PreviewColor(Color color)
        {
            color.a = Mathf.Min(color.a, 0.14f);
            return color;
        }

        protected override void ActivateEffect()
        {
            StartCoroutine(ExplodeRoutine());
        }

        private IEnumerator ExplodeRoutine()
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            Vector3 center = OriginPosition;
            EnvironmentalRuntimeEffects.ApplyExplosion(
                center,
                innerRadius,
                outerRadius,
                damage,
                forceRepresentation,
                ragdollEnemies,
                ragdollUpwardForce,
                ragdollTumbleTorque,
                ragdollDisappearDelay,
                affectedLayers);
            EnvironmentalRuntimeEffects.SpawnSpherePulse(
                center,
                outerRadius,
                effectColor);

            if (hideExplosiveObjectOnActivation && explosiveObject != null)
                explosiveObject.gameObject.SetActive(false);
            NotifyEffectCompleted();
        }
    }
}
