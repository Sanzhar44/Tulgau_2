using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Diagnostics;

public class SceneTransition : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("level1");
    }

}