using System;
using UnityEngine;

/// <summary>
/// Scenario controller for a straight bahn route with 3 stations:
/// Zürich -> Bern (stop) -> Luzern (stop) -> Bern (stop) -> Zürich (stop) -> repeat.
/// Movement is enabled only when the provided <see cref="ScenarioGate"/> is open.
/// </summary>
[AddComponentMenu("Siemens/Scenario/Straight Bahn Route Scenario")]
[DisallowMultipleComponent]
public sealed class StraightBahnRouteScenario : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    TrainOnRailAutoMover m_TrainMover;

    [SerializeField]
    RailwayPath m_Path;

    [SerializeField]
    [Tooltip("If set, the route runs only while this gate is open (puzzle solved).")]
    ScenarioGate m_Gate;

    [SerializeField]
    [Tooltip("If enabled, the route will not move unless a gate is assigned and open.")]
    bool m_RequireGate = true;

    [Header("Stations")]
    [SerializeField]
    Transform m_Zurich;

    [SerializeField]
    Transform m_Bern;

    [SerializeField]
    Transform m_Luzern;

    [Header("Timing & Movement")]
    [SerializeField]
    [Tooltip("Train speed along the rail while traveling.")]
    float m_CruiseSpeed = 0.5f;

    [SerializeField]
    [Tooltip("Seconds to stop at each station.")]
    float m_StopSeconds = 5f;

    [SerializeField]
    [Tooltip("Distance (meters) within which the train is considered 'arrived' at the target station.")]
    float m_ArriveDistance = 0.08f;

    [SerializeField]
    [Tooltip("If references are missing, automatically find the first TrainOnRailAutoMover/RailwayPath in the scene.")]
    bool m_AutoFindReferences = true;

    // Route indices: 0=Zürich, 1=Bern, 2=Luzern
    static readonly int[] k_Route = { 0, 1, 2, 1, 0 };

    int m_RouteStepIndex = -1;
    bool m_Waiting;
    float m_WaitUntil;

    void OnEnable()
    {
        m_RouteStepIndex = -1;
        m_Waiting = false;
    }

    void Update()
    {
        if (m_AutoFindReferences)
        {
            if (m_TrainMover == null)
                m_TrainMover = FindFirstObjectByType<TrainOnRailAutoMover>();
            if (m_Path == null)
                m_Path = FindFirstObjectByType<RailwayPath>();
        }

        if (m_TrainMover == null || m_Path == null)
            return;

        m_TrainMover.Path = m_Path;

        if (m_RequireGate)
        {
            if (m_Gate == null || !m_Gate.IsOpen)
            {
                m_TrainMover.Speed = 0f;
                m_Waiting = false;
                return;
            }
        }
        else
        {
            if (m_Gate != null && !m_Gate.IsOpen)
            {
                m_TrainMover.Speed = 0f;
                m_Waiting = false;
                return;
            }
        }

        if (m_Zurich == null || m_Bern == null || m_Luzern == null)
        {
            // Stations not configured.
            m_TrainMover.Speed = 0f;
            return;
        }

        if (m_RouteStepIndex < 0)
            InitializeRouteStep();

        if (m_Waiting)
        {
            m_TrainMover.Speed = 0f;
            if (Time.time >= m_WaitUntil)
            {
                m_Waiting = false;
                AdvanceRouteStep();
            }
            return;
        }

        // Travel toward the current target station.
        var targetStation = GetStationByIndex(k_Route[m_RouteStepIndex]);
        if (targetStation == null)
        {
            m_TrainMover.Speed = 0f;
            return;
        }

        if (!m_Path.TryGetClosestPoint(m_TrainMover.transform.position, out var trainOnRail, out var tangent))
            return;

        if (!m_Path.TryGetClosestPoint(targetStation.position, out var targetOnRail, out _))
            return;

        var arriveDist = Mathf.Max(0.01f, m_ArriveDistance);
        var arriveSqr = arriveDist * arriveDist;
        if ((trainOnRail - targetOnRail).sqrMagnitude <= arriveSqr)
        {
            // Snap to station, stop, then continue.
            m_TrainMover.transform.position = targetOnRail;
            m_TrainMover.Speed = 0f;
            m_Waiting = true;
            m_WaitUntil = Time.time + Mathf.Max(0f, m_StopSeconds);
            return;
        }

        // Decide direction based on dot against tangent at current location.
        var toTarget = targetOnRail - trainOnRail;
        var dot = Vector3.Dot(toTarget, tangent);
        var dir = dot >= 0f ? 1 : -1;

        m_TrainMover.Direction = dir;
        m_TrainMover.Speed = Mathf.Max(0f, m_CruiseSpeed);
    }

    void InitializeRouteStep()
    {
        // Choose a sensible start based on which station is closest to the train.
        var trainPos = m_TrainMover != null ? m_TrainMover.transform.position : Vector3.zero;

        var dZ = (m_Zurich.position - trainPos).sqrMagnitude;
        var dB = (m_Bern.position - trainPos).sqrMagnitude;
        var dL = (m_Luzern.position - trainPos).sqrMagnitude;

        var nearest = 0;
        var best = dZ;
        if (dB < best) { best = dB; nearest = 1; }
        if (dL < best) { best = dL; nearest = 2; }

        // Next destination based on where we are:
        // Zürich -> Bern, Bern -> Luzern, Luzern -> Bern
        m_RouteStepIndex = nearest switch
        {
            0 => 1, // go to Bern
            1 => 2, // go to Luzern
            _ => 3, // go to Bern
        };
    }

    void AdvanceRouteStep()
    {
        m_RouteStepIndex++;
        if (m_RouteStepIndex >= k_Route.Length)
            m_RouteStepIndex = 1; // after completing ...->Zürich, continue to Bern
    }

    Transform GetStationByIndex(int index)
    {
        return index switch
        {
            0 => m_Zurich,
            1 => m_Bern,
            2 => m_Luzern,
            _ => null,
        };
    }
}

