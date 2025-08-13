using UnityEngine;

public class PlayerLightController : MonoBehaviour
{
    public Transform player;          // キャラのTransform
    public float yawSmooth = 0.08f;   // 左右の追従なめらかさ（0で即時）
    public bool followPitch = false;  // 上下も追従するならtrue

    float _yawVel;
    float _lockedPitch;               // 起動時に固定するピッチ

    void Start()
    {
        _lockedPitch = Normalize180(transform.eulerAngles.x);
    }

    void LateUpdate()
    {
        if (!player) return;

        // ★位置は一切変更しない（transform.positionは触らない）

        // 向き合わせ用のベクトル
        Vector3 fwd = player.forward;
        if (!followPitch)
        {
            fwd.y = 0f; // 上下を無視
        }

        if (fwd.sqrMagnitude < 1e-6f) return; // 停止時は処理しない
        fwd.Normalize();

        // 現在ヨー角
        float currYaw = Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg;
        // 目標ヨー角
        float desiredYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;

        // スムーズ追従 or 即時
        float nextYaw = (yawSmooth <= 0f)
            ? desiredYaw
            : Mathf.SmoothDampAngle(currYaw, desiredYaw, ref _yawVel, yawSmooth);

        // ピッチ（上下角）
        float pitch = _lockedPitch;
        if (followPitch)
        {
            float horiz = new Vector2(player.forward.x, player.forward.z).magnitude;
            pitch = Mathf.Atan2(-player.forward.y, horiz) * Mathf.Rad2Deg;
        }

        // 回転を適用
        transform.rotation = Quaternion.Euler(pitch, nextYaw, 0f);
    }

    // 角度を-180〜180に正規化する
    private float Normalize180(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        if (angle < -180f) angle += 360f;
        return angle;
    }
}
