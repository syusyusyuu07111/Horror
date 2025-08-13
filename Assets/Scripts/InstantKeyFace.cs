using UnityEngine;
using UnityEngine.InputSystem;

public class InstantKeyFace : MonoBehaviour
{
    [Header("カメラ位置基準で向きを変える")]
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

        if (kb.wKey.wasPressedThisFrame) dir = GetForward();
        if (kb.sKey.wasPressedThisFrame) dir = -GetForward();
        if (kb.dKey.wasPressedThisFrame) dir = GetRight();
        if (kb.aKey.wasPressedThisFrame) dir = -GetRight();

        // 斜め入力（同時押し対応）
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
            // カメラ位置から見た「プレイヤーから遠ざかる方向」を前とする
            Vector3 fwd = (transform.position - cam.position);
            fwd.y = 0f;
            return fwd.sqrMagnitude > 0f ? fwd.normalized : Vector3.forward;
        }
        return Vector3.forward; // ワールド+Z基準
    }

    Vector3 GetRight()
    {
        if (cameraRelative && cam)
        {
            // 上方向と前方向から右方向を計算
            Vector3 fwd = GetForward();
            return Vector3.Cross(Vector3.up, fwd).normalized;
        }
        return Vector3.right; // ワールド+X基準
    }
}
