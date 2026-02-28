using System.Collections;
using UnityEngine;
using Oculus.Interaction;

public class SensorSnapZone : MonoBehaviour
{
    public Transform snapTarget;
    public float magnetRange = 0.20f;
    public float snapDistance = 0.03f;
    public float snapAngle = 25f;
    public float pullSpeed = 10f;

    public bool pullWhileGrabbed = true;
    public bool lockAfterSnap = true;

    public Collider tableCollider; // optional: assign table collider

    private SensorState _sensor;
    private Grabbable _grabbable;
    private Rigidbody _rb;
    private bool _snapped;
    private Coroutine _routine;

    private Collider[] _sensorCols;
    private MonoBehaviour[] _metaBehaviours;

    private void Reset()
    {
        snapTarget = transform;
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_snapped) return;

        var sensor = other.GetComponentInParent<SensorState>();
        if (sensor == null) return;

        // only allow install if training is complete (chips inserted)
        if (!sensor.TrainingComplete) return;

        var grabbable = sensor.GetComponent<Grabbable>();
        if (grabbable == null) return;

        _sensor = sensor;
        _grabbable = grabbable;
        _rb = sensor.GetComponent<Rigidbody>();
        _sensorCols = sensor.GetComponentsInChildren<Collider>(true);
        _metaBehaviours = sensor.GetComponents<MonoBehaviour>();

        SetTableCollisionIgnored(true);

        if (_routine == null)
            _routine = StartCoroutine(MagnetLoop());
    }

    private void OnTriggerExit(Collider other)
    {
        var sensor = other.GetComponentInParent<SensorState>();
        if (sensor == null || sensor != _sensor) return;
        StopAll();
    }

    private IEnumerator MagnetLoop()
    {
        while (!_snapped && _sensor != null)
        {
            float dist = Vector3.Distance(_sensor.transform.position, snapTarget.position);
            float ang = Quaternion.Angle(_sensor.transform.rotation, snapTarget.rotation);

            if (dist <= magnetRange)
            {
                if (pullWhileGrabbed && IsGrabbed(_grabbable))
                {
                    DisableGrabBehaviours(true);
                    yield return null;
                }

                PullStep();

                dist = Vector3.Distance(_sensor.transform.position, snapTarget.position);
                ang = Quaternion.Angle(_sensor.transform.rotation, snapTarget.rotation);

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
            var t = _sensor.transform;
            t.position = Vector3.MoveTowards(t.position, snapTarget.position, pullSpeed * Time.deltaTime);
            t.rotation = Quaternion.Slerp(t.rotation, snapTarget.rotation, pullSpeed * Time.deltaTime);
        }
    }

    private void SnapNow()
    {
        _snapped = true;

        _sensor.transform.SetPositionAndRotation(snapTarget.position, snapTarget.rotation);
        _sensor.transform.SetParent(snapTarget, true);

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        SetTableCollisionIgnored(false);

        _sensor.SetInstalled(true);

        if (lockAfterSnap)
            DisableGrabBehaviours(true);
        else
            DisableGrabBehaviours(false);

        StopAll();
    }

    private void StopAll()
    {
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        if (!_snapped) SetTableCollisionIgnored(false);
        _sensor = null; _grabbable = null; _rb = null; _sensorCols = null; _metaBehaviours = null;
    }

    private bool IsGrabbed(Grabbable g) => g != null && g.SelectingPointsCount > 0;

    private void DisableGrabBehaviours(bool disabled)
    {
        if (_metaBehaviours == null) return;
        foreach (var mb in _metaBehaviours)
        {
            if (mb == null) continue;
            var n = mb.GetType().Name;
            if (n.Contains("GrabInteractable")) mb.enabled = !disabled;
        }
    }

    private void SetTableCollisionIgnored(bool ignored)
    {
        if (tableCollider == null || _sensorCols == null) return;
        foreach (var c in _sensorCols)
            if (c != null) Physics.IgnoreCollision(c, tableCollider, ignored);
    }
}