using UnityEngine;

namespace ZombiePrototype
{
    [DisallowMultipleComponent]
    public sealed class ZombieHitbox : MonoBehaviour
    {
        [SerializeField] private bool isHead;

        public bool IsHead
        {
            get => isHead;
            set => isHead = value;
        }
    }
}
