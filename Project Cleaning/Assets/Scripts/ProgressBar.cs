 using UnityEngine;
  using UnityEngine.UI;
  using TMPro;

  public class ProgressBar : MonoBehaviour
  {
      [Header("Progress Bar Images")]
      [SerializeField] private Image backgroundImage;
      [SerializeField] private RectTransform fillContainer; // Container with Mask component
    [SerializeField] private RectTransform fillTransform; // Actual fill image
      [SerializeField] private Image fillImage;

      [Header("Optional Text")]
      [SerializeField] private TextMeshProUGUI percentageText;
      [SerializeField] private bool showPercentage = true;

      [Header("Progress Settings")]
      [Range(0f, 1f)]
      [SerializeField] private float currentProgress = 0f;
      [SerializeField] private bool animateProgress = false;
      [SerializeField] private float animationSpeed = 2f;

      [Header("Test Progress Controls")]
      [Range(0f, 1f)]
      [SerializeField] private float testProgress = 0f;
      [SerializeField] private bool applyTestProgress = false;
      [Space]
      [Header("Quick Test Buttons")]
      [SerializeField] private bool setTo25Percent = false;
      [SerializeField] private bool setTo50Percent = false;
      [SerializeField] private bool setTo75Percent = false;
      [SerializeField] private bool setTo100Percent = false;
      [SerializeField] private bool setToZero = false;

      private float targetProgress;

      void Start()
      {
          // Ensure fill container has mask component
          if (fillContainer != null && fillContainer.GetComponent<Mask>() == null)
          {
              fillContainer.gameObject.AddComponent<Mask>();
              Debug.Log("Mask component added to fillContainer automatically");
          }

          // Initialize with current progress
          SetProgress(currentProgress);
      }

      void Update()
      {
          // Test progress in inspector (only in editor)
          #if UNITY_EDITOR
          if (applyTestProgress)
          {
              SetProgress(testProgress);
              applyTestProgress = false;
          }

          // Quick test buttons
          if (setTo25Percent)
          {
              SetProgress(0.25f);
              setTo25Percent = false;
          }
          if (setTo50Percent)
          {
              SetProgress(0.5f);
              setTo50Percent = false;
          }
          if (setTo75Percent)
          {
              SetProgress(0.75f);
              setTo75Percent = false;
          }
          if (setTo100Percent)
          {
              SetProgress(1f);
              setTo100Percent = false;
          }
          if (setToZero)
          {
              SetProgress(0f);
              setToZero = false;
          }
          #endif

          // Smooth animation if enabled
          if (animateProgress && Mathf.Abs(currentProgress - targetProgress) > 0.01f)
          {
              currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, animationSpeed * Time.deltaTime);
              UpdateProgressVisual();
          }
      }

      public void SetProgress(float progress)
      {
          targetProgress = Mathf.Clamp01(progress);

          if (animateProgress)
          {
              // Will animate in Update()
          }
          else
          {
              currentProgress = targetProgress;
              UpdateProgressVisual();
          }
      }

      private void UpdateProgressVisual()
      {
          // Scale the fill image from left to right
          if (fillTransform != null)
          {
              Vector3 scale = new Vector3(currentProgress, 1f, 1f);
              fillTransform.localScale = scale;

              // Debug to help identify issues
              #if UNITY_EDITOR
              Debug.Log($"Progress: {currentProgress:F2}, Scale: {scale}, FillTransform: {fillTransform.name}");
              #endif
          }

          // Update percentage text
          if (showPercentage && percentageText != null)
          {
              percentageText.text = $"{currentProgress * 100:F0}%";
          }
      }

      // Public methods for easy access
      public void SetBackgroundImage(Sprite backgroundSprite)
      {
          if (backgroundImage != null)
              backgroundImage.sprite = backgroundSprite;
      }

      public void SetFillImage(Sprite fillSprite)
      {
          if (fillImage != null)
              fillImage.sprite = fillSprite;
      }

      public void SetFillColor(Color color)
      {
          if (fillImage != null)
              fillImage.color = color;
      }

      public void SetBackgroundColor(Color color)
      {
          if (backgroundImage != null)
              backgroundImage.color = color;
      }

      public float GetProgress()
      {
          return currentProgress;
      }
  }