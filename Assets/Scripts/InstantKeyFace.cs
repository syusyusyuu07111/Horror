using UnityEngine;
using UnityEngine.InputSystem;

public class InstantKeyFace : MonoBehaviour
{
    [Header("カメラ基準にする場合はON")]
    public bool cameraRelative = false;
    public Transform cam; // cameraRelative=true のときに参照（未設定なら Camera.main）

    void Awake()
    {
        if (cameraRelative && !cam && Camera.main) cam = Camera.main.transform;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        Vector3? dir = null;

        // 押された“瞬間”にだけ反応（連打や素早い切替に強い）
        if (kb.wKey.wasPressedThisFrame) dir = GetForward();
        if (kb.sKey.wasPressedThisFrame) dir = -GetForward();
        if (kb.dKey.wasPressedThisFrame) dir = GetRight();
        if (kb.aKey.wasPressedThisFrame) dir = -GetRight();

        // 斜めにも対応したい場合（同一フレームで2キー押下したら合成）
        // 例: W と D を同フレームで押したら 45° 右前へ
        if (kb.wKey.wasPressedThisFrame && kb.dKey.wasPressedThisFrame) dir = (GetForward() + GetRight()).normalized;
        if (kb.wKey.wasPressedThisFrame && kb.aKey.wasPressedThisFrame) dir = (GetForward() - GetRight()).normalized;
        if (kb.sKey.wasPressedThisFrame && kb.dKey.wasPressedThisFrame) dir = (-GetForward() + GetRight()).normalized;
        if (kb.sKey.wasPressedThisFrame && kb.aKey.wasPressedThisFrame) dir = (-GetForward() - GetRight()).normalized;

        if (dir.HasValue && dir.Value.sqrMagnitude > 1e-6f)
        {
            transform.rotation = Quaternion.LookRotation(dir.Value, Vector3.up);
        }
    }

    Vector3 GetForward()
    {
        if (cameraRelative && cam)
        {
            Vector3 f = cam.forward; f.y = 0f; return f.sqrMagnitude > 0f ? f.normalized : Vector3.forward;
        }
        return Vector3.forward; // ワールド+Z基準
    }

    Vector3 GetRight()
    {
        if (cameraRelative && cam)
        {
            Vector3 r = cam.right; r.y = 0f; return r.sqrMagnitude > 0f ? r.normalized : Vector3.right;
        }
        return Vector3.right; // ワールド+X基準
    }
}
