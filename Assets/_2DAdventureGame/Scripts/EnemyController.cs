using UnityEngine;
using UnityEngine.Tilemaps;

public class EnemyController : MonoBehaviour
{
    public float speed = 3f;
    public bool vertical;
    public float changeTime = 3f;

    [Header("Health")]
    public int maxHealth = 1;
    public bool showHealthBar;

    [Header("Ranged attack")]
    public bool rangedAttack;
    public GameObject projectilePrefab;
    public float attackRange = 7f;
    public float fireInterval = 1.25f;
    public float projectileSpeed = 6f;
    public float shootOriginOffset = 2.2f;

    [Header("Boss") ]
    public bool isBoss;
    public bool traverseWholeMap;
    public float bossTravelSpeed = 2.6f;

    private static readonly int MoveX = Animator.StringToHash("Move X");
    private static readonly int MoveY = Animator.StringToHash("Move Y");
    private static readonly int FixedTrigger = Animator.StringToHash("Fixed");

    private float timer;
    private float fireTimer;
    private int direction = 1;
    private int currentHealth;
    private Rigidbody2D body2D;
    private Animator animator;
    private PlayerController player;
    private bool broken = true;
    private SpriteRenderer[] healthSegments;
    private Sprite healthSprite;
    private Vector2[] patrolPoints;
    private int patrolWaypoint;

    public bool isBroken { get { return broken; } }
    public int health { get { return currentHealth; } }

    private void Start()
    {
        body2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = FindFirstObjectByType<PlayerController>();
        timer = changeTime;
        fireTimer = 0f;
        currentHealth = Mathf.Max(1, maxHealth);
        UpdateAnimationDirection();
        if (showHealthBar) CreateHealthBar();
        if (traverseWholeMap) InitializePatrol();
    }

    private void Update()
    {
        if (!broken) return;

        if (!traverseWholeMap)
        {
            timer -= Time.deltaTime;
            if (timer < 0f)
            {
                direction = -direction;
                timer = changeTime;
                UpdateAnimationDirection();
            }
        }

        if (rangedAttack) TryShootAtPlayer();
    }

    private void FixedUpdate()
    {
        if (!broken || body2D == null) return;

        if (traverseWholeMap && patrolPoints != null && patrolPoints.Length > 0)
        {
            MoveAlongPatrol();
            return;
        }

        Vector2 position = body2D.position;
        if (vertical)
            position.y += speed * direction * Time.fixedDeltaTime;
        else
            position.x += speed * direction * Time.fixedDeltaTime;

        body2D.MovePosition(position);
    }

    private void InitializePatrol()
    {
        Tilemap[] maps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        Bounds mapBounds = new Bounds(transform.position, new Vector3(30f, 16f, 0f));
        bool foundBounds = false;
        foreach (Tilemap map in maps)
        {
            if (map == null || map.cellBounds.size.x == 0 || map.cellBounds.size.y == 0) continue;
            BoundsInt cells = map.cellBounds;
            Vector3 min = map.CellToWorld(cells.min);
            Vector3 max = map.CellToWorld(cells.max);
            Bounds candidate = new Bounds((min + max) * 0.5f, max - min);
            if (!foundBounds || candidate.size.sqrMagnitude > mapBounds.size.sqrMagnitude)
            {
                mapBounds = candidate;
                foundBounds = true;
            }
        }

        Vector3 extents = mapBounds.extents;
        float margin = 1.4f;
        float minX = mapBounds.center.x - Mathf.Max(2f, extents.x - margin);
        float maxX = mapBounds.center.x + Mathf.Max(2f, extents.x - margin);
        float minY = mapBounds.center.y - Mathf.Max(2f, extents.y - margin);
        float maxY = mapBounds.center.y + Mathf.Max(2f, extents.y - margin);
        patrolPoints = new Vector2[]
        {
            new Vector2(minX, minY), new Vector2(maxX, minY),
            new Vector2(maxX, maxY), new Vector2(minX, maxY)
        };
        patrolWaypoint = GetClosestPatrolPoint(transform.position);
    }

    private int GetClosestPatrolPoint(Vector2 position)
    {
        int closest = 0;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = (patrolPoints[i] - position).sqrMagnitude;
            if (distance < bestDistance) { bestDistance = distance; closest = i; }
        }
        return closest;
    }

    private void MoveAlongPatrol()
    {
        Vector2 position = body2D.position;
        Vector2 target = patrolPoints[patrolWaypoint];
        Vector2 delta = target - position;
        Vector2 directionToTarget = delta.normalized;
        if (animator != null)
        {
            SetAnimatorFloat(MoveX, directionToTarget.x);
            SetAnimatorFloat(MoveY, directionToTarget.y);
        }
        body2D.MovePosition(Vector2.MoveTowards(position, target, bossTravelSpeed * Time.fixedDeltaTime));
        if (delta.sqrMagnitude < 0.16f) patrolWaypoint = (patrolWaypoint + 1) % patrolPoints.Length;
    }

    private void TryShootAtPlayer()
    {
        if (projectilePrefab == null) return;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        fireTimer -= Time.deltaTime;
        Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (toPlayer.sqrMagnitude > attackRange * attackRange || fireTimer > 0f) return;

        Vector2 shotDirection = toPlayer.normalized;
        Vector3 spawnPosition = GetShotSpawnPosition(shotDirection);
        GameObject shot = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        Projectile projectile = shot.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.damagesPlayer = true;
            projectile.damage = 1;
            projectile.SetTarget(player);
            shot.layer = 0;
            IgnoreShooterCollision(shot);
            projectile.LaunchAtSpeed(shotDirection, projectileSpeed);
        }
        else
        {
            Destroy(shot);
            Debug.LogError("Enemy projectile prefab is missing Projectile.cs", this);
        }

        RegisterShotHit(shotDirection, toPlayer.magnitude);
        fireTimer = fireInterval;
    }

    private Vector3 GetShotSpawnPosition(Vector2 shotDirection)
    {
        float offset = Mathf.Max(0f, shootOriginOffset);
        Collider2D shooterCollider = GetComponent<Collider2D>();
        if (shooterCollider != null)
        {
            Bounds bounds = shooterCollider.bounds;
            Vector2 origin = transform.position;
            float exitX = Mathf.Abs(shotDirection.x) > 0.001f
                ? (shotDirection.x > 0f ? bounds.max.x - origin.x : bounds.min.x - origin.x) / shotDirection.x
                : float.PositiveInfinity;
            float exitY = Mathf.Abs(shotDirection.y) > 0.001f
                ? (shotDirection.y > 0f ? bounds.max.y - origin.y : bounds.min.y - origin.y) / shotDirection.y
                : float.PositiveInfinity;
            float colliderExit = Mathf.Min(exitX, exitY);
            if (colliderExit > 0f)
                offset = Mathf.Max(offset, colliderExit + 0.5f);
        }
        return transform.position + (Vector3)(shotDirection * offset);
    }

    private void IgnoreShooterCollision(GameObject shot)
    {
        Collider2D[] shooterColliders = GetComponents<Collider2D>();
        Collider2D[] shotColliders = shot.GetComponents<Collider2D>();
        foreach (Collider2D shooter in shooterColliders)
        {
            foreach (Collider2D projectile in shotColliders)
                Physics2D.IgnoreCollision(shooter, projectile, true);
        }
    }
    private void RegisterShotHit(Vector2 shotDirection, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, shotDirection, distance);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.GetComponentInParent<EnemyController>() == this) continue;

            PlayerController hitPlayer = hit.collider.GetComponentInParent<PlayerController>();
            if (hitPlayer != null)
            {
                hitPlayer.ChangeHealth(-1);
                return;
            }

            if (!hit.collider.isTrigger) return;
        }
    }
    private bool HasAnimatorParameter(int parameterHash, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == parameterHash && parameter.type == type) return true;
        return false;
    }

    private void SetAnimatorFloat(int parameterHash, float value)
    {
        if (HasAnimatorParameter(parameterHash, AnimatorControllerParameterType.Float))
            animator.SetFloat(parameterHash, value);
    }

    private void UpdateAnimationDirection()
    {
        if (animator == null) return;
        SetAnimatorFloat(MoveX, vertical ? 0f : direction);
        SetAnimatorFloat(MoveY, vertical ? direction : 0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController hitPlayer = other.GetComponentInParent<PlayerController>();
        if (hitPlayer != null && broken)
            hitPlayer.ChangeHealth(-1);
    }

    public void TakeDamage(int amount = 1)
    {
        if (!broken || amount <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateHealthBar();
        if (currentHealth > 0)
        {
            GameAudioFeedback.PlayRobotHit(transform.position);
            SpriteFlashFeedback flash = GetComponent<SpriteFlashFeedback>();
            if (flash == null) flash = gameObject.AddComponent<SpriteFlashFeedback>();
            flash.Play(new Color(1f, 0.83f, 0.28f));
        }
        if (currentHealth == 0) Fix();
    }

    private void CreateHealthBar()
    {
        int count = Mathf.Max(1, maxHealth);
        healthSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        float segmentWidth = count >= 10 ? 0.1f : 0.26f;
        float gap = count >= 10 ? 0.035f : 0.05f;
        float totalWidth = count * segmentWidth + (count - 1) * gap;
        float barWidth = totalWidth + 0.16f;

        GameObject background = new GameObject("HealthBar_Background");
        background.transform.SetParent(transform, false);
        background.transform.localPosition = new Vector3(0f, 1.78f, 0f);
        background.transform.localScale = new Vector3(barWidth, 0.22f, 1f);
        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = healthSprite;
        backgroundRenderer.color = new Color(0.08f, 0.1f, 0.13f, 0.95f);
        backgroundRenderer.sortingOrder = 100;

        healthSegments = new SpriteRenderer[count];
        for (int i = 0; i < count; i++)
        {
            GameObject segment = new GameObject("HealthBar_HP_" + (i + 1));
            segment.transform.SetParent(background.transform, false);
            segment.transform.localScale = new Vector3(segmentWidth / barWidth, 0.58f, 1f);
            segment.transform.localPosition = new Vector3(
                (i - (count - 1) * 0.5f) * (segmentWidth + gap) / barWidth,
                0f,
                -0.01f);
            SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
            renderer.sprite = healthSprite;
            renderer.sortingOrder = 101;
            healthSegments[i] = renderer;
        }

        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthSegments == null) return;
        for (int i = 0; i < healthSegments.Length; i++)
            healthSegments[i].color = i < currentHealth
                ? new Color(0.2f, 0.95f, 0.35f, 1f)
                : new Color(0.28f, 0.31f, 0.34f, 1f);
    }

    private void OnDestroy()
    {
        if (healthSprite != null) Destroy(healthSprite);
    }

    public void Fix()
    {
        if (!broken) return;
        broken = false;
        GameAudioFeedback.PlayRobotFixed(transform.position);
        if (body2D != null) body2D.simulated = false;
        if (HasAnimatorParameter(FixedTrigger, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(FixedTrigger);
    }
}
