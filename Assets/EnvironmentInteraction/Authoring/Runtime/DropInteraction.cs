using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ZombiePrototype;

namespace EnvironmentInteraction.Authoring
{
    public sealed class DropInteraction : EnvironmentalInteractionBase
    {
        [SerializeField] private Transform dropObject;
        [SerializeField, Min(0f)] private float dropDelay;
        [SerializeField, Min(0f)] private float forwardTravelDistance = 1.5f;
        [SerializeField] private Transform impactZone;
        [SerializeField] private LayerMask floorLayers = ~0;
        [SerializeField, Min(0f)] private float surfaceClearance;
        [SerializeField] private EnvironmentalAreaShape impactShape = EnvironmentalAreaShape.Box;
        [SerializeField, Min(0.05f)] private float impactRadius = 2f;
        [SerializeField] private Vector3 impactBoxSize = new Vector3(4f, 2f, 2.5f);
        [SerializeField] private bool becomesNavMeshObstacle = true;
        [SerializeField] private NavMeshObstacle landedObstacle;
        [SerializeField, Min(0.05f)] private float fallDuration = 0.45f;
        [SerializeField, Min(0f)] private float impactDamage = 500f;
        [SerializeField, Min(0f)] private float impactForce = 8f;
        [SerializeField] private bool spawnImpactParticles = true;
        [SerializeField] private Color impactParticleColor = new Color(0.62f, 0.54f, 0.42f, 0.9f);
        [SerializeField, Min(0.1f)] private float impactParticleAmount = 1f;
        [SerializeField] private Color impactPulseColor = new Color(1f, 0.5f, 0.08f, 0.24f);
        [SerializeField] private LayerMask affectedLayers = ~0;

        public override EnvironmentalInteractionType Type => EnvironmentalInteractionType.Drop;
        public Transform DropObject => dropObject;
        public float DropDelay => dropDelay;
        public float ForwardTravelDistance => forwardTravelDistance;
        public Transform ImpactZone => impactZone;
        public LayerMask FloorLayers => floorLayers;
        public EnvironmentalAreaShape ImpactShape => impactShape;
        public float ImpactRadius => impactRadius;
        public Vector3 ImpactBoxSize => impactBoxSize;
        public bool BecomesNavMeshObstacle => becomesNavMeshObstacle;
        public NavMeshObstacle LandedObstacle => landedObstacle;
        public float FallDuration => fallDuration;
        public float ImpactDamage => impactDamage;
        public float ImpactForce => impactForce;
        public bool SpawnImpactParticles => spawnImpactParticles;
        public Color ImpactParticleColor => impactParticleColor;
        public float ImpactParticleAmount => impactParticleAmount;
        public Color ImpactPulseColor => impactPulseColor;
        public Vector3 DropStartPosition => dropObject != null ? dropObject.position : transform.position;
        public Vector3 ImpactPosition => TryResolveImpactPosition(out Vector3 position)
            ? position
            : DropStartPosition;

        protected override void Awake()
        {
            base.Awake();
            SetLandedObstacleActive(false, true);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            dropDelay = Mathf.Max(0f, dropDelay);
            forwardTravelDistance = Mathf.Max(0f, forwardTravelDistance);
            surfaceClearance = Mathf.Max(0f, surfaceClearance);
            impactRadius = Mathf.Max(0.05f, impactRadius);
            impactBoxSize = MaxVector(impactBoxSize, 0.05f);
            fallDuration = Mathf.Max(0.05f, fallDuration);
            impactDamage = Mathf.Max(0f, impactDamage);
            impactForce = Mathf.Max(0f, impactForce);
            impactParticleAmount = Mathf.Max(0.1f, impactParticleAmount);
            SynchronizeImpactZone();
            SetLandedObstacleActive(false, false);
        }

        public void Configure(
            Transform configuredDropObject,
            Transform configuredImpactZone,
            NavMeshObstacle configuredLandedObstacle = null)
        {
            dropObject = configuredDropObject;
            impactZone = configuredImpactZone;
            landedObstacle = configuredLandedObstacle;
            SynchronizeImpactZone();
            SetLandedObstacleActive(false, false);
        }

        public void ConfigureLandedObstacle(NavMeshObstacle configuredLandedObstacle)
        {
            landedObstacle = configuredLandedObstacle;
            SetLandedObstacleActive(false, false);
        }

        public bool TryResolveImpactPosition(out Vector3 position)
        {
            Vector3 origin = DropStartPosition + GetForwardDirection() * forwardTravelDistance;
            RaycastHit[] hits = Physics.RaycastAll(
                origin,
                Vector3.down,
                Mathf.Infinity,
                floorLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            foreach (RaycastHit hit in hits)
            {
                Collider surface = hit.collider;
                if (surface == null || IsIgnoredFloorCandidate(surface))
                    continue;
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.25f)
                    continue;

                position = hit.point + Vector3.up * (GetDropHalfHeight() + surfaceClearance);
                return true;
            }

            position = origin;
            return false;
        }

        private Vector3 GetForwardDirection()
        {
            Vector3 forward = dropObject != null ? dropObject.forward : transform.forward;
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }

        public void SetImpactRadius(float radius)
        {
            impactRadius = Mathf.Max(0.05f, radius);
            SynchronizeImpactZone();
        }

        public void SetImpactBoxSize(Vector3 size)
        {
            impactBoxSize = MaxVector(size, 0.05f);
            SynchronizeImpactZone();
        }

        public void SynchronizeImpactZone()
        {
            SynchronizeGeneratedBlockout();
            if (impactZone == null)
                return;

            if (TryResolveImpactPosition(out Vector3 position))
                impactZone.position = position;
            if (dropObject != null)
                impactZone.rotation = dropObject.rotation;

            EnvironmentalAreaVisual areaVisual = impactZone.GetComponent<EnvironmentalAreaVisual>();
            if (areaVisual != null)
                areaVisual.Synchronize(
                    impactShape,
                    impactBoxSize,
                    impactRadius,
                    PreviewColor(impactPulseColor));
        }

        private void SynchronizeGeneratedBlockout()
        {
            if (Application.isPlaying ||
                impactShape != EnvironmentalAreaShape.Box ||
                dropObject == null ||
                dropObject.name != "DropObject")
            {
                return;
            }

            Vector3 parentScale = dropObject.parent != null
                ? dropObject.parent.lossyScale
                : Vector3.one;
            dropObject.localScale = new Vector3(
                DivideScale(impactBoxSize.x, parentScale.x),
                DivideScale(impactBoxSize.y, parentScale.y),
                DivideScale(impactBoxSize.z, parentScale.z));
            Physics.SyncTransforms();

            Transform triggerTransform = Trigger != null ? Trigger.TriggerTransform : null;
            if (triggerTransform == null ||
                triggerTransform.name != "Trigger" ||
                !triggerTransform.IsChildOf(transform))
            {
                return;
            }

            if (!TryGetWorldBounds(dropObject, out Bounds dropBounds) ||
                !TryGetWorldBounds(triggerTransform, out Bounds triggerBounds))
            {
                return;
            }

            Vector3 triggerPosition = triggerTransform.position;
            triggerPosition.x = dropBounds.center.x;
            triggerPosition.y = dropBounds.max.y + triggerBounds.extents.y;
            triggerPosition.z = dropBounds.center.z;
            triggerTransform.position = triggerPosition;
            Physics.SyncTransforms();
        }

        private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider currentCollider in colliders)
            {
                if (currentCollider == null || currentCollider.isTrigger)
                    continue;

                if (!hasBounds)
                {
                    bounds = currentCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(currentCollider.bounds);
                }
            }

            if (hasBounds)
                return true;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer currentRenderer in renderers)
            {
                if (currentRenderer == null)
                    continue;

                if (!hasBounds)
                {
                    bounds = currentRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(currentRenderer.bounds);
                }
            }
            return hasBounds;
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

        protected override void ActivateEffect()
        {
            StartCoroutine(DropRoutine());
        }

        private IEnumerator DropRoutine()
        {
            SetLandedObstacleActive(false, true);
            if (dropDelay > 0f)
                yield return new WaitForSeconds(dropDelay);

            Vector3 start = DropStartPosition;
            Physics.SyncTransforms();
            if (!TryResolveImpactPosition(out Vector3 destination))
            {
                Debug.LogWarning(
                    $"DROP '{name}' could not find a solid floor beneath its drop object.",
                    this);
                NotifyEffectCompleted();
                yield break;
            }

            if (impactZone != null)
                impactZone.position = destination;
            if (dropObject != null)
            {
                float elapsed = 0f;
                while (elapsed < fallDuration)
                {
                    elapsed += Time.deltaTime;
                    float progress = Mathf.Clamp01(elapsed / fallDuration);
                    float accelerated = progress * progress;
                    dropObject.position = Vector3.LerpUnclamped(start, destination, accelerated);
                    yield return null;
                }
                dropObject.position = destination;
            }
            Physics.SyncTransforms();
            SetLandedObstacleActive(true, true);

            Quaternion areaRotation = dropObject != null
                ? dropObject.rotation
                : impactZone != null
                    ? impactZone.rotation
                    : transform.rotation;
            EnvironmentalRuntimeEffects.ApplyAreaImpact(
                destination,
                areaRotation,
                impactShape,
                impactRadius,
                impactBoxSize,
                impactDamage,
                impactForce,
                affectedLayers);
            if (spawnImpactParticles)
            {
                Vector3 floorContact = destination - Vector3.up * GetDropHalfHeight();
                Vector2 footprint = impactShape == EnvironmentalAreaShape.Box
                    ? new Vector2(impactBoxSize.x, impactBoxSize.z)
                    : Vector2.one * (impactRadius * 2f);
                EnvironmentalRuntimeEffects.SpawnDropImpactParticles(
                    floorContact,
                    areaRotation,
                    footprint,
                    impactParticleColor,
                    impactParticleAmount);
            }
            EnvironmentalRuntimeEffects.SpawnSpherePulse(
                destination,
                impactShape == EnvironmentalAreaShape.Sphere
                    ? impactRadius
                    : Mathf.Max(impactBoxSize.x, impactBoxSize.z) * 0.5f,
                impactPulseColor);
            NotifyEffectCompleted();
        }

        private void SetLandedObstacleActive(bool enabled, bool allowCreate)
        {
            if (dropObject == null)
                return;

            if (landedObstacle == null)
                landedObstacle = dropObject.GetComponent<NavMeshObstacle>();
            if (landedObstacle == null && allowCreate && becomesNavMeshObstacle)
                landedObstacle = dropObject.gameObject.AddComponent<NavMeshObstacle>();
            if (landedObstacle == null)
                return;

            ConfigureLandedObstacleGeometry();
            landedObstacle.enabled = enabled && becomesNavMeshObstacle;
        }

        private void ConfigureLandedObstacleGeometry()
        {
            if (landedObstacle == null || dropObject == null)
                return;

            landedObstacle.shape = NavMeshObstacleShape.Box;
            BoxCollider boxCollider = dropObject.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                landedObstacle.center = boxCollider.center;
                landedObstacle.size = boxCollider.size;
            }
            else if (TryGetWorldBounds(dropObject, out Bounds bounds))
            {
                landedObstacle.center = dropObject.InverseTransformPoint(bounds.center);
                Vector3 scale = dropObject.lossyScale;
                landedObstacle.size = new Vector3(
                    DivideScale(bounds.size.x, scale.x),
                    DivideScale(bounds.size.y, scale.y),
                    DivideScale(bounds.size.z, scale.z));
            }

            landedObstacle.carving = true;
            landedObstacle.carveOnlyStationary = true;
            landedObstacle.carvingMoveThreshold = 0.05f;
            landedObstacle.carvingTimeToStationary = 0.1f;
        }

        private bool IsIgnoredFloorCandidate(Collider candidate)
        {
            Transform candidateTransform = candidate.transform;
            if (candidateTransform == transform || candidateTransform.IsChildOf(transform))
                return true;
            if (candidate.GetComponentInParent<DropInteraction>() != null)
                return true;
            if (candidate.GetComponentInParent<EnvironmentalTrigger>() != null)
                return true;
            return candidate.GetComponentInParent<ZombieMovement>() != null;
        }

        private float GetDropHalfHeight()
        {
            if (dropObject == null)
                return 0f;
            return TryGetWorldBounds(dropObject, out Bounds bounds) ? bounds.extents.y : 0f;
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
