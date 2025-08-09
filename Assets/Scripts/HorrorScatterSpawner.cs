using System.Collections.Generic;
using UnityEngine;

public class HorrorScatterSpawner : MonoBehaviour
{
    [Header("配置対象")]
    public GameObject[] commonPrefabs;     // 通常オブジェクト（瓦礫/家具/雑草など）
    public GameObject[] rarePrefabs;       // レア演出（人形/血痕/黒い手形 等）

    [Header("数・範囲")]
    public int count = 60;                 // 総数（common+rare）
    [Range(0f, 0.3f)] public float rareChance = 0.05f;
    public Vector3 areaSize = new Vector3(30, 0, 30); // XZ範囲（Yは無視）
    public float minSpacing = 1.2f;        // 最小間隔（Poisson風）
    public int clusterCount = 5;           // クラスターの数
    public float clusterRadius = 3.5f;     // クラスター半径（ここは密に）
    [Range(0f, 1f)] public float clusterWeight = 0.55f; // クラスター優先度（高いほど密集）

    [Header("高さ合わせ")]
    public LayerMask groundMask = ~0;      // 地面レイヤー
    public float raycastHeight = 10f;      // 上からレイを飛ばす高さ
    public bool alignToSurface = true;     // 法線に合わせて傾ける

    [Header("見た目揺らぎ")]
    public Vector2 yRotation = new Vector2(0, 360);    // Y回転ランダム
    public Vector2 uniformScale = new Vector2(0.9f, 1.15f); // 一様スケール
    public Vector2 tilt = new Vector2(0f, 4f);         // わずかに傾ける角度（ホラーらしさ）

    [Header("空白ゾーン（導線を空ける）")]
    public Transform[] keepClearCenters;   // 空けたい中心（入口/通路/プレイヤースタート等）
    public float keepClearRadius = 3.0f;

    [Header("シード（再現性）")]
    public int seed = 12345;
    public bool useRandomSeed = true;

    // 生成物を管理（再生成用）
    List<Transform> spawned = new List<Transform>();

    void Start()
    {
        Scatter();
    }

    [ContextMenu("Scatter (Regenerate)")]
    public void Scatter()
    {
        // 既存削除
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) DestroyImmediate(spawned[i].gameObject);
        spawned.Clear();

        var rng = useRandomSeed ? new System.Random() : new System.Random(seed);

        // クラスター中心を先に決める
        var clusters = new List<Vector3>();
        for (int i = 0; i < clusterCount; i++)
        {
            clusters.Add(RandomPointInArea(rng));
        }

        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 30;

        // Poisson風：近すぎないように置く
        var positions = new List<Vector3>();

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            // クラスター寄り or ランダム
            Vector3 basePos;
            if (clusters.Count > 0 && rng.NextDouble() < clusterWeight)
            {
                var c = clusters[rng.Next(clusters.Count)];
                // クラスター円内にランダム
                Vector2 circle = RandomInsideCircle(rng) * clusterRadius;
                basePos = new Vector3(c.x + circle.x, 0f, c.z + circle.y);
            }
            else
            {
                basePos = RandomPointInArea(rng);
            }

            // 空白ゾーンは避ける
            if (IsInsideKeepClear(basePos)) continue;

            // 最小間隔チェック
            bool tooClose = false;
            for (int i = 0; i < positions.Count; i++)
            {
                if ((positions[i] - basePos).sqrMagnitude < minSpacing * minSpacing)
                {
                    tooClose = true; break;
                }
            }
            if (tooClose) continue;

            // 地面にRaycastで落とす
            Vector3 rayFrom = transform.TransformPoint(new Vector3(basePos.x, raycastHeight, basePos.z));
            Vector3 down = Vector3.down;
            if (!Physics.Raycast(rayFrom, down, out RaycastHit hit, raycastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // Prefab選択（レア混入）
            GameObject prefab = (rng.NextDouble() < rareChance && rarePrefabs != null && rarePrefabs.Length > 0)
                ? rarePrefabs[rng.Next(rarePrefabs.Length)]
                : commonPrefabs[rng.Next(commonPrefabs.Length)];

            // 生成
            Quaternion rot = Quaternion.Euler(
                rng.RangeFloat(-tilt.y, tilt.y),               // x ほんのり傾ける
                rng.RangeFloat(yRotation.x, yRotation.y),      // y ランダム向き
                rng.RangeFloat(-tilt.y, tilt.y)                // z ほんのり傾ける
            );
            if (alignToSurface) rot = Quaternion.FromToRotation(Vector3.up, hit.normal) * rot;

            float s = rng.RangeFloat(uniformScale.x, uniformScale.y);

            var go = Instantiate(prefab, hit.point, rot, transform);
            go.transform.localScale = Vector3.one * s;

            positions.Add(basePos);
            spawned.Add(go.transform);
            placed++;
        }
    }

    Vector3 RandomPointInArea(System.Random rng)
    {
        // ローカルXZ内でランダム
        float x = rng.RangeFloat(-areaSize.x * 0.5f, areaSize.x * 0.5f);
        float z = rng.RangeFloat(-areaSize.z * 0.5f, areaSize.z * 0.5f);
        return new Vector3(x, 0f, z);
    }

    bool IsInsideKeepClear(Vector3 localXZ)
    {
        if (keepClearCenters == null) return false;
        Vector3 world = transform.TransformPoint(localXZ);
        foreach (var c in keepClearCenters)
        {
            if (!c) continue;
            if ((new Vector3(world.x, 0, world.z) - new Vector3(c.position.x, 0, c.position.z)).sqrMagnitude
                < keepClearRadius * keepClearRadius) return true;
        }
        return false;
    }

    Vector2 RandomInsideCircle(System.Random rng)
    {
        // 均一サンプリング（半径√rで自然）
        float t = (float)rng.NextDouble() * 2f * Mathf.PI;
        float r = Mathf.Sqrt((float)rng.NextDouble());
        return new Vector2(Mathf.Cos(t), Mathf.Sin(t)) * r;
    }
}

static class RandomX
{
    public static float RangeFloat(this System.Random r, float min, float max)
        => min + (float)r.NextDouble() * (max - min);
}
