using System.Collections.Generic;
using UnityEngine;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ZombieHealth))]
    public sealed class ZombieDeathExplosion : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 4f;
        [SerializeField, Min(0f)] private float damage = 45f;
        [SerializeField, Min(0f)] private float force = 4f;
        [SerializeField] private Material effectMaterial;
        [SerializeField, Min(0.05f)] private float radiusVisualDuration = 0.55f;

        private ZombieHealth health;

        private void Awake()
        {
            health = GetComponent<ZombieHealth>();
        }

        private void OnEnable()
        {
            if (health == null)
                health = GetComponent<ZombieHealth>();
            health.Died += Explode;
        }

        private void OnDisable()
        {
            if (health != null)
                health.Died -= Explode;
        }

        public void Configure(ZombieArchetype archetype)
        {
            if (archetype == null)
                return;

            radius = archetype.ExplosionRadius;
            damage = archetype.ExplosionDamage;
            force = archetype.ExplosionForce;
            effectMaterial = archetype.ExplosionMaterial;
        }

        private void Explode()
        {
            Vector3 center = transform.position + Vector3.up;
            Collider[] targets = Physics.OverlapSphere(
                center,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore);
            HashSet<int> affected = new HashSet<int>();

            foreach (Collider targetCollider in targets)
            {
                IDamageable damageable = targetCollider.GetComponentInParent<IDamageable>();
                if (!(damageable is Component damageableComponent))
                    continue;

                int targetId = damageableComponent.gameObject.GetInstanceID();
                if (targetId == gameObject.GetInstanceID() || !affected.Add(targetId))
                    continue;

                Vector3 direction = damageableComponent.transform.position - center;
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.01f)
                    direction = transform.forward;
                direction.Normalize();

                ZombieMovement movement = damageableComponent.GetComponent<ZombieMovement>();
                if (movement != null)
                    movement.ApplyKnockback(direction * force);
                else if (targetCollider.attachedRigidbody != null)
                    targetCollider.attachedRigidbody.AddExplosionForce(force, center, radius, 0f, ForceMode.Impulse);

                damageable.TakeDamage(damage);
            }

            SpawnExplosionEffect(center);
        }

        private void SpawnExplosionEffect(Vector3 position)
        {
            SpawnRadiusSphere(position);

            GameObject effect = new GameObject("Tank Death Explosion");
            effect.transform.position = position;

            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.25f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.75f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.85f, 0.15f, 1f),
                new Color(0.9f, 0.08f, 0.01f, 1f));
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (effectMaterial != null)
                particleRenderer.sharedMaterial = effectMaterial;

            Light flash = effect.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.35f, 0.05f);
            flash.range = radius * 1.5f;
            flash.intensity = 5f;

            particles.Play();
            Destroy(effect, 1.2f);
        }

        private void SpawnRadiusSphere(Vector3 position)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Tank Blast Radius";
            sphere.transform.position = position;

            Collider sphereCollider = sphere.GetComponent<Collider>();
            if (sphereCollider != null)
                Destroy(sphereCollider);

            ExplosionRadiusVisual visual = sphere.AddComponent<ExplosionRadiusVisual>();
            visual.Initialize(
                radius,
                radiusVisualDuration,
                new Color(1f, 0.18f, 0.025f, 0.36f));
        }
    }
}
