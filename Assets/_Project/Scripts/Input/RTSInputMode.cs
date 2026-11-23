using System;
using UnityEngine;

namespace TinyTactics.Input
{
    public enum InputModeState
    {
        Normal,
        BuildingPlacement,
        AttackMove
    }

    /// <summary>
    /// Quản lý trạng thái Input đặc biệt của người chơi (Normal, BuildingPlacement, AttackMove).
    /// </summary>
    public static class RTSInputMode
    {
        private static bool _isBuildingPlacementActive;
        private static bool _isAttackMoveActive;
        private static int _lastExitFrame = -1;

        public static event Action<InputModeState> OnInputModeChanged;

        public static bool IsBuildingPlacementActive => _isBuildingPlacementActive;

        public static bool IsAttackMoveActive =>
            !_isBuildingPlacementActive && _isAttackMoveActive;

        public static InputModeState CurrentMode
        {
            get
            {
                if (_isBuildingPlacementActive)
                    return InputModeState.BuildingPlacement;

                if (_isAttackMoveActive)
                    return InputModeState.AttackMove;

                return InputModeState.Normal;
            }
        }

        /// <summary>
        /// Chặn Input điều khiển quân trong chế độ xây.
        /// Giữ chặn đến hết frame hủy xây để tránh
        /// click phải vừa hủy preview vừa ra lệnh di chuyển.
        /// </summary>
        public static bool IsWorldInputBlocked =>
            _isBuildingPlacementActive ||
            Time.frameCount == _lastExitFrame;

        public static void EnterBuildingPlacement()
        {
            if (_isBuildingPlacementActive)
                return;

            _isBuildingPlacementActive = true;
            _isAttackMoveActive = false;
            OnInputModeChanged?.Invoke(CurrentMode);
        }

        public static void ExitBuildingPlacement()
        {
            if (!_isBuildingPlacementActive)
                return;

            _isBuildingPlacementActive = false;
            _lastExitFrame = Time.frameCount;
            OnInputModeChanged?.Invoke(CurrentMode);
        }

        public static void EnterAttackMove()
        {
            if (IsWorldInputBlocked || _isAttackMoveActive)
                return;

            _isAttackMoveActive = true;
            OnInputModeChanged?.Invoke(InputModeState.AttackMove);
        }

        public static void ExitAttackMove()
        {
            if (!_isAttackMoveActive)
                return;

            _isAttackMoveActive = false;
            OnInputModeChanged?.Invoke(CurrentMode);
        }
    }
}
