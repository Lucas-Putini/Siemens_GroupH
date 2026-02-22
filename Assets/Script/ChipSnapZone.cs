using System.Collections;
using UnityEngine;


public class ChipSnapZone : MonoBehaviour
{
    [Header("Snap Requirements")]
    public ChipType accepts;
    public Transform snapTarget;
    public float magnetRange = 0.12f;     // start pulling when within this range
    public float snapDistance = 0.02f;    // final "lock" distance
    public float snapAngle = 20f;         // allowed rotation mismatch to snap

    [Header("Magnet Pull")]
    public float pullSpeed = 8f;          // higher = faster pull
    public bool pullWhileGrabbed = true;  // if true, will auto-release then pull

    [Header("Locking")]
    public bool lockAfterSnap = true;

    private ChipId _chip;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _grab;
    private Rigidbody _rb;
    private bool _snapped;
    private Coroutine _pullRoutine;

    private void Reset()
    {
        snapTarget = transform;
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_snapped) return;

        var chip = other.GetComponentInParent<ChipId>();
        if (!chip || chip.type != accepts) return;

        var grab = chip.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (!grab) return;

        _chip = chip;
        _grab = grab;
        _rb = chip.GetComponent<Rigidbody>();

        // start checking/pulling while inside
        if (_pullRoutine == null)
            _pullRoutine = StartCoroutine(MagnetLoop());
    }

    private void OnTriggerExit(Collider other)
    {
        var chip = other.GetComponentInParent<ChipId>();
        if (!chip || chip != _chip) return;

        StopMagnet();
    }

    private void StopMagnet()
    {
        if (_pullRoutine != null)
        {
            StopCoroutine(_pullRoutine);
            _pullRoutine = null;
        }

        _chip = null;
        _grab = null;
        _rb = null;
    }

    private IEnumerator MagnetLoop()
    {
        while (!_snapped && _chip != null && _grab != null)
        {
            float dist = Vector3.Distance(_chip.transform.position, snapTarget.position);
            float ang = Quaternion.Angle(_chip.transform.rotation, snapTarget.rotation);

            // If within magnet range, begin pull behavior
            if (dist <= magnetRange)
            {
                // If it's being held and we want pull-while-grabbed:
                if (_grab.isSelected && pullWhileGrabbed)
                {
                    // Force release from the hand
                    var interactor = _grab.firstInteractorSelecting;
                    if (interactor != null && _grab.interactionManager != null)
                    {
                        _grab.interactionManager.SelectExit(interactor, _grab);
                    }
                }

                // Now pull toward target (works whether it was grabbed or not)
                PullStep();

                // Snap condition
                if (dist <= snapDistance && ang <= snapAngle)
                {
                    SnapNow();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void PullStep()
    {
        Transform t = _chip.transform;

        // Move
        t.position = Vector3.MoveTowards(t.position, snapTarget.position, pullSpeed * Time.deltaTime);

        // Rotate
        t.rotation = Quaternion.Slerp(t.rotation, snapTarget.rotation, pullSpeed * Time.deltaTime);

        // Prevent physics fighting movement
        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    private void SnapNow()
    {
        _snapped = true;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        _chip.transform.position = snapTarget.position;
        _chip.transform.rotation = snapTarget.rotation;

        if (lockAfterSnap && _grab != null)
        {
            _grab.enabled = false;
        }

        StopMagnet();
    }
}