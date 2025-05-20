using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGame : MonoBehaviour
{
    public string sceneName;

    public void LoadGame()
    {
        if (!string.IsNullOrEmpty(sceneName) && SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity") != -1)
        {
            SceneManager.LoadScene(sceneName);
        }
        
    }
}
