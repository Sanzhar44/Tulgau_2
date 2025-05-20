using UnityEngine;
using UnityEngine.SceneManagement;  // Fixed capitalization

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void NextLevel()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex + 1);  // Fixed typos and capitalization
    }

    public void LoadScene(string sceneName)  // Fixed typo in method name
    {
        SceneManager.LoadSceneAsync(sceneName);  // Fixed typo and capitalization
    }
}
