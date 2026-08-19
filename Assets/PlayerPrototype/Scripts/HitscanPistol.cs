using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ZombiePrototype;

namespace PlayerPrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class HitscanPistol : MonoBehaviour
    {
        [Serializable]
        public sealed class WeaponView
        {
            [SerializeField] private WeaponProfile profile;
            [SerializeField] private Transform visual;
            [SerializeField] private Transform muzzle;
            [SerializeField] private ParticleSystem muzzleFlash;

            public WeaponProfile Profile => profile;
            public Transform Visual => visual;
            public Transform Muzzle => muzzle;
            public ParticleSystem MuzzleFlash => muzzleFlash;

            public WeaponView(WeaponProfile weaponProfile, Transform weaponVisual, Transform muzzleTransform, ParticleSystem flash)
            {
                profile = weaponProfile;
                visual = weaponVisual;
                muzzle = muzzleTransform;
                muzzleFlash = flash;
            }
        }

        private sealed class AccumulatedHit
        {
            public IDamageable Damageable;
            public ZombieMovement Movement;
            public float Damage;
            public Vector3 Position;
            public Vector3 Normal;
            public Vector3 KnockbackDirection;
            public bool IsCritical;
        }

        private sealed class SpreadCandidate
        {
            public IDamageable Damageable;
            public ZombieMovement Movement;
            public Vector3 Point;
            public float Angle = float.MaxValue;
        }

        private struct ShotHit
        {
            public bool HitSomething;
            public Vector3 Point;
            public Vector3 Normal;
            public IDamageable Damageable;
            public ZombieMovement Movement;
            public float DamageMultiplier;
        }

        [Header("Loadout")]
        [SerializeField] private WeaponProfile[] weapons;
        [SerializeField] private WeaponView[] weaponViews;
        [SerializeField] private int startingWeaponIndex;

        [Header("Hit Detection")]
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Shared Feedback")]
        [SerializeField, Min(0.01f)] private float recoilReturnSpeed = 12f;
        [SerializeField, Min(0.01f)] private float tracerDuration = 0.06f;
        [SerializeField, Min(0.001f)] private float tracerWidth = 0.015f;

        private Camera aimCamera;
        private Material tracerMaterial;
        private Material bloodMaterial;
        private int[] ammunition;
        private int currentWeaponIndex;
        private WeaponView currentView;
        private Vector3 weaponRestPosition;
        private float recoilAmount;
        private float nextFireTime;
        private bool isReloading;

        public event Action HitConfirmed;
        public event Action WeaponChanged;

        public WeaponProfile CurrentWeapon => IsValidWeaponIndex(currentWeaponIndex) ? weapons[currentWeaponIndex] : null;
        public string CurrentWeaponName => CurrentWeapon != null ? CurrentWeapon.DisplayName : "No Weapon";
        public int CurrentAmmo => ammunition != null && currentWeaponIndex < ammunition.Length ? ammunition[currentWeaponIndex] : 0;
        public int MagazineSize => CurrentWeapon != null ? CurrentWeapon.MagazineSize : 0;
        public bool IsReloading => isReloading;

        private void Awake()
        {
            aimCamera = GetComponent<Camera>();
            InitializeAmmunition();

            tracerMaterial = CreateEffectMaterial("Weapon Tracer Material", new Color(1f, 0.8f, 0.2f, 1f));
            bloodMaterial = CreateEffectMaterial("Blood Particle Material", new Color(0.55f, 0.01f, 0.01f, 1f));

            SelectWeapon(Mathf.Clamp(startingWeaponIndex, 0, Mathf.Max(0, weapons.Length - 1)));
        }

        private void Update()
        {
            HandleWeaponSelection();

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                BeginReload();

            WeaponProfile profile = CurrentWeapon;
            if (!isReloading && profile != null && Cursor.lockState == CursorLockMode.Locked && Mouse.current != null)
            {
                bool fireInput = profile.Automatic
                    ? Mouse.current.leftButton.isPressed
                    : Mouse.current.leftButton.wasPressedThisFrame;
                if (fireInput)
                    TryFire();
            }

            UpdateWeaponFeedback();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            isReloading = false;
        }

        private void OnDestroy()
        {
            if (tracerMaterial != null)
                Destroy(tracerMaterial);
            if (bloodMaterial != null)
                Destroy(bloodMaterial);
        }

        public void ConfigureLoadout(WeaponProfile[] configuredWeapons, WeaponView[] configuredViews)
        {
            weapons = configuredWeapons ?? Array.Empty<WeaponProfile>();
            weaponViews = configuredViews ?? Array.Empty<WeaponView>();
            startingWeaponIndex = 0;
        }

        public void SelectWeapon(int index)
        {
            if (!IsValidWeaponIndex(index))
                return;

            StopAllCoroutines();
            isReloading = false;
            currentWeaponIndex = index;
            currentView = FindView(CurrentWeapon);

            if (weaponViews != null)
            {
                foreach (WeaponView view in weaponViews)
                {
                    if (view != null && view.Visual != null)
                        view.Visual.gameObject.SetActive(view == currentView);
                }
            }

            if (currentView != null && currentView.Visual != null)
                weaponRestPosition = currentView.Visual.localPosition;
            recoilAmount = 0f;
            WeaponChanged?.Invoke();
        }

        public void TryFire()
        {
            WeaponProfile profile = CurrentWeapon;
            if (profile == null || isReloading || Time.time < nextFireTime)
                return;

            if (CurrentAmmo <= 0)
            {
                BeginReload();
                return;
            }

            ammunition[currentWeaponIndex]--;
            nextFireTime = Time.time + 1f / profile.FireRate;
            recoilAmount = 1f;
            currentView?.MuzzleFlash?.Play();

            Vector3 tracerStart = currentView != null && currentView.Muzzle != null
                ? currentView.Muzzle.position
                : aimCamera.transform.position;
            Dictionary<int, AccumulatedHit> accumulatedHits = new Dictionary<int, AccumulatedHit>();

            for (int pellet = 0; pellet < profile.PelletCount; pellet++)
            {
                Vector3 direction = GetSpreadDirection(profile.SpreadAngle);
                ShotHit hit = TraceShot(new Ray(aimCamera.transform.position, direction), profile.Range, profile.HeadshotMultiplier);
                Vector3 tracerEnd = hit.HitSomething
                    ? hit.Point
                    : aimCamera.transform.position + direction * profile.Range;
                SpawnTracer(tracerStart, tracerEnd);

                if (hit.Damageable != null && hit.Damageable is Component damageableComponent)
                {
                    int id = damageableComponent.gameObject.GetInstanceID();
                    if (!accumulatedHits.TryGetValue(id, out AccumulatedHit accumulated))
                    {
                        accumulated = new AccumulatedHit
                        {
                            Damageable = hit.Damageable,
                            Movement = hit.Movement,
                            Position = hit.Point,
                            Normal = hit.Normal
                        };
                        accumulatedHits.Add(id, accumulated);
                    }

                    accumulated.Damage += profile.DamagePerPellet * hit.DamageMultiplier;
                    accumulated.KnockbackDirection += direction;
                    if (hit.DamageMultiplier > 1f)
                    {
                        accumulated.IsCritical = true;
                        accumulated.Position = hit.Point;
                        accumulated.Normal = hit.Normal;
                    }
                }
            }

            if (profile.PelletCount > 1)
                GuaranteeSpreadCoverage(profile, tracerStart, accumulatedHits);

            foreach (AccumulatedHit accumulated in accumulatedHits.Values)
            {
                Vector3 pushDirection = accumulated.KnockbackDirection.sqrMagnitude > 0.001f
                    ? accumulated.KnockbackDirection.normalized
                    : aimCamera.transform.forward;
                accumulated.Movement?.ApplyKnockback(pushDirection * profile.KnockbackImpulse);
                accumulated.Damageable.TakeDamage(accumulated.Damage);
                if (accumulated.Movement != null)
                    SpawnBlood(accumulated.Position, accumulated.Normal);
                FloatingDamageText.Spawn(
                    accumulated.Position,
                    accumulated.Damage,
                    accumulated.IsCritical);
            }

            if (accumulatedHits.Count > 0)
                HitConfirmed?.Invoke();

            if (CurrentAmmo <= 0)
                BeginReload();
        }

        public void BeginReload()
        {
            WeaponProfile profile = CurrentWeapon;
            if (profile == null || isReloading || CurrentAmmo >= profile.MagazineSize)
                return;

            StartCoroutine(ReloadRoutine(currentWeaponIndex, profile.ReloadDuration));
        }

        private IEnumerator ReloadRoutine(int weaponIndex, float duration)
        {
            isReloading = true;
            yield return new WaitForSeconds(duration);
            if (IsValidWeaponIndex(weaponIndex))
                ammunition[weaponIndex] = weapons[weaponIndex].MagazineSize;
            isReloading = false;
        }

        private void HandleWeaponSelection()
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.digit1Key.wasPressedThisFrame)
                    SelectWeapon(0);
                if (Keyboard.current.digit2Key.wasPressedThisFrame)
                    SelectWeapon(1);
                if (Keyboard.current.digit3Key.wasPressedThisFrame)
                    SelectWeapon(2);
            }

            if (Mouse.current == null || weapons == null || weapons.Length < 2)
                return;

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                int direction = scroll > 0f ? -1 : 1;
                int next = (currentWeaponIndex + direction + weapons.Length) % weapons.Length;
                SelectWeapon(next);
            }
        }

        private ShotHit TraceShot(Ray ray, float range, float headshotMultiplier)
        {
            RaycastHit[] hits = Physics.RaycastAll(ray, range, hitLayers, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
                return default;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            RaycastHit first = hits[0];
            IDamageable damageable = first.collider.GetComponentInParent<IDamageable>();
            ShotHit result = new ShotHit
            {
                HitSomething = true,
                Point = first.point,
                Normal = first.normal,
                Damageable = damageable,
                Movement = first.collider.GetComponentInParent<ZombieMovement>(),
                DamageMultiplier = 1f
            };

            if (damageable == null)
                return result;

            foreach (RaycastHit candidate in hits)
            {
                IDamageable candidateDamageable = candidate.collider.GetComponentInParent<IDamageable>();
                if (!ReferenceEquals(candidateDamageable, damageable))
                    continue;

                ZombieHitbox hitbox = candidate.collider.GetComponent<ZombieHitbox>();
                if (hitbox == null || !hitbox.IsHead)
                    continue;

                result.Point = candidate.point;
                result.Normal = candidate.normal;
                result.DamageMultiplier = headshotMultiplier;
                break;
            }

            return result;
        }

        private void GuaranteeSpreadCoverage(
            WeaponProfile profile,
            Vector3 tracerStart,
            Dictionary<int, AccumulatedHit> accumulatedHits)
        {
            Vector3 origin = aimCamera.transform.position;
            Vector3 forward = aimCamera.transform.forward;
            Collider[] overlaps = Physics.OverlapSphere(
                origin,
                profile.Range,
                hitLayers,
                QueryTriggerInteraction.Ignore);
            Dictionary<int, SpreadCandidate> candidates = new Dictionary<int, SpreadCandidate>();

            foreach (Collider overlap in overlaps)
            {
                IDamageable damageable = overlap.GetComponentInParent<IDamageable>();
                if (!(damageable is Component damageableComponent))
                    continue;

                ZombieMovement movement = damageableComponent.GetComponent<ZombieMovement>();
                if (movement == null)
                    continue;

                int id = damageableComponent.gameObject.GetInstanceID();
                if (accumulatedHits.ContainsKey(id))
                    continue;

                Vector3 boundsCenter = overlap.bounds.center;
                float forwardDistance = Vector3.Dot(boundsCenter - origin, forward);
                if (forwardDistance <= 0f || forwardDistance > profile.Range)
                    continue;

                Vector3 pointOnConeAxis = origin + forward * forwardDistance;
                Vector3 closestPoint = overlap.ClosestPoint(pointOnConeAxis);
                Vector3 offset = closestPoint - origin;
                float distance = offset.magnitude;
                if (distance <= 0.01f || distance > profile.Range)
                    continue;

                float angle = Vector3.Angle(forward, offset);
                if (angle > profile.SpreadAngle)
                    continue;

                if (!candidates.TryGetValue(id, out SpreadCandidate candidate))
                {
                    candidate = new SpreadCandidate
                    {
                        Damageable = damageable,
                        Movement = movement
                    };
                    candidates.Add(id, candidate);
                }

                if (angle < candidate.Angle)
                {
                    candidate.Angle = angle;
                    candidate.Point = closestPoint;
                }
            }

            foreach (KeyValuePair<int, SpreadCandidate> pair in candidates)
            {
                SpreadCandidate candidate = pair.Value;
                if (!TryResolveSpreadImpact(
                        candidate.Damageable,
                        candidate.Point,
                        profile.HeadshotMultiplier,
                        out Vector3 hitPoint,
                        out Vector3 hitNormal,
                        out Vector3 shotDirection,
                        out float damageMultiplier))
                {
                    continue;
                }

                accumulatedHits.Add(pair.Key, new AccumulatedHit
                {
                    Damageable = candidate.Damageable,
                    Movement = candidate.Movement,
                    Damage = profile.DamagePerPellet * damageMultiplier,
                    Position = hitPoint,
                    Normal = hitNormal,
                    KnockbackDirection = shotDirection,
                    IsCritical = damageMultiplier > 1f
                });
                SpawnTracer(tracerStart, hitPoint);
            }
        }

        private bool TryResolveSpreadImpact(
            IDamageable target,
            Vector3 desiredPoint,
            float headshotMultiplier,
            out Vector3 hitPoint,
            out Vector3 hitNormal,
            out Vector3 shotDirection,
            out float damageMultiplier)
        {
            Vector3 origin = aimCamera.transform.position;
            Vector3 offset = desiredPoint - origin;
            float targetDistance = offset.magnitude;
            shotDirection = offset / Mathf.Max(0.001f, targetDistance);
            hitPoint = desiredPoint;
            hitNormal = -shotDirection;
            damageMultiplier = 1f;

            RaycastHit[] hits = Physics.RaycastAll(
                new Ray(origin, shotDirection),
                targetDistance + 0.25f,
                hitLayers,
                QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                IDamageable hitDamageable = hit.collider.GetComponentInParent<IDamageable>();
                if (ReferenceEquals(hitDamageable, target))
                {
                    hitPoint = hit.point;
                    hitNormal = hit.normal;
                    ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
                    if (hitbox != null && hitbox.IsHead)
                        damageMultiplier = headshotMultiplier;
                    return true;
                }

                // Other zombies do not shield targets that are also inside the shotgun ring.
                if (hitDamageable != null)
                    continue;

                // Solid level geometry still blocks the spread coverage.
                return false;
            }

            // ClosestPoint can produce a tangent point that a ray query narrowly misses.
            // The overlap/cone test already proved the target intersects the spread volume.
            return true;
        }

        private Vector3 GetSpreadDirection(float spreadAngle)
        {
            if (spreadAngle <= 0f)
                return aimCamera.transform.forward;

            float spreadRadius = Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
            Vector2 offset = UnityEngine.Random.insideUnitCircle * spreadRadius;
            return (aimCamera.transform.forward +
                    aimCamera.transform.right * offset.x +
                    aimCamera.transform.up * offset.y).normalized;
        }

        private void InitializeAmmunition()
        {
            if (weapons == null)
                weapons = Array.Empty<WeaponProfile>();
            ammunition = new int[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
                ammunition[i] = weapons[i] != null ? weapons[i].MagazineSize : 0;
        }

        private bool IsValidWeaponIndex(int index)
        {
            return weapons != null && index >= 0 && index < weapons.Length && weapons[index] != null;
        }

        private WeaponView FindView(WeaponProfile profile)
        {
            if (weaponViews == null)
                return null;
            foreach (WeaponView view in weaponViews)
            {
                if (view != null && view.Profile == profile)
                    return view;
            }
            return null;
        }

        private void UpdateWeaponFeedback()
        {
            if (currentView == null || currentView.Visual == null || CurrentWeapon == null)
                return;

            recoilAmount = Mathf.MoveTowards(recoilAmount, 0f, recoilReturnSpeed * Time.deltaTime);
            currentView.Visual.localPosition = weaponRestPosition +
                                               Vector3.back * (CurrentWeapon.RecoilDistance * recoilAmount);
        }

        private void SpawnTracer(Vector3 start, Vector3 end)
        {
            GameObject tracerObject = new GameObject("Shot Tracer");
            LineRenderer line = tracerObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.startWidth = tracerWidth;
            line.endWidth = tracerWidth * 0.35f;
            line.sharedMaterial = tracerMaterial;
            line.startColor = new Color(1f, 0.9f, 0.35f, 1f);
            line.endColor = new Color(1f, 0.35f, 0.05f, 0.15f);
            Destroy(tracerObject, tracerDuration);
        }

        private void SpawnBlood(Vector3 position, Vector3 surfaceNormal)
        {
            GameObject bloodObject = new GameObject("Blood Impact");
            bloodObject.transform.position = position;
            bloodObject.transform.rotation = Quaternion.LookRotation(surfaceNormal);

            ParticleSystem particles = bloodObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.12f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.11f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0f, 0f, 1f),
                new Color(0.8f, 0.03f, 0.03f, 1f));
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.025f;

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = bloodMaterial;

            particles.Play();
            Destroy(bloodObject, 1f);
        }

        private static Material CreateEffectMaterial(string materialName, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader)
            {
                name = materialName,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
