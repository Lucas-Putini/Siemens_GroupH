using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class ChipSnapZone : MonoBehaviour
{
    [Header("Snap Requirements")]
    public ChipType accepts;
    public Transform snapTarget;
    public float magnetRange = 0.12f;
    public float snapDistance = 0.02f;
    public float snapAngle = 20f;

    [Header("Magnet Pull")]
    public float pullSpeed = 8f;
    public bool pullWhileGrabbed = true;

    [Header("Locking")]
    public bool lockAfterSnap = true;

    [Header("Optional: Collision helpers")]
    public Collider boardCollider; // assign the board collider here

    private ChipId _chip;
    private Grabbable _grabbable;
    private Rigidbody _rb;
    private bool _snapped;
    private Coroutine _pullRoutine;

    private Collider[] _chipCols;
    private MonoBehaviour[] _metaGrabBehaviours; // GrabInteractable / HandGrabInteractable etc.

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
        if (chip == null || chip.type != accepts) return;

        var grabbable = chip.GetComponent<Grabbable>();
        if (grabbable == null) return;

        _chip = chip;
        _grabbable = grabbable;
        _rb = chip.GetComponent<Rigidbody>();
        _chipCols = chip.GetComponentsInChildren<Collider>(true);

        // cache common Meta grab behaviours so we can disable/enable them
        _metaGrabBehaviours = chip.GetComponents<MonoBehaviour>();

        // prevent board collision from blocking the pull
        SetBoardCollisionIgnored(true);

        if (_pullRoutine == null)
            _pullRoutine = StartCoroutine(MagnetLoop());
    }

    private void OnTriggerExit(Collider other)
    {
        var chip = other.GetComponentInParent<ChipId>();
        if (chip == null || chip != _chip) return;

        StopMagnet();
    }

    private void StopMagnet()
    {
        if (_pullRoutine != null)
        {
            StopCoroutine(_pullRoutine);
            _pullRoutine = null;
        }

        if (!_snapped)
            SetBoardCollisionIgnored(false);

        _chip = null;
        _grabbable = null;
        _rb = null;
        _chipCols = null;
        _metaGrabBehaviours = null;
    }

    private IEnumerator MagnetLoop()
    {
        while (!_snapped && _chip != null && _grabbable != null)
        {
            float dist = Vector3.Distance(_chip.transform.position, snapTarget.position);
            float ang = Quaternion.Angle(_chip.transform.rotation, snapTarget.rotation);

            if (dist <= magnetRange)
            {
                // If it is currently grabbed, force-release it when close enough
                if (pullWhileGrabbed && IsGrabbed(_grabbable))
                {
                    ForceReleaseByDisablingGrab();
                    // give 1 frame so the grab system fully drops it
                    yield return null;
                }

                PullStep();

                // re-check after pull step
                dist = Vector3.Distance(_chip.transform.position, snapTarget.position);
                ang = Quaternion.Angle(_chip.transform.rotation, snapTarget.rotation);

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
        if (_rb != null && !_rb.isKinematic)
        {
            _rb.MovePosition(Vector3.MoveTowards(_rb.position, snapTarget.position, pullSpeed * Time.deltaTime));
            _rb.MoveRotation(Quaternion.Slerp(_rb.rotation, snapTarget.rotation, pullSpeed * Time.deltaTime));
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        else
        {
            Transform t = _chip.transform;
            t.position = Vector3.MoveTowards(t.position, snapTarget.position, pullSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, snapTarget.rotation, pullSpeed * Time.deltaTime);
        }
    }

    private void SnapNow()
    {
        _snapped = true;

        // exact placement
        _chip.transform.SetPositionAndRotation(snapTarget.position, snapTarget.rotation);

        // parent so it follows the board/sensor movement
        _chip.transform.SetParent(snapTarget, true);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        // restore board collision (safe now)
        SetBoardCollisionIgnored(false);

        // lock it (keep disabled) so it can't be grabbed again
        if (lockAfterSnap)
        {
            DisableGrabBehaviours(true);
        }
        else
        {
            // if you want it still grabbable after snapping, re-enable:
            DisableGrabBehaviours(false);
        }

        StopMagnet();
    }

    private bool IsGrabbed(Grabbable g)
    {
        // works in most Meta Interaction SDK versions:
        return g.SelectingPointsCount > 0;
    }

    private void ForceReleaseByDisablingGrab()
    {
        // temporarily disable grab so hands drop it immediately
        DisableGrabBehaviours(true);
    }

    private void DisableGrabBehaviours(bool disabled)
    {
        if (_metaGrabBehaviours == null) return;

        foreach (var mb in _metaGrabBehaviours)
        {
            if (mb == null) continue;
            var n = mb.GetType().Name;

            // covers GrabInteractable + HandGrabInteractable
            if (n.Contains("GrabInteractable"))
            {
                mb.enabled = !disabled;
            }
        }
    }

    private void SetBoardCollisionIgnored(bool ignored)
    {
        if (boardCollider == null || _chipCols == null) return;

        foreach (var c in _chipCols)
        {
            if (c != null)
                Physics.IgnoreCollision(c, boardCollider, ignored);
        }
    }
}