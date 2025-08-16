using UnityEngine;

public class GhostAngerUIBinder : MonoBehaviour
{
    public GhostAnger anger;      // �H��
    public AngerGaugeUI gauge;    // UI

    void LateUpdate()
    {
        if (anger && gauge) gauge.SetLevel(anger.Anger01);
    }
}
