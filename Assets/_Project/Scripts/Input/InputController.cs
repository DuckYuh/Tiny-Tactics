using System;
using AoEMini.Army;
using AoEMini.Selection;
using System.Collections.Generic;
using TinyTactics.Buildings;
using TinyTactics.Combat;
using TinyTactics.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TinyTactics.Economy;
using TinyTactics.Units;

namespace AoEMini.Input
{
    /// <summary>
    /// Converts Unity Input System actions into gameplay input.
    ///
    /// Responsibilities:
    /// - Read mouse position.
    /// - Detect selection start/end.
    /// - Detect selection drag.
    /// - Optionally display drag selection box.
    /// - Forward army hotkeys to ArmyHotkeySystem.
    ///
    /// This class does NOT contain gameplay logic.
    /// </summary>
    public sealed class InputController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera _mainCamera;

        [Tooltip("Optional. If not assigned, drag selection still works without visual box.")]
        [SerializeField] private RectTransform _selectionBox;

        [Header("Selection")]
        [SerializeField] private float _dragThreshold = 8f;

        [Header("Building Selection")]
        [SerializeField]
        private BuildingDemolitionController _buildingDemolition;

        [Header("Combat & Attack-Move")]
        [SerializeField] private Faction _playerFaction = Faction.Blue;
        [SerializeField] private Texture2D _attackMoveCursor;
        [SerializeField] private Vector2 _attackMoveCursorHotspot = Vector2.zero;

        public static event Action<bool> OnAttackMoveModeChanged;

        private GameInputActions _input;

        private Vector2 _selectionStart;
        private Vector2 _currentMousePosition;

        private bool _isSelecting;
        private bool _isDragging;

        private readonly List<RaycastResult> _uiRaycastResults =
            new List<RaycastResult>(16);

        private PointerEventData _uiPointerEventData;

        private void Awake()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            _input = new GameInputActions();

            // Selection box is optional.
            // If assigned, make sure it is hidden when the game starts.
            HideSelectionBox();
        }

        private void OnEnable()
        {
            _input.Enable();

            // Command
            _input.Player.Command.performed += OnCommand;

            // Selection
            _input.Player.Select.started += OnSelectStarted;
            _input.Player.Select.canceled += OnSelectCanceled;

            // Army groups
            _input.Player.AssignGroup1.performed += OnAssignGroup1;
            _input.Player.AssignGroup2.performed += OnAssignGroup2;
            _input.Player.AssignGroup3.performed += OnAssignGroup3;

            _input.Player.RecallGroup1.performed += OnRecallGroup1;
            _input.Player.RecallGroup2.performed += OnRecallGroup2;
            _input.Player.RecallGroup3.performed += OnRecallGroup3;

            // Cancel
            _input.Player.Cancel.performed += OnCancel;

            // Combat
            _input.Player.AttackMove.performed += OnAttackMove;
            RTSInputMode.OnInputModeChanged += HandleInputModeChanged;
        }

        private void OnDisable()
        {
            // Command
            _input.Player.Command.performed -= OnCommand;

            // Selection
            _input.Player.Select.started -= OnSelectStarted;
            _input.Player.Select.canceled -= OnSelectCanceled;

            // Army groups
            _input.Player.AssignGroup1.performed -= OnAssignGroup1;
            _input.Player.AssignGroup2.performed -= OnAssignGroup2;
            _input.Player.AssignGroup3.performed -= OnAssignGroup3;

            _input.Player.RecallGroup1.performed -= OnRecallGroup1;
            _input.Player.RecallGroup2.performed -= OnRecallGroup2;
            _input.Player.RecallGroup3.performed -= OnRecallGroup3;

            // Cancel
            _input.Player.Cancel.performed -= OnCancel;

            // Combat
            _input.Player.AttackMove.performed -= OnAttackMove;
            RTSInputMode.OnInputModeChanged -= HandleInputModeChanged;

            if (RTSInputMode.IsAttackMoveActive)
            {
                RTSInputMode.ExitAttackMove();
            }

            ApplyAttackMoveCursor(false);

            _input.Disable();
        }

        private void Update()
        {
            UpdateMousePosition();
            UpdateDragState();
        }

        // =========================================================
        // MOUSE INPUT
        // =========================================================

        /// <summary>
        /// Kiểm tra vị trí màn hình có đang nằm trên UI
        /// nhận Raycast hay không.
        /// Dùng được trong InputAction callback.
        /// </summary>
        private bool IsPointerOverUI(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
                return false;

            if (_uiPointerEventData == null)
            {
                _uiPointerEventData = new PointerEventData(eventSystem);
            }

            _uiPointerEventData.position = screenPosition;

            _uiRaycastResults.Clear();

            eventSystem.RaycastAll(
                _uiPointerEventData,
                _uiRaycastResults);

            bool isOverUI = _uiRaycastResults.Count > 0;

            _uiRaycastResults.Clear();

            return isOverUI;
        }

        private void UpdateMousePosition()
        {
            _currentMousePosition =
                _input.Player.Point.ReadValue<Vector2>();
        }

        private void UpdateDragState()
        {
            if (RTSInputMode.IsWorldInputBlocked)
            {
                _isSelecting = false;
                _isDragging = false;
                HideSelectionBox();
                return;
            }

            if (!_isSelecting)
                return;

            if (!_isDragging)
            {
                float distance =
                    Vector2.Distance(
                        _selectionStart,
                        _currentMousePosition);

                if (distance >= _dragThreshold)
                {
                    _isDragging = true;

                    // Only show the visual box if one was assigned.
                    ShowSelectionBox();
                }
            }

            // Update visual box while dragging.
            if (_isDragging)
            {
                UpdateSelectionBox(
                    _selectionStart,
                    _currentMousePosition);
            }
        }

        private void OnSelectStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (RTSInputMode.IsWorldInputBlocked)
                return;

            Vector2 screenPosition =
                _input.Player.Point.ReadValue<Vector2>();

            // Không bắt đầu chọn Unit/Building nếu click lên UI.
            if (IsPointerOverUI(screenPosition))
                return;

            if (RTSInputMode.IsAttackMoveActive)
            {
                ExecuteAttackMoveAtScreenPosition(screenPosition);
                _isSelecting = false;
                _isDragging = false;
                HideSelectionBox();
                return;
            }

            _selectionStart = screenPosition;
            _isSelecting = true;
            _isDragging = false;
        }

        private void OnSelectCanceled(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!_isSelecting)
                return;

            // Luôn dọn trạng thái kéo trước.
            if (RTSInputMode.IsWorldInputBlocked)
            {
                _isSelecting = false;
                _isDragging = false;
                HideSelectionBox();
                return;
            }

            Vector2 selectionEnd =
                _input.Player.Point.ReadValue<Vector2>();

            if (_isDragging)
                HandleDragSelection(_selectionStart, selectionEnd);
            else
                HandleSingleSelection(selectionEnd);

            _isSelecting = false;
            _isDragging = false;

            HideSelectionBox();
        }

        // =========================================================
        // COMMAND
        // =========================================================

        private void OnCommand(
    UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (_mainCamera == null)
                return;

            if (RTSInputMode.IsWorldInputBlocked)
                return;

            if (RTSInputMode.IsAttackMoveActive)
            {
                RTSInputMode.ExitAttackMove();
                return;
            }

            if (RTSUnitManager.Instance == null)
            {
                Debug.LogWarning(
                    "InputController: RTSUnitManager not found!"
                );
                return;
            }

            Vector2 screenPosition =
                _input.Player.Point.ReadValue<Vector2>();

            // Ngăn click phải xuyên qua các UI có raycast.
            if (IsPointerOverUI(screenPosition))
                return;

            Vector3 worldPosition = ScreenToWorld2D(screenPosition);

            Collider2D[] hits = Physics2D.OverlapPointAll(
                (Vector2)worldPosition
            );

            // 1. Ưu tiên tấn công Unit hoặc Công trình phe địch nếu click trúng.
            IDamageable enemyTarget = FindHostileTargetAtPoint(hits);
            if (enemyTarget != null)
            {
                RTSUnitManager.Instance.CommandAttackTarget(enemyTarget);
                return;
            }

            // 2. Kiểm tra tài nguyên tại vị trí click.
            for (int i = 0; i < hits.Length; i++)
            {
                ResourceDeposit resource =
                    hits[i].GetComponentInParent<ResourceDeposit>();

                if (resource == null || resource.currentAmount <= 0)
                    continue;

                // Click tài nguyên -> ra lệnh khai thác.
                RTSUnitManager.Instance.CommandHarvest(resource);
                return;
            }

            // 3. Không click địch hay tài nguyên -> di chuyển thông thường.
            RTSUnitManager.Instance.CommandMoveTo(worldPosition);
        }

        // =========================================================
        // SELECTION
        // =========================================================

        private void HandleSingleSelection(Vector2 screenPosition)
        {
            if (RTSInputMode.IsWorldInputBlocked)
                return;

            // Bỏ IsPointerOverGameObject() để tránh warning.
            if (IsPointerOverUI(screenPosition))
                return;

            if (SelectionSystem.Instance == null)
                return;

            // Ưu tiên chọn Unit.
            SelectableUnit unit =
                FindUnitAtScreenPosition(screenPosition);

            if (unit != null)
            {
                _buildingDemolition?.ClearSelection();
                SelectionSystem.Instance.SelectSingle(unit);
                return;
            }

            // Nếu không có Unit, kiểm tra Building.
            if (_buildingDemolition != null &&
                _buildingDemolition.TrySelectAtScreenPosition(screenPosition))
            {
                SelectionSystem.Instance.ClearSelection();
                return;
            }

            // Click vùng trống.
            _buildingDemolition?.ClearSelection();
            SelectionSystem.Instance.ClearSelection();
        }

        private void HandleDragSelection(Vector2 start, Vector2 end)
        {
            if (RTSInputMode.IsWorldInputBlocked)
                return;

            if (SelectionSystem.Instance == null || _mainCamera == null)
                return;

            // Khi quét chọn Unit, bỏ chọn Building.
            _buildingDemolition?.ClearSelection();

            SelectionSystem.Instance.SelectInRectangle(
                start,
                end,
                _mainCamera);
        }

        private SelectableUnit FindUnitAtScreenPosition(
            Vector2 screenPosition)
        {
            if (_mainCamera == null)
                return null;

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(screenPosition);
            Vector2 worldPoint2D = new Vector2(worldPoint.x, worldPoint.y);

            Collider2D hit = Physics2D.OverlapPoint(worldPoint2D);

            if (hit != null)
            {
                return hit.GetComponentInParent<SelectableUnit>();
            }

            return null;
        }

        // =========================================================
        // DRAG SELECTION BOX - OPTIONAL
        // =========================================================

        private void ShowSelectionBox()
        {
            // No SelectionBox assigned -> visual is simply disabled.
            if (_selectionBox == null)
                return;

            _selectionBox.gameObject.SetActive(true);
        }

        private void HideSelectionBox()
        {
            // No SelectionBox assigned -> nothing to hide.
            if (_selectionBox == null)
                return;

            _selectionBox.gameObject.SetActive(false);
        }

        private void UpdateSelectionBox(
            Vector2 start,
            Vector2 end)
        {
            // No SelectionBox assigned.
            // Drag selection logic continues to work normally.
            if (_selectionBox == null)
                return;

            Vector2 min =
                Vector2.Min(start, end);

            Vector2 max =
                Vector2.Max(start, end);

            Vector2 size =
                max - min;

            // Center the UI rectangle between start and end.
            _selectionBox.position =
                (min + max) * 0.5f;

            _selectionBox.sizeDelta = size;
        }

        // =========================================================
        // ARMY GROUPS
        // =========================================================

        private void OnAssignGroup1(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            AssignGroup(0, 1);
        }

        private void OnAssignGroup2(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            AssignGroup(1, 2);
        }

        private void OnAssignGroup3(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            AssignGroup(2, 3);
        }

        private void AssignGroup(
            int groupIndex,
            int displayGroupNumber)
        {
            if (ArmyHotkeySystem.Instance == null)
                return;

            ArmyHotkeySystem.Instance.AssignGroup(groupIndex);

            Debug.Log(
                $"Assign Group {displayGroupNumber}");
        }

        private void OnRecallGroup1(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            RecallGroup(0, 1);
        }

        private void OnRecallGroup2(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            RecallGroup(1, 2);
        }

        private void OnRecallGroup3(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            RecallGroup(2, 3);
        }

        private void RecallGroup(
            int groupIndex,
            int displayGroupNumber)
        {
            if (ArmyHotkeySystem.Instance == null)
                return;

            ArmyHotkeySystem.Instance.RecallGroup(groupIndex);

            Debug.Log(
                $"Recall Group {displayGroupNumber}");
        }

        // =========================================================
        // CANCEL
        // =========================================================

        private void OnCancel(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (RTSInputMode.IsAttackMoveActive)
            {
                RTSInputMode.ExitAttackMove();
                return;
            }

            if (SelectionSystem.Instance != null)
            {
                SelectionSystem.Instance.ClearSelection();
            }

            _isSelecting = false;
            _isDragging = false;

            _buildingDemolition?.ClearSelection();

            HideSelectionBox();
        }

        // =========================================================
        // ATTACK-MOVE & TARGETING
        // =========================================================

        private void OnAttackMove(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (RTSInputMode.IsWorldInputBlocked)
                return;

            if (!HasSelectedCombatUnit())
                return;

            RTSInputMode.EnterAttackMove();
        }

        private void ExecuteAttackMoveAtScreenPosition(Vector2 screenPosition)
        {
            if (_mainCamera == null || RTSUnitManager.Instance == null)
            {
                RTSInputMode.ExitAttackMove();
                return;
            }

            Vector3 worldPosition = ScreenToWorld2D(screenPosition);
            RTSUnitManager.Instance.IssueAttackMoveCommand(worldPosition);
            RTSInputMode.ExitAttackMove();
        }

        private void HandleInputModeChanged(InputModeState newMode)
        {
            bool isAttackMove = newMode == InputModeState.AttackMove;
            ApplyAttackMoveCursor(isAttackMove);
            OnAttackMoveModeChanged?.Invoke(isAttackMove);
        }

        private void ApplyAttackMoveCursor(bool isAttackMove)
        {
            if (isAttackMove && _attackMoveCursor != null)
            {
                Cursor.SetCursor(_attackMoveCursor, _attackMoveCursorHotspot, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private bool HasSelectedCombatUnit()
        {
            if (SelectionSystem.Instance == null)
                return false;

            IReadOnlyList<SelectableUnit> selected = SelectionSystem.Instance.SelectedUnits;
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] != null && selected[i].GetComponent<UnitCombatController>() != null)
                {
                    return true;
                }
            }

            return false;
        }

        private IDamageable FindHostileTargetAtPoint(Collider2D[] hits)
        {
            if (hits == null)
                return null;

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null)
                    continue;

                IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead)
                    continue;

                if (damageable.TeamFaction == Faction.Neutral ||
                    damageable.TeamFaction == _playerFaction)
                {
                    continue;
                }

                return damageable;
            }

            return null;
        }

        private Vector3 ScreenToWorld2D(Vector2 screenPosition)
        {
            float distanceFromCamera = Mathf.Abs(_mainCamera.transform.position.z);
            Vector3 screenPoint = new Vector3(
                screenPosition.x,
                screenPosition.y,
                distanceFromCamera
            );

            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPoint);
            worldPosition.z = 0f;
            return worldPosition;
        }
    }
}