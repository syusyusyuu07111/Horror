using UnityEngine;

[DisallowMultipleComponent]
public class LightFlicker : MonoBehaviour
{
    public float baseIntensity = 1f;
    public float depth = 0.4f;     // 0〜1: 揺れ幅
    public float speed = 8f;       // 速さ
    public float glitchChance = 0.04f;  // 稀に大きく瞬く

    Light _light; float _t;

    void Awake() { _light = GetComponent<Light>(); }

    void Update()
    {
        _t += Time.deltaTime * speed;
        float n = Mathf.PerlinNoise(_t, 0f) * 2f - 1f;          // -1..1
        float flicker = 1f + n * depth;                          // 1±depth
        if (Random.value < glitchChance * Time.deltaTime * 60f)  // たまにグリッチ
            flicker *= Random.Range(0.2f, 1.6f);

        _light.intensity = Mathf.Max(0f, baseIntensity * flicker);
    }
}
