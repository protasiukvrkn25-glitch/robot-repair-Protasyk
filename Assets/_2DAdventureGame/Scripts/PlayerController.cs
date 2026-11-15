using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction LaunchAction; // Поле для дії стрільби
    public InputAction TalkAction = new InputAction("Talk", InputActionType.Button, "<Keyboard>/x");

    public GameObject projectilePrefab; // Поле для префаба снаряда

    Rigidbody2D rigidbody2d;
    Vector2 move;
    
    Animator animator;
    Vector2 moveDirection = new Vector2(1, 0);

    public float speed = 3.0f;
    public int maxHealth = 5;
    
    public int health { get { return currentHealth; } }
    int currentHealth;

    public float timeInvincible = 2.0f;
    bool isInvincible;
    float damageCooldown;
    NonPlayerCharacter lastNonPlayerCharacter;
    bool isDead;

    void Start()
    {
        MoveAction.Enable();
        LaunchAction.Enable(); // Вмикаємо дію стрільби
        TalkAction.Enable();

        rigidbody2d = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        UIHandler uiHandler = UIHandler.instance;
        if (uiHandler == null) uiHandler = FindFirstObjectByType<UIHandler>();
        if (uiHandler != null) uiHandler.SetHealthValue(1.0f);
    }

    void Update()
    {
        if (isDead) return;
        move = MoveAction.ReadValue<Vector2>();

        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            moveDirection.Set(move.x, move.y);
            moveDirection.Normalize();
        }

        if (animator != null)
        {
            animator.SetFloat("Look X", moveDirection.x);
            animator.SetFloat("Look Y", moveDirection.y);
            animator.SetFloat("Speed", move.magnitude);
        }

        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }

        // Перевірка натискання кнопки стрільби у цьому кадрі
        if (LaunchAction.WasPressedThisFrame())
        {
            Launch();
        }

        NonPlayerCharacter targetNPC = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            rigidbody2d.position + Vector2.up * 0.2f, moveDirection, 1.5f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider.attachedRigidbody == rigidbody2d) continue;
            targetNPC = hit.collider.GetComponent<NonPlayerCharacter>();
            if (targetNPC != null) break;
            if (!hit.collider.isTrigger) break;
        }
        if (targetNPC != null)
        {
            if (lastNonPlayerCharacter != null && lastNonPlayerCharacter != targetNPC)
                lastNonPlayerCharacter.SetBubbleVisible(false);
            targetNPC.SetBubbleVisible(true);
            lastNonPlayerCharacter = targetNPC;
            if (TalkAction.WasPressedThisFrame() && UIHandler.instance != null)
                UIHandler.instance.DisplayDialogue();
        }
        else if (lastNonPlayerCharacter != null)
        {
            lastNonPlayerCharacter.SetBubbleVisible(false);
            lastNonPlayerCharacter = null;
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        Vector2 position = (Vector2)rigidbody2d.position + move * speed * Time.fixedDeltaTime;
        rigidbody2d.MovePosition(position);
    }

    // Створення та запуск снаряда з захистом від NullReferenceException
    void Launch()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("Помилка: Поле Projectile Prefab порожнє! Перетягніть префаб Projectile в Inspector робота.");
            return;
        }

        GameObject projectileObject = Instantiate(
            projectilePrefab,
            rigidbody2d.position + Vector2.up * 0.5f,
            Quaternion.identity
        );

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        if (projectile == null)
        {
            Debug.LogError("Помилка: На об'єкті Projectile відсутній скрипт Projectile.cs!");
            return;
        }

        projectile.Launch(moveDirection, 300);

        if (animator != null)
        {
            animator.SetTrigger("Launch");
        }
        else
        {
            Debug.LogError("Помилка: На PlayerCharacter відсутній компонент Animator!");
        }
    }

    public void ChangeHealth(int amount)
    {
        if (isDead) return;
        if (amount < 0)
        {
            if (isInvincible)
            {
                return;
            }
            isInvincible = true;
            damageCooldown = timeInvincible;

            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
        }

        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        if (currentHealth != previousHealth)
        {
            if (amount < 0)
            {
                GameAudioFeedback.PlayPlayerHit(transform.position);
                SpriteFlashFeedback flash = GetComponent<SpriteFlashFeedback>();
                if (flash == null) flash = gameObject.AddComponent<SpriteFlashFeedback>();
                flash.Play(new Color(1f, 0.83f, 0.28f));
            }
            else
            {
                GameAudioFeedback.PlayPickup(transform.position);
                SpriteFlashFeedback flash = GetComponent<SpriteFlashFeedback>();
                if (flash == null) flash = gameObject.AddComponent<SpriteFlashFeedback>();
                flash.Play(new Color(0.35f, 0.95f, 0.78f));
            }
        }
        Debug.Log($"Здоров'я робота: {currentHealth}/{maxHealth}");

        UIHandler uiHandler = UIHandler.instance;
        if (uiHandler == null) uiHandler = FindFirstObjectByType<UIHandler>();
        if (uiHandler != null)
        {
            uiHandler.SetHealthValue(currentHealth / (float)maxHealth);
            if (currentHealth == 0) uiHandler.DisplayLoseScreen();
        }

        if (currentHealth == 0) Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        EnemyController enemy = other.GetComponent<EnemyController>();
        if (enemy != null && enemy.isBroken) ChangeHealth(-1);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        EnemyController enemy = collision.collider.GetComponent<EnemyController>();
        if (enemy != null && enemy.isBroken) ChangeHealth(-1);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        move = Vector2.zero;
        MoveAction.Disable();
        LaunchAction.Disable();
        TalkAction.Disable();
        if (rigidbody2d != null) rigidbody2d.linearVelocity = Vector2.zero;
    }
}
