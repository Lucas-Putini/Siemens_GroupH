using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChipSnapZone : MonoBehaviour
{
    [Header("Snap Requirements")]
    public ChipType accepts;
    public Transform snapTarget;             // where chip ends up
    public float snapDistance = 0.03f;       // must be within this when released
    public float snapAngle = 15f;            // degrees allowed

    [Header("Magnet Pull (after release)")]
    public float pullSpeed = 12f;            // higher = faster
    public float pullDuration = 0.25f;       // seconds

    [Header("Locking")]
    public bool lockAfterSnap = true;

    private ChipId _candidate;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable _candidateGrab;
    private Rigidbody _candidateRb;
    private bool _occupied;

    private void Reset()
    {
        snapTarget = transform;
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_occupied) return;

        var chip = other.GetComponentInParent<ChipId>();
        if (!chip || chip.type != accepts) return;

        var grab = chip.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (!grab) return;

        _candidate = chip;
        _candidateGrab = grab;
        _candidateRb = chip.GetComponent<Rigidbody>();

        // Listen for release
        _candidateGrab.selectExited.AddListener(OnChipReleased);
    }

    private void OnTriggerExit(Collider other)
    {
        var chip = other.GetComponentInParent<ChipId>();
        if (!chip || chip != _candidate) return;

        CleanupCandidate();
    }

    private void OnChipReleased(SelectExitEventArgs args)
    {
        if (_occupied || _candidate == null) return;

        // Only snap if it's still inside trigger range AND close enough
        float dist = Vector3.Distance(_candidate.transform.position, snapTarget.position);
        float ang = Quaternion.Angle(_candidate.transform.rotation, snapTarget.rotation);

        if (dist <= snapDistance && ang <= snapAngle)
        {
            StartCoroutine(PullAndSnap());
        }
        // else: do nothing, user released too far or misaligned
    }

    private IEnumerator PullAndSnap()
    {
        _occupied = true;

        // Safety: stop physics fighting the pull
        if (_candidateRb)
        {
            _candidateRb.linearVelocity = Vector3.zero;
            _candidateRb.angularVelocity = Vector3.zero;
            _candidateRb.isKinematic = true;
        }

        Transform chipT = _candidate.transform;

        Vector3 startPos = chipT.position;
        Quaternion startRot = chipT.rotation;

        float t = 0f;
        while (t < pullDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pullDuration);

            chipT.position = Vector3.Lerp(startPos, snapTarget.position, k);
            chipT.rotation = Quaternion.Slerp(startRot, snapTarget.rotation, k);

            yield return null;
        }

        chipT.position = snapTarget.position;
        chipT.rotation = snapTarget.rotation;

        if (lockAfterSnap && _candidateGrab)
        {
            _candidateGrab.enabled = false; // locks it permanently
        }

        CleanupCandidate(); // remove listeners etc.
    }

    private void CleanupCandidate()
    {
        if (_candidateGrab != null)
            _candidateGrab.selectExited.RemoveListener(OnChipReleased);

        _candidate = null;
        _candidateGrab = null;
        _candidateRb = null;

        // NOTE: don't clear _occupied here; socket stays filled once snapped
    }
}