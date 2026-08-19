using System;
using UnityEngine;
using UnityEngine.Events;

namespace EnvironmentInteraction.Authoring
{
    [DisallowMultipleComponent]
    public abstract class EnvironmentalInteractionBase : MonoBehaviour
    {
        [SerializeField] private string interactionId;
        [SerializeField] private string displayName = "Environmental Interaction";
        [SerializeField] private bool interactionEnabled = true;
        [SerializeField] private bool isOneUse = true;
        [SerializeField] private EnvironmentalTrigger trigger;
        [SerializeField] private Renderer[] objectRenderers;
        [SerializeField] private Color objectColor = Color.white;
        [SerializeField] private Color triggerColor = new Color(0.85f, 0.08f, 0.04f, 1f);
        [SerializeField] private bool showSceneGizmos = true;
        [SerializeField] private bool showLabels = true;
        [SerializeField] private UnityEvent onActivated;
        [SerializeField] private UnityEvent onEffectCompleted;
        [SerializeField, TextArea(3, 8)] private string designerNotes;

        private bool hasActivated;

        public abstract EnvironmentalInteractionType Type { get; }

        public string InteractionId => interactionId;
        public string DisplayName => displayName;
        public bool InteractionEnabled => interactionEnabled;
        public bool IsOneUse => isOneUse;
        public EnvironmentalTrigger Trigger => trigger;
        public Renderer[] ObjectRenderers => objectRenderers;
        public Color ObjectColor => objectColor;
        public Color TriggerColor => triggerColor;
        public bool ShowSceneGizmos => showSceneGizmos;
        public bool ShowLabels => showLabels;
        public bool HasActivated => hasActivated;
        public string DesignerNotes => designerNotes;

        protected virtual void Awake()
        {
            hasActivated = false;
            BindTrigger();
            ApplyVisualColors();
            trigger?.ResetRuntimeState(interactionEnabled);
        }

        protected virtual void Reset()
        {
            RegenerateInteractionId();
            displayName = Type + " Interaction";
        }

        protected virtual void OnValidate()
        {
            EnsureInteractionId();
            BindTrigger();
            ApplyVisualColors();
            if (!Application.isPlaying && trigger != null)
                trigger.SetHighlightVisible(interactionEnabled);
        }

        public void ConfigureCommon(string configuredDisplayName, EnvironmentalTrigger configuredTrigger)
        {
            EnsureInteractionId();
            displayName = string.IsNullOrWhiteSpace(configuredDisplayName)
                ? Type + " Interaction"
                : configuredDisplayName;
            trigger = configuredTrigger;
            interactionEnabled = true;
            isOneUse = true;
            BindTrigger();
        }

        public void SetTrigger(EnvironmentalTrigger configuredTrigger)
        {
            trigger = configuredTrigger;
            BindTrigger();
        }

        public void ConfigureVisuals(
            Renderer[] configuredObjectRenderers,
            Color configuredObjectColor,
            Color configuredTriggerColor)
        {
            objectRenderers = configuredObjectRenderers;
            objectColor = configuredObjectColor;
            triggerColor = configuredTriggerColor;
            ApplyVisualColors();
        }

        public void ApplyVisualColors()
        {
            EnvironmentalVisualUtility.ApplyColor(objectRenderers, objectColor);
            trigger?.ApplyVisualColor(triggerColor);
        }

        public bool TryActivate()
        {
            if (!interactionEnabled || !isActiveAndEnabled || (isOneUse && hasActivated))
                return false;

            hasActivated = true;
            if (isOneUse)
                trigger?.Consume();
            onActivated?.Invoke();
            ActivateEffect();
            return true;
        }

        public void ResetActivationState()
        {
            hasActivated = false;
            trigger?.ResetRuntimeState(interactionEnabled);
        }

        protected abstract void ActivateEffect();

        protected void NotifyEffectCompleted()
        {
            onEffectCompleted?.Invoke();
        }

        public void RegenerateInteractionId()
        {
            interactionId = Guid.NewGuid().ToString("N");
        }

        private void EnsureInteractionId()
        {
            if (string.IsNullOrWhiteSpace(interactionId))
                RegenerateInteractionId();
        }

        private void BindTrigger()
        {
            trigger?.Bind(this);
        }
    }
}
