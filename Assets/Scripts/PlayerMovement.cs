using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("参照（任意）")]
    [Tooltip("カメラ基準で移動したい場合に指定。未指定なら Camera.main")]
    public Transform cam;
    [Tooltip("true: カメラ基準 / false: ワールド基準")]
    public bool cameraRelative = true;

    [Header("移動")]
    [Tooltip("最大移動速度 (m/s)")]
    public float speed = 5f;
    [Tooltip("スティックの遊び（これ未満は無視）")]
    public float inputDeadZone = 0.05f;

    [Header("ジャンプ")]
    public float jumpForce = 5f;

    // 入力
    Vector2 moveInput;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // 横転防止。Y回転はプレイヤー操作外なので凍結しない
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!cam && Camera.main) cam = Camera.main.transform;
    }

    // Input System コールバック
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        // 垂直速度を一旦ゼロにしてからジャンプ
#if UNITY_6000_0_OR_NEWER
        Vector3 v = rb.linearVelocity; v.y = 0f; rb.linearVelocity = v;
#else
        Vector3 v = rb.velocity; v.y = 0f; rb.velocity = v;
#endif
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // 入力ベクトル（XZ）
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        float mag = input.magnitude;

        // デッドゾーン
        if (mag < inputDeadZone)
        {
            // 入力なし：横移動は止める（Yだけ維持）
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
            return;
        }

        if (mag > 1f) input.Normalize(); // アナログ入力を正規化

        // カメラ基準 or ワールド基準で移動方向を決定（回転は一切変更しない）
        Vector3 moveDir = input;
        if (cameraRelative && cam)
        {
            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            moveDir = (right * input.x + fwd * input.z);
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
        }

        // 速度を設定（XZのみ）。向き（rotation）は絶対に触らない
        Vector3 horiz = moveDir * (speed * Mathf.Clamp01(mag));
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(horiz.x, rb.linearVelocity.y, horiz.z);
#else
        Vector3 v2 = rb.velocity;
        rb.velocity = new Vector3(horiz.x, v2.y, horiz.z);
#endif
    }
}
