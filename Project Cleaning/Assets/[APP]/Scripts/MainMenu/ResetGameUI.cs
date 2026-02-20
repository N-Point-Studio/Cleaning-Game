using UnityEngine;
using UnityEngine.UI;

public class ResetGameUI : MonoBehaviour
{
    [Header("Reset Buttons")]
    [SerializeField] private Button yesResetButton;

    private void Start()
    {
        if (yesResetButton != null)
        {
            yesResetButton.onClick.AddListener(OnYesClicked);
        }
        else
        {
            Debug.LogWarning("[ResetGameUI] yes button belum dimasukkan di Inspector!");
        }
    }

    private void OnYesClicked()
    {
        if (yesResetButton != null) yesResetButton.interactable = false;

        if (SaveSystem.Instance != null)
        {
            SaveSystem.Instance.ResetAllProgress();
        }
        else
        {
            Debug.LogError("SaveSystem Not found!");
            if (yesResetButton != null) yesResetButton.interactable = true;
        }
    }

    private void OnDestroy()
    {
        if (yesResetButton != null)
        {
            yesResetButton.onClick.RemoveListener(OnYesClicked);
        }
    }
}