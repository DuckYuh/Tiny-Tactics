using System.Collections.Generic;
using UnityEngine;
using AoEMini.Selection;

namespace AoEMini.Army
{
    /// <summary>
    /// Stores and recalls player army control groups.
    /// Supports groups 1, 2 and 3.
    /// </summary>
    public sealed class ArmyHotkeySystem : MonoBehaviour
    {
        public static ArmyHotkeySystem Instance { get; private set; }

        private const int GroupCount = 3;

        private readonly List<SelectableUnit>[] _groups =
        {
            new(),
            new(),
            new()
        };

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

        public void AssignGroup(int groupIndex)
        {
            if (!IsValidGroup(groupIndex))
                return;

            if (SelectionSystem.Instance == null)
                return;

            List<SelectableUnit> group =
                _groups[groupIndex];

            group.Clear();

            IReadOnlyList<SelectableUnit> selected =
                SelectionSystem.Instance.SelectedUnits;

            for (int i = 0; i < selected.Count; i++)
            {
                SelectableUnit unit = selected[i];

                if (unit != null)
                    group.Add(unit);
            }

            Debug.Log(
                $"Assign Group {groupIndex + 1}: " +
                $"{group.Count} units");
        }

        public void RecallGroup(int groupIndex)
        {
            if (!IsValidGroup(groupIndex))
                return;

            if (SelectionSystem.Instance == null)
                return;

            List<SelectableUnit> group =
                _groups[groupIndex];

            // Remove destroyed units.
            for (int i = group.Count - 1; i >= 0; i--)
            {
                if (group[i] == null)
                    group.RemoveAt(i);
            }

            SelectionSystem.Instance.SelectUnits(group);

            Debug.Log(
                $"Recall Group {groupIndex + 1}: " +
                $"{group.Count} units");
        }

        private bool IsValidGroup(int groupIndex)
        {
            return groupIndex >= 0 &&
                   groupIndex < GroupCount;
        }
    }
}