using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public InputAction LaunchAction; // Поле для дії стрільби

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

    void Start()
    {
        MoveAction.Enable();
        LaunchAction.Enable(); // Вмикаємо дію стрільби

        rigidbody2d = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
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
    }

    void FixedUpdate()
    {
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

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log(currentHealth + "/" + maxHealth);
    }
}