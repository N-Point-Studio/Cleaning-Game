using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple debug UI for SaveSystem testing and reset functionality
/// </summary>
public class SaveSystemDebugUI : MonoBehaviour
{
    [Header("Auto-Create UI")]
    [SerializeField] private bool createUIOnStart = true;
    [SerializeField] private bool createTopLeftPanel = true;

    [Header("UI References (Auto-assigned if null)")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private Button printSaveDataButton;
    [SerializeField] private Button deleteSaveFileButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private Canvas canvas;

    private void Start()
    {
        if (createUIOnStart)
        {
            CreateDebugUI();
        }
    }

    /// <summary>
    /// Create debug UI automatically
    /// </summary>
    public void CreateDebugUI()
    {
        // Find or create canvas
        canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("SaveSystemDebugCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // Create debug panel
        if (debugPanel == null)
        {
            debugPanel = CreateDebugPanel();
        }

        SetupButtons();
        UpdateStatusText();
    }

    /// <summary>
    /// Create the main debug panel
    /// </summary>
    private GameObject CreateDebugPanel()
    {
        // Main panel
        GameObject panel = new GameObject("SaveSystemDebugPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        if (createTopLeftPanel)
        {
            // Position in top-left corner
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(300, 250);
        }
        else
        {
            // Position in center
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(350, 300);
        }

        // Title
        CreateTitle(panel);

        // Status text
        CreateStatusText(panel);

        // Buttons
        CreateButtons(panel);

        return panel;
    }

    /// <summary>
    /// Create title text
    /// </summary>
    private void CreateTitle(GameObject parent)
    {
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(parent.transform, false);

        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "SaveSystem Debug";
        title.fontSize = 16;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 25);
    }

    /// <summary>
    /// Create status text
    /// </summary>
    private void CreateStatusText(GameObject parent)
    {
        GameObject statusGO = new GameObject("StatusText");
        statusGO.transform.SetParent(parent.transform, false);

        statusText = statusGO.AddComponent<TextMeshProUGUI>();
        statusText.text = "Loading...";
        statusText.fontSize = 12;
        statusText.color = Color.yellow;
        statusText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0.6f);
        statusRect.anchorMax = new Vector2(1, 1);
        statusRect.pivot = new Vector2(0.5f, 1);
        statusRect.anchoredPosition = new Vector2(0, -40);
        statusRect.sizeDelta = new Vector2(-20, 0);
    }

    /// <summary>
    /// Create all debug buttons
    /// </summary>
    private void CreateButtons(GameObject parent)
    {
        float yStart = -150f;
        float buttonHeight = 30f;
        float spacing = 5f;
        int buttonIndex = 0;

        // Reset progress button
        resetProgressButton = CreateButton(parent, "🔄 Reset All Progress", yStart - (buttonHeight + spacing) * buttonIndex++);

        // Print save data button
        printSaveDataButton = CreateButton(parent, "📊 Print Save Data", yStart - (buttonHeight + spacing) * buttonIndex++);

        // Delete save file button
        deleteSaveFileButton = CreateButton(parent, "🗑️ Delete Save File", yStart - (buttonHeight + spacing) * buttonIndex++);

        // Close button
        closeButton = CreateButton(parent, "❌ Close", yStart - (buttonHeight + spacing) * buttonIndex++);
    }

    /// <summary>
    /// Create individual button
    /// </summary>
    private Button CreateButton(GameObject parent, string text, float yPosition)
    {
        GameObject buttonGO = new GameObject($"Button_{text.Replace(" ", "")}");
        buttonGO.transform.SetParent(parent.transform, false);

        Button button = buttonGO.AddComponent<Button>();
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);

        TextMeshProUGUI buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        // Position button
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(0.5f, 1);
        buttonRect.anchoredPosition = new Vector2(0, yPosition);
        buttonRect.sizeDelta = new Vector2(-20, 30);

        // Position text
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return button;
    }

    /// <summary>
    /// Setup button listeners
    /// </summary>
    private void SetupButtons()
    {
        if (resetProgressButton != null)
            resetProgressButton.onClick.AddListener(() => {
                ResetAllProgress();
                UpdateStatusText();
            });

        if (printSaveDataButton != null)
            printSaveDataButton.onClick.AddListener(() => {
                PrintSaveData();
                UpdateStatusText();
            });

        if (deleteSaveFileButton != null)
            deleteSaveFileButton.onClick.AddListener(() => {
                DeleteSaveFile();
                UpdateStatusText();
            });

        if (closeButton != null)
            closeButton.onClick.AddListener(() => {
                TogglePanel();
            });
    }

    /// <summary>
    /// Update status text with current save info
    /// </summary>
    private void UpdateStatusText()
    {
        if (statusText == null) return;

        if (SaveSystem.Instance == null)
        {
            statusText.text = "❌ SaveSystem: NOT FOUND";
            statusText.color = Color.red;
            return;
        }

        var saveData = SaveSystem.Instance.GetSaveData();
        string status = "✅ SaveSystem: Active\n";
        status += $"📁 Save File: {(SaveSystem.Instance.SaveFileExists() ? "Found" : "Not Found")}\n";
        status += $"📊 Completed: {saveData.GetCompletedCount()} objects\n";
        status += $"🕒 Last Save: {saveData.lastSaveTime}";

        statusText.text = status;
        statusText.color = Color.green;
    }

    /// <summary>
    /// Reset all progress
    /// </summary>
    private void ResetAllProgress()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ResetAllProgress();
            Debug.Log("All progress reset via debug UI");
        }
        else
        {
            Debug.LogWarning("SaveSystem not found!");
        }
    }

    /// <summary>
    /// Print save data to console
    /// </summary>
    private void PrintSaveData()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.PrintSaveDataInfo();
        }
        else
        {
            Debug.LogWarning("SaveSystem not found!");
        }
    }

    /// <summary>
    /// Delete save file
    /// </summary>
    private void DeleteSaveFile()
    {
        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.DeleteSaveFile();
            Debug.Log("Save file deleted via debug UI");
        }
        else
        {
            Debug.LogWarning("SaveSystem not found!");
        }
    }

    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeInHierarchy);
            if (debugPanel.activeInHierarchy)
            {
                UpdateStatusText();
            }
        }
    }

    /// <summary>
    /// Auto-create SaveSystem debug UI at runtime
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    private static void CreateSaveSystemDebugUIOnLoad()
    {
        if (Debug.isDebugBuild)
        {
            GameObject debugUIGO = new GameObject("SaveSystemDebugUI");
            debugUIGO.AddComponent<SaveSystemDebugUI>();
            DontDestroyOnLoad(debugUIGO);
        }
    }

    // Keyboard shortcuts
    private void Update()
    {
        // Press F3 to toggle debug panel
        if (Input.GetKeyDown(KeyCode.F3))
        {
            if (debugPanel == null)
            {
                CreateDebugUI();
            }
            else
            {
                TogglePanel();
            }
        }

        // Press F4 to quick reset
        if (Input.GetKeyDown(KeyCode.F4))
        {
            ResetAllProgress();
            UpdateStatusText();
        }
    }
}