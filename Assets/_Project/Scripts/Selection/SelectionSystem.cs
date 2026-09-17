using System.Collections.Generic;
using UnityEngine;

namespace AoEMini.Selection
{
    /// <summary>
    /// Manages currently selected units.
    /// </summary>
    public sealed class SelectionSystem : MonoBehaviour
    {
        public static SelectionSystem Instance { get; private set; }

        private readonly List<SelectableUnit> _selectedUnits = new();

        public IReadOnlyList<SelectableUnit> SelectedUnits =>
            _selectedUnits;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ClearSelection()
        {
            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                SelectableUnit unit = _selectedUnits[i];

                if (unit != null)
                    unit.SetSelected(false);
            }

            _selectedUnits.Clear();
        }

        public void SelectSingle(SelectableUnit unit)
        {
            if (unit == null)
                return;

            ClearSelection();

            unit.SetSelected(true);
            _selectedUnits.Add(unit);
        }

        public void SelectInRectangle(
            Vector2 screenStart,
            Vector2 screenEnd,
            Camera camera)
        {
            if (camera == null)
                return;

            ClearSelection();

            Vector2 min =
                Vector2.Min(screenStart, screenEnd);

            Vector2 max =
                Vector2.Max(screenStart, screenEnd);

            SelectableUnit[] units =
                FindObjectsByType<SelectableUnit>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None);

            for (int i = 0; i < units.Length; i++)
            {
                SelectableUnit unit = units[i];

                if (unit == null)
                    continue;

                Vector3 screenPosition =
                    camera.WorldToScreenPoint(
                        unit.transform.position);

                // Unit is behind the camera.
                if (screenPosition.z < 0f)
                    continue;

                bool inside =
                    screenPosition.x >= min.x &&
                    screenPosition.x <= max.x &&
                    screenPosition.y >= min.y &&
                    screenPosition.y <= max.y;

                if (!inside)
                    continue;

                unit.SetSelected(true);
                _selectedUnits.Add(unit);
            }
        }

        public void SelectUnits(
            IReadOnlyList<SelectableUnit> units)
        {
            ClearSelection();

            if (units == null)
                return;

            for (int i = 0; i < units.Count; i++)
            {
                SelectableUnit unit = units[i];

                if (unit == null)
                    continue;

                unit.SetSelected(true);
                _selectedUnits.Add(unit);
            }
        }

    }
}