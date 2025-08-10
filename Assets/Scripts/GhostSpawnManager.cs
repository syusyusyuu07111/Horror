using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GhostSpawnManager : MonoBehaviour
{
    [Header("必須参照")]
    public Transform player;
    public Camera playerCamera;
    public GameObject ghostPrefab;

    [Header("出現タイミング")]
    [Tooltip("ゲーム開始から最初のスポーンまでの遅延秒数")]
    public float firstSpawnDelay = 3f;     // ← ここが「開始3秒」
    [Tooltip("2回目以降のスポーン間隔（秒）")]
    public float spawnCooldown = 6f;

    [Header("出現位置設定")]
    [Tooltip("最小/最大スポーン距離（プレイヤー中心の円周上から抽選）")]
    public float minSpawnDistance = 2.5f;
    public float maxSpawnDistance = 4.5f;
    [Tooltip("スポーン位置のYオフセット（背丈調整など）")]
    public float heightOffset = 0.0f;
    [Tooltip("視野から外すための安全角度（度）。小さいほど見えやすい")]
    public float viewSafeAngle = 50f;
    [Tooltip("障害物（壁など）のレイヤー")]
    public LayerMask obstacleMask;

    [Header("演出（画面）")]
    public Image vignetteUI;              // 画面フェード用UI（任意）
    public float vignetteMaxAlpha = 0.45f;
    public float warningLeadTime = 0.6f;  // 出現前の“予兆”演出時間

    [Header("演出（音/エフェクト）")]
    public AudioSource oneShotSource;
    public AudioClip warningSE;
    public AudioClip spawnSE;
    [Tooltip("出現時に再生するパーティクルPrefab（煙・黒靄など）")]
    public GameObject spawnFxPrefab;

    [Header("フェード（幽霊本体の透明フェードイン）")]
    [Tooltip("0→1に上げる時間（秒）")]
    public float fadeInTime = 0.3f;

    [Header("デバッグ")]
    public bool drawGizmos = true;

    float _cooldownTimer;

    void Start()
    {
        // 最初のスポーンだけ「開始3秒後」
        _cooldownTimer = firstSpawnDelay;
        if (vignetteUI) SetVignetteAlpha(0f);
    }

    void Update()
    {
        if (!player || !playerCamera || !ghostPrefab) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
        {
            // 次回以降は通常クールダウン
            _cooldownTimer = spawnCooldown;
            StartCoroutine(SpawnRoutine());
        }
    }

    IEnumerator SpawnRoutine()
    {
        // 予兆（ビネット＋SE）
        if (warningSE && oneShotSource) oneShotSource.PlayOneShot(warningSE);
        if (vignetteUI) yield return StartCoroutine(VignetteBlink(warningLeadTime));
        else yield return new WaitForSeconds(warningLeadTime);

        // 出現位置を決定（複数回トライ）
        const int MAX_TRY = 12;
        Vector3 spawnPos = Vector3.zero;
        bool ok = false;
        for (int i = 0; i < MAX_TRY; i++)
        {
            if (TryPickSpawnPoint(out spawnPos)) { ok = true; break; }
        }

        // それでも見つからなければ、最終手段でプレイヤー背後に寄せる
        if (!ok)
        {
            Vector3 fwd = playerCamera.transform.forward; fwd.y = 0; fwd.Normalize();
            float dist = Mathf.Clamp((minSpawnDistance + maxSpawnDistance) * 0.5f, 0.5f, 999f);
            spawnPos = player.position - fwd * dist;
            spawnPos.y += heightOffset;
        }

        // 出現SE
        if (spawnSE && oneShotSource) oneShotSource.PlayOneShot(spawnSE);

        // 幽霊生成（Prefab複製）
        var ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);

        // 出現パーティクル
        if (spawnFxPrefab) Instantiate(spawnFxPrefab, spawnPos, Quaternion.identity);

        // 透明度フェードイン
        yield return StartCoroutine(FadeInGhost(ghost));
    }

    // ==== 出現位置を選ぶ ====
    bool TryPickSpawnPoint(out Vector3 pos)
    {
        pos = Vector3.zero;

        // カメラ基準の方向ベクトル
        Vector3 fwd = playerCamera.transform.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = playerCamera.transform.right; right.y = 0; right.Normalize();

        // 基本は「背後〜斜め後ろ」から選ぶ（120°〜240°）
        float yaw = Random.Range(120f, 240f);
        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * fwd;
        dir.Normalize();

        float dist = Random.Range(minSpawnDistance, maxSpawnDistance);
        Vector3 candidate = player.position + dir * dist;
        candidate.y += heightOffset;

        // カメラ視野外チェック（視線方向との角度で判定）
        Vector3 camTo = (candidate - playerCamera.transform.position).normalized;
        float dot = Vector3.Dot(playerCamera.transform.forward, camTo);
        float angleFromView = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
        if (angleFromView < viewSafeAngle) return false; // 見えすぎる

        // 壁に埋まっていないか
        if (Physics.CheckSphere(candidate, 0.2f, obstacleMask)) return false;

        pos = candidate;
        return true;
    }

    // ==== フェードイン処理 ====
    IEnumerator FadeInGhost(GameObject ghost)
    {
        // 子オブジェクト含む Renderer を取得
        var renderers = ghost.GetComponentsInChildren<Renderer>(true);

        // マテリアルを実体化＆初期アルファ0
        List<Material> mats = new List<Material>();
        foreach (var r in renderers)
        {
            var newMats = r.materials; // 実体化
            r.materials = newMats;
            mats.AddRange(newMats);
        }
        foreach (var m in mats)
        {
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 0f; m.color = c;
            ForceTransparent(m); // 半透明描画に切替（Standard/URP簡易対応）
        }

        // 0→1フェード
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
        // 最終値を1に
        for (int i = 0; i < mats.Count; i++)
        {
            var m = mats[i];
            if (!m.HasProperty("_Color")) continue;
            var c = m.color; c.a = 1f; m.color = c;
        }
    }

    // ==== マテリアルを半透明モードへ（簡易対応） ====
    static void ForceTransparent(Material m)
    {
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // URP Lit: 0=Opaque,1=Transparent
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
        m.DisableKeyword("_ALPHATEST_ON");
        m.EnableKeyword("_ALPHABLEND_ON");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    // ==== 予兆ビネット ====
    IEnumerator VignetteBlink(float duration)
    {
        float half = duration * 0.5f;
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

    // ==== ギズモ ====
    void OnDrawGizmosSelected()
    {
        if (!drawGizmos || !player) return;
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(player.position, minSpawnDistance);
        Gizmos.color = Color.blue; Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
    }
}
