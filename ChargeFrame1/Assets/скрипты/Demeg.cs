using UnityEngine;

public class Demeg : MonoBehaviour
{
    public int damege = 1;
    public float damageCooldown = 0.1f; // Время между уронами
    private float lastDamageTime = -1f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                Player player = other.GetComponent<Player>();
                if (player != null)
                {
                    player.health -= damege;
                    lastDamageTime = Time.time;
                }
            }
        }
    }
}

