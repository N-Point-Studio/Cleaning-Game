using UnityEngine;

/// <summary>
/// Example script demonstrating how to use the new ObjectType detection system
/// This script shows different ways to detect and respond to specific clicked objects
/// </summary>
public class ObjectDetectionExample : MonoBehaviour
{
    [Header("Demo Configuration")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool respondToSpecificObjects = true;

    private void Start()
    {
        // Subscribe to object click events
        SetupObjectClickListeners();

        if (enableDebugLogs)
        {
            Debug.Log("=== Object Detection Example Started ===");
            Debug.Log("Available Object Types:");
            Debug.Log("- ChinaCoin, ChinaJar (China Chapter)");
            Debug.Log("- IndonesiaKendin (Indonesia Chapter)");
            Debug.Log("- MesirWingedScared (Mesir Chapter)");
        }
    }

    /// <summary>
    /// Example: Setup listeners for all ClickableObjects in scene
    /// </summary>
    private void SetupObjectClickListeners()
    {
        // Find all ClickableObject components in the scene
        ClickableObject[] clickableObjects = FindObjectsOfType<ClickableObject>();

        foreach (ClickableObject clickable in clickableObjects)
        {
            // Add listener to each object's OnObjectClicked event
            clickable.OnObjectClicked.AddListener(() => OnAnyObjectClicked(clickable));
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Found and subscribed to {clickableObjects.Length} clickable objects");
        }
    }

    /// <summary>
    /// Example: Handle clicks on any object and respond based on ObjectType
    /// </summary>
    private void OnAnyObjectClicked(ClickableObject clickedObject)
    {
        if (!respondToSpecificObjects) return;

        ObjectType objType = clickedObject.GetObjectType();
        ChapterType chapter = clickedObject.GetChapterFromObjectType();

        if (enableDebugLogs)
        {
            Debug.Log($"=== OBJECT DETECTION EXAMPLE ===");
            Debug.Log($"Clicked: {clickedObject.name}");
            Debug.Log($"Type: {objType}");
            Debug.Log($"Chapter: {chapter}");
            Debug.Log($"================================");
        }

        // Respond to specific object types
        HandleSpecificObjectType(objType, clickedObject);

        // Or respond by chapter
        HandleSpecificChapter(chapter, clickedObject);
    }

    /// <summary>
    /// Example: Handle specific object types
    /// </summary>
    private void HandleSpecificObjectType(ObjectType objType, ClickableObject clickedObject)
    {
        switch (objType)
        {
            case ObjectType.ChinaCoin:
                Debug.Log("🪙 Chinese Coin clicked! Playing coin sound effect...");
                // Add coin-specific logic here
                PlayCoinEffect();
                break;

            case ObjectType.ChinaJar:
                Debug.Log("🏺 Chinese Jar clicked! Showing jar information...");
                // Add jar-specific logic here
                ShowJarInfo();
                break;

            case ObjectType.IndonesiaKendin:
                Debug.Log("🎭 Indonesian Kendin clicked! Playing traditional music...");
                // Add kendin-specific logic here
                PlayIndonesianMusic();
                break;

            case ObjectType.MesirWingedScared:
                Debug.Log("🪶 Egyptian Winged Scared clicked! Revealing ancient secrets...");
                // Add winged scared-specific logic here
                RevealEgyptianSecrets();
                break;

            default:
                Debug.Log("Unknown object type clicked");
                break;
        }
    }

    /// <summary>
    /// Example: Handle objects by chapter type
    /// </summary>
    private void HandleSpecificChapter(ChapterType chapter, ClickableObject clickedObject)
    {
        switch (chapter)
        {
            case ChapterType.China:
                Debug.Log("🐉 Chinese artifact detected! Setting Chinese theme...");
                SetChineseTheme();
                break;

            case ChapterType.Indonesia:
                Debug.Log("🌺 Indonesian artifact detected! Setting Indonesian theme...");
                SetIndonesianTheme();
                break;

            case ChapterType.Mesir:
                Debug.Log("🏜️ Egyptian artifact detected! Setting Egyptian theme...");
                SetEgyptianTheme();
                break;
        }
    }

    // Example effect methods (implement with your actual game logic)
    private void PlayCoinEffect()
    {
        // TODO: Implement coin sound effect
        Debug.Log("   💫 Coin effect triggered!");
    }

    private void ShowJarInfo()
    {
        // TODO: Implement jar information display
        Debug.Log("   📜 Jar information displayed!");
    }

    private void PlayIndonesianMusic()
    {
        // TODO: Implement Indonesian music
        Debug.Log("   🎵 Indonesian music playing!");
    }

    private void RevealEgyptianSecrets()
    {
        // TODO: Implement Egyptian secrets reveal
        Debug.Log("   ✨ Ancient secrets revealed!");
    }

    private void SetChineseTheme()
    {
        // TODO: Implement Chinese theme
        Debug.Log("   🎨 Chinese theme activated!");
    }

    private void SetIndonesianTheme()
    {
        // TODO: Implement Indonesian theme
        Debug.Log("   🎨 Indonesian theme activated!");
    }

    private void SetEgyptianTheme()
    {
        // TODO: Implement Egyptian theme
        Debug.Log("   🎨 Egyptian theme activated!");
    }

    /// <summary>
    /// Example: Find and interact with specific object types programmatically
    /// </summary>
    [System.Obsolete("For testing only")]
    public void FindSpecificObjects()
    {
        ClickableObject[] allObjects = FindObjectsOfType<ClickableObject>();

        Debug.Log("=== FINDING SPECIFIC OBJECTS ===");

        foreach (ClickableObject obj in allObjects)
        {
            ObjectType type = obj.GetObjectType();
            Debug.Log($"Found: {obj.name} - Type: {type} - Chapter: {obj.GetChapterFromObjectType()}");

            // Example: Find only Chinese objects
            if (obj.BelongsToChapter(ChapterType.China))
            {
                Debug.Log($"   ⭐ This is a Chinese artifact!");
            }
        }
    }

    /// <summary>
    /// Example: Programmatically click objects by type
    /// </summary>
    [System.Obsolete("For testing only")]
    public void ClickObjectsByType(ObjectType targetType)
    {
        ClickableObject[] allObjects = FindObjectsOfType<ClickableObject>();

        foreach (ClickableObject obj in allObjects)
        {
            if (obj.GetObjectType() == targetType)
            {
                Debug.Log($"Programmatically clicking: {obj.name}");
                obj.OnClick();
            }
        }
    }
}