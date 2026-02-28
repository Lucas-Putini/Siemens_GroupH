using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class RailSensorTriggerSpline : MonoBehaviour
{
    public SensorState installedSensor;          // drag your sensor object here
    public TrafficLightController trafficLight;  // drag the traffic light controller
    public float stopSeconds = 3f;

    private bool _busy;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_busy) return;

        var splineAnim = other.GetComponentInParent<SplineAnimate>();
        if (splineAnim == null) return;

        if (installedSensor == null) return;
        if (!installedSensor.TrainingComplete) return;
        if (!installedSensor.InstalledOnTable) return;

        StartCoroutine(StopRoutine(splineAnim));
    }

    private IEnumerator StopRoutine(SplineAnimate splineAnim)
    {
        _busy = true;

        if (trafficLight) trafficLight.SetRed();

        // Pause train
        splineAnim.Pause();

        yield return new WaitForSeconds(stopSeconds);

        // Resume train
        if (trafficLight) trafficLight.SetGreen();
        splineAnim.Play();

        yield return new WaitForSeconds(0.5f);
        _busy = false;
    }
}