using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Player player; // Ссылка на скрипт Player
    [SerializeField] private string direction; // "up", "down", "left" или "right"

    private bool isHeld = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        isHeld = true;
        UpdatePlayer(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
        UpdatePlayer(false);
    }

    private void Update()
    {
        if (isHeld)
        {
            UpdatePlayer(true); // Вызываем непрерывно при удержании
        }
    }

    private void UpdatePlayer(bool isPressed)
    {
        if (player != null)
        {
            switch (direction.ToLower())
            {
                case "up":
                    player.up(isPressed);
                    break;
                case "down":
                    player.down(isPressed);
                    break;
                case "left":
                    player.left(isPressed);
                    break;
                case "right":
                    player.right(isPressed);
                    break;
            }
        }
    }
}
