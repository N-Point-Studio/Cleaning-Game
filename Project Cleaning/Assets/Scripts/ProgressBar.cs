using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public int minimum = 0;
    public int maximum = 100;

    public int current = 0;

    public Image fill;
    public Image checkList;
    public TextMeshProUGUI progressText;

    [SerializeField] private AudioSource AudioSourceProgress;
    [SerializeField] public AudioClip ProfressSound;

    private int lastValue = -1;

    private void Update()
    {
        UpdateFill();
    }

    public void SetValue(float value01)
    {
        if (value01 >= 0.995f)
            value01 = 1f;
        else
            value01 = Mathf.Clamp01(value01);

        current = Mathf.RoundToInt(value01 * maximum);

        // 👇 Play sound WHEN we transition into 100 (not every frame)
        if (current == 100 && lastValue != 100)
        {
            if (AudioSourceProgress != null && ProfressSound != null)
            {
                AudioSourceProgress.PlayOneShot(ProfressSound);
                HapticManager.Instance.Light();
            }
        }

        lastValue = current;

        progressText.text = current.ToString();
        checkList.enabled = current == 100;
        progressText.enabled = current != 100;

        UpdateFill();
    }

    public void UpdateFill()
    {
        var progress = (float)current / maximum;

        if (progress >= 0.99f)
            fill.fillAmount = 1f;
        else
            fill.fillAmount = progress;
    }

    public int GetValue()
    {
        return current;
    }
}
