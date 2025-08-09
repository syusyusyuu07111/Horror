using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotateOnly : MonoBehaviour
{
    [Tooltip("true: カメラ基準 / false: ワールド基準")]
    public bool cameraRelative = true;
    [Tooltip("カメラ基準で回転したい場合は指定（未設定なら Camera.main）")]
    public Transform cam;

    Vector2 moveInput;

    void Awake()
    {
        if (!cam && Camera.main) cam = Camera.main.transform;
    }

    // Input Systemコールバック
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    void Update()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        if (input.sqrMagnitude < 0.001f) return; // 入力なしなら何もしない

        Vector3 dir = input.normalized;

        // カメラ基準に変換
        if (cameraRelative && cam)
        {
            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            dir = (right * input.x + fwd * input.z).normalized;
        }

        // 方向を即スナップ
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
