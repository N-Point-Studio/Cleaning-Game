using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Plays menu BGM on menu scenes, stops it automatically when entering gameplay.
/// Attach this to a GameObject in the menu scene and assign the AudioSource (looped) + clip.
/// Uses DontDestroyOnLoad so the music survives transition to TransitionScreen but halts on gameplay.
/// </summary>
public class MenuMusicController : MonoBehaviour
{
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private string[] menuSceneKeywords = { "menu", "start game", "selectchapter", "sandy" };
    [SerializeField] private string[] gameplaySceneKeywords = { "gameplay", "fix", "rnd" };
    [SerializeField] private float volume = 1f;

    private static MenuMusicController instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
        {
            musicSource = GetComponent<AudioSource>();
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        TryPlayForCurrentScene();
    }

    private void OnEnable()
    {
        SettingManager.OnBgmVolumeChanged += SetVolume;
    }

    private void OnDisable()
    {
        SettingManager.OnBgmVolumeChanged -= SetVolume;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        SettingManager.OnBgmVolumeChanged -= SetVolume;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryPlayForCurrentScene();
    }

    private void TryPlayForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower();

        if (ContainsAny(sceneName, gameplaySceneKeywords))
        {
            StopMusic();
            return;
        }

        if (ContainsAny(sceneName, menuSceneKeywords))
        {
            PlayMusic();
            return;
        }
    }

    private bool ContainsAny(string haystack, string[] needles)
    {
        foreach (var n in needles)
        {
            if (haystack.Contains(n.ToLower()))
                return true;
        }
        return false;
    }

    private void PlayMusic()
    {
        if (musicSource == null || menuMusic == null)
            return;

        if (musicSource.clip != menuMusic)
        {
            musicSource.clip = menuMusic;
        }

        musicSource.loop = true;
        musicSource.volume = volume;
        musicSource.mute = volume <= 0.001f;
        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    private void SetVolume(float value)
    {
        volume = value;
        if (musicSource != null)
        {
            musicSource.volume = value;
            musicSource.mute = value <= 0.001f;
        }
    }
}
