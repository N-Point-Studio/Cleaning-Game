using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ContentSwitcher Debugger Tool
/// Attach this to a GameObject to debug and test ContentSwitcher functionality
/// Provides UI buttons and methods to manually trigger and test ContentSwitchers
/// </summary>
public class ContentSwitcherDebugger : MonoBehaviour
{
    [Header("Manual Testing Controls")]
    [SerializeField] private Button detectButton;
    [SerializeField] private Button triggerChinaCoinButton;
    [SerializeField] private Button triggerChinaJarButton;
    [SerializeField] private Button triggerIndonesiaKendinButton;
    [SerializeField] private Button triggerMesirWingedButton;

    [Header("Debug Settings")]
    [SerializeField] private bool enableDetailedLogs = true;
    [SerializeField] private bool autoDetectOnStart = true;
    [SerializeField] private bool requireSceneTransitionManager = false; // Don't require in menu scenes

    [Header("Current Simulation")]
    [SerializeField] private ObjectType simulatedObjectType = ObjectType.ChinaCoin;
    [SerializeField] private ChapterType simulatedChapterType = ChapterType.China;

    private ContentSwitcher[] allContentSwitchers;

    private void Start()
    {
        SetupButtons();

        if (autoDetectOnStart)
        {
            DetectAllContentSwitchers();
        }

        Debug.Log("=== CONTENT SWITCHER DEBUGGER READY ===");
        Debug.Log("Click 'Detect' button to scan for ContentSwitchers");
        Debug.Log("Use other buttons to manually trigger specific ObjectTypes");
    }

    private void SetupButtons()
    {
        if (detectButton != null)
            detectButton.onClick.AddListener(DetectAllContentSwitchers);

        if (triggerChinaCoinButton != null)
            triggerChinaCoinButton.onClick.AddListener(() => TriggerContentSwitcherManually(ObjectType.ChinaCoin));

        if (triggerChinaJarButton != null)
            triggerChinaJarButton.onClick.AddListener(() => TriggerContentSwitcherManually(ObjectType.ChinaJar));

        if (triggerIndonesiaKendinButton != null)
            triggerIndonesiaKendinButton.onClick.AddListener(() => TriggerContentSwitcherManually(ObjectType.IndonesiaKendin));

        if (triggerMesirWingedButton != null)
            triggerMesirWingedButton.onClick.AddListener(() => TriggerContentSwitcherManually(ObjectType.MesirWingedScared));

        Debug.Log("[Debugger] Buttons setup complete");
    }

    /// <summary>
    /// Scan and detect all ContentSwitchers in the scene
    /// </summary>
    [ContextMenu("Detect All ContentSwitchers")]
    public void DetectAllContentSwitchers()
    {
        allContentSwitchers = FindObjectsOfType<ContentSwitcher>();

        Debug.Log("=== CONTENT SWITCHER DETECTION REPORT ===");
        Debug.Log($"Found {allContentSwitchers.Length} ContentSwitcher(s) in scene:");

        if (allContentSwitchers.Length == 0)
        {
            Debug.LogError("❌ NO CONTENT SWITCHERS FOUND!");
            Debug.LogError("Make sure you have ContentSwitcher components in the scene");
            return;
        }

        for (int i = 0; i < allContentSwitchers.Length; i++)
        {
            var switcher = allContentSwitchers[i];
            AnalyzeContentSwitcher(switcher, i + 1);
        }

        Debug.Log("=== END OF DETECTION REPORT ===");

        // Check for potential issues
        CheckForCommonIssues();
    }

    /// <summary>
    /// Analyze individual ContentSwitcher
    /// </summary>
    private void AnalyzeContentSwitcher(ContentSwitcher switcher, int index)
    {
        Debug.Log($"\n📋 ContentSwitcher #{index}:");
        Debug.Log($"   Name: {switcher.name}");
        Debug.Log($"   GameObject Path: {GetGameObjectPath(switcher.gameObject)}");
        Debug.Log($"   Active in Hierarchy: {switcher.gameObject.activeInHierarchy}");
        Debug.Log($"   Component Enabled: {switcher.enabled}");

        // Chapter and Object info
        Debug.Log($"   📖 Chapter Type: {switcher.GetChapterType()}");
        Debug.Log($"   🎯 Object Type: {switcher.GetObjectType()}");

        // Check trigger button
        var triggerButtonField = switcher.GetType().GetField("triggerButton",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (triggerButtonField != null)
        {
            Button triggerButton = triggerButtonField.GetValue(switcher) as Button;
            if (triggerButton != null)
            {
                Debug.Log($"   🔘 Trigger Button: {triggerButton.name} (Active: {triggerButton.gameObject.activeInHierarchy}, Interactable: {triggerButton.interactable})");
            }
            else
            {
                Debug.LogWarning($"   ⚠️ Trigger Button: NULL - ContentSwitcher might not work!");
            }
        }

        // Check testing mode
        CheckTestingModeSettings(switcher);

        // Check content GameObjects
        CheckContentGameObjects(switcher);
    }

    /// <summary>
    /// Check testing mode settings that might block execution
    /// </summary>
    private void CheckTestingModeSettings(ContentSwitcher switcher)
    {
        var enableTestingModeField = switcher.GetType().GetField("enableTestingMode",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var currentTestingChapterField = switcher.GetType().GetField("currentTestingChapter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (enableTestingModeField != null && currentTestingChapterField != null)
        {
            bool testingMode = (bool)enableTestingModeField.GetValue(switcher);
            ChapterType testingChapter = (ChapterType)currentTestingChapterField.GetValue(switcher);

            Debug.Log($"   🧪 Testing Mode: {testingMode}");
            Debug.Log($"   📚 Testing Chapter: {testingChapter}");

            if (testingMode && testingChapter != switcher.GetChapterType())
            {
                Debug.LogError($"   ❌ POTENTIAL ISSUE: Testing mode enabled but testing chapter ({testingChapter}) doesn't match content switcher chapter ({switcher.GetChapterType()})!");
                Debug.LogError($"   This will BLOCK execution! Set currentTestingChapter to {switcher.GetChapterType()} or disable testing mode.");
            }
            else if (testingMode)
            {
                Debug.Log($"   ✅ Testing mode OK - chapters match");
            }
        }
    }

    /// <summary>
    /// Check if content GameObjects are properly assigned
    /// </summary>
    private void CheckContentGameObjects(ContentSwitcher switcher)
    {
        var fields = new string[] { "initialText", "text1", "text2", "text3", "afterImage", "nextArtifactImage1", "nextArtifactImage2" };

        foreach (string fieldName in fields)
        {
            var field = switcher.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                GameObject obj = field.GetValue(switcher) as GameObject;
                if (obj != null)
                {
                    Debug.Log($"   📦 {fieldName}: {obj.name} (Active: {obj.activeInHierarchy})");
                }
                else
                {
                    Debug.LogWarning($"   ⚠️ {fieldName}: NULL");
                }
            }
        }
    }

    /// <summary>
    /// Check for common issues that prevent ContentSwitcher from working
    /// </summary>
    private void CheckForCommonIssues()
    {
        Debug.Log("\n🔍 CHECKING FOR COMMON ISSUES:");

        // Check SceneTransitionManager (only if required)
        if (requireSceneTransitionManager)
        {
            if (SceneTransitionManager.Instance == null)
            {
                Debug.LogError("❌ SceneTransitionManager.Instance is NULL!");
                Debug.LogError("   Make sure SceneTransitionManager exists in the scene");
            }
            else
            {
                Debug.Log("✅ SceneTransitionManager found");
                Debug.Log($"   Current ObjectType: {SceneTransitionManager.Instance.GetCurrentObjectType()}");
                Debug.Log($"   Current ChapterType: {SceneTransitionManager.Instance.GetCurrentChapterType()}");
                Debug.Log($"   Should Trigger: {SceneTransitionManager.Instance.ShouldTriggerContentSwitcher()}");
            }
        }
        else
        {
            if (SceneTransitionManager.Instance == null)
            {
                Debug.Log("ℹ️ SceneTransitionManager not found (not required in menu scenes)");
            }
            else
            {
                Debug.Log("✅ SceneTransitionManager found");
                Debug.Log($"   Current ObjectType: {SceneTransitionManager.Instance.GetCurrentObjectType()}");
                Debug.Log($"   Current ChapterType: {SceneTransitionManager.Instance.GetCurrentChapterType()}");
            }
        }

        // Check if any ContentSwitcher matches current simulation
        bool foundMatch = false;
        foreach (var switcher in allContentSwitchers)
        {
            if (switcher.GetChapterType() == simulatedChapterType)
            {
                foundMatch = true;
                Debug.Log($"✅ Found matching ContentSwitcher for {simulatedChapterType}: {switcher.name}");
                break;
            }
        }

        if (!foundMatch)
        {
            Debug.LogWarning($"⚠️ No ContentSwitcher found for ChapterType: {simulatedChapterType}");
        }
    }

    /// <summary>
    /// Manually trigger ContentSwitcher for specific ObjectType
    /// </summary>
    [ContextMenu("Trigger ChinaCoin")]
    public void TriggerChinaCoin() => TriggerContentSwitcherManually(ObjectType.ChinaCoin);

    [ContextMenu("Trigger ChinaJar")]
    public void TriggerChinaJar() => TriggerContentSwitcherManually(ObjectType.ChinaJar);

    [ContextMenu("Trigger IndonesiaKendin")]
    public void TriggerIndonesiaKendin() => TriggerContentSwitcherManually(ObjectType.IndonesiaKendin);

    [ContextMenu("Trigger MesirWingedScared")]
    public void TriggerMesirWinged() => TriggerContentSwitcherManually(ObjectType.MesirWingedScared);

    /// <summary>
    /// Force trigger ContentSwitcher for specific ObjectType
    /// </summary>
    public void TriggerContentSwitcherManually(ObjectType objectType)
    {
        ChapterType chapterType = GetChapterFromObjectType(objectType);

        Debug.Log($"=== MANUAL TRIGGER ATTEMPT ===");
        Debug.Log($"Requested ObjectType: {objectType}");
        Debug.Log($"Corresponding ChapterType: {chapterType}");

        // Find matching ContentSwitcher
        ContentSwitcher targetSwitcher = null;

        foreach (var switcher in allContentSwitchers)
        {
            if (switcher.GetChapterType() == chapterType)
            {
                targetSwitcher = switcher;
                break;
            }
        }

        if (targetSwitcher == null)
        {
            Debug.LogError($"❌ No ContentSwitcher found for ChapterType: {chapterType}");
            Debug.LogError("Available ContentSwitchers:");
            foreach (var switcher in allContentSwitchers)
            {
                Debug.LogError($"   - {switcher.name}: {switcher.GetChapterType()}");
            }
            return;
        }

        Debug.Log($"✅ Found target ContentSwitcher: {targetSwitcher.name}");

        // Setup the ContentSwitcher
        targetSwitcher.SetObjectType(objectType);
        targetSwitcher.SetTestingChapter(chapterType);  // Ensure testing mode allows execution
        targetSwitcher.EnableTestingMode(true);         // Enable testing mode to control behavior

        Debug.Log($"🔧 Setup complete:");
        Debug.Log($"   ChapterType: {targetSwitcher.GetChapterType()}");
        Debug.Log($"   ObjectType: {targetSwitcher.GetObjectType()}");

        // Trigger it
        Debug.Log($"🚀 Triggering ContentSwitcher...");
        targetSwitcher.OnButtonClicked();

        Debug.Log($"=== MANUAL TRIGGER COMPLETE ===");
    }

    /// <summary>
    /// Test the SceneTransitionManager trigger system
    /// </summary>
    [ContextMenu("Test SceneTransitionManager Trigger")]
    public void TestSceneTransitionManagerTrigger()
    {
        Debug.Log("=== TESTING SCENE TRANSITION MANAGER ===");

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("❌ SceneTransitionManager not found!");
            return;
        }

        // Simulate finish game scenario
        SceneTransitionManager.Instance.SetObjectTypeForTransition(simulatedObjectType);

        // Wait a frame then trigger ContentSwitcher
        StartCoroutine(DelayedTriggerTest());
    }

    private System.Collections.IEnumerator DelayedTriggerTest()
    {
        yield return new WaitForEndOfFrame();

        Debug.Log("🧪 Simulating SceneTransitionManager trigger...");

        // Call the same method that SceneTransitionManager uses
        var triggerMethod = SceneTransitionManager.Instance.GetType().GetMethod("TriggerContentSwitcher",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (triggerMethod != null)
        {
            bool result = (bool)triggerMethod.Invoke(SceneTransitionManager.Instance, null);
            Debug.Log($"Trigger result: {result}");
        }
        else
        {
            Debug.LogError("Could not find TriggerContentSwitcher method");
        }
    }

    /// <summary>
    /// Get chapter type from object type
    /// </summary>
    private ChapterType GetChapterFromObjectType(ObjectType objectType)
    {
        switch (objectType)
        {
            case ObjectType.ChinaCoin:
            case ObjectType.ChinaJar:
                return ChapterType.China;
            case ObjectType.IndonesiaKendin:
                return ChapterType.Indonesia;
            case ObjectType.MesirWingedScared:
                return ChapterType.Mesir;
            default:
                return ChapterType.China;
        }
    }

    /// <summary>
    /// Get full path of GameObject in hierarchy
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    /// <summary>
    /// Reset all ContentSwitchers to initial state
    /// </summary>
    [ContextMenu("Reset All ContentSwitchers")]
    public void ResetAllContentSwitchers()
    {
        Debug.Log("=== RESETTING ALL CONTENT SWITCHERS ===");

        foreach (var switcher in allContentSwitchers)
        {
            switcher.ResetToInitialState();
            Debug.Log($"✅ Reset: {switcher.name}");
        }
    }

    /// <summary>
    /// Set simulation parameters
    /// </summary>
    public void SetSimulationObjectType(ObjectType objectType)
    {
        simulatedObjectType = objectType;
        simulatedChapterType = GetChapterFromObjectType(objectType);
        Debug.Log($"Simulation set to: {objectType} ({simulatedChapterType})");
    }
}