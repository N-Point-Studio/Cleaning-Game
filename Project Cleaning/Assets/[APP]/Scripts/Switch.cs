using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Switch : MonoBehaviour
{
    public Image onImage;
    public Image offImage;
    public bool isOn = true;

    void Start()
    {
        UpdateVisual();
    }

    public void Toggle()
    {
        isOn = !isOn;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        onImage.gameObject.SetActive(isOn);
        offImage.gameObject.SetActive(!isOn);
    }
}
