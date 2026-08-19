using UnityEngine;

namespace ZombiePrototype
{
    [CreateAssetMenu(fileName = "ZombieArchetype", menuName = "Zombie Prototype/Zombie Archetype")]
    public sealed class ZombieArchetype : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Zombie";
        [SerializeField] private int sortOrder;
        [SerializeField] private Color editorColor = new Color(0.35f, 0.65f, 0.3f, 1f);
        [SerializeField] private GameObject prefab;

        [Header("Core Balance")]
        [SerializeField, Min(1f)] private float health = 100f;
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(0f)] private float attackDamage = 10f;
        [SerializeField, Min(0.05f)] private float attackCooldown = 1f;
        [SerializeField, Min(0.1f)] private float bodyMass = 1f;
        [SerializeField, Min(0f)] private float knockbackDuration = 0.16f;

        [Header("Death Explosion")]
        [SerializeField] private bool explodesOnDeath;
        [SerializeField, Min(0.1f)] private float explosionRadius = 4f;
        [SerializeField, Min(0f)] private float explosionDamage = 45f;
        [SerializeField, Min(0f)] private float explosionForce = 4f;
        [SerializeField] private Material explosionMaterial;

        public string DisplayName { get => displayName; set => displayName = value; }
        public int SortOrder { get => sortOrder; set => sortOrder = value; }
        public Color EditorColor { get => editorColor; set => editorColor = value; }
        public GameObject Prefab { get => prefab; set => prefab = value; }
        public float Health { get => health; set => health = Mathf.Max(1f, value); }
        public float MoveSpeed { get => moveSpeed; set => moveSpeed = Mathf.Max(0f, value); }
        public float AttackDamage { get => attackDamage; set => attackDamage = Mathf.Max(0f, value); }
        public float AttackCooldown { get => attackCooldown; set => attackCooldown = Mathf.Max(0.05f, value); }
        public float BodyMass { get => bodyMass; set => bodyMass = Mathf.Max(0.1f, value); }
        public float KnockbackDuration { get => knockbackDuration; set => knockbackDuration = Mathf.Max(0f, value); }
        public bool ExplodesOnDeath { get => explodesOnDeath; set => explodesOnDeath = value; }
        public float ExplosionRadius { get => explosionRadius; set => explosionRadius = Mathf.Max(0.1f, value); }
        public float ExplosionDamage { get => explosionDamage; set => explosionDamage = Mathf.Max(0f, value); }
        public float ExplosionForce { get => explosionForce; set => explosionForce = Mathf.Max(0f, value); }
        public Material ExplosionMaterial { get => explosionMaterial; set => explosionMaterial = value; }
    }
}
