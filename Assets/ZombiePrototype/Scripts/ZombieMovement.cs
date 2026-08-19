using UnityEngine;
using UnityEngine.AI;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(ZombieTarget), typeof(NavMeshAgent))]
    public sealed class ZombieMovement : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0f)] private float stoppingDistance = 1.25f;
        [SerializeField, Min(0f)] private float turnSpeed = 360f;
        [SerializeField, Min(0f)] private float knockbackDuration = 0.16f;
        [SerializeField, Min(0.05f)] private float destinationRefreshInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float navMeshSampleRadius = 3f;

        private Rigidbody body;
        private ZombieTarget targetSource;
        private NavMeshAgent agent;
        private Vector3 knockbackVelocity;
        private float resumeMovementTime;
        private float nextDestinationUpdate;

        public float MoveSpeed
        {
            get => moveSpeed;
            set
            {
                moveSpeed = Mathf.Max(0f, value);
                if (agent != null)
                    agent.speed = moveSpeed;
            }
        }

        public float KnockbackDuration
        {
            get => knockbackDuration;
            set => knockbackDuration = Mathf.Max(0f, value);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            targetSource = GetComponent<ZombieTarget>();
            agent = GetComponent<NavMeshAgent>();

            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.constraints = RigidbodyConstraints.FreezeRotation;

            ConfigureAgent();
        }

        private void Update()
        {
            if (!TryPlaceOnNavMesh())
            {
                UpdateDirectFallback();
                return;
            }

            if (Time.time < resumeMovementTime)
            {
                agent.isStopped = true;
                agent.Move(knockbackVelocity * Time.deltaTime);
                return;
            }

            knockbackVelocity = Vector3.zero;
            if (agent.isStopped)
                agent.isStopped = false;

            Transform target = targetSource.Current;
            if (target == null)
            {
                agent.ResetPath();
                return;
            }

            if (Time.time < nextDestinationUpdate)
                return;

            nextDestinationUpdate = Time.time + destinationRefreshInterval;
            if (NavMesh.SamplePosition(target.position, out NavMeshHit targetPoint, navMeshSampleRadius, agent.areaMask))
                agent.SetDestination(targetPoint.position);
        }

        public void ApplyKnockback(Vector3 impulse)
        {
            impulse.y = 0f;
            float mass = body != null ? Mathf.Max(0.1f, body.mass) : 1f;
            knockbackVelocity = impulse / mass;
            resumeMovementTime = Time.time + Mathf.Max(0.02f, knockbackDuration);

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.isStopped = true;
            }
        }

        public void StopImmediately()
        {
            knockbackVelocity = Vector3.zero;
            resumeMovementTime = Time.time;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.ResetPath();
                agent.velocity = Vector3.zero;
                agent.isStopped = true;
            }
        }

        private void ConfigureAgent()
        {
            float scale = Mathf.Max(0.1f, transform.lossyScale.x);
            agent.speed = moveSpeed;
            agent.acceleration = Mathf.Max(8f, moveSpeed * 6f);
            agent.angularSpeed = turnSpeed;
            agent.stoppingDistance = stoppingDistance;
            agent.radius = 0.38f * scale;
            agent.height = 2f * scale;
            agent.baseOffset = 0f;
            agent.autoRepath = true;
            agent.autoBraking = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.LowQualityObstacleAvoidance;
            agent.avoidancePriority = 25 + Mathf.Abs(GetInstanceID()) % 50;
        }

        private bool TryPlaceOnNavMesh()
        {
            if (agent == null || !agent.enabled)
                return false;
            if (agent.isOnNavMesh)
                return true;

            if (!NavMesh.SamplePosition(transform.position, out NavMeshHit nearbyPoint, navMeshSampleRadius, agent.areaMask))
                return false;

            return agent.Warp(nearbyPoint.position);
        }

        private void UpdateDirectFallback()
        {
            Transform target = targetSource.Current;
            if (target == null)
                return;

            Vector3 movement;
            if (Time.time < resumeMovementTime)
            {
                movement = knockbackVelocity * Time.deltaTime;
            }
            else
            {
                Vector3 offset = target.position - transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude <= stoppingDistance * stoppingDistance)
                    return;
                movement = offset.normalized * (moveSpeed * Time.deltaTime);
            }

            transform.position += movement;
            Vector3 horizontal = movement;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude > 0.0001f)
            {
                Quaternion desired = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, turnSpeed * Time.deltaTime);
            }
        }
    }
}
