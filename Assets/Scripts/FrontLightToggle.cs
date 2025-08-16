using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Enter/テンキーEnterでライトON/OFF（親Transformは変更しない）
/// ・Attached   : 親の前方に固定
/// ・SurfaceLock: 前方の面へライトだけ貼り付け（ビームは手元→前方のまま）
///
/// 点灯中は「ライト円錐（※高さ無視：XZ平面だけで角度判定）」に入った幽霊へスタン付与。
/// Trigger も検出。怒りシステム(GhostAnger)がある場合は命中で怒り加算、激怒時はスタン無効にできます。
public class FrontLightToggle : MonoBehaviour
{
    public enum FollowMode { Attached, SurfaceLock }

    [Header("取り付け先（未設定ならこのオブジェクト）")]
    public Transform attachTo;

    [Header("モード")]
    public FollowMode followMode = FollowMode.Attached;

    [Header("Attached（固定）設定")]
    public Vector3 localOffset = new Vector3(0f, 1.2f, 0.35f);
    public Vector3 localEulerOffset = Vector3.zero;

    [Header("SurfaceLock（直近面にライトを貼り付け）※任意")]
    public float lockDistance = 1.8f;
    public float liftFromSurface = 0.03f;
    public bool fallbackToAttached = true;

    [Header("ライト（実際に照らす光）")]
    public LightType lightType = LightType.Spot;
    public float range = 8f;
    [Range(1f, 179f)] public float spotAngle = 50f;
#if UNITY_2019_1_OR_NEWER
    [Range(0f, 179f)] public float innerSpotAngle = 25f;
#endif
    public float intensity = 3.0f;
    public Color color = Color.white;
    public LightShadows shadows = LightShadows.Soft;

    [Header("可視ビーム（メッシュ円錐）")]
    public bool useMeshBeam = true;
    public float beamLength = 20f;          // 見える筋の長さ
    public float beamStartRadius = 0.04f;   // 根元の太さ
    public bool beamMatchSpotAngle = true; // 先端半径をスポット角から自動計算
    public float beamEndRadius = 0.5f;      // 手動時のみ使用
    [Range(8, 64)] public int beamSides = 24;
    public Color beamColor = new Color(1f, 1f, 1f, 0.25f);
    public bool syncLightRangeToBeam = false; // 実照射距離もビーム長に合わせる

    [Header("幽霊ヒット（ライト点灯中のみ有効）")]
    public bool affectGhostOnLight = true;     // 当たり判定を行う
    public LayerMask ghostMask = ~0;           // 幽霊のLayer
    public float hitCheckInterval = 0.05f;     // 何秒おきに判定
    [Tooltip("<0 なら light.range、>=0 ならこの距離で判定")]
    public float hitRangeOverride = -1f;

    public bool clearAlertOnHit = true;        // 見つかったフラグOFF
    public bool stunGhostOnHit = true;       // 動きを止める
    public float stunSeconds = 1.0f;       // スタン時間

    [Header("再スタンのポリシー")]
    [Tooltip("ON: 照射し続ける限りスタン延長。OFF: 再入射時のみ再スタン")]
    public bool continuousStunWhileLit = false;
    [Tooltip("OFFの時、ビームから外れて再入射できるまでのクール（秒）")]
    public float restunReenterCooldown = 0.0f;

    [Header("怒り連動（任意）")]
    [Tooltip("命中時に GhostAnger へ通知して怒りを加算")]
    public bool informAngerOnHit = true;
    [Tooltip("1回命中あたりの怒りスケール（GhostAnger.addPerHit に掛け算）")]
    public float angerAddScale = 1f;
    [Tooltip("激怒中(IsEnraged)はスタンを無効化する")]
    public bool skipStunWhenEnraged = true;

    [Header("入力（新InputSystem）")]
    public Key toggleKey = Key.Enter;
    public Key toggleKey2 = Key.NumpadEnter;

    [Header("Debug")]
    public bool debugLogHits = false;
    public bool debugGizmos = true;

    // --- 内部 ---
    Light _light; Transform _lightT;

    // ビーム描画
    Transform _beamT; MeshFilter _beamMF; MeshRenderer _beamMR; Mesh _beamMesh; Material _beamMat;

    // ヒットスキャン
    float _hitTimer;
    readonly Dictionary<Component, Coroutine> _stunCo = new(); // GhostChase or Rigidbody
    readonly HashSet<Component> _currentlyHit = new();
    readonly HashSet<Component> _lastFrameHit = new();
    readonly Dictionary<Component, float> _lastExitTime = new();

    void Awake()
    {
        if (!attachTo) attachTo = transform;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            Toggle();

        if (_light && _light.enabled)
        {
            Follow();
            HitScanGhosts();
        }
        else
        {
            if (_lastFrameHit.Count > 0) { _lastFrameHit.Clear(); _currentlyHit.Clear(); }
        }
    }

    // ── トグル ─────────────────────────────────────────
    void Toggle()
    {
        if (!_light) CreateLight();

        bool next = !_light.enabled;
        _light.enabled = next;

        if (next)
        {
            ApplyLightParams();
            if (useMeshBeam)
            {
                if (!_beamT) CreateBeamMesh();
                UpdateBeamGeometry();
                _beamMR.enabled = true;
            }
            if (syncLightRangeToBeam) _light.range = Mathf.Max(range, beamLength);
        }
        else
        {
            if (_beamMR) _beamMR.enabled = false;
        }
    }

    // ── 追従（ライト/ビーム） ──────────────────────────
    void Follow()
    {
        if (followMode == FollowMode.Attached)
        {
            if (_lightT.parent != attachTo) _lightT.SetParent(attachTo, false);
            _lightT.localPosition = localOffset;
            _lightT.localRotation = Quaternion.Euler(localEulerOffset);

            if (useMeshBeam && _beamT)
            {
                if (_beamT.parent != attachTo) _beamT.SetParent(attachTo, false);
                _beamT.localPosition = localOffset;
                _beamT.localRotation = Quaternion.Euler(localEulerOffset);
            }
        }
        else // SurfaceLock：ライトだけ面へ貼り付け（ビームは手元→前方のまま）
        {
            Vector3 origin = attachTo.position;
            Vector3 dir = attachTo.forward;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, lockDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = hit.point + hit.normal * liftFromSurface;
                if (_lightT.parent != null) _lightT.SetParent(null, true);
                _lightT.position = pos;
                _lightT.rotation = Quaternion.LookRotation(-hit.normal, Vector3.up);
            }
            else if (fallbackToAttached)
            {
                if (_lightT.parent != attachTo) _lightT.SetParent(attachTo, false);
                _lightT.localPosition = localOffset;
                _lightT.localRotation = Quaternion.Euler(localEulerOffset);
            }

            if (useMeshBeam && _beamT)
            {
                if (_beamT.parent != attachTo) _beamT.SetParent(attachTo, false);
                _beamT.localPosition = localOffset;
                _beamT.localRotation = Quaternion.Euler(localEulerOffset);
            }
        }
    }

    // ── ライト生成/設定 ────────────────────────────────
    void CreateLight()
    {
        var go = new GameObject("FrontLight");
        _lightT = go.transform;
        _lightT.SetParent(attachTo, false);
        _lightT.localPosition = localOffset;
        _lightT.localRotation = Quaternion.Euler(localEulerOffset);

        _light = go.AddComponent<Light>();
        _light.enabled = false;
        ApplyLightParams();
    }

    void ApplyLightParams()
    {
        _light.type = lightType;
        _light.color = color;
        _light.intensity = intensity;
        _light.range = range;
        _light.shadows = shadows;
        if (_light.type == LightType.Spot)
        {
            _light.spotAngle = spotAngle;
#if UNITY_2019_1_OR_NEWER
            _light.innerSpotAngle = innerSpotAngle;
#endif
        }
    }

    // ── ビーム（メッシュ円錐） ───────────────────────────
    void CreateBeamMesh()
    {
        var go = new GameObject("FrontLightBeamMesh");
        _beamT = go.transform;
        _beamT.SetParent(attachTo, false);
        _beamT.localPosition = localOffset;
        _beamT.localRotation = Quaternion.Euler(localEulerOffset);

        _beamMF = go.AddComponent<MeshFilter>();
        _beamMR = go.AddComponent<MeshRenderer>();
        _beamMesh = new Mesh { name = "BeamCone" };
        _beamMF.sharedMesh = _beamMesh;

        var sh = Shader.Find("Legacy Shaders/Particles/Additive");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (!sh) sh = Shader.Find("Unlit/Color");
        _beamMat = new Material(sh);
        SetMatColor(_beamMat, beamColor);
        _beamMR.sharedMaterial = _beamMat;
        _beamMR.enabled = false;
    }

    void SetMatColor(Material m, Color c)
    {
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
        else if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        else if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }

    void UpdateBeamGeometry()
    {
        if (!_beamMesh) return;

        float endR = beamMatchSpotAngle
            ? Mathf.Tan(spotAngle * 0.5f * Mathf.Deg2Rad) * beamLength
            : beamEndRadius;

        int sides = Mathf.Clamp(beamSides, 8, 128);
        int vCount = (sides + 1) * 2; // 近・遠リング
        int tCount = sides * 2 * 3;

        var verts = new Vector3[vCount];
        var cols = new Color[vCount];
        var uvs = new Vector2[vCount];
        var tris = new int[tCount];

        for (int i = 0; i <= sides; i++)
        {
            float a = (float)i / sides * Mathf.PI * 2f;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);

            // 根元
            verts[i] = new Vector3(ca * beamStartRadius, sa * beamStartRadius, 0f);
            cols[i] = new Color(beamColor.r, beamColor.g, beamColor.b, beamColor.a);
            uvs[i] = new Vector2((float)i / sides, 0f);

            // 先端
            int j = i + (sides + 1);
            verts[j] = new Vector3(ca * endR, sa * endR, beamLength);
            cols[j] = new Color(beamColor.r, beamColor.g, beamColor.b, 0f);
            uvs[j] = new Vector2((float)i / sides, 1f);
        }

        int idx = 0;
        for (int i = 0; i < sides; i++)
        {
            int i0 = i, i1 = i + 1;
            int i0f = i0 + (sides + 1), i1f = i1 + (sides + 1);

            tris[idx++] = i0; tris[idx++] = i0f; tris[idx++] = i1f;
            tris[idx++] = i0; tris[idx++] = i1f; tris[idx++] = i1;
        }

        _beamMesh.Clear();
        _beamMesh.SetVertices(verts);
        _beamMesh.SetColors(cols);
        _beamMesh.SetUVs(0, uvs);
        _beamMesh.SetTriangles(tris, 0, true);
        _beamMesh.RecalculateBounds();

        // 裏面も見せたいならカリングOFF
        if (_beamMR && _beamMR.sharedMaterial && _beamMR.sharedMaterial.HasProperty("_Cull"))
            _beamMR.sharedMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
    }

    // ── 幽霊ヒット（ライト点灯中のみ / 高さ無視） ───────────────────────
    void HitScanGhosts()
    {
        if (!affectGhostOnLight) return;

        _hitTimer += Time.deltaTime;
        if (_hitTimer < hitCheckInterval) return;
        _hitTimer = 0f;

        // 起点・向き（ライトTransformそのもの）
        Vector3 origin = _lightT ? _lightT.position : (attachTo ? attachTo.TransformPoint(localOffset) : transform.TransformPoint(localOffset));
        Vector3 dir = _lightT ? _lightT.forward : (attachTo ? (attachTo.rotation * Quaternion.Euler(localEulerOffset) * Vector3.forward) : (transform.rotation * Quaternion.Euler(localEulerOffset) * Vector3.forward));

        // 高さ無視：XZ平面で判定
        Vector3 dirPlanar = new Vector3(dir.x, 0f, dir.z).normalized;
        if (dirPlanar.sqrMagnitude < 1e-6f) dirPlanar = Vector3.forward;

        float rangeUse = (hitRangeOverride >= 0f) ? hitRangeOverride : (_light ? _light.range : range);
        float halfDeg = Mathf.Max(1f, spotAngle) * 0.5f;
        float cosHalf = Mathf.Cos(halfDeg * Mathf.Deg2Rad);

        _currentlyHit.Clear();

        // Trigger も含めて検出
        Collider[] cols = Physics.OverlapSphere(origin, rangeUse, ghostMask, QueryTriggerInteraction.Collide);

        foreach (var col in cols)
        {
            // 角度判定（高さ無視）
            Vector3 toPlanar = col.bounds.center - origin;
            toPlanar.y = 0f;
            float sq = toPlanar.sqrMagnitude;
            if (sq < 1e-6f) continue;

            Vector3 toN = toPlanar / Mathf.Sqrt(sq);
            float dot = Vector3.Dot(dirPlanar, toN);
            if (dot < cosHalf) continue; // 円錐外

            // 基準コンポーネント（優先: GhostChase、無ければ Rigidbody 親子どちらでも）
            GhostChase chase = col.GetComponentInParent<GhostChase>();
            if (!chase) chase = col.GetComponentInChildren<GhostChase>();
            Component key = (Component)chase;

            Rigidbody rb = null;
            if (!key)
            {
                rb = col.GetComponentInParent<Rigidbody>() ?? col.GetComponentInChildren<Rigidbody>();
                key = rb;
            }
            else
            {
                rb = chase.GetComponentInParent<Rigidbody>() ?? chase.GetComponentInChildren<Rigidbody>();
            }

            if (!key) continue; // 幽霊成分なし

            _currentlyHit.Add(key);

            // 再スタンポリシー
            bool reenter = !_lastFrameHit.Contains(key);
            if (!continuousStunWhileLit && !reenter) continue;
            if (!continuousStunWhileLit && restunReenterCooldown > 0f)
            {
                if (_lastExitTime.TryGetValue(key, out float lastExit) &&
                    Time.time - lastExit < restunReenterCooldown) continue;
            }

            // ① 見つかったフラグOFF（在れば）
            if (clearAlertOnHit)
                col.transform.SendMessageUpwards("ClearAlert", SendMessageOptions.DontRequireReceiver);

            // ② 怒り：命中通知 → 激怒中ならスタンをスキップ
            GhostAnger anger = col.GetComponentInParent<GhostAnger>() ?? col.GetComponentInChildren<GhostAnger>();
            if (informAngerOnHit && anger) anger.OnLightHit(angerAddScale);

            bool isEnraged = anger && anger.IsEnraged;
            if (skipStunWhenEnraged && isEnraged)
            {
                if (debugLogHits) Debug.Log($"[FrontLightToggle] Enraged: no stun on {col.name}");
                continue; // ★スタン処理へ進まない
            }

            if (debugLogHits)
                Debug.Log($"[FrontLightToggle] Hit: {col.name} (reenter={reenter})");

            // ③ スタン
            if (stunGhostOnHit)
            {
                if (chase)
                {
                    if (_stunCo.TryGetValue(key, out var co)) StopCoroutine(co);
                    _stunCo[key] = StartCoroutine(StunChaseRoutine(chase, rb, stunSeconds));
                }
                else if (rb)
                {
                    if (_stunCo.TryGetValue(key, out var co2)) StopCoroutine(co2);
                    _stunCo[key] = StartCoroutine(StunRigidBody(rb, stunSeconds));
                }
            }
        }

        // 外れたタイミングを記録
        foreach (var k in _lastFrameHit)
            if (!_currentlyHit.Contains(k)) _lastExitTime[k] = Time.time;

        _lastFrameHit.Clear();
        foreach (var k in _currentlyHit) _lastFrameHit.Add(k);
    }

    IEnumerator StunChaseRoutine(GhostChase chase, Rigidbody rb, float sec)
    {
        // GhostChase を停止
        chase.enabled = false;

        // NavMeshAgent があれば止める（存在すれば）
        var agent = chase.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() ??
                    chase.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
        bool hadAgent = agent != null;
        if (hadAgent) agent.isStopped = true;

        float t = Mathf.Max(0.01f, sec);
        while (t > 0f)
        {
            if (rb)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
                var v = rb.velocity; rb.velocity = new Vector3(0f, v.y, 0f);
#endif
                rb.angularVelocity = Vector3.zero;
            }
            t -= Time.deltaTime;
            yield return null;
        }

        // 必ず復帰
        if (hadAgent) agent.isStopped = false;
        chase.enabled = true;
    }

    IEnumerator StunRigidBody(Rigidbody rb, float sec)
    {
        float t = Mathf.Max(0.01f, sec);
        while (t > 0f)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
            var v = rb.velocity; rb.velocity = new Vector3(0f, v.y, 0f);
#endif
            rb.angularVelocity = Vector3.zero;
            t -= Time.deltaTime;
            yield return null;
        }
    }

    // ── エディタ反映/Gizmos ────────────────────────────
    void OnValidate()
    {
        if (_light) ApplyLightParams();

        if (useMeshBeam && _beamMesh != null)
        {
            SetMatColor(_beamMat, beamColor);
            UpdateBeamGeometry();
        }

        if (_lightT)
        {
            _lightT.localPosition = localOffset;
            _lightT.localRotation = Quaternion.Euler(localEulerOffset);
        }
        if (_beamT)
        {
            _beamT.localPosition = localOffset;
            _beamT.localRotation = Quaternion.Euler(localEulerOffset);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;

        // 表示用も高さ無視で描く
        Vector3 origin = _lightT ? _lightT.position : (attachTo ? attachTo.TransformPoint(localOffset) : transform.TransformPoint(localOffset));
        Vector3 dir = _lightT ? _lightT.forward : (attachTo ? (attachTo.rotation * Quaternion.Euler(localEulerOffset) * Vector3.forward) : (transform.rotation * Quaternion.Euler(localEulerOffset) * Vector3.forward));
        Vector3 dirPlanar = new Vector3(dir.x, 0f, dir.z).normalized;

        float rangeUse = (hitRangeOverride >= 0f) ? hitRangeOverride : range;
        float r = Mathf.Tan(Mathf.Deg2Rad * spotAngle * 0.5f) * rangeUse;

        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawLine(origin, origin + dirPlanar * rangeUse);
        Handles.color = Gizmos.color;
        Handles.DrawWireDisc(origin + dirPlanar * rangeUse, Vector3.up, r); // 平面円
    }
#endif
}
