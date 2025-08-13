using UnityEngine;

public class FixedAngleFollowCamera : MonoBehaviour
{
    [Header("ターゲット")]
    public Transform target;

    [Header("回転（位置は初期相対オフセットを維持）")]
    public float yawSmoothTime = 0.12f; // 左右追従のなめらかさ

    // 内部保持：起動時の相対オフセット＆ピッチ
    Vector3 _initialOffsetWS; // ワールド空間での(カメラ - ターゲット)
    float _lockedPitchDeg;    // 起動時のピッチ角
    float _yawVel;

    void Start()
    {
        if (!target) return;

        // ★ここで“今の見た目”を記録する
        _initialOffsetWS = transform.position - target.position;

        // ピッチ(上下)をロック：現在角度をそのまま使う
        var e = transform.rotation.eulerAngles;
        _lockedPitchDeg = Normalize180(e.x);
    }

    void LateUpdate()
    {
        if (!target) return;

        // 位置：常に「起動時の相対オフセット」を維持（距離・高さも固定）
        Vector3 desiredPos = target.position + _initialOffsetWS;
        transform.position = desiredPos;

        // 左右だけターゲットへ向ける（XZ平面に投影）
        Vector3 dirPlanar = target.position - transform.position;
        dirPlanar.y = 0f;
        if (dirPlanar.sqrMagnitude < 1e-6f) return;

        float desiredYaw = Mathf.Atan2(dirPlanar.x, dirPlanar.z) * Mathf.Rad2Deg;

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        float currYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;

        float nextYaw = Mathf.SmoothDampAngle(currYaw, desiredYaw, ref _yawVel, yawSmoothTime);

        // 上下は固定、ロールは0
        transform.rotation = Quaternion.Euler(_lockedPitchDeg, nextYaw, 0f);
    }

    static float Normalize180(float x)
    {
        x %= 360f;
        if (x > 180f) x -= 360f;
        if (x < -180f) x += 360f;
        return x;
    }
}
