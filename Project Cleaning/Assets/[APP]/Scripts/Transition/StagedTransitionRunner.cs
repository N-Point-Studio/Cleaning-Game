using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// A self-destroying MonoBehaviour that persists across a single scene load 
/// to execute a staged scene transition.
/// 1. Loads an intermediary scene.
/// 2. Waits for a specified delay.
/// 3. Loads a final destination scene.
/// </summary>
public class StagedTransitionRunner : MonoBehaviour
{
    /// <summary>
    /// Starts the staged transition coroutine.
    /// </summary>
    /// <param name="intermediaryScene">The name of the scene to load first.</param>
    /// <param name="finalScene">The name of the final scene to load after the delay.</param>
    /// <param name="delay">The time to wait in the intermediary scene.</param>
    public void StartTransition(string intermediaryScene, string finalScene, float delay)
    {
        StartCoroutine(RunTransition(intermediaryScene, finalScene, delay));
    }

    private IEnumerator RunTransition(string intermediaryScene, string finalScene, float delay)
    {
        // 1. Asynchronously load the intermediary scene.
        // This allows the coroutine on this DontDestroyOnLoad object to continue running.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(intermediaryScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 2. Now in the intermediary scene, wait for the specified delay.
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // 3. Load the final destination scene.
        SceneManager.LoadScene(finalScene);

        // 4. The job is done, this object can now be destroyed.
        Destroy(gameObject);
    }
}
