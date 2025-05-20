using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    public float dashDistance = 3f;
    public float cellSize = 0.15f;
    public float dashCooldown = 0.2f; // Cooldown between dashes
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private bool isDashing = false;
    public int health = 500;
    public Text healthDisplay;
    private float lastDashTime = -1f; // Time of last dash

    private int blockObstacleMask; // Mask for blocks, includes "Finish"
    private int playerDashMask;    // Mask for player, excludes "Finish" and "DamageObject"
    private int damageObjectMask;  // Mask for DamageObject

    // Damage variables
    public int blockMoveDamage = 50; // Damage taken when moving a block
    public int damageObjectDamage = 1; // Damage taken when passing through DamageObject
    private float damageCooldown = 0.1f; // Cooldown between damage applications
    private float lastDamageTime = -1f; // Time of last damage

    // Button input tracking
    private bool isUpPressed = false;
    private bool isDownPressed = false;
    private bool isLeftPressed = false;
    private bool isRightPressed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        // Mask for checking block movement (includes Finish)
        blockObstacleMask = LayerMask.GetMask("Block", "Walls", "NewBlock", "Finish");
        // Mask for player dash (excludes Finish and DamageObject to allow passing through)
        playerDashMask = LayerMask.GetMask("Block", "Walls", "NewBlock");
        // Mask for detecting DamageObject
        damageObjectMask = LayerMask.GetMask("DamageObject");
    }

    void Update()
    {
        healthDisplay.text = health.ToString();
        float movementH = Input.GetAxis("Horizontal");

        if (health <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (isDashing) return;

        if (movementH != 0)
        {
            sr.flipX = movementH < 0;
        }

        // Check for continuous keyboard input
        if (Input.GetKey(KeyCode.W) && CanDash())
        {
            Dash(Vector2.up);
        }
        else if (Input.GetKey(KeyCode.S) && CanDash())
        {
            Dash(Vector2.down);
        }
        else if (Input.GetKey(KeyCode.A) && CanDash())
        {
            Dash(Vector2.left);
        }
        else if (Input.GetKey(KeyCode.D) && CanDash())
        {
            Dash(Vector2.right);
        }

        // Check for continuous button input
        if (isUpPressed && CanDash())
        {
            Dash(Vector2.up);
        }
        else if (isDownPressed && CanDash())
        {
            Dash(Vector2.down);
        }
        else if (isLeftPressed && CanDash())
        {
            Dash(Vector2.left);
        }
        else if (isRightPressed && CanDash())
        {
            Dash(Vector2.right);
        }
    }

    // Button input methods
    public void up(bool isPressed)
    {
        isUpPressed = isPressed;
    }

    public void down(bool isPressed)
    {
        isDownPressed = isPressed;
    }

    public void left(bool isPressed)
    {
        isLeftPressed = isPressed;
    }

    public void right(bool isPressed)
    {
        isRightPressed = isPressed;
    }

    bool CanDash()
    {
        return Time.time - lastDashTime >= dashCooldown;
    }

    void Dash(Vector2 direction)
    {
        isDashing = true;
        lastDashTime = Time.time;

        // Use playerDashMask to allow passing through Finish and DamageObject
        RaycastHit2D hit = Physics2D.Raycast(rb.position, direction, dashDistance, playerDashMask);
        float k = cellSize / 2 + 0.01f;

        if (hit.collider != null)
        {
            if (hit.collider.CompareTag("Walls"))
            {
                // If hitting a wall, just stop at the hit point without applying any damage
                rb.MovePosition(rb.position + direction * (hit.distance - k));
            }
            else
            {
                // Check for DamageObject collision along the dash path
                Vector2 startPos = rb.position;
                Vector2 endPos = rb.position + direction * (hit.distance - k);
                Vector2 boxSize = new Vector2(cellSize, cellSize); // Adjust to player's collider size
                RaycastHit2D damageHit = Physics2D.BoxCast(startPos, boxSize, 0f, direction, Vector2.Distance(startPos, endPos), damageObjectMask);

                if (damageHit.collider != null && damageHit.collider.CompareTag("DamageObject"))
                {
                    ApplyDamageObjectDamage();
                }

                if (hit.collider.CompareTag("Block"))
                {
                    GameObject block = hit.collider.gameObject;
                    Vector3 newBlockPosition = block.transform.position + (Vector3)(direction * cellSize);
                    if (IsPositionClear(newBlockPosition, block))
                    {
                        block.transform.position = newBlockPosition;
                        rb.MovePosition(rb.position + direction * (hit.distance - k));
                        ApplyBlockMoveDamage();
                    }
                    else
                    {
                        rb.MovePosition(rb.position + direction * (hit.distance - k));
                    }
                }
                else if (hit.collider.CompareTag("NewBlock"))
                {
                    GameObject newBlock = hit.collider.gameObject;
                    Vector3 newBlockPosition = newBlock.transform.position + (Vector3)(direction * cellSize);
                    if (WillHitWallOrBlock(newBlockPosition, newBlock))
                    {
                        Destroy(newBlock);
                        rb.MovePosition(rb.position + direction * (hit.distance - k));
                        ApplyBlockMoveDamage();
                    }
                    else if (IsPositionClear(newBlockPosition, newBlock))
                    {
                        newBlock.transform.position = newBlockPosition;
                        rb.MovePosition(rb.position + direction * (hit.distance - k));
                        ApplyBlockMoveDamage();
                    }
                    else
                    {
                        rb.MovePosition(rb.position + direction * (hit.distance - k));
                    }
                }
            }
        }
        else
        {
            // No obstacle, check for DamageObject along the full dash path
            Vector2 startPos = rb.position;
            Vector2 endPos = rb.position + direction * dashDistance;
            Vector2 boxSize = new Vector2(cellSize, cellSize);
            RaycastHit2D damageHit = Physics2D.BoxCast(startPos, boxSize, 0f, direction, Vector2.Distance(startPos, endPos), damageObjectMask);

            if (damageHit.collider != null && damageHit.collider.CompareTag("DamageObject"))
            {
                ApplyDamageObjectDamage();
            }

            rb.MovePosition(rb.position + direction * dashDistance);
        }

        Invoke(nameof(ResetDash), 0.1f);
    }

    private bool IsPositionClear(Vector3 position, GameObject block)
    {
        BoxCollider2D blockCollider = block.GetComponent<BoxCollider2D>();
        return Physics2D.OverlapBox(position, blockCollider.size, 0f, blockObstacleMask) == null;
    }

    private bool WillHitWallOrBlock(Vector3 position, GameObject block)
    {
        BoxCollider2D blockCollider = block.GetComponent<BoxCollider2D>();
        return Physics2D.OverlapBox(position, blockCollider.size, 0f, blockObstacleMask) != null;
    }

    void ApplyBlockMoveDamage()
    {
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            health -= blockMoveDamage;
            lastDamageTime = Time.time;
        }
    }

    void ApplyDamageObjectDamage()
    {
        if (Time.time - lastDamageTime >= damageCooldown)
        {
            health -= damageObjectDamage;
            lastDamageTime = Time.time;
        }
    }

    void ResetDash()
    {
        isDashing = false;
    }
}