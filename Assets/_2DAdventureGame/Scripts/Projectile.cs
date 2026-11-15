using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D body2D;
    private PlayerController targetPlayer;
    private Vector2 previousPosition;
    private bool hasHit;

    public bool damagesPlayer;
    public int damage = 1;

    private void Awake()
    {
        body2D = GetComponent<Rigidbody2D>();
        previousPosition = transform.position;
    }

    private void Update()
    {
        if (damagesPlayer && targetPlayer != null && !hasHit)
        {
            Vector2 currentPosition = transform.position;
            Vector2 segment = currentPosition - previousPosition;
            float lengthSquared = segment.sqrMagnitude;
            float t = lengthSquared > 0f
                ? Mathf.Clamp01(Vector2.Dot((Vector2)targetPlayer.transform.position - previousPosition, segment) / lengthSquared)
                : 0f;
            Vector2 closestPoint = previousPosition + t * segment;
            if (((Vector2)targetPlayer.transform.position - closestPoint).sqrMagnitude <= 0.8f * 0.8f)
            {
                HitTarget(targetPlayer.gameObject);
                return;
            }
            previousPosition = currentPosition;
        }

        if (transform.position.magnitude > 100f)
            Destroy(gameObject);
    }

    public void SetTarget(PlayerController player)
    {
        targetPlayer = player;
    }

    public void Launch(Vector2 direction, float force)
    {
        if (body2D != null)
            body2D.AddForce(direction * force);
    }

    public void LaunchAtSpeed(Vector2 direction, float speed)
    {
        previousPosition = transform.position;
        if (body2D != null)
            body2D.linearVelocity = direction.normalized * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HitTarget(other.gameObject);
    }

    private void HitTarget(GameObject other)
    {
        if (hasHit) return;

        if (damagesPlayer)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null) player.ChangeHealth(-Mathf.Abs(damage));
        }
        else
        {
            EnemyController enemy = other.GetComponentInParent<EnemyController>();
            if (enemy != null) enemy.TakeDamage(Mathf.Abs(damage));
        }

        hasHit = true;
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        Destroy(gameObject);
    }
}
