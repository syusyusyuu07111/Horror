using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ForwardGhostSpawner : MonoBehaviour
{
    [Header("�K�{�Q��")]
    public Transform player;
    public Camera playerCamera;
    public GameObject ghostPrefab;

    [Header("�v���C���[�ړ��̎擾")]
    public Rigidbody playerRb;                 // �C�Ӂi����ΐ��xUP�j
    public float speedThreshold = 0.05f;       // ��~����
    public float headingSmoothTime = 0.2f;     // �i�s�����̂Ȃ߂炩��

    [Header("�o���^�C�~���O")]
    public float firstSpawnDelay = 3f;         // �J�n���珉��
    public float spawnCooldown = 6f;         // �ȍ~�̊Ԋu

    [Header("�o���ʒu�i�i�s�����x�[�X�j")]
    public float minSpawnDistance = 3.0f;
    public float maxSpawnDistance = 5.0f;
    public float lateralJitterMin = -1.0f;     // ���E�u���ŏ�
    public float lateralJitterMax = 1.0f;     // ���E�u���ő�
    public float heightOffset = 0.0f;

    [Header("����Ə�Q��")]
    public bool requireOffscreen = false;      // ��ʊO�Ɍ��肵�����Ȃ�ON
    public float viewSafeAngle = 50f;        // ��ʊO�v�����̈��S�p�x
    public LayerMask obstacleMask;

    [Header("���o�i��ʁj")]
    public Image vignetteUI;
    public float vignetteMaxAlpha = 0.45f;
    public float warningLeadTime = 0.6f;

    [Header("���o�i��/�G�t�F�N�g�j")]
    public AudioSource oneShotSource;
    public AudioClip warningSE;
    public AudioClip spawnSE;
    public GameObject spawnFxPrefab;

    [Header("�t�F�[�h�ݒ�i�����x�ŏo���j")]
    public float fadeInTime = 0.25f;

    [Header("�C�x���g/�f�o�b�O")]
    public System.Action<GameObject> onGhostSpawned;
    public bool drawGizmos = true;

    float _cooldownTimer;
    Vector3 _lastPlayerPos;
    Vector3 _smoothedHeading;
    bool _initialized;

    void Start()
    {
        _cooldownTimer = firstSpawnDelay;
        if (vignetteUI) SetVignetteAlpha(0f);
        if (!playerRb && player) playerRb = player.GetComponent<Rigidbody>();
        if (player) { _lastPlayerPos = player.position; _initialized = true; }
    }

    void Update()
    {
        if (!player || !playerCamera || !ghostPrefab) return;

        UpdateHeading(Time.deltaTime);

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            _cooldownTimer = spawnCooldown;
            StartCoroutine(SpawnRoutine());
        }
    }

    // ---- �i�s�������� ----
    void UpdateHeading(float dt)
    {
        if (!_initialized) return;

        Vector3 heading = Vector3.zero;

        if (playerRb && playerRb.linearVelocity.sqrMagnitude > speedThreshold * speedThreshold)
        {
            heading = playerRb.linearVelocity;
        }
        else
        {
            Vector3 delta = player.position - _lastPlayerPos;
            if (delta.sqrMagnitude > (speedThreshold * speedThreshold) * dt)
                heading = delta / Mathf.Max(dt, 0.0001f);
        }

        heading.y = 0f;
        if (heading.sqrMagnitude < 0.0001f)
        {
            heading = playerCamera.transform.forward;
            heading.y = 0f;
        }

        if (_smoothedHeading == Vector3.zero) _smoothedHeading = heading.normalized;
        float t = Mathf.Clamp01(dt / Mathf.Max(headingSmoothTime, 0.0001f));
        _smoothedHeading = Vector3.Slerp(_smoothedHeading, heading.normalized, t);

        _lastPlayerPos = player.position;
    }

    IEnumerator SpawnRoutine()
    {
        if (warningSE && oneShotSource) oneShotSource.PlayOneShot(warningSE);
        if (vignetteUI) yield return StartCoroutine(VignetteBlink(warningLeadTime));
        else yield return new WaitForSeconds(warningLeadTime);

        const int MAX_TRY = 12;
        Vector3 spawnPos = Vector3.zero;
        bool ok = false;
        for (int i = 0; i < MAX_TRY; i++)
        {
            if (TryPickSpawnPointForward(out spawnPos)) { ok = true; break; }
        }
        if (!ok)
        {
            spawnPos = player.position + _smoothedHeading.normalized * ((minSpawnDistance + maxSpawnDistance) * 0.5f);
            spawnPos.y += heightOffset;
        }

        if (spawnSE && oneShotSource) oneShotSource.PlayOneShot(spawnSE);

        var ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);

        if (spawnFxPrefab) Instantiate(spawnFxPrefab, spawnPos, Quaternion.identity);

        yield return StartCoroutine(FadeInGhost(ghost));

        onGhostSpawned?.Invoke(ghost);
    }

    bool TryPickSpawnPointForward(out Vector3 pos)
    {
        pos = Vector3.zero;

        Vector3 forward = _smoothedHeading.sqrMagnitude > 0.0001f
            ? _smoothedHeading
            : playerCamera.transform.forward;

        forward.y = 0f; forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float lateral = Random.Range(lateralJitterMin, lateralJitterMax);
        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);

        Vector3 candidate = player.position + forward * dist + right * lateral;
        candidate.y += heightOffset;

        if (requireOffscreen)
        {
            Vector3 camTo = (candidate - playerCamera.transform.position).normalized;
            float dot = Vector3.Dot(playerCamera.transform.forward, camTo);
            float ang = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
            if (ang < viewSafeAngle) return false;
        }

        if (Physics.CheckSphere(candidate, 0.2f, obstacleMask)) return false;

        pos = candidate;
        return true;
    }

    // ---- �o���t�F�[�h�i�����x�j ----
    IEnumerator FadeInGhost(GameObject ghost)
    {
        var renderers = ghost.GetComponentsInChildren<Renderer>(true);
        List<Material> mats = new List<Material>();
        foreach (var r in renderers)
        {
            var inst = r.materials; r.materials = inst; mats.AddRange(inst);
        }
        foreach (var m in mats)
        {
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 0f; m.color = c;
            ForceTransparent(m);
        }

        float t = 0f;
        while (t < fadeInTime)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / fadeInTime);
            foreach (var m in mats)
            {
                if (!m.HasProperty("_Color")) continue;
                var c = m.color; c.a = a; m.color = c;
            }
            yield return null;
        }
    }

    // ---- ���� ----
    static void ForceTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // URP
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    IEnumerator VignetteBlink(float duration)
    {
        float half = duration * 0.5f, t = 0f;
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

    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !player) return;
        Vector3 fwd = (playerCamera ? playerCamera.transform.forward : Vector3.forward);
        fwd.y = 0; fwd.Normalize();
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(player.position, minSpawnDistance);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
        Gizmos.color = Color.yellow; Gizmos.DrawLine(player.position, player.position + fwd * maxSpawnDistance);
    }
}
