using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    [Min(0f)] public float smoothTime = 0.15f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            target = player.transform;
        }

        Vector3 desired = new Vector3(target.position.x, target.position.y, transform.position.z);
        transform.position = smoothTime > 0f
            ? Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime)
            : desired;
    }
}
