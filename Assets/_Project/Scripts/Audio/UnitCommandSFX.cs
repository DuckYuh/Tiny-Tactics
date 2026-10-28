
using UnityEngine;
using AoEMini.Selection;
using TinyTactics.Units;

namespace TinyTactics.Audio
{
    /// <summary>
    /// Kết nối sự kiện Selection/Move với SFXManager.
    /// Không can thiệp vào gameplay logic.
    /// </summary>
    public class UnitCommandSFX : MonoBehaviour
    {
        private SelectionSystem selectionSystem;
        private RTSUnitManager unitManager;

        private void OnEnable()
        {
            selectionSystem = SelectionSystem.Instance;
            unitManager = RTSUnitManager.Instance;

            if (selectionSystem != null)
            {
                selectionSystem.SelectionCompleted += OnSelectionCompleted;
            }

            if (unitManager != null)
            {
                unitManager.MoveCommandIssued += OnMoveCommandIssued;
            }
        }

        private void OnDisable()
        {
            if (selectionSystem != null)
            {
                selectionSystem.SelectionCompleted -= OnSelectionCompleted;
            }

            if (unitManager != null)
            {
                unitManager.MoveCommandIssued -= OnMoveCommandIssued;
            }

            selectionSystem = null;
            unitManager = null;
        }

        private void OnSelectionCompleted()
        {
            SFXManager.Instance?.Play(SFXType.UnitSelect);
        }

        private void OnMoveCommandIssued()
        {
            SFXManager.Instance?.Play(SFXType.UnitMove);
        }
    }
}
