using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI Component for ContentSwitcher Debugging
/// Creates a simple debug panel with buttons for testing
/// </summary>
public class ContentSwitcherDebugUI : MonoBehaviour
{
    [Header("Auto-Create UI")]
    [SerializeField] private bool createUIOnStart = true;
    [SerializeField] private bool createTopRightPanel = true;

    [Header("UI References (Auto-assigned if null)")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private Button detectButton;
    [SerializeField] private Button chinaCoinButton;
    [SerializeField] private Button chinaJarButton;
    [SerializeField] private Button indonesiaButton;
    [SerializeField] private Button mesirButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    private ContentSwitcherDebugger debugger;
    private Canvas canvas;

    private void Start()
    {
        // Get or create debugger component
        debugger = GetComponent<ContentSwitcherDebugger>();
        if (debugger == null)
        {
            debugger = gameObject.AddComponent<ContentSwitcherDebugger>();
        }

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
            GameObject canvasGO = new GameObject("DebugCanvas");
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
    }

    /// <summary>
    /// Create the main debug panel
    /// </summary>
    private GameObject CreateDebugPanel()
    {
        // Main panel
        GameObject panel = new GameObject("ContentSwitcherDebugPanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        if (createTopRightPanel)
        {
            // Position in top-right corner
            panelRect.anchorMin = new Vector2(1, 1);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.pivot = new Vector2(1, 1);
            panelRect.anchoredPosition = new Vector2(-10, -10);
            panelRect.sizeDelta = new Vector2(300, 400);
        }
        else
        {
            // Position in center
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(350, 450);
        }

        // Title
        CreateTitle(panel);

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
        title.text = "ContentSwitcher Debugger";
        title.fontSize = 18;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -10);
        titleRect.sizeDelta = new Vector2(-20, 30);
    }

    /// <summary>
    /// Create all debug buttons
    /// </summary>
    private void CreateButtons(GameObject parent)
    {
        float yStart = -50f;
        float buttonHeight = 35f;
        float spacing = 5f;
        int buttonIndex = 0;

        // Detect button
        detectButton = CreateButton(parent, "🔍 Detect ContentSwitchers", yStart - (buttonHeight + spacing) * buttonIndex++);

        // Trigger buttons
        chinaCoinButton = CreateButton(parent, "🪙 Trigger China Coin", yStart - (buttonHeight + spacing) * buttonIndex++);
        chinaJarButton = CreateButton(parent, "🏺 Trigger China Jar", yStart - (buttonHeight + spacing) * buttonIndex++);
        indonesiaButton = CreateButton(parent, "🎭 Trigger Indonesia Kendin", yStart - (buttonHeight + spacing) * buttonIndex++);
        mesirButton = CreateButton(parent, "🪶 Trigger Mesir Winged", yStart - (buttonHeight + spacing) * buttonIndex++);

        // Utility buttons
        resetButton = CreateButton(parent, "🔄 Reset All", yStart - (buttonHeight + spacing) * buttonIndex++);
        closeButton = CreateButton(parent, "❌ Close Panel", yStart - (buttonHeight + spacing) * buttonIndex++);
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
        buttonText.fontSize = 14;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        // Position button
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 1);
        buttonRect.anchorMax = new Vector2(1, 1);
        buttonRect.pivot = new Vector2(0.5f, 1);
        buttonRect.anchoredPosition = new Vector2(0, yPosition);
        buttonRect.sizeDelta = new Vector2(-20, 35);

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
        if (detectButton != null)
            detectButton.onClick.AddListener(() => {
                Debug.Log("🔍 DETECT BUTTON CLICKED");
                debugger.DetectAllContentSwitchers();
            });

        if (chinaCoinButton != null)
            chinaCoinButton.onClick.AddListener(() => {
                Debug.Log("🪙 CHINA COIN BUTTON CLICKED");
                debugger.TriggerContentSwitcherManually(ObjectType.ChinaCoin);
            });

        if (chinaJarButton != null)
            chinaJarButton.onClick.AddListener(() => {
                Debug.Log("🏺 CHINA JAR BUTTON CLICKED");
                debugger.TriggerContentSwitcherManually(ObjectType.ChinaJar);
            });

        if (indonesiaButton != null)
            indonesiaButton.onClick.AddListener(() => {
                Debug.Log("🎭 INDONESIA KENDIN BUTTON CLICKED");
                debugger.TriggerContentSwitcherManually(ObjectType.IndonesiaKendin);
            });

        if (mesirButton != null)
            mesirButton.onClick.AddListener(() => {
                Debug.Log("🪶 MESIR WINGED BUTTON CLICKED");
                debugger.TriggerContentSwitcherManually(ObjectType.MesirWingedScared);
            });

        if (resetButton != null)
            resetButton.onClick.AddListener(() => {
                Debug.Log("🔄 RESET BUTTON CLICKED");
                debugger.ResetAllContentSwitchers();
            });

        if (closeButton != null)
            closeButton.onClick.AddListener(() => {
                Debug.Log("❌ CLOSE BUTTON CLICKED");
                TogglePanel();
            });
    }

    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void TogglePanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(!debugPanel.activeInHierarchy);
        }
    }

    /// <summary>
    /// Show panel
    /// </summary>
    public void ShowPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hide panel
    /// </summary>
    public void HidePanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Quick access method to create debugger in scene
    /// </summary>
    [RuntimeInitializeOnLoadMethod]
    private static void CreateDebuggerOnLoad()
    {
        // Only create in development builds
        if (Debug.isDebugBuild)
        {
            GameObject debuggerGO = new GameObject("ContentSwitcherDebugger");
            debuggerGO.AddComponent<ContentSwitcherDebugUI>();
            DontDestroyOnLoad(debuggerGO);
        }
    }

    // Keyboard shortcuts
    private void Update()
    {
        // Press F1 to toggle debug panel
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TogglePanel();
        }

        // Press F2 to quick detect
        if (Input.GetKeyDown(KeyCode.F2))
        {
            debugger?.DetectAllContentSwitchers();
        }

        // Press 1-4 for quick triggers
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            debugger?.TriggerContentSwitcherManually(ObjectType.ChinaCoin);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            debugger?.TriggerContentSwitcherManually(ObjectType.ChinaJar);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            debugger?.TriggerContentSwitcherManually(ObjectType.IndonesiaKendin);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            debugger?.TriggerContentSwitcherManually(ObjectType.MesirWingedScared);
        }
    }
}