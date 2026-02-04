using UnityEngine;
using System.Collections;

public class EarthSpinner : MonoBehaviour
{
    public float focusSpinSpeed = 120f; // Degrees per second
    public float resumeSpinSpeed = 10f; // Normal spin speed after focus

    private bool focusing = false;

    public IEnumerator SpinToFace(Vector3 worldPoint)
    {
        focusing = true;
        // Direction from Earth center to target point (in world space)
        Vector3 toTarget = (worldPoint - transform.position).normalized;
        // Calculate the rotation that makes 'forward' face the target
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(toTarget, Vector3.up);

        float angle = Quaternion.Angle(startRotation, targetRotation);
        float duration = angle / focusSpinSpeed;
        float t = 0;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
        focusing = false;
    }
}
