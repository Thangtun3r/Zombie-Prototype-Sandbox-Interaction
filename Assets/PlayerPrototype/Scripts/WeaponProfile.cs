using UnityEngine;

namespace PlayerPrototype
{
    [CreateAssetMenu(fileName = "WeaponProfile", menuName = "Zombie Prototype/Weapon Profile")]
    public sealed class WeaponProfile : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Weapon";
        [SerializeField] private int sortOrder;
        [SerializeField] private Color editorColor = Color.gray;

        [Header("Ballistics")]
        [SerializeField, Min(0f)] private float damagePerPellet = 34f;
        [SerializeField, Min(0.01f)] private float fireRate = 5f;
        [SerializeField, Min(0.1f)] private float range = 100f;
        [SerializeField, Min(0f)] private float knockbackImpulse = 1.5f;
        [SerializeField, Min(1)] private int pelletCount = 1;
        [SerializeField, Range(0f, 30f)] private float spreadAngle;
        [SerializeField, Min(1f)] private float headshotMultiplier = 2f;
        [SerializeField] private bool automatic;

        [Header("Magazine")]
        [SerializeField, Min(1)] private int magazineSize = 12;
        [SerializeField, Min(0.05f)] private float reloadDuration = 1.25f;

        [Header("Feel")]
        [SerializeField, Min(0.001f)] private float recoilDistance = 0.05f;

        public string DisplayName { get => displayName; set => displayName = value; }
        public int SortOrder { get => sortOrder; set => sortOrder = value; }
        public Color EditorColor { get => editorColor; set => editorColor = value; }
        public float DamagePerPellet { get => damagePerPellet; set => damagePerPellet = Mathf.Max(0f, value); }
        public float FireRate { get => fireRate; set => fireRate = Mathf.Max(0.01f, value); }
        public float Range { get => range; set => range = Mathf.Max(0.1f, value); }
        public float KnockbackImpulse { get => knockbackImpulse; set => knockbackImpulse = Mathf.Max(0f, value); }
        public int PelletCount { get => pelletCount; set => pelletCount = Mathf.Max(1, value); }
        public float SpreadAngle { get => spreadAngle; set => spreadAngle = Mathf.Clamp(value, 0f, 30f); }
        public float HeadshotMultiplier { get => headshotMultiplier; set => headshotMultiplier = Mathf.Max(1f, value); }
        public bool Automatic { get => automatic; set => automatic = value; }
        public int MagazineSize { get => magazineSize; set => magazineSize = Mathf.Max(1, value); }
        public float ReloadDuration { get => reloadDuration; set => reloadDuration = Mathf.Max(0.05f, value); }
        public float RecoilDistance { get => recoilDistance; set => recoilDistance = Mathf.Max(0.001f, value); }
    }
}
