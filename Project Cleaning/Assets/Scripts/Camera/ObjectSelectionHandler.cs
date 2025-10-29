using UnityEngine;
using System;

/// <summary>
/// Handles object selection via mouse clicks for close-up inspection
/// </summary>
public class ObjectSelectionHandler : MonoBehaviour
{
    [Header("Selection Settings")]
    public bool enableSelection = true;
    public bool requireInspectableComponent = false;

    // Runtime settings
    private LayerMask selectableLayerMask = -1;
    private ObjectCloseUpManager closeUpManager;
    private Camera playerCamera;

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = FindObjectOfType<Camera>();
    }

    void Update()
    {
        // Input is now handled by ObjectCloseUpManager to avoid conflicts
        // This prevents the selection handler from interfering with rotation
    }

    /// <summary>
    /// Setup selection handler with parameters
    /// </summary>
    public void Setup(LayerMask layerMask, ObjectCloseUpManager manager)
    {
        selectableLayerMask = layerMask;
        closeUpManager = manager;

        Debug.Log("Object selection handler initialized");
    }

    /// <summary>
    /// Handle mouse input for object selection
    /// </summary>
    void HandleSelectionInput()
    {
        // Only allow selection if no object is currently in close-up
        if (closeUpManager.HasObjectInCloseUp) return;

        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectObject();
        }
    }

    /// <summary>
    /// Try to select an object under the mouse cursor (called by manager)
    /// </summary>
    public void TrySelectObjectAtMousePosition()
    {
        if (playerCamera == null)
        {
            Debug.LogError("ObjectSelectionHandler: playerCamera is null!");
            return;
        }

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Debug.Log($"Raycast from: {ray.origin} direction: {ray.direction}");
        Debug.Log($"Layer mask: {selectableLayerMask} (binary: {Convert.ToString(selectableLayerMask, 2)})");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableLayerMask))
        {
            Transform hitObject = hit.transform;
            Debug.Log($"Raycast hit object: {hitObject.name} on layer {hitObject.gameObject.layer}");

            // Check if object can be selected
            if (CanSelectObject(hitObject))
            {
                Debug.Log($"Object {hitObject.name} can be selected - proceeding with selection");
                SelectObject(hitObject);
            }
            else
            {
                Debug.Log($"Cannot select object: {hitObject.name}");
            }
        }
        else
        {
            Debug.Log("Raycast did not hit any objects in the selectable layer mask");
        }
    }

    /// <summary>
    /// Try to select an object under the mouse cursor (legacy method)
    /// </summary>
    void TrySelectObject()
    {
        TrySelectObjectAtMousePosition();
    }

    /// <summary>
    /// Check if an object can be selected for close-up
    /// </summary>
    bool CanSelectObject(Transform targetObject)
    {
        // Check if object is active
        if (!targetObject.gameObject.activeInHierarchy)
            return false;

        // Jar pieces can be inspected at any time (before, during, or after assembly)
        var jarComponent = targetObject.GetComponent<JarAutoAssembly>();
        if (jarComponent != null)
        {
            Debug.Log($"Jar piece {jarComponent.pieceType} can be inspected (assembled: {jarComponent.isAssembled})");
            // No restriction - jar pieces can always be inspected
        }

        // If we require inspectable component, check for it
        if (requireInspectableComponent)
        {
            var inspectable = targetObject.GetComponent<IInspectable>();
            if (inspectable == null || !inspectable.CanBeInspected())
                return false;
        }

        // Additional checks can be added here
        // For example: check if object is too far, too small, etc.

        return true;
    }

    /// <summary>
    /// Select an object for close-up inspection
    /// </summary>
    void SelectObject(Transform targetObject)
    {
        Debug.Log($"Selected object for close-up: {targetObject.name}");

        // Notify inspectable component if it exists
        var inspectable = targetObject.GetComponent<IInspectable>();
        inspectable?.OnInspectionStart();

        // Tell the close-up manager to bring the object to camera
        closeUpManager.BringObjectToCloseUp(targetObject);
    }

    /// <summary>
    /// Enable or disable object selection
    /// </summary>
    public void SetSelectionEnabled(bool enabled)
    {
        enableSelection = enabled;
    }

    /// <summary>
    /// Update the selectable layer mask
    /// </summary>
    public void SetSelectableLayerMask(LayerMask layerMask)
    {
        selectableLayerMask = layerMask;
    }

    /// <summary>
    /// Set whether inspectable component is required
    /// </summary>
    public void SetRequireInspectableComponent(bool require)
    {
        requireInspectableComponent = require;
    }

    /// <summary>
    /// Get the object currently under the mouse cursor
    /// </summary>
    public Transform GetObjectUnderMouse()
    {
        if (playerCamera == null) return null;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, selectableLayerMask))
        {
            return hit.transform;
        }

        return null;
    }

    /// <summary>
    /// Check if there's a selectable object under the mouse
    /// </summary>
    public bool IsSelectableObjectUnderMouse()
    {
        Transform objectUnderMouse = GetObjectUnderMouse();
        return objectUnderMouse != null && CanSelectObject(objectUnderMouse);
    }

    // Public properties
    public bool SelectionEnabled => enableSelection;
    public LayerMask SelectableLayerMask => selectableLayerMask;

    void OnDrawGizmosSelected()
    {
        // Draw selection ray in Scene view
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                playerCamera.nearClipPlane
            ));

            Vector3 rayDirection = (mouseWorldPos - playerCamera.transform.position).normalized;
            Gizmos.DrawRay(playerCamera.transform.position, rayDirection * 10f);
        }
    }
}