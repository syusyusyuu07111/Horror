using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Rendering;

public class HorrorLightingSetup : MonoBehaviour
{
    [Header("Ambient / Fog")]
    public Color ambientColor = new Color(0.05f, 0.07f, 0.09f);   // 暗い寒色
    public bool enableFog = true;
    public Color fogColor = new Color(0.04f, 0.05f, 0.06f, 1f);
    public float fogDensity = 0.015f; // 0.008〜0.03くらいで調整

    [Header("Main Light 処理")]
    public bool dimAllDirectionalLights = true;
    [Range(0f, 1.5f)] public float mainLightIntensity = 0.25f;
    public Color mainLightColor = new Color(0.6f, 0.7f, 0.9f);    // 冷たい色

    [Header("Quality")]
    public bool softerShadows = true;

    [ContextMenu("Apply Horror Lighting")]
    public void Apply()
    {
        // Ambient
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor;

        // Fog
        RenderSettings.fog = enableFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity;

        // Directional Lights を弱く
        if (dimAllDirectionalLights)
        {
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))

            {
                if (l.type == LightType.Directional)
                {
                    l.intensity = mainLightIntensity;
                    l.color = mainLightColor;
                }
            }
        }

        // 影を柔らかく（見た目が怖くなる）
        if (softerShadows)
        {
            QualitySettings.shadowProjection = ShadowProjection.CloseFit;
            QualitySettings.shadowCascades = 2;
        }

#if UNITY_EDITOR
        Debug.Log("Horror lighting applied.");
        EditorUtility.SetDirty(this);
#endif
    }
}
