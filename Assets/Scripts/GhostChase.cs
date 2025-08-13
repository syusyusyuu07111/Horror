using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GhostChase : MonoBehaviour
{
    [Header("対象")]
    public Transform player;
    [Tooltip("プレイヤーの移動/スニーク判定用（未設定なら自動取得）")]
    public PlayerMovement playerMovement;

    [Header("UI（任意）")]
    public TMP_Text statusText;

    [Header("移動速度")]
    public float wanderSpeed = 1.6f;  // 徘徊
    public float approachSpeed = 3.0f;  // 寄る/追跡

    [Header("プレイヤー状態判定")]
    [Tooltip("『歩いている』とみなす水平速度(m/s)のしきい値")]
    public float moveDetectSpeed = 0.08f;
    [Tooltip("プレイヤーがスニーク中は『移動中でも寄らない』")]
    public bool ignorePlayerWhileSneaking = true;

    [Header("視認（見つける判定はRayで行う）")]
    public float detectionRadius = 12f;
    public float fovAngle = 120f;                   // 0で全方位
    public LayerMask obstacleMask = ~0;             // 壁/棚レイヤーのみ推奨（自分とプレイヤーは除外）
    public Transform eye;                           // 未設定なら頭高さを自動使用

    [Header("警戒フラグ（視認でON）")]
    [SerializeField] bool alerted = false;          // ← 見つけたらtrueに切替
    [Tooltip(">0 にすると『見失ってからこの秒数』で警戒解除。0以下なら解除しない")]
    public float alertLoseSightDelay = 0f;          // 既定は“解除しない”
    float _loseSightTimer = 0f;

    [Header("徘徊エリア（Colliderが無くてもOK）")]
    public Collider roomBounds;                     // あれば優先
    public Transform areaRoot;                      // 子のRenderer/ColliderからXZ外接矩形を算出
    public float wanderRadius = 6f;                 // 上2つ無ければ円
    public bool confineToArea = true;

    [Header("ゲームオーバー")]
    public string gameOverScene = "GameOver";

    // 内部
    Rigidbody _rb; bool _ending;
    Vector3 _startPos, _wanderTarget;
    float _reselectTimer;
    Rigidbody _playerRb; Vector3 _prevPlayerPos;

    // areaRoot 由来の矩形
    bool _hasAreaAABB; float _minX, _maxX, _minZ, _maxZ;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb) { _rb.interpolation = RigidbodyInterpolation.Interpolate; _rb.freezeRotation = true; }

        _startPos = transform.position;

        if (!playerMovement && player) playerMovement = player.GetComponent<PlayerMovement>();
        if (player) { _playerRb = player.GetComponent<Rigidbody>(); _prevPlayerPos = player.position; }

        if (!roomBounds && areaRoot)
            ComputeAreaAABB(areaRoot, out _hasAreaAABB, out _minX, out _maxX, out _minZ, out _maxZ);

        PickNewWanderPoint(true);
    }

    void Update()
    {
        if (_ending) return;

        // ----- 視認：見つけた瞬間だけ警戒フラグをON -----
        bool sawNow = CanSeePlayer();
        if (sawNow)
        {
            alerted = true;
            _loseSightTimer = 0f;
        }
        else if (alerted && alertLoseSightDelay > 0f)
        {
            _loseSightTimer += Time.deltaTime;
            if (_loseSightTimer >= alertLoseSightDelay) alerted = false; // 任意の自動解除
        }

        // ----- プレイヤー状態 -----
        bool playerMoving = IsPlayerMoving();
        bool sneaking = playerMovement && playerMovement.IsSneaking;

        // 『移動中なら寄る』はフラグ変更なしで発動（スニーク中は抑止）
        bool approachByMove = playerMoving && !(ignorePlayerWhileSneaking && sneaking);

        // 追跡の最終判定：
        //  - alerted が true：常に追跡
        //  - alerted が false：プレイヤーが移動中の間だけ寄る（止まったら徘徊へ）
        bool shouldApproach = alerted || approachByMove;

        // ----- 目標点＆速度を決定（状態フラグの追加はしない） -----
        Vector3 targetPos;
        float speed;
        if (shouldApproach && player)
        {
            targetPos = player.position;      // 追跡/寄り：常に座標ベクトルで向かう
            speed = approachSpeed;
        }
        else
        {
            if ((transform.position - _wanderTarget).sqrMagnitude <= 0.3f * 0.3f || _reselectTimer <= 0f)
                PickNewWanderPoint();
            targetPos = _wanderTarget;         // 徘徊
            speed = wanderSpeed;
        }

        // 実移動
        MoveTowards(targetPos, speed);

        // 範囲Clamp（任意）
        if (confineToArea) ClampToArea();

        // UI（任意）
        if (statusText)
        {
            string s = alerted ? "追跡（警戒ON）"
                     : approachByMove ? "徘徊中（近寄り中）"
                     : "徘徊中";
            statusText.text = s;
            statusText.color = (alerted || approachByMove) ? Color.red : Color.white;
        }

        // 至近の保険
        if (player && HorizontalDistance(transform.position, player.position) < 0.4f)
            HandleCaught();
    }

    // ===== 共通移動 =====
    void MoveTowards(Vector3 targetPos, float spd)
    {
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;

        dir.Normalize();
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);

        if (_rb && !_rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            _rb.linearVelocity = new Vector3(dir.x * spd, _rb.linearVelocity.y, dir.z * spd);
#else
            _rb.velocity       = new Vector3(dir.x * spd, _rb.velocity.y, dir.z * spd);
#endif
        }
        else
        {
            transform.position += dir * spd * Time.deltaTime;
        }
    }

    // ===== 徘徊 =====
    void PickNewWanderPoint(bool immediate = false)
    {
        _wanderTarget = SamplePointInArea();
        _reselectTimer = Random.Range(1.5f, 3.0f);
        if (immediate) _reselectTimer *= 0.5f;
    }

    Vector3 SamplePointInArea()
    {
        if (roomBounds)
        {
            var b = roomBounds.bounds;
            return new Vector3(Random.Range(b.min.x, b.max.x), transform.position.y, Random.Range(b.min.z, b.max.z));
        }
        if (_hasAreaAABB)
        {
            return new Vector3(Random.Range(_minX, _maxX), transform.position.y, Random.Range(_minZ, _maxZ));
        }
        Vector2 p = Random.insideUnitCircle * wanderRadius;
        return new Vector3(_startPos.x + p.x, transform.position.y, _startPos.z + p.y);
    }

    void ClampToArea()
    {
        Vector3 p = transform.position;
        if (roomBounds)
        {
            var b = roomBounds.bounds; p.x = Mathf.Clamp(p.x, b.min.x, b.max.x); p.z = Mathf.Clamp(p.z, b.min.z, b.max.z);
        }
        else if (_hasAreaAABB)
        {
            p.x = Mathf.Clamp(p.x, _minX, _maxX); p.z = Mathf.Clamp(p.z, _minZ, _maxZ);
        }
        transform.position = p;
    }

    // ===== 見つける判定（Ray） =====
    bool CanSeePlayer()
    {
        if (!player) return false;

        Vector3 eyePos = eye ? eye.position : transform.position + Vector3.up * 1.5f;
        Vector3 toP = player.position - eyePos;
        float dist = toP.magnitude;

        if (dist > detectionRadius) return false;

        if (fovAngle > 0.01f)
        {
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 flat = toP; flat.y = 0f;
            if (flat.sqrMagnitude > 1e-6f && Vector3.Angle(fwd, flat) > fovAngle * 0.5f) return false;
        }

        // RaycastAll：最前ヒットで判定（自分は無視）
        Ray ray = new Ray(eyePos, toP.normalized);
        var hits = Physics.RaycastAll(ray, dist, ~0, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return true;
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.transform == player || h.collider.transform.IsChildOf(player)) return true;
            return false; // 手前に壁など
        }
        return true;
    }

    // ===== プレイヤー移動判定 =====
    bool IsPlayerMoving()
    {
        if (!player) return false;

        float spd;
        if (_playerRb)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 v = _playerRb.linearVelocity; v.y = 0f; spd = v.magnitude;
#else
            Vector3 v = _playerRb.velocity;       v.y = 0f; spd = v.magnitude;
#endif
        }
        else
        {
            Vector3 d = player.position - _prevPlayerPos; d.y = 0f;
            spd = d.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
        }
        _prevPlayerPos = player.position;
        return spd > moveDetectSpeed;
    }

    // ===== areaRoot から外接矩形 =====
    static void ComputeAreaAABB(Transform root, out bool ok, out float minX, out float maxX, out float minZ, out float maxZ)
    {
        ok = false; minX = maxX = minZ = maxZ = 0f;
        var rs = root.GetComponentsInChildren<Renderer>(true);
        var cs = root.GetComponentsInChildren<Collider>(true);
        bool any = false; Bounds acc = new Bounds(root.position, Vector3.zero);
        foreach (var r in rs) { if (!any) { acc = r.bounds; any = true; } else acc.Encapsulate(r.bounds); }
        foreach (var c in cs) { if (!any) { acc = c.bounds; any = true; } else acc.Encapsulate(c.bounds); }
        if (any) { ok = true; minX = acc.min.x; maxX = acc.max.x; minZ = acc.min.z; maxZ = acc.max.z; }
    }

    static float HorizontalDistance(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return (a - b).magnitude; }

    void OnCollisionEnter(Collision c) { if (c.gameObject.CompareTag("Player")) HandleCaught(); }
    void OnTriggerEnter(Collider o) { if (o.CompareTag("Player")) HandleCaught(); }

    void HandleCaught()
    {
        if (_ending) return; _ending = true;
        SceneTracker.SaveCurrentScene(); SceneManager.LoadScene(gameOverScene);
    }

    // 追記（GhostChase クラスの中に置く）
    public void ClearAlert()
    {
        // 見つかったフラグをOFFにして、見失いタイマーもリセット
#pragma warning disable CS0414
        // 'alerted' と '_loseSightTimer' は既存フィールド想定
        alerted = false;
        _loseSightTimer = 0f;
#pragma warning restore CS0414
    }

}
