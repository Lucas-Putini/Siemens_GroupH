using UnityEngine;
using System.Collections;

/// <summary>
/// Spin the object at a specified speed. Can also focus on a specific target and resume spinning after.
/// </summary>
public class SpinFree : MonoBehaviour
{
    [Tooltip("Spin: Yes or No")]
    public bool spin = true;
    [Tooltip("Spin the parent object instead of the object this script is attached to")]
    public bool spinParent = false;
    public float speed = 10f;

    [HideInInspector] public bool clockwise = true;
    [HideInInspector] public float direction = 1f;
    [HideInInspector] public float directionChangeSpeed = 2f;

    private bool focusing = false;
    private Quaternion originalRotation;
    private float resumeSpeed = 10f;

    void Update()
    {
        if (direction < 1f)
            direction += Time.deltaTime / (directionChangeSpeed / 2);

        if (!focusing && spin)
        {
            if (clockwise)
            {
                if (spinParent)
                    transform.parent.transform.Rotate(Vector3.up, (speed * direction) * Time.deltaTime);
                else
                    transform.Rotate(Vector3.up, (speed * direction) * Time.deltaTime);
            }
            else
            {
                if (spinParent)
                    transform.parent.transform.Rotate(-Vector3.up, (speed * direction) * Time.deltaTime);
                else
                    transform.Rotate(-Vector3.up, (speed * direction) * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Instantly spins Earth so a world position comes to the front (camera side).
    /// </summary>
    public void FocusOnPoint(Vector3 targetWorld, float focusSpinSpeed = 120f, float resumeSpinSpeed = 10f)
    {
        StartCoroutine(FocusRoutine(targetWorld, focusSpinSpeed, resumeSpinSpeed));
    }

    private IEnumerator FocusRoutine(Vector3 targetWorld, float focusSpinSpeed, float normalSpinSpeed)
    {
        focusing = true;
        spin = false; // Stop normal spinning
        Transform rotTarget = spinParent ? transform.parent : transform;

        Vector3 toTarget = (targetWorld - rotTarget.position).normalized;
        Quaternion startRotation = rotTarget.rotation;
        Quaternion lookRotation = Quaternion.LookRotation(toTarget, Vector3.up);

        float t = 0;
        float duration = Quaternion.Angle(startRotation, lookRotation) / focusSpinSpeed; // Spin time = angle/speed

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            rotTarget.rotation = Quaternion.Slerp(startRotation, lookRotation, t);
            yield return null;
        }

        // (Optional: small pause here)
        yield return new WaitForSeconds(0.2f);

        spin = true;
        speed = normalSpinSpeed; // Restore normal speed (you can set a default)
        focusing = false;
    }
}
