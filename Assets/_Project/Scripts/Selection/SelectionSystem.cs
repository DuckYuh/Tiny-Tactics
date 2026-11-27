
using System;
using System.Collections.Generic;
using UnityEngine;
using TinyTactics.Combat;
using TinyTactics.Factions;

namespace AoEMini.Selection
{
    /// <summary>
    /// Quản lý lựa chọn unit của Player.
    /// Player chỉ được chọn unit thuộc phe Blue.
    /// </summary>
    public sealed class SelectionSystem : MonoBehaviour
    {
        public static SelectionSystem Instance { get; private set; }

        public event Action SelectionCompleted;

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

        /// <summary>
        /// Chỉ cho phép chọn unit có HealthSystem thuộc phe Blue.
        /// </summary>
        private bool IsPlayerOwned(SelectableUnit unit)
        {
            if (unit == null)
                return false;

            HealthSystem health = unit.GetComponent<HealthSystem>();

            if (health == null)
                health = unit.GetComponentInParent<HealthSystem>();

            return health != null &&
                   health.TeamFaction == Faction.Blue;
        }

        public void ClearSelection()
        {
            for (int i = 0; i < _selectedUnits.Count; i++)
            {
                if (_selectedUnits[i] != null)
                    _selectedUnits[i].SetSelected(false);
            }

            _selectedUnits.Clear();
        }

        public void SelectSingle(SelectableUnit unit)
        {
            ClearSelection();

            if (!IsPlayerOwned(unit))
                return;

            unit.SetSelected(true);
            _selectedUnits.Add(unit);

            NotifySelectionCompleted();
        }

        public void SelectInRectangle(
            Vector2 screenStart,
            Vector2 screenEnd,
            Camera camera)
        {
            if (camera == null)
                return;

            ClearSelection();

            Vector2 min = Vector2.Min(screenStart, screenEnd);
            Vector2 max = Vector2.Max(screenStart, screenEnd);

            SelectableUnit[] units =
                FindObjectsByType<SelectableUnit>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );

            for (int i = 0; i < units.Length; i++)
            {
                SelectableUnit unit = units[i];

                if (!IsPlayerOwned(unit))
                    continue;

                Vector3 screenPosition =
                    camera.WorldToScreenPoint(unit.transform.position);

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

            NotifySelectionCompleted();
        }

        /// <summary>
        /// Dùng cho ArmyHotkeySystem và các hệ thống chọn theo danh sách.
        /// Vẫn lọc phe để không thể chọn nhầm unit Red.
        /// </summary>
        public void SelectUnits(IReadOnlyList<SelectableUnit> units)
        {
            ClearSelection();

            if (units == null)
                return;

            for (int i = 0; i < units.Count; i++)
            {
                SelectableUnit unit = units[i];

                if (!IsPlayerOwned(unit))
                    continue;

                unit.SetSelected(true);
                _selectedUnits.Add(unit);
            }

            NotifySelectionCompleted();
        }

        private void NotifySelectionCompleted()
        {
            if (_selectedUnits.Count > 0)
                SelectionCompleted?.Invoke();
        }
    }
}
