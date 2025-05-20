using UnityEngine;

public class DestroyWells : MonoBehaviour
{
    public string tagToDestroyOn = "Wells"; // Тег объекта, который должен уничтожить текущий объект

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(tagToDestroyOn))
        {
            Destroy(gameObject); // Уничтожаем текущий объект при столкновении с объектом нужного тега
        }
    }
}
