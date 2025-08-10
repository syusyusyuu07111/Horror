using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PaintingInteraction : MonoBehaviour
{
    [Header("操作/UI")]
    [Tooltip("回転に使うキー。Inspectorで変更可")]
    public KeyCode interactKey = KeyCode.Return;
    [Tooltip("接近時に出すプロンプト（任意）")]
    public TMP_Text promptUI;
    [Tooltip("距離判定に使うプレイヤーTransform")]
    public Transform player;
    [Tooltip("この距離以内で操作可能")]
    public float interactRadius = 1.5f;
    [Tooltip("距離判定で高さ成分を無視する")]
    public bool lockYPlane = true;

    [Header("回転設定")]
    [Tooltip("1回押すとZを何度回すか")]
    public float rotateStepDegZ = 90f;
    [Tooltip("正解角度（Z）")]
    public float targetZ = 180f;
    [Tooltip("正解判定の許容誤差（度）")]
    public float tolerance = 2f;

    [Header("パズル（同じIDの絵画を束ねる）")]
    [Tooltip("同一グループIDでカウントを共有")]
    public string groupId = "PuzzleA";
    [Tooltip("この枚数そろったらクリア")]
    public int requiredSolved = 2;
    [Tooltip("生成するドアPrefab（任意）")]
    public GameObject doorPrefab;
    [Tooltip("ドアの生成位置・向き（任意）")]
    public Transform doorSpawnPoint;
    [Tooltip("グループごとに1回だけ生成する")]
    public bool spawnOncePerGroup = true;

    [Header("起動/デバッグ")]
    [Tooltip("Play押下ごとに静的カウントをクリア（Domain Reload無効対策）")]
    public bool resetStaticsOnAwake = true;
    [Tooltip("回すたびに角度や判定をログ")]
    public bool logOnAngleChange = true;
    [Tooltip("条件達成や生成可否をログ")]
    public bool logWhenBothSolved = true;

    // 共有管理（グループごとの達成数・生成済み）
    static readonly Dictionary<string, int> s_solvedCount = new Dictionary<string, int>();
    static readonly HashSet<string> s_spawnedGroup = new HashSet<string>();

    // 起動→操作の状態管理
    bool _isNear;           // プレイヤー接近
    bool _isSolvedBoot;     // 起動時点の正解状態（表示のみ、加点はしない）
    bool _isSolvedNow;      // 直近の判定（回転後に更新）
    bool _hasEverCounted;   // この個体が一度でも加点側に遷移したか（OnDisableでの減算に使用）

    static bool s_initialized; // Domain Reload無効時の多重初期化防止

    void Awake()
    {
        if (!s_initialized || resetStaticsOnAwake)
        {
            s_solvedCount.Clear();
            s_spawnedGroup.Clear();
            s_initialized = true;
            if (logOnAngleChange) Debug.Log("[PUZZLE] 静的カウントをリセットしました");
        }
    }

    void Start()
    {
        if (promptUI) promptUI.gameObject.SetActive(false);
        if (!s_solvedCount.ContainsKey(groupId)) s_solvedCount[groupId] = 0;

        // 起動時の角度を確認するが、カウントはしない（＝操作で揃えたときだけ加点）
        float z = transform.localEulerAngles.z;
        _isSolvedBoot = IsZAtTarget(z, targetZ, tolerance);
        _isSolvedNow = _isSolvedBoot;

        if (logOnAngleChange)
        {
            if (_isSolvedBoot)
                Debug.Log($"[{name}] 起動時に正解角度（ノーカウント）。Z={z:0.0} 目標={targetZ}±{tolerance}");
            else
                Debug.Log($"[{name}] 起動時は未達。Z={z:0.0} 目標={targetZ}±{tolerance}");
        }
    }

    void OnDisable()
    {
        // この個体が“加点済み”なら、破棄時に念のため減算（シーン遷移やDestroy対策）
        if (_hasEverCounted && _isSolvedNow && s_solvedCount.ContainsKey(groupId))
        {
            s_solvedCount[groupId] = Mathf.Max(0, s_solvedCount[groupId] - 1);
            if (logOnAngleChange)
                Debug.Log($"[{name}] OnDisableで減算。group='{groupId}' -> {s_solvedCount[groupId]}/{requiredSolved}");
        }
    }

    void Update()
    {
        // 接近表示（距離判定）
        if (player)
        {
            Vector3 a = transform.position;
            Vector3 p = player.position;
            if (lockYPlane) p.y = a.y;
            bool nowNear = Vector3.Distance(a, p) <= interactRadius;
            if (nowNear != _isNear && promptUI) promptUI.gameObject.SetActive(nowNear);
            _isNear = nowNear;
        }

        // 回転操作 → 状態更新
        if (_isNear && Input.GetKeyDown(interactKey))
        {
            transform.Rotate(0f, 0f, rotateStepDegZ, Space.Self);
            UpdateSolvedState(afterRotate: true);
        }
    }

    void UpdateSolvedState(bool afterRotate)
    {
        float z = transform.localEulerAngles.z;
        bool nowSolved = IsZAtTarget(z, targetZ, tolerance);

        if (logOnAngleChange && afterRotate)
            Debug.Log($"[{name}] 回転後Z={z:0.0} 正解?={nowSolved} (目標={targetZ}±{tolerance})");

        // “起動時は正解だったがノーカウント” → “未達に戻す” → “再度正解へ”
        // のような往復にも対応：カウントは「未達→正解」に遷移した時だけ+1、
        // 「正解→未達」に遷移した時だけ-1。
        if (nowSolved == _isSolvedNow) return; // 変化なし

        // グループカウントの加減算
        if (!s_solvedCount.ContainsKey(groupId)) s_solvedCount[groupId] = 0;

        if (nowSolved && !_isSolvedNow)
        {
            s_solvedCount[groupId] += 1;
            _hasEverCounted = true; // 一度でも+1した個体
        }
        else if (!nowSolved && _isSolvedNow)
        {
            s_solvedCount[groupId] = Mathf.Max(0, s_solvedCount[groupId] - 1);
        }

        _isSolvedNow = nowSolved;

        if (logOnAngleChange)
            Debug.Log($"[{name}] 状態変更 → group='{groupId}' solved={s_solvedCount[groupId]}/{requiredSolved}");

        TrySpawnDoorIfReady();
    }

    void TrySpawnDoorIfReady()
    {
        if (!s_solvedCount.TryGetValue(groupId, out int cnt)) cnt = 0;

        if (cnt >= requiredSolved)
        {
            if (logWhenBothSolved)
                Debug.Log($"[PUZZLE] group='{groupId}' 条件達成！（{cnt}/{requiredSolved}）");

            // ドア生成可否
            if (!doorPrefab || !doorSpawnPoint)
            {
                if (logWhenBothSolved)
                    Debug.LogWarning($"[PUZZLE] ドア未生成：doorPrefab/doorSpawnPoint 未設定（group='{groupId}'）");
                return;
            }

            if (spawnOncePerGroup && s_spawnedGroup.Contains(groupId))
            {
                if (logWhenBothSolved)
                    Debug.Log($"[PUZZLE] 既に生成済みのためスキップ（group='{groupId}'）");
                return;
            }

            Instantiate(doorPrefab, doorSpawnPoint.position, doorSpawnPoint.rotation);
            if (spawnOncePerGroup) s_spawnedGroup.Add(groupId);

            if (logWhenBothSolved)
                Debug.Log($"[PUZZLE] ドア生成：{doorPrefab.name} @ {doorSpawnPoint.position}（group='{groupId}'）");
        }
    }

    static bool IsZAtTarget(float currentZ, float target, float tol)
    {
        float diff = Mathf.DeltaAngle(currentZ, target);
        return Mathf.Abs(diff) <= tol;
    }

    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Vector3 c = transform.position;
        if (lockYPlane) c.y = transform.position.y;
        Gizmos.DrawWireSphere(c, interactRadius);
    }
}
