using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GhostChase : MonoBehaviour
{
    [Header("対象")]
    public Transform player;
    [Tooltip("プレイヤーの移動スクリプト（未指定なら自動取得）")]
    public PlayerMovement playerMovement;

    [Header("UI")]
    public TMP_Text statusText; // 状態表示

    [Header("速度設定")]
    public float wanderSpeed = 1.6f;
    public float approachSpeed = 3f;

    [Header("検知設定")]
    public float moveDetectSpeed = 0.08f; // 歩き判定
    public float stopToWanderDelay = 0.8f;
    public bool requireSightToApproach = true;
    public float fovAngle = 120f;
    public float detectionRadius = 12f;
    public LayerMask obstacleMask = ~0;
    public Transform eye;

    [Header("スニーク挙動")]
    [Tooltip("プレイヤーがスニーク中は近寄らない")]
    public bool ignorePlayerWhileSneaking = true;

    [Header("徘徊範囲（省略）")]
    public Collider roomBounds; public Transform areaRoot; public float wanderRadius = 6f; public bool confineToArea = true;

    [Header("ゲームオーバー")]
    public string gameOverScene = "GameOver";

    enum State { Wander, Approach }
    State _state = State.Wander;

    Rigidbody _rb; bool _ending;
    Vector3 _startPos, _wanderTarget;
    float _reselectTimer, _loseTimer;
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

        if (!roomBounds && areaRoot) ComputeAreaAABB(areaRoot, out _hasAreaAABB, out _minX, out _maxX, out _minZ, out _maxZ);

        PickNewWanderPoint(true);
    }

    void Update()
    {
        if (_ending) return;

        bool playerMoving = IsPlayerMoving();
        bool canSee = requireSightToApproach ? CanSeePlayer() : true;
        bool sneaking = playerMovement && playerMovement.IsSneaking;

        // スニーク中は「寄らない」条件を加味
        bool shouldApproach = playerMoving && canSee && !(ignorePlayerWhileSneaking && sneaking);

        if (_state == State.Wander)
        {
            if (shouldApproach) { _state = State.Approach; _loseTimer = 0f; }
        }
        else
        {
            if (!shouldApproach)
            {
                _loseTimer += Time.deltaTime;
                if (_loseTimer >= stopToWanderDelay) { _state = State.Wander; PickNewWanderPoint(true); }
            }
            else _loseTimer = 0f;
        }

        if (_state == State.Wander) TickWander(); else TickApproach();

        if (confineToArea) ClampToArea();

        if (statusText)
        {
            if (_state == State.Approach) statusText.text = sneaking ? "徘徊中（スニーク無視）" : "寄ってくる";
            else statusText.text = sneaking ? "徘徊中（プレイヤー：スニーク）" : "徘徊中";
            statusText.color = (_state == State.Approach) ? Color.red : Color.white;
        }
    }

    // --- Wander / Approach ---
    void TickWander()
    {
        MoveTowards(_wanderTarget, wanderSpeed);
        _reselectTimer -= Time.deltaTime;
        if ((transform.position - _wanderTarget).sqrMagnitude <= 0.09f || _reselectTimer <= 0f) PickNewWanderPoint();
    }
    void TickApproach() { if (!player) return; MoveTowards(player.position, approachSpeed); }

    void MoveTowards(Vector3 targetPos, float spd)
    {
        Vector3 dir = targetPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        dir.Normalize();
        Quaternion look = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, Time.deltaTime * 8f);
#if UNITY_6000_0_OR_NEWER
        if (_rb && !_rb.isKinematic) _rb.linearVelocity = new Vector3(dir.x * spd, _rb.linearVelocity.y, dir.z * spd);
#else
        if (_rb && !_rb.isKinematic) _rb.velocity = new Vector3(dir.x * spd, _rb.velocity.y, dir.z * spd);
#endif
        if (!_rb || _rb.isKinematic) transform.position += dir * spd * Time.deltaTime;
    }

    // --- Utility ---
    void PickNewWanderPoint(bool immediate = false)
    { _wanderTarget = SamplePointInArea(); _reselectTimer = Random.Range(1.5f, 3f); if (immediate) _reselectTimer *= 0.5f; }

    Vector3 SamplePointInArea()
    {
        if (roomBounds) { var b = roomBounds.bounds; return new Vector3(Random.Range(b.min.x, b.max.x), transform.position.y, Random.Range(b.min.z, b.max.z)); }
        if (_hasAreaAABB) { return new Vector3(Random.Range(_minX, _maxX), transform.position.y, Random.Range(_minZ, _maxZ)); }
        Vector2 p = Random.insideUnitCircle * 6f; return new Vector3(_startPos.x + p.x, transform.position.y, _startPos.z + p.y);
    }

    void ClampToArea()
    {
        Vector3 p = transform.position;
        if (roomBounds) { var b = roomBounds.bounds; p.x = Mathf.Clamp(p.x, b.min.x, b.max.x); p.z = Mathf.Clamp(p.z, b.min.z, b.max.z); }
        else if (_hasAreaAABB) { p.x = Mathf.Clamp(p.x, _minX, _maxX); p.z = Mathf.Clamp(p.z, _minZ, _maxZ); }
        transform.position = p;
    }

    bool CanSeePlayer()
    {
        if (!player) return false;
        Vector3 eyePos = eye ? eye.position : transform.position + Vector3.up * 1.5f;
        Vector3 toP = player.position - eyePos; float dist = toP.magnitude;
        if (dist > detectionRadius) return false;
        if (fovAngle > 0.01f)
        {
            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 flat = toP; flat.y = 0f;
            if (flat.sqrMagnitude > 1e-6f && Vector3.Angle(fwd, flat) > fovAngle * 0.5f) return false;
        }
        if (Physics.Raycast(eyePos, toP.normalized, out RaycastHit hit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            if (!hit.collider.CompareTag("Player")) return false;
        return true;
    }

    bool IsPlayerMoving()
    {
        if (!player) return false;
        float spd;
        if (_playerRb)
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 v = _playerRb.linearVelocity; v.y = 0f; spd = v.magnitude;
#else
            Vector3 v = _playerRb.velocity; v.y=0f; spd = v.magnitude;
#endif
        }
        else
        {
            Vector3 d = player.position - _prevPlayerPos; d.y = 0f; spd = d.magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
        }
        _prevPlayerPos = player.position;
        return spd > moveDetectSpeed;
    }

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

    void OnCollisionEnter(Collision c) { if (c.gameObject.CompareTag("Player")) HandleCaught(); }
    void OnTriggerEnter(Collider o) { if (o.CompareTag("Player")) HandleCaught(); }

    void HandleCaught()
    {
        if (_ending) return; _ending = true;
        SceneTracker.SaveCurrentScene(); SceneManager.LoadScene(gameOverScene);
    }
}
