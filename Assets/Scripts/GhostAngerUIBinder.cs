using UnityEngine;

public class GhostAngerUIBinder : MonoBehaviour
{
    public GhostAnger anger;      // —H—ì
    public AngerGaugeUI gauge;    // UI

    void LateUpdate()
    {
        if (anger && gauge) gauge.SetLevel(anger.Anger01);
    }
}
