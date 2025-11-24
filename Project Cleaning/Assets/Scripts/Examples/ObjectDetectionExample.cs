using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the ObjectType detection system.
/// This script now provides a single, general log when any object is clicked.
/// </summary>
public class ObjectDetectionExample : MonoBehaviour
{
    [Header("Demo Configuration")]
    [Tooltip("Enable this to log a message when an object is clicked.")]
    [SerializeField] private bool enableClickLogs = true;

    private void Start()
    {
        // Subscribe to object click events
        SetupObjectClickListeners();
    }

    /// <summary>
    /// Finds all ClickableObjects in the scene and adds a listener for their click events.
    /// </summary>
    private void SetupObjectClickListeners()
    {
        // Find all ClickableObject components in the scene
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();

        foreach (ClickableObject clickable in clickableObjects)
        {
            // Add a listener to each object's OnObjectClicked event
            clickable.OnObjectClicked.AddListener(() => OnAnyObjectClicked(clickable));
        }

        Debug.Log($"[ObjectDetectionExample] Subscribed to {clickableObjects.Length} clickable objects.");
    }

    /// <summary>
    /// A single, general handler for when any ClickableObject is clicked.
    /// </summary>
    private void OnAnyObjectClicked(ClickableObject clickedObject)
    {
        if (!enableClickLogs) return;

        // The object type is already correctly detected by the system.
        // We can get it directly from the object that was clicked.
        ObjectType objType = clickedObject.GetObjectType();
        ChapterType chapter = clickedObject.GetChapterFromObjectType();

        // Log a single, clear message as requested.
        Debug.Log($"[ObjectDetectionExample] Object Clicked! Name: '{clickedObject.name}', Type: '{objType}', Chapter: '{chapter}'");

        // All specific logic (like for 'ChinaCoin' or 'China' chapter) has been removed
        // to make this system general. You can add your own generalized logic here.
    }
}