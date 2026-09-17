using UnityEngine;

namespace AoEMini.Selection
{
    public sealed class SelectableUnit : MonoBehaviour
    {
        [Header("Selection")]
        [Tooltip("Optional. Object shown when this unit is selected.")]
        [SerializeField] private GameObject _selectionIndicator;

        public bool IsSelected { get; private set; }

        private void Awake()
        {
            HideIndicator();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (_selectionIndicator != null)
                _selectionIndicator.SetActive(selected);

            Debug.Log($"{gameObject.name} selection state changed to: {selected}");
        }

        private void HideIndicator()
        {
            if (_selectionIndicator != null)
                _selectionIndicator.SetActive(false);
        }
    }
}