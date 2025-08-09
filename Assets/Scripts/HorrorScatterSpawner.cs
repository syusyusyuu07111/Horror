using System.Collections.Generic;
using UnityEngine;

public class HorrorScatterSpawner : MonoBehaviour
{
    [Header("�z�u�Ώ�")]
    public GameObject[] commonPrefabs;     // �ʏ�I�u�W�F�N�g�i���I/�Ƌ�/�G���Ȃǁj
    public GameObject[] rarePrefabs;       // ���A���o�i�l�`/����/������` ���j

    [Header("���E�͈�")]
    public int count = 60;                 // �����icommon+rare�j
    [Range(0f, 0.3f)] public float rareChance = 0.05f;
    public Vector3 areaSize = new Vector3(30, 0, 30); // XZ�͈́iY�͖����j
    public float minSpacing = 1.2f;        // �ŏ��Ԋu�iPoisson���j
    public int clusterCount = 5;           // �N���X�^�[�̐�
    public float clusterRadius = 3.5f;     // �N���X�^�[���a�i�����͖��Ɂj
    [Range(0f, 1f)] public float clusterWeight = 0.55f; // �N���X�^�[�D��x�i�����قǖ��W�j

    [Header("�������킹")]
    public LayerMask groundMask = ~0;      // �n�ʃ��C���[
    public float raycastHeight = 10f;      // �ォ�烌�C���΂�����
    public bool alignToSurface = true;     // �@���ɍ��킹�ČX����

    [Header("�����ڗh�炬")]
    public Vector2 yRotation = new Vector2(0, 360);    // Y��]�����_��
    public Vector2 uniformScale = new Vector2(0.9f, 1.15f); // ��l�X�P�[��
    public Vector2 tilt = new Vector2(0f, 4f);         // �킸���ɌX����p�x�i�z���[�炵���j

    [Header("�󔒃]�[���i�������󂯂�j")]
    public Transform[] keepClearCenters;   // �󂯂������S�i����/�ʘH/�v���C���[�X�^�[�g���j
    public float keepClearRadius = 3.0f;

    [Header("�V�[�h�i�Č����j")]
    public int seed = 12345;
    public bool useRandomSeed = true;

    // ���������Ǘ��i�Đ����p�j
    List<Transform> spawned = new List<Transform>();

    void Start()
    {
        Scatter();
    }

    [ContextMenu("Scatter (Regenerate)")]
    public void Scatter()
    {
        // �����폜
        for (int i = spawned.Count - 1; i >= 0; i--)
            if (spawned[i]) DestroyImmediate(spawned[i].gameObject);
        spawned.Clear();

        var rng = useRandomSeed ? new System.Random() : new System.Random(seed);

        // �N���X�^�[���S���Ɍ��߂�
        var clusters = new List<Vector3>();
        for (int i = 0; i < clusterCount; i++)
        {
            clusters.Add(RandomPointInArea(rng));
        }

        int placed = 0;
        int attempts = 0;
        int maxAttempts = count * 30;

        // Poisson���F�߂����Ȃ��悤�ɒu��
        var positions = new List<Vector3>();

        while (placed < count && attempts < maxAttempts)
        {
            attempts++;

            // �N���X�^�[��� or �����_��
            Vector3 basePos;
            if (clusters.Count > 0 && rng.NextDouble() < clusterWeight)
            {
                var c = clusters[rng.Next(clusters.Count)];
                // �N���X�^�[�~���Ƀ����_��
                Vector2 circle = RandomInsideCircle(rng) * clusterRadius;
                basePos = new Vector3(c.x + circle.x, 0f, c.z + circle.y);
            }
            else
            {
                basePos = RandomPointInArea(rng);
            }

            // �󔒃]�[���͔�����
            if (IsInsideKeepClear(basePos)) continue;

            // �ŏ��Ԋu�`�F�b�N
            bool tooClose = false;
            for (int i = 0; i < positions.Count; i++)
            {
                if ((positions[i] - basePos).sqrMagnitude < minSpacing * minSpacing)
                {
                    tooClose = true; break;
                }
            }
            if (tooClose) continue;

            // �n�ʂ�Raycast�ŗ��Ƃ�
            Vector3 rayFrom = transform.TransformPoint(new Vector3(basePos.x, raycastHeight, basePos.z));
            Vector3 down = Vector3.down;
            if (!Physics.Raycast(rayFrom, down, out RaycastHit hit, raycastHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // Prefab�I���i���A�����j
            GameObject prefab = (rng.NextDouble() < rareChance && rarePrefabs != null && rarePrefabs.Length > 0)
                ? rarePrefabs[rng.Next(rarePrefabs.Length)]
                : commonPrefabs[rng.Next(commonPrefabs.Length)];

            // ����
            Quaternion rot = Quaternion.Euler(
                rng.RangeFloat(-tilt.y, tilt.y),               // x �ق�̂�X����
                rng.RangeFloat(yRotation.x, yRotation.y),      // y �����_������
                rng.RangeFloat(-tilt.y, tilt.y)                // z �ق�̂�X����
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
        // ���[�J��XZ���Ń����_��
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
        // �ψ�T���v�����O�i���a��r�Ŏ��R�j
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
