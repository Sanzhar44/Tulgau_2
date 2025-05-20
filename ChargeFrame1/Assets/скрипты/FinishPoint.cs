using UnityEngine;

public class FinishPoint : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collesion)
    {
        if(collesion.CompareTag("Player"))
        {
            SceneController.instance.NextLevel();
        }
    }
}
