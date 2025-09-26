using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;    // drag your Player here
    public float smoothTime = 0.15f;
    public Vector3 offset = new Vector3(0, 1, -10);

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (!target) return;

        // Smooth follow
        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, smoothTime);
    }
}
