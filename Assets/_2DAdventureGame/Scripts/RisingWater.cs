using UnityEngine;

public class RisingWater : MonoBehaviour
{
    [Min(0f)] public float riseSpeed = 0.2f;
    public float maxHeight = -1.5f;
    [Min(1)] public int damage = 1;

    private void Update()
    {
        if (transform.position.y < maxHeight)
        {
            float nextY = Mathf.Min(maxHeight, transform.position.y + riseSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, nextY, transform.position.z);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerController controller = other.GetComponent<PlayerController>();
        if (controller != null) controller.ChangeHealth(-damage);
    }
}
