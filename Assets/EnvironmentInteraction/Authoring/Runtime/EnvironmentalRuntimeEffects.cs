using System.Collections.Generic;
using UnityEngine;
using ZombiePrototype;

namespace EnvironmentInteraction.Authoring
{
    internal static class EnvironmentalRuntimeEffects
    {
        private const string ParticleShaderName = "Zombie Prototype/Environmental Particle Unlit";

        public static int ApplyAreaImpact(
            Vector3 center,
            Quaternion rotation,
            EnvironmentalAreaShape shape,
            float radius,
            Vector3 boxSize,
            float damage,
            float force,
            LayerMask layers)
        {
            Collider[] overlaps = shape == EnvironmentalAreaShape.Sphere
                ? Physics.OverlapSphere(center, radius, layers, QueryTriggerInteraction.Collide)
                : Physics.OverlapBox(center, boxSize * 0.5f, rotation, layers, QueryTriggerInteraction.Collide);
            return ApplyTargets(overlaps, damage, force, center, Vector3.zero, true);
        }

        public static int ApplyDirectionalPush(
            Vector3 center,
            Quaternion rotation,
            Vector3 boxSize,
            Vector3 direction,
            float force,
            LayerMask layers)
        {
            Collider[] overlaps = Physics.OverlapBox(
                center,
                boxSize * 0.5f,
                rotation,
                layers,
                QueryTriggerInteraction.Collide);
            return ApplyTargets(
                overlaps,
                0f,
                force,
                center,
                direction.normalized,
                false);
        }

        public static int ApplyShock(
            Vector3 center,
            Quaternion rotation,
            EnvironmentalAreaShape shape,
            float radius,
            Vector3 boxSize,
            float damage,
            float slowMultiplier,
            float feedbackDuration,
            bool spawnTazeParticles,
            Color tazeParticleColor,
            float tazeParticleAmount,
            float tazeShakeStrength,
            float tazeShakeSpeed,
            LayerMask layers)
        {
            Collider[] overlaps = shape == EnvironmentalAreaShape.Sphere
                ? Physics.OverlapSphere(center, radius, layers, QueryTriggerInteraction.Collide)
                : Physics.OverlapBox(center, boxSize * 0.5f, rotation, layers, QueryTriggerInteraction.Collide);

            HashSet<ZombieHealth> healthTargets = new HashSet<ZombieHealth>();
            HashSet<ZombieMovement> movements = new HashSet<ZombieMovement>();
            foreach (Collider overlap in overlaps)
            {
                if (overlap == null)
                    continue;

                ZombieHealth health = overlap.GetComponentInParent<ZombieHealth>();
                if (health != null)
                    healthTargets.Add(health);

                ZombieMovement movement = overlap.GetComponentInParent<ZombieMovement>();
                if (movement != null)
                    movements.Add(movement);
            }

            foreach (ZombieHealth health in healthTargets)
                health.TakeDamage(damage);
            foreach (ZombieMovement movement in movements)
            {
                EnvironmentalTemporarySlow.ApplyTo(movement, slowMultiplier, feedbackDuration);
                EnvironmentalTazeFeedback.ApplyTo(
                    movement,
                    feedbackDuration,
                    spawnTazeParticles,
                    tazeParticleColor,
                    tazeParticleAmount,
                    tazeShakeStrength,
                    tazeShakeSpeed);
            }
            return Mathf.Max(healthTargets.Count, movements.Count);
        }

        public static int ApplyExplosion(
            Vector3 center,
            float innerRadius,
            float outerRadius,
            float damage,
            float force,
            bool ragdollEnemies,
            float ragdollUpwardForce,
            float ragdollTumbleTorque,
            float ragdollDisappearDelay,
            LayerMask layers)
        {
            Collider[] overlaps = Physics.OverlapSphere(
                center,
                outerRadius,
                layers,
                QueryTriggerInteraction.Collide);
            HashSet<IDamageable> damageables = new HashSet<IDamageable>();
            HashSet<ZombieMovement> movements = new HashSet<ZombieMovement>();
            HashSet<Rigidbody> rigidbodies = new HashSet<Rigidbody>();
            CollectTargets(overlaps, damageables, movements, rigidbodies);

            float falloffRange = Mathf.Max(0.001f, outerRadius - innerRadius);
            foreach (IDamageable damageable in damageables)
            {
                Component component = damageable as Component;
                ZombieMovement zombieMovement = component != null
                    ? component.GetComponentInParent<ZombieMovement>()
                    : null;
                if (ragdollEnemies && zombieMovement != null)
                    continue;
                float distance = component != null
                    ? Vector3.Distance(center, component.transform.position)
                    : innerRadius;
                float falloff = 1f - Mathf.Clamp01((distance - innerRadius) / falloffRange) * 0.65f;
                damageable.TakeDamage(damage * falloff);
            }

            foreach (ZombieMovement movement in movements)
            {
                Vector3 direction = HorizontalDirection(center, movement.transform.position);
                if (ragdollEnemies)
                {
                    EnvironmentalZombieRagdoll.Activate(
                        movement,
                        direction * force,
                        ragdollUpwardForce,
                        ragdollTumbleTorque,
                        ragdollDisappearDelay,
                        true);
                }
                else
                {
                    movement.ApplyKnockback(direction * force);
                }
            }

            foreach (Rigidbody body in rigidbodies)
            {
                if (body != null && !body.isKinematic)
                    body.AddExplosionForce(force, center, outerRadius, 0.35f, ForceMode.Impulse);
            }

            return Mathf.Max(damageables.Count, Mathf.Max(movements.Count, rigidbodies.Count));
        }

        public static void SpawnSpherePulse(Vector3 center, float radius, Color color)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Environmental Effect Radius";
            visual.transform.position = center;
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
                Object.Destroy(visualCollider);
            ExplosionRadiusVisual radiusVisual = visual.AddComponent<ExplosionRadiusVisual>();
            radiusVisual.Initialize(Mathf.Max(0.1f, radius), 0.45f, color);
        }

        public static void SpawnDropImpactParticles(
            Vector3 floorContact,
            Quaternion rotation,
            Vector2 footprint,
            Color dustColor,
            float amount)
        {
            GameObject effect = new GameObject("DROP Impact Dust and Debris");
            effect.transform.position = floorContact;
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 0f;
            main.startSize = 0.2f;
            main.gravityModifier = 0.85f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.72f, 0.66f, 0.57f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.7f, 0.45f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.sortingOrder = 5;
            ConfigureParticleRenderer(particles, 1.5f);

            float width = Mathf.Max(0.5f, Mathf.Abs(footprint.x));
            float depth = Mathf.Max(0.5f, Mathf.Abs(footprint.y));
            float area = width * depth;
            float particleAmount = Mathf.Max(0.1f, amount);
            int dustCount = Mathf.Clamp(Mathf.RoundToInt(area * 4f * particleAmount), 8, 80);
            int debrisCount = Mathf.Clamp(Mathf.RoundToInt(area * 1.5f * particleAmount), 4, 36);
            Color lighterDust = Color.Lerp(dustColor, Color.white, 0.28f);
            lighterDust.a = dustColor.a * 0.85f;
            Color darkDebris = Color.Lerp(dustColor, Color.black, 0.7f);
            darkDebris.a = 1f;
            Color lightDebris = Color.Lerp(dustColor, Color.black, 0.38f);
            lightDebris.a = 1f;

            particles.Play(false);
            EmitImpactLayer(
                particles,
                floorContact,
                rotation,
                width,
                depth,
                dustCount,
                new Vector2(1.5f, 3.8f),
                new Vector2(1.2f, 2.8f),
                new Vector2(0.45f, 0.9f),
                new Vector2(0.18f, 0.42f),
                dustColor,
                lighterDust);
            EmitImpactLayer(
                particles,
                floorContact,
                rotation,
                width,
                depth,
                debrisCount,
                new Vector2(3.5f, 7f),
                new Vector2(2.5f, 5.5f),
                new Vector2(0.35f, 0.75f),
                new Vector2(0.06f, 0.16f),
                darkDebris,
                lightDebris);
        }

        public static void SpawnWaterJet(
            Vector3 origin,
            Vector3 direction,
            float range,
            float width,
            float duration,
            Color waterColor,
            float amount)
        {
            Vector3 forward = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;
            GameObject effect = new GameObject("PUSH Hydrant Water Jet");
            effect.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(forward, Vector3.up));
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float effectDuration = Mathf.Max(0.1f, duration);
            float jetRange = Mathf.Max(0.5f, range);
            float lifetime = Mathf.Clamp(jetRange / 12f, 0.25f, 0.85f);
            ParticleSystem.MainModule main = particles.main;
            main.duration = effectDuration;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.75f, lifetime);
            main.startSpeed = new ParticleSystem.MinMaxCurve(jetRange / lifetime * 0.8f, jetRange / lifetime);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
            Color lighterWater = Color.Lerp(waterColor, Color.white, 0.35f);
            lighterWater.a = waterColor.a * 0.75f;
            main.startColor = new ParticleSystem.MinMaxGradient(waterColor, lighterWater);
            main.gravityModifier = 0.22f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Clamp(Mathf.CeilToInt(120f * effectDuration * amount), 48, 420);
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(12f, 90f * amount);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.12f;
            shape.angle = Mathf.Clamp(
                Mathf.Atan2(Mathf.Max(0.1f, width) * 0.5f, jetRange) * Mathf.Rad2Deg,
                2f,
                22f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.65f;
            noise.scrollSpeed = 0.35f;

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.75f, 0.9f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.8f, 0.75f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.12f;
            particleRenderer.lengthScale = 0.45f;
            particleRenderer.sortingOrder = 6;
            ConfigureParticleRenderer(particles, effectDuration + lifetime + 0.5f);
            particles.Play(false);
        }

        public static ParticleSystem SpawnShockField(
            Vector3 center,
            Quaternion rotation,
            EnvironmentalAreaShape areaShape,
            float radius,
            Vector3 boxSize,
            float duration,
            Color electricColor,
            float amount)
        {
            GameObject effect = new GameObject("SHOCK Active Electrical Field");
            effect.transform.SetPositionAndRotation(
                center + rotation * new Vector3(0f, 0.08f, 0f),
                rotation);
            ParticleSystem particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            float effectDuration = Mathf.Max(0.15f, duration);
            float particleAmount = Mathf.Max(0.1f, amount);
            Color brightElectric = Color.Lerp(electricColor, Color.white, 0.55f);
            brightElectric.a = electricColor.a;

            ParticleSystem.MainModule main = particles.main;
            main.duration = effectDuration;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(electricColor, brightElectric);
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Clamp(
                Mathf.CeilToInt(180f * particleAmount),
                64,
                520);
            main.stopAction = ParticleSystemStopAction.Destroy;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(35f, 95f * particleAmount);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            if (areaShape == EnvironmentalAreaShape.Sphere)
            {
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = Mathf.Max(0.05f, radius);
                shape.radiusThickness = 0.18f;
            }
            else
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.scale = new Vector3(
                    Mathf.Max(0.05f, Mathf.Abs(boxSize.x)),
                    Mathf.Clamp(Mathf.Abs(boxSize.y), 0.08f, 0.35f),
                    Mathf.Max(0.05f, Mathf.Abs(boxSize.z)));
            }

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            // X and Z use Unity's default Constant mode, so Y must use Constant too.
            // The Noise module below supplies the per-particle electrical variation.
            velocity.y = new ParticleSystem.MinMaxCurve(0.425f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.42f;
            noise.frequency = 2.1f;
            noise.scrollSpeed = 2.8f;

            Gradient fade = new Gradient();
            fade.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(electricColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.85f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.1f;
            particleRenderer.lengthScale = 2.2f;
            particleRenderer.sortingOrder = 7;
            ConfigureParticleRenderer(particles, effectDuration + 0.75f);
            particles.Play(false);
            return particles;
        }

        public static void ConfigureParticleRenderer(ParticleSystem particles, float materialLifetime)
        {
            if (particles == null)
                return;

            Material material = CreateParticleMaterial("Runtime Environmental Particle Material");
            if (material == null)
                return;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            if (particleRenderer != null)
                particleRenderer.sharedMaterial = material;
            Object.Destroy(material, Mathf.Max(0.25f, materialLifetime));
        }

        public static Material CreateParticleMaterial(string materialName)
        {
            Shader shader = Shader.Find(ParticleShaderName);
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                return null;

            Material material = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.DontSave
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            return material;
        }

        private static void EmitImpactLayer(
            ParticleSystem particles,
            Vector3 center,
            Quaternion rotation,
            float width,
            float depth,
            int count,
            Vector2 outwardSpeed,
            Vector2 upwardSpeed,
            Vector2 lifetime,
            Vector2 size,
            Color colorA,
            Color colorB)
        {
            for (int index = 0; index < count; index++)
            {
                Vector3 localOffset = RandomPointOnRectangle(width * 0.5f, depth * 0.5f);
                Vector3 localOutward = new Vector3(localOffset.x, 0f, localOffset.z).normalized;
                if (localOutward.sqrMagnitude < 0.001f)
                    localOutward = Random.insideUnitSphere.WithY(0f).normalized;

                Vector3 localVelocity =
                    localOutward * Random.Range(outwardSpeed.x, outwardSpeed.y) +
                    Vector3.up * Random.Range(upwardSpeed.x, upwardSpeed.y);
                ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams
                {
                    position = center + rotation * localOffset,
                    velocity = rotation * localVelocity,
                    startLifetime = Random.Range(lifetime.x, lifetime.y),
                    startSize = Random.Range(size.x, size.y),
                    startColor = Color.Lerp(colorA, colorB, Random.value)
                };
                particles.Emit(particle, 1);
            }
        }

        private static Vector3 RandomPointOnRectangle(float halfWidth, float halfDepth)
        {
            if (Random.value < 0.5f)
            {
                float x = Random.value < 0.5f ? -halfWidth : halfWidth;
                return new Vector3(x, 0.03f, Random.Range(-halfDepth, halfDepth));
            }

            float z = Random.value < 0.5f ? -halfDepth : halfDepth;
            return new Vector3(Random.Range(-halfWidth, halfWidth), 0.03f, z);
        }

        private static int ApplyTargets(
            Collider[] overlaps,
            float damage,
            float force,
            Vector3 center,
            Vector3 fixedDirection,
            bool radial,
            bool ragdollEnemies = false,
            float ragdollUpwardForce = 0f,
            float ragdollTumbleTorque = 0f,
            float ragdollDisappearDelay = 0f)
        {
            HashSet<IDamageable> damageables = new HashSet<IDamageable>();
            HashSet<ZombieMovement> movements = new HashSet<ZombieMovement>();
            HashSet<Rigidbody> rigidbodies = new HashSet<Rigidbody>();
            CollectTargets(overlaps, damageables, movements, rigidbodies);

            if (damage > 0f)
            {
                foreach (IDamageable damageable in damageables)
                    damageable.TakeDamage(damage);
            }

            if (force > 0f)
            {
                foreach (ZombieMovement movement in movements)
                {
                    Vector3 direction = radial
                        ? HorizontalDirection(center, movement.transform.position)
                        : fixedDirection;
                    if (ragdollEnemies)
                    {
                        EnvironmentalZombieRagdoll.Activate(
                            movement,
                            direction * force,
                            ragdollUpwardForce,
                            ragdollTumbleTorque,
                            ragdollDisappearDelay,
                            false);
                    }
                    else
                    {
                        movement.ApplyKnockback(direction * force);
                    }
                }

                foreach (Rigidbody body in rigidbodies)
                {
                    if (body == null || body.isKinematic)
                        continue;
                    if (radial)
                        body.AddExplosionForce(force, center, 10f, 0.25f, ForceMode.Impulse);
                    else
                        body.AddForce(fixedDirection * force, ForceMode.Impulse);
                }
            }

            return Mathf.Max(damageables.Count, Mathf.Max(movements.Count, rigidbodies.Count));
        }

        private static void CollectTargets(
            Collider[] overlaps,
            HashSet<IDamageable> damageables,
            HashSet<ZombieMovement> movements,
            HashSet<Rigidbody> rigidbodies)
        {
            foreach (Collider overlap in overlaps)
            {
                if (overlap == null)
                    continue;

                IDamageable damageable = overlap.GetComponentInParent<IDamageable>();
                if (damageable != null && !(damageable is EnvironmentalTrigger))
                    damageables.Add(damageable);

                ZombieMovement movement = overlap.GetComponentInParent<ZombieMovement>();
                if (movement != null)
                    movements.Add(movement);

                if (rigidbodies == null || movement != null)
                    continue;
                Rigidbody body = overlap.attachedRigidbody;
                if (body != null)
                    rigidbodies.Add(body);
            }
        }

        private static Vector3 HorizontalDirection(Vector3 origin, Vector3 target)
        {
            Vector3 direction = target - origin;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        private static Vector3 WithY(this Vector3 value, float y)
        {
            value.y = y;
            return value;
        }
    }
}
