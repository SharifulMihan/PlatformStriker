using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MinimalMenu
{
    public class MainMenuController : MonoBehaviour
    {
        public string FirstLevelSceneName { get; set; }
        public ScreenFader Fader { get; set; }

        public void OnPlayPressed()
        {
            OnLevelPressed(FirstLevelSceneName);
        }

        public void OnLevelPressed(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("MainMenuController: No level scene name specified.");
                return;
            }

            StartCoroutine(LoadLevelRoutine(sceneName));
        }

        private IEnumerator LoadLevelRoutine(string sceneName)
        {
            if (Fader != null)
            {
                yield return Fader.FadeOut(0.35f);
            }

            var asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            while (asyncLoad != null && !asyncLoad.isDone)
            {
                yield return null;
            }
        }

        public void OnQuitPressed()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}