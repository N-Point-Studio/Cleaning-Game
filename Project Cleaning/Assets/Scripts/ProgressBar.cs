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

    private void Update()
    {
        UpdateFill();
    }

    public void SetValue(float value01)
    {
        //Debug.Log("progress value A : " + value01);
        if (value01 >= 0.99)
        {
            value01 = 1;
        }
        else
        {
            value01 = Mathf.Clamp01(value01);
        }

        current = Mathf.RoundToInt(value01 * maximum);
        progressText.text = current.ToString();
        checkList.enabled = current == 100;
        progressText.enabled = current != 100;
        UpdateFill();
    }

    public void UpdateFill()
    {
        var progress = (float)current / maximum;
        //Debug.Log("progress value B : " + progress);
        //Debug.Log("progress value C : " + current);


        // Debug.Log($"progress bar dari {name} adalah {current}");
        if (progress >= 99)
        {
            fill.fillAmount = 100;
        }
        else
        {
            fill.fillAmount = progress;
        }
    }

    public int GetValue()
    {
        return current;
    }
}

