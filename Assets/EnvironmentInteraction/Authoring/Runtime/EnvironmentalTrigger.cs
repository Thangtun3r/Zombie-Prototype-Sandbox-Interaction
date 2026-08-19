using UnityEngine;
using ZombiePrototype;

namespace EnvironmentInteraction.Authoring
{
    [DisallowMultipleComponent]
    public sealed class EnvironmentalTrigger : MonoBehaviour, IDamageable
    {
        [SerializeField, HideInInspector] private EnvironmentalInteractionBase ownerInteraction;
        [SerializeField] private Transform triggerTransform;
        [SerializeField] private Collider triggerCollider;
        [SerializeField] private Renderer triggerVisualReference;
        [SerializeField] private EnvironmentalInteractableHighlight interactableHighlight;
        [SerializeField] private bool disableColliderAfterActivation = true;
        [SerializeField, TextArea(2, 6)] private string triggerNotes;

        private bool consumed;

        public Transform TriggerTransform => triggerTransform != null ? triggerTransform : transform;
        public Collider TriggerCollider => triggerCollider;
        public Renderer TriggerVisualReference => triggerVisualReference;
        public EnvironmentalInteractableHighlight InteractableHighlight => interactableHighlight;
        public EnvironmentalInteractionBase OwnerInteraction => ownerInteraction;
        public bool IsConsumed => consumed;
        public string TriggerNotes => triggerNotes;

        private void Reset()
        {
            triggerTransform = transform;
            triggerCollider = GetComponent<Collider>();
            triggerVisualReference = GetComponentInChildren<Renderer>();
            interactableHighlight = GetComponent<EnvironmentalInteractableHighlight>();
        }

        private void OnValidate()
        {
            if (triggerTransform == null)
                triggerTransform = transform;
            if (triggerCollider == null)
                triggerCollider = GetComponent<Collider>();
            if (triggerVisualReference == null)
                triggerVisualReference = GetComponentInChildren<Renderer>();
            if (interactableHighlight == null)
                interactableHighlight = GetComponent<EnvironmentalInteractableHighlight>();
        }

        public void Configure(
            Transform configuredTransform,
            Collider configuredCollider,
            Renderer configuredVisual,
            EnvironmentalInteractableHighlight configuredHighlight)
        {
            triggerTransform = configuredTransform != null ? configuredTransform : transform;
            triggerCollider = configuredCollider;
            triggerVisualReference = configuredVisual;
            interactableHighlight = configuredHighlight;
        }

        public void Bind(EnvironmentalInteractionBase interaction)
        {
            ownerInteraction = interaction;
        }

        public void TakeDamage(float amount)
        {
            if (ownerInteraction == null)
                ownerInteraction = GetComponentInParent<EnvironmentalInteractionBase>();
            if (amount <= 0f || consumed || ownerInteraction == null)
                return;

            ownerInteraction.TryActivate();
        }

        public void Consume()
        {
            consumed = true;
            SetHighlightVisible(false);
            if (disableColliderAfterActivation && triggerCollider != null)
                triggerCollider.enabled = false;
        }

        public void ResetRuntimeState(bool showHighlight)
        {
            consumed = false;
            if (disableColliderAfterActivation && triggerCollider != null)
                triggerCollider.enabled = true;
            SetHighlightVisible(showHighlight);
        }

        public void SetHighlightVisible(bool visible)
        {
            interactableHighlight?.SetHighlighted(visible);
        }

        public void ApplyVisualColor(Color color)
        {
            EnvironmentalVisualUtility.ApplyColor(triggerVisualReference, color);
        }
    }
}
