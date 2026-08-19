using System.Collections.Generic;
using UnityEngine;
using ZombiePrototype;

namespace EnvironmentInteraction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class ExplosiveBarrel : MonoBehaviour, IDamageable
    {
        [Header("Durability")]
        [SerializeField, Min(1f)] private float health = 20f;

        [Header("Explosion")]
        [SerializeField, Min(0.1f)] private float radius = 5f;
        [SerializeField, Min(0f)] private float damage = 125f;
        [SerializeField, Min(0f)] private float knockbackForce = 8f;
        [SerializeField] private LayerMask affectedLayers = ~0;

        private bool exploded;

        public float Health => health;
        public float Radius => radius;
        public float Damage => damage;
        public float KnockbackForce => knockbackForce;

        public void Configure(float configuredHealth, float configuredRadius, float configuredDamage, float configuredKnockback)
        {
            health = Mathf.Max(1f, configuredHealth);
            radius = Mathf.Max(0.1f, configuredRadius);
            damage = Mathf.Max(0f, configuredDamage);
            knockbackForce = Mathf.Max(0f, configuredKnockback);
        }

        public void TakeDamage(float amount)
        {
            if (exploded || amount <= 0f)
                return;

            health -= amount;
            if (health <= 0f)
                Explode();
        }

        private void Explode()
        {
            if (exploded)
                return;

            exploded = true;
            Vector3 center = transform.position + Vector3.up * 0.75f;
            SpawnRadiusVisual(center);
            SpawnExplosionParticles(center);
            SpawnFlash(center);

            Collider[] overlaps = Physics.OverlapSphere(
                center,
                radius,
                affectedLayers,
                QueryTriggerInteraction.Collide);

            HashSet<IDamageable> damageables = new HashSet<IDamageable>();
            HashSet<IExplosionBreakable> breakables = new HashSet<IExplosionBreakable>();

            foreach (Collider overlap in overlaps)
            {
                IDamageable damageable = FindInParents<IDamageable>(overlap.transform);
                if (damageable != null && !ReferenceEquals(damageable, this))
                    damageables.Add(damageable);

                IExplosionBreakable breakable = FindInParents<IExplosionBreakable>(overlap.transform);
                if (breakable != null)
                    breakables.Add(breakable);
            }

            foreach (IDamageable damageable in damageables)
            {
                Component target = damageable as Component;
                if (target != null)
                {
                    Vector3 direction = target.transform.position - center;
                    direction.y = 0f;
                    if (direction.sqrMagnitude < 0.001f)
                        direction = transform.forward;

                    ZombieMovement movement = target.GetComponentInParent<ZombieMovement>();
                    if (movement != null)
                        movement.ApplyKnockback(direction.normalized * knockbackForce);
                    else
                    {
                        Rigidbody targetBody = target.GetComponentInParent<Rigidbody>();
                        if (targetBody != null && !targetBody.isKinematic)
                            targetBody.AddExplosionForce(knockbackForce, center, radius, 0.4f, ForceMode.Impulse);
                    }
                }

                damageable.TakeDamage(damage);
            }

            foreach (IExplosionBreakable breakable in breakables)
                breakable.BreakFromExplosion(center, knockbackForce);

            Destroy(gameObject);
        }

        private static T FindInParents<T>(Transform source) where T : class
        {
            Transform current = source;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (behaviour is T match)
                        return match;
                }
                current = current.parent;
            }
            return null;
        }

        private void SpawnRadiusVisual(Vector3 center)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Barrel Explosion Radius";
            visual.transform.position = center;
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
                Destroy(visualCollider);

            ExplosionRadiusVisual radiusVisual = visual.AddComponent<ExplosionRadiusVisual>();
            radiusVisual.Initialize(radius, 0.55f, new Color(1f, 0.2f, 0.02f, 0.28f));
        }

        private static void SpawnExplosionParticles(Vector3 center)
        {
            GameObject effect = new GameObject("Barrel Explosion Particles");
            effect.transform.position = center;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.65f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.9f, 0.15f),
                new Color(0.8f, 0.04f, 0.01f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 55) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            particles.Play();
        }

        private static void SpawnFlash(Vector3 center)
        {
            GameObject flashObject = new GameObject("Barrel Explosion Flash");
            flashObject.transform.position = center;
            Light flash = flashObject.AddComponent<Light>();
            flash.type = LightType.Point;
            flash.color = new Color(1f, 0.35f, 0.08f);
            flash.range = 8f;
            flash.intensity = 7f;
            flashObject.AddComponent<ExplosionFlashLight>().Initialize(flash, 0.18f);
        }
    }
}
