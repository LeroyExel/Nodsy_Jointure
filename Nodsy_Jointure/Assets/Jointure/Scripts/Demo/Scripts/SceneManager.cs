using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jointure.Samples
{
    public class SceneManager : MonoBehaviour
    {
        public void Quit()
        {
            Application.Quit(); //Quits the application
        }

        public void LoadScene(int sceneIndex)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneIndex);
        }

        public void LoadMainMenu()
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
                if (NameFromIndex(i) == "MainMenu")
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(i);
                    return;
                }

            Debug.LogError("Must have a scene called \"MainMenu\" in your build profile!");
        }

        private string NameFromIndex(int BuildIndex)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(BuildIndex);
            int slash = path.LastIndexOf('/');
            string name = path.Substring(slash + 1);
            int dot = name.LastIndexOf('.');
            return name.Substring(0, dot);
        }
    }
}
