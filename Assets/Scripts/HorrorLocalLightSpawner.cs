using System.Collections.Generic;
using UnityEngine;

public class HorrorLocalLightSpawner : MonoBehaviour
{
    [Header("配置範囲 (XZ)")]
    public Vector3 areaSize = new Vector3(20, 0, 20);
    public int lightCount = 6;
    public float minSpacing = 3f;

    [Header("接地")]
    public LayerMask groundMask = ~0;
    public float raycastHeight = 6f;

    [Header("ライト設定")]
    public Vector2 intensityRange = new Vector2(0.6f, 1.2f);
    public Vector2 rangeRange = new Vector2(5f, 9f);
    public Color[] palette = { new Color(1f, 0.85f, 0.7f), new Color(0.7f, 0.8f, 1f) }; // 暖色/寒色
    public bool useSpotLights = false;            // trueにするとスポットライトで配置
    public float spotAngle = 45f;

    [Header("演出")]
    public bool addFlicker = true;
    public Vector2 flickerSpeed = new Vector2(6f, 12f);
    public Vector2 flickerDepth = new Vector2(0.25f, 0.5f); // 0?1で変動幅
    public bool randomSway = true;                // わずかな揺れ
    public float swayAmplitude = 2f;              // 度
    public float swaySpeed = 0.4f;

    [Header("シード")]
    public int seed = 1234;
    public bool randomSeed = true;

    readonly List<Light> spawned = new List<Light>();

    void Start() { Spawn(); }

    [ContextMenu("Respawn")]
    public void Spawn()
    {
        foreach (var l in spawned) if (l) DestroyImmediate(l.gameObject);
        spawned.Clear();

        var rng = randomSeed ? new System.Random() : new System.Random(seed);
        var placed = new List<Vector3>();
        int attempts = 0, maxAttempts = lightCount * 20;

        while (spawned.Count < lightCount && attempts++ < maxAttempts)
        {
            // XZ内に乱数
            Vector3 local = new Vector3(
                Rand(rng, -areaSize.x * 0.5f, areaSize.x * 0.5f),
                0,
                Rand(rng, -areaSize.z * 0.5f, areaSize.z * 0.5f)
            );
            // 最小間隔
            bool tooClose = false;
            foreach (var p in placed)
                if ((p - local).sqrMagnitude < minSpacing * minSpacing) { tooClose = true; break; }
            if (tooClose) continue;

            // 地面に落とす
            Vector3 from = transform.TransformPoint(local + Vector3.up * raycastHeight);
            if (!Physics.Raycast(from, Vector3.down, out var hit, raycastHeight * 2f, groundMask)) continue;

            // ライト生成
            var go = new GameObject("HorrorLight");
            go.transform.SetParent(transform);
            go.transform.position = hit.point + Vector3.up * 1.6f; // 低い天井を想定して少し上げる

            var light = go.AddComponent<Light>();
            light.type = useSpotLights ? LightType.Spot : LightType.Point;
            light.range = Rand(rng, rangeRange.x, rangeRange.y);
            light.intensity = Rand(rng, intensityRange.x, intensityRange.y);
            light.color = palette.Length > 0 ? palette[rng.Next(palette.Length)] : Color.white;
            if (useSpotLights)
            {
                light.spotAngle = spotAngle;
                go.transform.rotation = Quaternion.Euler(90f, Rand(rng, 0f, 360f), 0f); // 真下向き＋ランダムY
            }

            if (addFlicker)
            {
                var f = go.AddComponent<LightFlicker>();
                f.baseIntensity = light.intensity;
                f.speed = Rand(rng, flickerSpeed.x, flickerSpeed.y);
                f.depth = Rand(rng, flickerDepth.x, flickerDepth.y);
            }

            if (randomSway)
            {
                var s = go.AddComponent<LightSway>();
                s.amplitude = swayAmplitude;
                s.speed = Rand(rng, 0.6f * swaySpeed, 1.4f * swaySpeed);
            }

            spawned.Add(light);
            placed.Add(local);
        }
    }

    float Rand(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
