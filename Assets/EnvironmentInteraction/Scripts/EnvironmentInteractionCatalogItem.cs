using UnityEngine;

namespace EnvironmentInteraction
{
    [CreateAssetMenu(
        fileName = "EnvironmentInteraction",
        menuName = "Zombie Prototype/Environment Interaction Catalog Item")]
    public sealed class EnvironmentInteractionCatalogItem : ScriptableObject
    {
        [SerializeField] private string displayName = "Environment Interaction";
        [SerializeField] private int sortOrder;
        [SerializeField] private Color editorColor = new Color(0.85f, 0.3f, 0.08f);
        [SerializeField] private GameObject prefab;

        public string DisplayName
        {
            get => displayName;
            set => displayName = value;
        }

        public int SortOrder
        {
            get => sortOrder;
            set => sortOrder = value;
        }

        public Color EditorColor
        {
            get => editorColor;
            set => editorColor = value;
        }

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }
    }
}
