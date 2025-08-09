using System.Collections.Generic;
using UnityEngine;

public class HorrorLocalLightSpawner : MonoBehaviour
{
    [Header("�z�u�͈� (XZ)")]
    public Vector3 areaSize = new Vector3(20, 0, 20);
    public int lightCount = 6;
    public float minSpacing = 3f;

    [Header("�ڒn")]
    public LayerMask groundMask = ~0;
    public float raycastHeight = 6f;

    [Header("���C�g�ݒ�")]
    public Vector2 intensityRange = new Vector2(0.6f, 1.2f);
    public Vector2 rangeRange = new Vector2(5f, 9f);
    public Color[] palette = { new Color(1f, 0.85f, 0.7f), new Color(0.7f, 0.8f, 1f) }; // �g�F/���F
    public bool useSpotLights = false;            // true�ɂ���ƃX�|�b�g���C�g�Ŕz�u
    public float spotAngle = 45f;

    [Header("���o")]
    public bool addFlicker = true;
    public Vector2 flickerSpeed = new Vector2(6f, 12f);
    public Vector2 flickerDepth = new Vector2(0.25f, 0.5f); // 0?1�ŕϓ���
    public bool randomSway = true;                // �킸���ȗh��
    public float swayAmplitude = 2f;              // �x
    public float swaySpeed = 0.4f;

    [Header("�V�[�h")]
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
            // XZ���ɗ���
            Vector3 local = new Vector3(
                Rand(rng, -areaSize.x * 0.5f, areaSize.x * 0.5f),
                0,
                Rand(rng, -areaSize.z * 0.5f, areaSize.z * 0.5f)
            );
            // �ŏ��Ԋu
            bool tooClose = false;
            foreach (var p in placed)
                if ((p - local).sqrMagnitude < minSpacing * minSpacing) { tooClose = true; break; }
            if (tooClose) continue;

            // �n�ʂɗ��Ƃ�
            Vector3 from = transform.TransformPoint(local + Vector3.up * raycastHeight);
            if (!Physics.Raycast(from, Vector3.down, out var hit, raycastHeight * 2f, groundMask)) continue;

            // ���C�g����
            var go = new GameObject("HorrorLight");
            go.transform.SetParent(transform);
            go.transform.position = hit.point + Vector3.up * 1.6f; // �Ⴂ�V���z�肵�ď����グ��

            var light = go.AddComponent<Light>();
            light.type = useSpotLights ? LightType.Spot : LightType.Point;
            light.range = Rand(rng, rangeRange.x, rangeRange.y);
            light.intensity = Rand(rng, intensityRange.x, intensityRange.y);
            light.color = palette.Length > 0 ? palette[rng.Next(palette.Length)] : Color.white;
            if (useSpotLights)
            {
                light.spotAngle = spotAngle;
                go.transform.rotation = Quaternion.Euler(90f, Rand(rng, 0f, 360f), 0f); // �^�������{�����_��Y
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
