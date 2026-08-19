using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using ZombiePrototype;

namespace EnvironmentInteraction.Authoring
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class EnvironmentalZombieRagdoll : MonoBehaviour
    {
        private bool active;
        private bool lethal;
        private ZombieMovement movement;
        private ZombieAttack attack;
        private NavMeshAgent agent;
        private Rigidbody body;
        private bool movementWasEnabled;
        private bool attackWasEnabled;
        private bool agentWasEnabled;
        private bool bodyWasKinematic;
        private bool bodyUsedGravity;
        private bool bodyDetectedCollisions;
        private RigidbodyInterpolation bodyInterpolation;
        private CollisionDetectionMode bodyCollisionDetection;
        private RigidbodyConstraints bodyConstraints;
        private Quaternion standingRotation;
        private Vector3 flowDirection;
        private Vector3 flowToppleAxis;
        private bool flowRotationSettled;

        public bool IsActive => active;
        public bool IsLethal => lethal;

        public static bool Activate(
            ZombieMovement movement,
            Vector3 directionalImpulse,
            float upwardForce,
            float tumbleTorque,
            float recoveryOrDisappearDelay,
            bool killEnemy)
        {
            if (movement == null || !movement.gameObject.activeInHierarchy)
                return false;

            EnvironmentalZombieRagdoll ragdoll =
                movement.GetComponent<EnvironmentalZombieRagdoll>();
            if (ragdoll == null)
                ragdoll = movement.gameObject.AddComponent<EnvironmentalZombieRagdoll>();
            if (ragdoll.active)
            {
                if (!killEnemy || ragdoll.lethal)
                    return false;

                ragdoll.BecomeLethal(recoveryOrDisappearDelay);
                ragdoll.ApplyImpulse(directionalImpulse, upwardForce, tumbleTorque);
                return true;
            }

            ragdoll.Begin(
                movement,
                directionalImpulse,
                upwardForce,
                tumbleTorque,
                recoveryOrDisappearDelay,
                killEnemy);
            return true;
        }

        private void Begin(
            ZombieMovement movement,
            Vector3 directionalImpulse,
            float upwardForce,
            float tumbleTorque,
            float recoveryOrDisappearDelay,
            bool killEnemy)
        {
            active = true;
            lethal = killEnemy;
            this.movement = movement;
            movementWasEnabled = movement.enabled;
            standingRotation = GetStandingRotation(transform.forward);

            ZombieHealth health = GetComponent<ZombieHealth>();
            if (lethal && health != null)
                health.Kill(false);

            attack = GetComponent<ZombieAttack>();
            if (attack != null)
            {
                attackWasEnabled = attack.enabled;
                attack.enabled = false;
            }

            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agentWasEnabled = agent.enabled;
                if (agent.enabled)
                {
                    if (agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                        agent.isStopped = true;
                    }
                    agent.enabled = false;
                }
            }
            movement.enabled = false;

            EnvironmentalTemporarySlow slow = GetComponent<EnvironmentalTemporarySlow>();
            if (slow != null)
                slow.enabled = false;
            EnvironmentalTazeFeedback taze = GetComponent<EnvironmentalTazeFeedback>();
            if (taze != null)
                taze.enabled = false;

            body = GetComponent<Rigidbody>();
            if (body == null)
                body = gameObject.AddComponent<Rigidbody>();
            bodyWasKinematic = body.isKinematic;
            bodyUsedGravity = body.useGravity;
            bodyDetectedCollisions = body.detectCollisions;
            bodyInterpolation = body.interpolation;
            bodyCollisionDetection = body.collisionDetectionMode;
            bodyConstraints = body.constraints;
            body.isKinematic = false;
            body.useGravity = true;
            body.detectCollisions = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.constraints = RigidbodyConstraints.None;

            ApplyImpulse(directionalImpulse, upwardForce, tumbleTorque);

            if (!Application.isPlaying)
                return;

            float delay = Mathf.Max(0f, recoveryOrDisappearDelay);
            StartCoroutine(lethal ? DisappearAfter(delay) : RecoverAfter(delay));
        }

        private void ApplyImpulse(
            Vector3 directionalImpulse,
            float upwardForce,
            float tumbleTorque)
        {
            if (body == null)
                return;

            Vector3 impulse = directionalImpulse;
            impulse.y = 0f;

            if (!lethal)
            {
                ConfigureFlowKnockdown(impulse, tumbleTorque);
                return;
            }

            impulse += Vector3.up * Mathf.Max(0f, upwardForce);
            body.AddForce(impulse, ForceMode.Impulse);

            float torque = Mathf.Max(0f, tumbleTorque);
            if (torque > 0f)
            {
                Vector3 tumbleAxis = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.35f, 0.35f),
                    Random.Range(-1f, 1f)).normalized;
                body.AddTorque(tumbleAxis * torque, ForceMode.Impulse);
            }
        }

        private void ConfigureFlowKnockdown(Vector3 impulse, float toppleTorque)
        {
            flowDirection = impulse.sqrMagnitude > 0.0001f
                ? impulse.normalized
                : Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            if (flowDirection.sqrMagnitude < 0.0001f)
                flowDirection = Vector3.forward;

            flowToppleAxis = Vector3.Cross(Vector3.up, flowDirection).normalized;
            flowRotationSettled = false;
            body.angularVelocity = Vector3.zero;
            body.AddForce(impulse, ForceMode.Impulse);

            float torque = Mathf.Max(0f, toppleTorque);
            if (torque > 0f)
                body.AddTorque(flowToppleAxis * torque, ForceMode.Impulse);
        }

        private void FixedUpdate()
        {
            if (!active || lethal || body == null || flowDirection.sqrMagnitude < 0.0001f)
                return;

            Vector3 velocity = body.linearVelocity;
            float forwardSpeed = Mathf.Max(0f, Vector3.Dot(
                Vector3.ProjectOnPlane(velocity, Vector3.up),
                flowDirection));
            float downwardSpeed = Mathf.Min(0f, velocity.y);
            body.linearVelocity = flowDirection * forwardSpeed + Vector3.up * downwardSpeed;

            if (!flowRotationSettled && Mathf.Abs(Vector3.Dot(transform.up, Vector3.up)) <= 0.2f)
            {
                flowRotationSettled = true;
                body.angularVelocity = Vector3.zero;
                body.constraints = RigidbodyConstraints.FreezeRotation;
                return;
            }

            if (flowRotationSettled)
            {
                body.angularVelocity = Vector3.zero;
                return;
            }

            float toppleSpeed = Mathf.Max(0f, Vector3.Dot(body.angularVelocity, flowToppleAxis));
            body.angularVelocity = flowToppleAxis * toppleSpeed;
        }

        private void BecomeLethal(float disappearDelay)
        {
            lethal = true;
            flowRotationSettled = false;
            if (body != null)
                body.constraints = RigidbodyConstraints.None;
            ZombieHealth health = GetComponent<ZombieHealth>();
            if (health != null)
                health.Kill(false);

            StopAllCoroutines();
            if (Application.isPlaying)
                StartCoroutine(DisappearAfter(Mathf.Max(0f, disappearDelay)));
        }

        private IEnumerator RecoverAfter(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            CompleteRecovery();
        }

        private void CompleteRecovery()
        {
            if (!active || lethal)
                return;

            ZombieHealth health = GetComponent<ZombieHealth>();
            if (health != null && health.IsDead)
                return;

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = bodyUsedGravity;
                body.detectCollisions = bodyDetectedCollisions;
                body.interpolation = bodyInterpolation;
                body.collisionDetectionMode = bodyCollisionDetection;
                body.constraints = bodyConstraints;
                body.isKinematic = bodyWasKinematic;
            }

            transform.rotation = standingRotation;
            if (agent != null && agentWasEnabled)
            {
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit nearbyPoint, 4f, agent.areaMask))
                {
                    transform.position = nearbyPoint.position;
                    agent.enabled = true;
                    if (agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                        agent.isStopped = false;
                    }
                }
            }

            if (movement != null)
                movement.enabled = movementWasEnabled;
            if (attack != null)
                attack.enabled = attackWasEnabled;

            active = false;
            if (Application.isPlaying)
                Destroy(this);
            else
                DestroyImmediate(this);
        }

        private IEnumerator DisappearAfter(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        private static Quaternion GetStandingRotation(Vector3 forward)
        {
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f
                ? Quaternion.LookRotation(forward.normalized, Vector3.up)
                : Quaternion.identity;
        }
    }
}
