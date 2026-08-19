using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZombiePrototype;

namespace EnvironmentInteraction.Authoring
{
    [DisallowMultipleComponent]
    public sealed class EnvironmentalTazeFeedback : MonoBehaviour
    {
        private readonly List<Transform> visualTransforms = new List<Transform>();
        private readonly List<Vector3> restingLocalPositions = new List<Vector3>();
        private ParticleSystem particles;
        private Material particleMaterial;
        private float endTime;
        private float shakeStrength;
        private float shakeSpeed;

        public bool IsTazing => enabled && Time.time < endTime;

        public static void ApplyTo(
            ZombieMovement movement,
            float duration,
            bool spawnParticles,
            Color particleColor,
            float particleAmount,
            float configuredShakeStrength,
            float configuredShakeSpeed)
        {
            if (movement == null)
                return;

            EnvironmentalTazeFeedback feedback =
                movement.GetComponent<EnvironmentalTazeFeedback>();
            if (feedback == null)
                feedback = movement.gameObject.AddComponent<EnvironmentalTazeFeedback>();
            feedback.BeginOrExtend(
                duration,
                spawnParticles,
                particleColor,
                particleAmount,
                configuredShakeStrength,
                configuredShakeSpeed);
        }

        private void Awake()
        {
            CacheVisualTransforms();
            enabled = false;
        }

        private void LateUpdate()
        {
            if (Time.time >= endTime)
            {
                StopFeedback();
                return;
            }

            float phase = Time.time * shakeSpeed;
            Vector3 offset = new Vector3(
                Mathf.Sin(phase),
                Mathf.Sin(phase * 1.73f + 0.8f),
                Mathf.Sin(phase * 1.31f + 2.1f)) * shakeStrength;

            for (int index = 0; index < visualTransforms.Count; index++)
            {
                Transform visual = visualTransforms[index];
                if (visual != null)
                    visual.localPosition = restingLocalPositions[index] + offset;
            }
        }

        private void OnDisable()
        {
            RestoreVisuals();
            if (particles != null)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void OnDestroy()
        {
            if (particleMaterial != null)
                Destroy(particleMaterial);
        }

        private void BeginOrExtend(
            float duration,
            bool spawnParticles,
            Color particleColor,
            float particleAmount,
            float configuredShakeStrength,
            float configuredShakeSpeed)
        {
            if (visualTransforms.Count == 0)
                CacheVisualTransforms();

            endTime = Mathf.Max(endTime, Time.time + Mathf.Max(0.08f, duration));
            shakeStrength = Mathf.Max(0f, configuredShakeStrength);
            shakeSpeed = Mathf.Max(0f, configuredShakeSpeed);
            enabled = true;

            if (!spawnParticles)
            {
                if (particles != null)
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }

            EnsureParticles();
            ConfigureParticles(particleColor, particleAmount);
            if (!particles.isPlaying)
                particles.Play(true);
        }

        private void StopFeedback()
        {
            RestoreVisuals();
            if (particles != null)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            enabled = false;
        }

        private void CacheVisualTransforms()
        {
            visualTransforms.Clear();
            restingLocalPositions.Clear();

            IEnumerable<Transform> candidates = GetComponentsInChildren<Renderer>(true)
                .Select(targetRenderer => targetRenderer.transform)
                .Where(candidate =>
                    candidate != transform &&
                    candidate.GetComponent<Collider>() == null)
                .Distinct();

            foreach (Transform candidate in candidates)
            {
                visualTransforms.Add(candidate);
                restingLocalPositions.Add(candidate.localPosition);
            }
        }

        private void RestoreVisuals()
        {
            for (int index = 0; index < visualTransforms.Count; index++)
            {
                Transform visual = visualTransforms[index];
                if (visual != null)
                    visual.localPosition = restingLocalPositions[index];
            }
        }

        private void EnsureParticles()
        {
            if (particles != null)
                return;

            GameObject effect = new GameObject("Active Taze Particles");
            effect.transform.SetParent(transform, false);
            effect.transform.localPosition = new Vector3(0f, 1f, 0f);
            particles = effect.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 96;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.75f;
            shape.radiusThickness = 0.2f;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.32f;
            noise.frequency = 1.4f;
            noise.scrollSpeed = 1.8f;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.velocityScale = 0.08f;
            particleRenderer.lengthScale = 1.6f;
            particleRenderer.sortingOrder = 8;
            particleMaterial = EnvironmentalRuntimeEffects.CreateParticleMaterial(
                "Runtime Taze Particle Material");
            if (particleMaterial != null)
                particleRenderer.sharedMaterial = particleMaterial;
        }

        private void ConfigureParticles(Color color, float amount)
        {
            float safeAmount = Mathf.Max(0.1f, amount);
            Color brightColor = Color.Lerp(color, Color.white, 0.45f);
            brightColor.a = color.a;

            ParticleSystem.MainModule main = particles.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, brightColor);
            main.maxParticles = Mathf.Clamp(Mathf.CeilToInt(72f * safeAmount), 24, 240);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(18f, 52f * safeAmount);
        }
    }
}
