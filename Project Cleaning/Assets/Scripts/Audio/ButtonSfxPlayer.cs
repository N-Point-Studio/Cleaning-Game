using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Simple SFX hook for buttons and swipes.
/// Attach to a button (assign AudioSource + clip) or call PlayClick() manually.
/// For swipe/page change, call PlaySwipe().
/// </summary>
public class ButtonSfxPlayer : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSfx;
    [SerializeField] private AudioClip swipeSfx;
    [SerializeField] private float volume = 1f;
    private Button cachedButton;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        cachedButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (cachedButton != null)
        {
            cachedButton.onClick.AddListener(PlayClick);
        }
    }

    private void OnDisable()
    {
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(PlayClick);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }

    public void PlayClick()
    {
        if (audioSource != null && clickSfx != null)
        {
            audioSource.PlayOneShot(clickSfx, volume);
        }
    }

    public void PlaySwipe()
    {
        if (audioSource != null && swipeSfx != null)
        {
            audioSource.PlayOneShot(swipeSfx, volume);
        }
    }
}
