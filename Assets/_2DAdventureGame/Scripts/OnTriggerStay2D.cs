using UnityEngine;

public class DamageZone : MonoBehaviour
{
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
                controller.ChangeHealth(-1);
                nextDamageTime = Time.time + damageInterval; // Встановлюємо час наступної шкоди
            }
        }
    }
}