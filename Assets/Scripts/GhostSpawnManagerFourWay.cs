using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhostSpawnManagerFourWay : MonoBehaviour
{
    [Header("�K�{�Q��")]
    public Transform player;
    public Camera playerCamera;
    public GameObject ghostPrefab;

    [Header("�����̊")]
    [Tooltip("ON: �J�����̌�����őO�㍶�E / OFF: �v���C���[ transform.forward �")]
    public bool useCameraForward = true;

    [Header("�o���^�C�~���O")]
    public float firstSpawnDelay = 3f;   // �J�n3�b�ōŏ���4��
    public float spawnCooldown = 6f;     // 2��ڈȍ~�̊Ԋu

    [Header("�o���ʒu�ݒ�i4�������ʁj")]
    [Tooltip("�v���C���[����̋����i���a�j")]
    public float spawnRadius = 3.5f;
    [Tooltip("�㉺�̃I�t�Z�b�g")]
    public float heightOffset = 0.0f;

    [Header("���S����")]
    [Tooltip("�ǂȂǂ̃��C���[�B�����Ɏh����Ȃ��悤����")]
    public LayerMask obstacleMask;
    [Tooltip("��₪�ǂɖ��܂��Ă����炱�̋������������ɉ����čĎ��s�i�ő�retries��j")]
    public float inwardStep = 0.4f;
    public int retries = 5;

    [Header("���o�i��ʁj")]
    public Image vignetteUI;
    public float vignetteMaxAlpha = 0.45f;
    public float warningLeadTime = 0.4f;

    [Header("���o�i��/�G�t�F�N�g�j")]
    public AudioSource oneShotSource;
    public AudioClip warningSE;
    public AudioClip spawnSE;
    [Tooltip("�o�����ɍĐ�����p�[�e�B�N��Prefab�i���E���ɂȂǁj")]
    public GameObject spawnFxPrefab;

    [Header("�t�F�[�h�i�H��{�̂̓����t�F�[�h�C���j")]
    public float fadeInTime = 0.25f;

    [Header("��]���킹")]
    [Tooltip("��������Ƀv���C���[��������������")]
    public bool facePlayerOnSpawn = true;

    [Header("�f�o�b�O")]
    public bool drawGizmos = true;
    [Tooltip("Gizmos�̖�󒷂�")]
    public float gizmoDirLen = 1.2f;

    float _cooldownTimer;

    void Start()
    {
        _cooldownTimer = firstSpawnDelay; // �����3�b��
        if (vignetteUI) SetVignetteAlpha(0f);
    }

    void Update()
    {
        if (!player || !playerCamera || !ghostPrefab) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = spawnCooldown;
            StartCoroutine(SpawnFourRoutine());
        }
    }

    IEnumerator SpawnFourRoutine()
    {
        // �\��
        if (warningSE && oneShotSource) oneShotSource.PlayOneShot(warningSE);
        if (vignetteUI) yield return StartCoroutine(VignetteBlink(warningLeadTime));
        else yield return new WaitForSeconds(warningLeadTime);

        // �O�㍶�E�̊�x�N�g��
        GetBasis(out Vector3 fwd, out Vector3 right);

        // 4�����̃^�[�Q�b�g�ʒu
        Vector3[] dirs = new Vector3[4] { fwd, -fwd, right, -right };

        // �e�������ƂɈʒu���聨����
        List<GameObject> spawned = new List<GameObject>(4);
        for (int i = 0; i < 4; i++)
        {
            if (TryPlaceAround(dirs[i], out Vector3 pos))
            {
                // �o��SE�i�ŏ���1�̂Ŗ炷���A�S�̂�1�񂾂��j
                if (i == 0 && spawnSE && oneShotSource) oneShotSource.PlayOneShot(spawnSE);

                // �H�쐶��
                var ghost = Instantiate(ghostPrefab, pos, Quaternion.identity);
                if (facePlayerOnSpawn)
                {
                    Vector3 look = player.position - pos; look.y = 0f;
                    if (look.sqrMagnitude > 0.001f)
                        ghost.transform.rotation = Quaternion.LookRotation(look.normalized);
                }

                // �o��FX
                if (spawnFxPrefab) Instantiate(spawnFxPrefab, pos, ghost.transform.rotation);

                // �t�F�[�h�C��
                StartCoroutine(FadeInGhost(ghost));

                spawned.Add(ghost);
            }
        }

        yield break;
    }

    // �O�㍶�E�̊�𓾂�i���K���A�������j
    void GetBasis(out Vector3 fwd, out Vector3 right)
    {
        if (useCameraForward && playerCamera)
        {
            fwd = playerCamera.transform.forward; fwd.y = 0f; fwd.Normalize();
            right = playerCamera.transform.right; right.y = 0f; right.Normalize();
        }
        else
        {
            // �v���C���[��Bforward�������łȂ��ꍇ�͐�����
            fwd = player ? player.forward : Vector3.forward;
            fwd.y = 0f; fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            right = new Vector3(fwd.z, 0f, -fwd.x); // ����n����
        }
    }

    // �w������� spawnRadius �������ꂽ�ʒu��T���i�ǂɐH�����񂾂�����ɑޔ��j
    bool TryPlaceAround(Vector3 dir, out Vector3 pos)
    {
        dir.y = 0f; dir.Normalize();
        Vector3 basePos = player.position + dir * spawnRadius;
        basePos.y += heightOffset;

        // ���̂܂ܖ��܂�`�F�b�N
        if (!Physics.CheckSphere(basePos, 0.2f, obstacleMask))
        {
            pos = basePos;
            return true;
        }

        // ���܂��Ă���������ɏ������߂��čĎ��s
        Vector3 step = -dir * inwardStep;
        Vector3 probe = basePos;
        for (int i = 0; i < retries; i++)
        {
            probe += step;
            if (!Physics.CheckSphere(probe, 0.2f, obstacleMask))
            {
                pos = probe;
                return true;
            }
        }

        // �_���Ȃ�v���C���[�̈ʒu�M���i���a��1/3�j�܂Ŋ񂹂�
        probe = player.position + dir * Mathf.Max(spawnRadius * 0.33f, 0.6f);
        probe.y += heightOffset;
        if (!Physics.CheckSphere(probe, 0.2f, obstacleMask))
        {
            pos = probe;
            return true;
        }

        pos = Vector3.zero;
        return false;
    }

    // --- �����t�F�[�h�C�� ---
    IEnumerator FadeInGhost(GameObject ghost)
    {
        var renderers = ghost.GetComponentsInChildren<Renderer>(true);

        List<Material> mats = new List<Material>();
        foreach (var r in renderers)
        {
            var newMats = r.materials; // ���̉�
            r.materials = newMats;
            mats.AddRange(newMats);
        }

        // �����A���t�@0 & �������ݒ�
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i];
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 0f; m.color = c;
            ForceTransparent(m);
        }

        // 0��1
        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeInTime);
            for (int i = 0; i < mats.Count; i++)
            {
                var m = mats[i];
                if (!m.HasProperty("_Color")) continue;
                var c = m.color; c.a = a; m.color = c;
            }
            yield return null;
        }

        // �ŏI1.0
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i];
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 1f; m.color = c;
        }
    }

    // --- �}�e���A���𔼓������[�h�ɁiStandard/URP�ȈՑΉ��j ---
    static void ForceTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // URP Lit: 1 Transparent
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite"))   m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // --- �\���r�l�b�g ---
    IEnumerator VignetteBlink(float duration)
    {
        float half = Mathf.Max(0.01f, duration * 0.5f);
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            SetVignetteAlpha(Mathf.Lerp(0f, vignetteMaxAlpha, t / half));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            SetVignetteAlpha(Mathf.Lerp(vignetteMaxAlpha, 0f, t / half));
            yield return null;
        }
        SetVignetteAlpha(0f);
    }

    void SetVignetteAlpha(float a)
    {
        if (!vignetteUI) return;
        var c = vignetteUI.color; c.a = a; vignetteUI.color = c;
    }

    // --- �M�Y�� ---
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !player) return;

        // ���a
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position + Vector3.up * heightOffset, spawnRadius);

        // �O�㍶�E���i�J������z��j
        GetBasis(out var fwd, out var right);
        Vector3 center = player.position + Vector3.up * heightOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center + fwd * (spawnRadius + gizmoDirLen));
        Gizmos.DrawLine(center, center - fwd * (spawnRadius + gizmoDirLen));
        Gizmos.DrawLine(center, center + right * (spawnRadius + gizmoDirLen));
        Gizmos.DrawLine(center, center - right * (spawnRadius + gizmoDirLen));
    }
}
