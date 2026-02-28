using UnityEngine;

public class TrafficLightController : MonoBehaviour
{
    public GameObject redLight;
    public GameObject yellowLight;
    public GameObject greenLight;

    public void SetRed()
    {
        if (redLight) redLight.SetActive(true);
        if (yellowLight) yellowLight.SetActive(false);
        if (greenLight) greenLight.SetActive(false);
    }

    public void SetGreen()
    {
        if (redLight) redLight.SetActive(false);
        if (yellowLight) yellowLight.SetActive(false);
        if (greenLight) greenLight.SetActive(true);
    }
}