using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public int minimum = 0;
    public int maximum = 100;

    public int current = 0;

    public Image fill;

    private void Update()
    {
        UpdateFill();
    }

    public void SetValue(float value01)
    {
        value01 = Mathf.Clamp01(value01);
        current = Mathf.RoundToInt(value01 * maximum);
        UpdateFill();
    }

    public void UpdateFill()
    {
        fill.fillAmount = (float)current / maximum;
    }
}

