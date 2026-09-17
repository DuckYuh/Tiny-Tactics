using AoEMini.Army;
using AoEMini.Selection;
using UnityEngine;
using UnityEngine.UI;

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

        private GameInputActions _input;

        private Vector2 _selectionStart;
        private Vector2 _currentMousePosition;

        private bool _isSelecting;
        private bool _isDragging;

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

        private void UpdateMousePosition()
        {
            _currentMousePosition =
                _input.Player.Point.ReadValue<Vector2>();
        }

        private void UpdateDragState()
        {
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

        private void OnSelectStarted(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _selectionStart =
                _input.Player.Point.ReadValue<Vector2>();

            _isSelecting = true;
            _isDragging = false;
        }

        private void OnSelectCanceled(
            UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (!_isSelecting)
                return;

            Vector2 selectionEnd =
                _input.Player.Point.ReadValue<Vector2>();

            if (_isDragging)
            {
                HandleDragSelection(
                    _selectionStart,
                    selectionEnd);
            }
            else
            {
                HandleSingleSelection(selectionEnd);
            }

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

            Vector2 screenPosition =
                _input.Player.Point.ReadValue<Vector2>();

            Ray ray =
                _mainCamera.ScreenPointToRay(screenPosition);

            Debug.Log(
                $"Ray Origin: {ray.origin}, Direction: {ray.direction}");

            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    1000f))
            {
                Debug.Log(
                    $"Right Click Hit: {hit.collider.name}, " +
                    $"World Position: {hit.point}");

                return;
            }

            Debug.Log("Right Click: No world position found.");
        }

        // =========================================================
        // SELECTION
        // =========================================================

        private void HandleSingleSelection(
            Vector2 screenPosition)
        {
            if (SelectionSystem.Instance == null)
                return;

            SelectableUnit unit =
                FindUnitAtScreenPosition(screenPosition);

            if (unit != null)
            {
                SelectionSystem.Instance.SelectSingle(unit);
            }
            else
            {
                // Click empty area -> clear selection.
                SelectionSystem.Instance.ClearSelection();
            }
        }

        private void HandleDragSelection(
            Vector2 start,
            Vector2 end)
        {
            if (SelectionSystem.Instance == null)
                return;

            if (_mainCamera == null)
                return;

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

            Ray ray =
                _mainCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    Mathf.Infinity))
            {
                return null;
            }

            return hit.collider
                .GetComponentInParent<SelectableUnit>();
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
            if (SelectionSystem.Instance != null)
            {
                SelectionSystem.Instance.ClearSelection();
            }

            _isSelecting = false;
            _isDragging = false;

            HideSelectionBox();
        }
    }
}