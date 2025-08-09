using System.Collections.Generic;
using UnityEngine;

public class FollowOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("未指定なら target を見る")] public Transform lookAt;

    [Header("カメラ移動/回転のスムーズさ")]
    public float posDamping = 9f;
    public float rotDamping = 18f;

    [Header("衝突/遮蔽")]
    public LayerMask collisionMask = ~0;
    public LayerMask occluderMask = ~0;
    public float collisionRadius = 0.25f;
    public float collisionBuffer = 0.06f;
    public bool allowDistanceShrinkAsLastResort = false;

    [Header("センタリング保証")]
    public Rect safeViewportRect = new Rect(0.35f, 0.35f, 0.30f, 0.30f);
    public float recenterBoostDuration = 0.25f;
    public float posDampingBoost = 3f;
    public float rotDampingBoost = 3f;

    // 内部
    Vector3 _initialDirFromPivot;
    float _initialDistance;
    float _initialPitchDeg;
    float _initialPivotY;

    float _recenteringUntil = -1f;
    Camera _cam;

    readonly HashSet<Renderer> _hidden = new HashSet<Renderer>();

    void Start()
    {
        if (!target)
        {
            Debug.LogWarning("FollowOrbitCamera: target 未設定");
            enabled = false;
            return;
        }
        if (!lookAt) lookAt = target;

        _cam = GetComponent<Camera>();
        if (!_cam) _cam = Camera.main;

        // 開始時の注視点（高さ固定）
        Vector3 pivotNow = lookAt.position;
        _initialPivotY = pivotNow.y;

        // 開始構図を記録
        Vector3 fromPivot = transform.position - pivotNow;
        _initialDistance = fromPivot.magnitude > 0.0001f ? fromPivot.magnitude : 6f;
        _initialDirFromPivot = fromPivot.normalized;
        _initialPitchDeg = transform.rotation.eulerAngles.x;
    }

    void LateUpdate()
    {
        if (!target) return;

        // pivotはY固定（開始時の高さ）
        Vector3 pivot = new Vector3(
            lookAt.position.x,
            _initialPivotY,
            lookAt.position.z
        );

        // 開始構図の距離を維持
        Vector3 desiredPos = pivot + _initialDirFromPivot * _initialDistance;

        // 衝突回避
        Vector3 dirFromPivot = desiredPos - pivot;
        Quaternion baseRotForSlide = Quaternion.LookRotation(-dirFromPivot.normalized, Vector3.up);
        Vector3 safePos = KeepDistanceSlideAround(pivot, desiredPos, baseRotForSlide, out bool hitSomething);

        if (hitSomething && allowDistanceShrinkAsLastResort)
        {
            Vector3 d = desiredPos - pivot;
            float L = d.magnitude;
            if (L > 0.0001f)
            {
                Vector3 unit = d / L;
                if (Physics.SphereCast(pivot, collisionRadius, unit, out var hit, L, collisionMask, QueryTriggerInteraction.Ignore))
                    safePos = hit.point - unit * collisionBuffer;
            }
        }

        // セーフ枠チェック → はみ出たらリセンター
        bool forceRecentering = false;
        if (_cam)
        {
            Vector3 vp = _cam.WorldToViewportPoint(lookAt.position);
            if (vp.z <= 0f ||
                vp.x < safeViewportRect.xMin ||
                vp.x > safeViewportRect.xMax ||
                vp.y < safeViewportRect.yMin ||
                vp.y > safeViewportRect.yMax)
            {
                _recenteringUntil = Time.time + recenterBoostDuration;
            }
            forceRecentering = (Time.time < _recenteringUntil);
        }

        // 位置適用
        float posDampNow = forceRecentering ? posDamping * posDampingBoost : posDamping;
        float lerp = 1f - Mathf.Exp(-posDampNow * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, safePos, lerp);

        // 回転適用（上下回転なし）
        Vector3 toPivot = pivot - transform.position;
        Vector3 toPivotXZ = new Vector3(toPivot.x, 0f, toPivot.z);
        float yawDeg = toPivotXZ.sqrMagnitude > 1e-6f
            ? Mathf.Atan2(toPivotXZ.x, toPivotXZ.z) * Mathf.Rad2Deg
            : transform.eulerAngles.y;

        Quaternion targetRot = Quaternion.Euler(_initialPitchDeg, yawDeg, 0f);
        float rotDampNow = forceRecentering ? rotDamping * rotDampingBoost : rotDamping;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotDampNow * Time.deltaTime);

        HandleOccluders(transform.position, pivot);
    }

    Vector3 KeepDistanceSlideAround(Vector3 pivot, Vector3 desiredPos, Quaternion baseRot, out bool hitSomething)
    {
        hitSomething = false;
        Vector3 dirCam = desiredPos - pivot;
        float dist = dirCam.magnitude;
        if (dist < 0.0001f) return desiredPos;
        dirCam /= dist;

        if (!Physics.SphereCast(pivot, collisionRadius, dirCam, out var _hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            return desiredPos;

        hitSomething = true;
        float[] angles = { 4f, -4f, 8f, -8f, 12f, -12f };

        foreach (float ay in angles)
        {
            Quaternion q = Quaternion.Euler(0f, ay, 0f) * baseRot;
            Vector3 cand = pivot + (q * Vector3.back) * _initialDistance;
            Vector3 d = cand - pivot; float L = d.magnitude; if (L < 0.0001f) continue; d /= L;
            if (!Physics.SphereCast(pivot, collisionRadius, d, out _hit, L, collisionMask, QueryTriggerInteraction.Ignore))
                return cand;
        }
        foreach (float ax in angles)
        {
            Quaternion q = Quaternion.Euler(ax, 0f, 0f) * baseRot;
            Vector3 cand = pivot + (q * Vector3.back) * _initialDistance;
            Vector3 d = cand - pivot; float L = d.magnitude; if (L < 0.0001f) continue; d /= L;
            if (!Physics.SphereCast(pivot, collisionRadius, d, out _hit, L, collisionMask, QueryTriggerInteraction.Ignore))
                return cand;
        }
        return desiredPos;
    }

    void HandleOccluders(Vector3 from, Vector3 to)
    {
        foreach (var r in _hidden) if (r) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        _hidden.Clear();

        Vector3 dir = (to - from);
        float d = dir.magnitude;
        if (d < 0.0001f) return;
        dir /= d;

        var hits = Physics.SphereCastAll(from, collisionRadius * 0.9f, dir, d, occluderMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            var r = hits[i].collider.GetComponent<Renderer>();
            if (!r) continue;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            _hidden.Add(r);
        }
    }
}
