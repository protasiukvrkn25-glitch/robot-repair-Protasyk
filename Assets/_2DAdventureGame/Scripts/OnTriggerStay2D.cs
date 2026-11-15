using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Min(1)]
    public int damage = 1;
    public float damageInterval = 1.0f; // Інтервал між шкодою (в секундах)
    private float nextDamageTime;

    void OnTriggerStay2D(Collider2D other)
    {
        PlayerController controller = other.GetComponent<PlayerController>();

        if (controller != null)
        {
            // Перевіряємо, чи минув час інтервалу
            if (Time.time >= nextDamageTime)
            {
                controller.ChangeHealth(-damage);
                nextDamageTime = Time.time + damageInterval; // Встановлюємо час наступної шкоди
            }
        }
    }
}
