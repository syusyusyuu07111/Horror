using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhostSpawnManagerFourWay : MonoBehaviour
{
    [Header("必須参照")]
    public Transform player;
    public Camera playerCamera;
    public GameObject ghostPrefab;

    [Header("向きの基準")]
    [Tooltip("ON: カメラの向き基準で前後左右 / OFF: プレイヤー transform.forward 基準")]
    public bool useCameraForward = true;

    [Header("出現タイミング")]
    public float firstSpawnDelay = 3f;   // 開始3秒で最初の4体
    public float spawnCooldown = 6f;     // 2回目以降の間隔

    [Header("出現位置設定（4方向共通）")]
    [Tooltip("プレイヤーからの距離（半径）")]
    public float spawnRadius = 3.5f;
    [Tooltip("上下のオフセット")]
    public float heightOffset = 0.0f;

    [Header("安全判定")]
    [Tooltip("壁などのレイヤー。ここに刺さらないよう調整")]
    public LayerMask obstacleMask;
    [Tooltip("候補が壁に埋まっていたらこの距離だけ内側に下げて再試行（最大retries回）")]
    public float inwardStep = 0.4f;
    public int retries = 5;

    [Header("演出（画面）")]
    public Image vignetteUI;
    public float vignetteMaxAlpha = 0.45f;
    public float warningLeadTime = 0.4f;

    [Header("演出（音/エフェクト）")]
    public AudioSource oneShotSource;
    public AudioClip warningSE;
    public AudioClip spawnSE;
    [Tooltip("出現時に再生するパーティクルPrefab（煙・黒靄など）")]
    public GameObject spawnFxPrefab;

    [Header("フェード（幽霊本体の透明フェードイン）")]
    public float fadeInTime = 0.25f;

    [Header("回転合わせ")]
    [Tooltip("生成直後にプレイヤー方向を向かせる")]
    public bool facePlayerOnSpawn = true;

    [Header("デバッグ")]
    public bool drawGizmos = true;
    [Tooltip("Gizmosの矢印長さ")]
    public float gizmoDirLen = 1.2f;

    float _cooldownTimer;

    void Start()
    {
        _cooldownTimer = firstSpawnDelay; // 初回は3秒後
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
        // 予兆
        if (warningSE && oneShotSource) oneShotSource.PlayOneShot(warningSE);
        if (vignetteUI) yield return StartCoroutine(VignetteBlink(warningLeadTime));
        else yield return new WaitForSeconds(warningLeadTime);

        // 前後左右の基準ベクトル
        GetBasis(out Vector3 fwd, out Vector3 right);

        // 4方向のターゲット位置
        Vector3[] dirs = new Vector3[4] { fwd, -fwd, right, -right };

        // 各方向ごとに位置決定→生成
        List<GameObject> spawned = new List<GameObject>(4);
        for (int i = 0; i < 4; i++)
        {
            if (TryPlaceAround(dirs[i], out Vector3 pos))
            {
                // 出現SE（最初の1体で鳴らすか、全体で1回だけ）
                if (i == 0 && spawnSE && oneShotSource) oneShotSource.PlayOneShot(spawnSE);

                // 幽霊生成
                var ghost = Instantiate(ghostPrefab, pos, Quaternion.identity);
                if (facePlayerOnSpawn)
                {
                    Vector3 look = player.position - pos; look.y = 0f;
                    if (look.sqrMagnitude > 0.001f)
                        ghost.transform.rotation = Quaternion.LookRotation(look.normalized);
                }

                // 出現FX
                if (spawnFxPrefab) Instantiate(spawnFxPrefab, pos, ghost.transform.rotation);

                // フェードイン
                StartCoroutine(FadeInGhost(ghost));

                spawned.Add(ghost);
            }
        }

        yield break;
    }

    // 前後左右の基準を得る（正規化、水平化）
    void GetBasis(out Vector3 fwd, out Vector3 right)
    {
        if (useCameraForward && playerCamera)
        {
            fwd = playerCamera.transform.forward; fwd.y = 0f; fwd.Normalize();
            right = playerCamera.transform.right; right.y = 0f; right.Normalize();
        }
        else
        {
            // プレイヤー基準。forwardが水平でない場合は水平化
            fwd = player ? player.forward : Vector3.forward;
            fwd.y = 0f; fwd = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
            right = new Vector3(fwd.z, 0f, -fwd.x); // 左手系直交
        }
    }

    // 指定方向に spawnRadius だけ離れた位置を探す（壁に食い込んだら内側に退避）
    bool TryPlaceAround(Vector3 dir, out Vector3 pos)
    {
        dir.y = 0f; dir.Normalize();
        Vector3 basePos = player.position + dir * spawnRadius;
        basePos.y += heightOffset;

        // そのまま埋まりチェック
        if (!Physics.CheckSphere(basePos, 0.2f, obstacleMask))
        {
            pos = basePos;
            return true;
        }

        // 埋まっていたら内側に少しずつ戻して再試行
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

        // ダメならプレイヤーの位置ギリ（半径の1/3）まで寄せる
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

    // --- 透明フェードイン ---
    IEnumerator FadeInGhost(GameObject ghost)
    {
        var renderers = ghost.GetComponentsInChildren<Renderer>(true);

        List<Material> mats = new List<Material>();
        foreach (var r in renderers)
        {
            var newMats = r.materials; // 実体化
            r.materials = newMats;
            mats.AddRange(newMats);
        }

        // 初期アルファ0 & 半透明設定
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i];
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 0f; m.color = c;
            ForceTransparent(m);
        }

        // 0→1
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

        // 最終1.0
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i];
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 1f; m.color = c;
        }
    }

    // --- マテリアルを半透明モードに（Standard/URP簡易対応） ---
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

    // --- 予兆ビネット ---
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

    // --- ギズモ ---
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !player) return;

        // 半径
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(player.position + Vector3.up * heightOffset, spawnRadius);

        // 前後左右矢印（カメラ基準想定）
        GetBasis(out var fwd, out var right);
        Vector3 center = player.position + Vector3.up * heightOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(center, center + fwd * (spawnRadius + gizmoDirLen));
        Gizmos.DrawLine(center, center - fwd * (spawnRadius + gizmoDirLen));
        Gizmos.DrawLine(center, center + right * (spawnRadius + gizmoDirLen));
        Gizmos.DrawLine(center, center - right * (spawnRadius + gizmoDirLen));
    }
}
