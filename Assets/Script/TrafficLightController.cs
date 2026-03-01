using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    [Header("Use only what you need")]
    public GameObject redON;
    public GameObject redOFF;

    public GameObject greenON;
    public GameObject greenOFF;

    private void Start()
    {
        SetGreen(); // default state at game start
    }

    public void SetRed()
    {
        if (redON) redON.SetActive(true);
        if (redOFF) redOFF.SetActive(false);

        if (greenON) greenON.SetActive(false);
        if (greenOFF) greenOFF.SetActive(true);
    }

    public void SetGreen()
    {
        if (redON) redON.SetActive(false);
        if (redOFF) redOFF.SetActive(true);

        if (greenON) greenON.SetActive(true);
        if (greenOFF) greenOFF.SetActive(false);
    }
}