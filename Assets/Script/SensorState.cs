using UnityEngine;

public class SensorState : MonoBehaviour
{
    public bool chipD25Installed;
    public bool chipD27Installed;

    public bool TrainingComplete => chipD25Installed && chipD27Installed;

    public bool InstalledOnTable { get; private set; }

    public void SetInstalled(bool installed)
    {
        InstalledOnTable = installed;
    }
}