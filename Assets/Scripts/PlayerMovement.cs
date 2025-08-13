using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("参照（任意）")]
    [Tooltip("未指定なら Camera.main")]
    public Transform cam;

    [Header("移動（W=カメラの“位置”から離れる方向）")]
    public bool cameraRelative = true;
    public float speed = 5f;
    public float inputDeadZone = 0.05f;

    [Header("ジャンプ")]
    public float jumpForce = 5f;

    [Header("向き（進行方向へ向く）")]
    [Tooltip("見た目だけ回したい場合はメッシュの親(visual root)を指定")]
    public Transform visualRoot;

    public enum ModelForwardAxis { ZPlus = 0, XPlus = 90, ZMinus = 180, XMinus = -90 }
    [Tooltip("モデルがどの軸を“前”として作られているか。Wで右を向くなら XPlus を選択")]
    public ModelForwardAxis modelForward = ModelForwardAxis.ZPlus;

    [Tooltip("微調整用の追加オフセット（度）")]
    public float extraYawOffset = 0f;

    public float turnSpeedDeg = 720f; // 見た目の回転速度

    // 入力
    Vector2 moveInput;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (!cam && Camera.main) cam = Camera.main.transform;
        if (!visualRoot) visualRoot = transform; // 未指定なら自分を回す
    }

    // Input System
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
#if UNITY_6000_0_OR_NEWER
        var v = rb.linearVelocity; v.y = 0f; rb.linearVelocity = v;
#else
        var v = rb.velocity; v.y = 0f; rb.velocity = v;
#endif
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        // 入力（XZ）
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        float mag = input.magnitude;

        // デッドゾーン
        if (mag < inputDeadZone)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
#else
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
#endif
            return;
        }
        if (mag > 1f) input.Normalize();

        // ==== 前＝「カメラ位置から離れる方向」 ====
        Vector3 moveDir = input;
        if (cameraRelative && cam)
        {
            Vector3 fwd = (transform.position - cam.position); // カメラ→プレイヤー（=画面奥）
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f) { fwd = -cam.forward; fwd.y = 0f; }
            fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);

            moveDir = right * input.x + fwd * input.z;
            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();
        }

        // 速度適用（水平のみ）
        Vector3 horiz = moveDir * (speed * Mathf.Clamp01(mag));
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector3(horiz.x, rb.linearVelocity.y, horiz.z);
#else
        var v2 = rb.velocity;
        rb.velocity = new Vector3(horiz.x, v2.y, horiz.z);
#endif

        // ==== 進行方向へ見た目を向ける（モデルの前向きズレを補正） ====
        if (moveDir.sqrMagnitude > 1e-6f)
        {
            // 進行方向 → 目標回転
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);

            // モデルの“前”が+Z以外なら補正
            float axisOffset = (int)modelForward; // 0, 90, 180, -90
            targetRot *= Quaternion.Euler(0f, axisOffset + extraYawOffset, 0f);

            visualRoot.rotation = Quaternion.RotateTowards(
                visualRoot.rotation, targetRot, turnSpeedDeg * Time.deltaTime);
        }
    }
}
