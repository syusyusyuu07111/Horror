using UnityEngine;

public class FixedAngleFollowCamera : MonoBehaviour
{
    [Header("ターゲット")]
    public Transform target;

    [Header("回転設定")]
    public float yawSmoothTime = 0.12f; // 左右追従のなめらかさ
    public float rotateSpeed = 90f;     // 矢印キーで回す速度（度/秒）

    // 内部保持
    Vector3 _initialOffsetWS; // ワールド空間での(カメラ - ターゲット)
    float _lockedPitchDeg;    // 起動時のピッチ角
    float _yawVel;
    float _manualYawOffset;   // 矢印キーで加えた回転角度

    void Start()
    {
        if (!target) return;

        // 初期相対オフセットを記録
        _initialOffsetWS = transform.position - target.position;

        // 初期ピッチをロック
        var e = transform.rotation.eulerAngles;
        _lockedPitchDeg = Normalize180(e.x);
    }

    void Update()
    {
        // 矢印キー入力で回転角を加算
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            _manualYawOffset -= rotateSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            _manualYawOffset += rotateSpeed * Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // 矢印キーの入力を反映した相対オフセットを計算
        Quaternion yawRot = Quaternion.Euler(0f, _manualYawOffset, 0f);
        Vector3 rotatedOffset = yawRot * _initialOffsetWS;

        // カメラ位置更新
        Vector3 desiredPos = target.position + rotatedOffset;
        transform.position = desiredPos;

        // カメラの向き（上下固定）
        Vector3 dirPlanar = target.position - transform.position;
        dirPlanar.y = 0f;
        if (dirPlanar.sqrMagnitude < 1e-6f) return;

        float desiredYaw = Mathf.Atan2(dirPlanar.x, dirPlanar.z) * Mathf.Rad2Deg;

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        float currYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;

        float nextYaw = Mathf.SmoothDampAngle(currYaw, desiredYaw, ref _yawVel, yawSmoothTime);

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
