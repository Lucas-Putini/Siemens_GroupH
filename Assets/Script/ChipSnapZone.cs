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

    // tracks if we temporarily disabled grab to force-release
    private bool _tempGrabDisabled;

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

        // cache Meta grab behaviours (GrabInteractable / HandGrabInteractable)
        _metaGrabBehaviours = chip.GetComponents<MonoBehaviour>();
        _tempGrabDisabled = false;

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

        // If we force-disabled grabbing but never snapped, restore it
        if (!_snapped && _tempGrabDisabled)
        {
            DisableGrabBehaviours(false);
            _tempGrabDisabled = false;
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
                // If grabbed and we want pull-while-grabbed: force-release near target
                if (pullWhileGrabbed && IsGrabbed(_grabbable) && !_tempGrabDisabled)
                {
                    ForceReleaseByDisablingGrab();
                    _tempGrabDisabled = true;

                    // give 1 frame so Meta grab system fully drops it
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

        // Mark sensor training progress (if chip belongs to a sensor)
        // Prefer looking up from snapTarget (since snap zones are usually on the sensor/board).
        var sensor = snapTarget != null ? snapTarget.GetComponentInParent<SensorState>() : null;
        if (sensor == null && _chip != null) sensor = _chip.GetComponentInParent<SensorState>();

        if (sensor != null)
        {
            if (accepts == ChipType.D25) sensor.chipD25Installed = true;
            if (accepts == ChipType.D27) sensor.chipD27Installed = true;
        }

        // exact placement
        _chip.transform.SetPositionAndRotation(snapTarget.position, snapTarget.rotation);

        // parent so it follows the sensor/board movement
        _chip.transform.SetParent(snapTarget, true);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        // restore collisions (safe now)
        SetBoardCollisionIgnored(false);

        // Lock behavior:
        // - if lockAfterSnap: keep grab disabled
        // - else: re-enable grab so user can remove chip later
        if (lockAfterSnap)
        {
            DisableGrabBehaviours(true);
        }
        else
        {
            DisableGrabBehaviours(false);
            _tempGrabDisabled = false;
        }

        StopMagnet();
    }

    private bool IsGrabbed(Grabbable g)
    {
        // works in most Meta Interaction SDK versions:
        return g != null && g.SelectingPointsCount > 0;
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