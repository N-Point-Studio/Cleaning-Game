using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SimpleSplashScreen : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float splashDuration = 2f;
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Logo")]
    [SerializeField] private Image logoImage;
    [SerializeField] private bool fadeInLogo = true;

    void Start()
    {
        // Setup logo
        if (logoImage != null && fadeInLogo)
        {
            // Start with transparent logo
            Color logoColor = logoImage.color;
            logoColor.a = 0f;
            logoImage.color = logoColor;
        }

        // Start splash
        StartCoroutine(ShowSplash());
    }

    private IEnumerator ShowSplash()
    {
        // Fade in logo
        if (logoImage != null && fadeInLogo)
        {
            yield return StartCoroutine(FadeInLogo());
            yield return new WaitForSeconds(splashDuration - 1f); // Show logo for remaining time
        }
        else
        {
            // Just wait
            yield return new WaitForSeconds(splashDuration);
        }

        // Go to next scene
        LoadNextScene();
    }

    private IEnumerator FadeInLogo()
    {
        float fadeTime = 1f;
        float elapsed = 0f;
        Color startColor = logoImage.color;
        Color endColor = startColor;
        endColor.a = 1f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            logoImage.color = Color.Lerp(startColor, endColor, t);
            yield return null;
        }

        logoImage.color = endColor;
    }

    private void LoadNextScene()
    {
        // Use UnitySceneManager if available
        if (UnitySceneManager.Instance != null)
        {
            UnitySceneManager.Instance.LoadScene(nextSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}