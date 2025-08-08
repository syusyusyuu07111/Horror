using UnityEngine;
using UnityEngine.InputSystem;

public class FollowOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("ターゲットの頭あたり。未指定なら target を使う")]
    public Transform lookAt;

    [Header("距離/角度")]
    public float distance = 4f;
    public float minDistance = 1.2f;
    public float maxDistance = 6f;
    public float height = 1.6f;         // 目線の高さ
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("操作感")]
    public float yawSpeed = 180f;       // 水平回転 角度/秒
    public float pitchSpeed = 120f;     // 垂直回転 角度/秒
    public float zoomSpeed = 6f;        // ズーム距離/秒
    public float posDamping = 12f;      // 位置スムーズ
    public float rotDamping = 18f;      // 回転スムーズ

    [Header("自動追従（移動方向にゆっくり向く）")]
    public Rigidbody targetRb;          // 付けなくてもOK
    public float autoAlignSpeed = 90f;  // 無操作時に進行方向へ回頭
    public float autoAlignVelThreshold = 0.5f;
    public float noLookInputTimeToAuto = 0.4f;

    [Header("カメラ衝突回避")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.2f; // スフィアキャスト半径
    public float collisionBuffer = 0.05f;

    [Header("Input (Input Actions の参照)")]
    public InputActionReference lookAction; // Vector2 (Mouse delta / RightStick)
    public InputActionReference zoomAction; // float (Mouse scroll Y / Gamepad triggers等)

    float yaw, pitch;
    float noLookTimer;
    Vector3 vel; // SmoothDamp用

    void OnEnable()
    {
        lookAction?.action.Enable();
        zoomAction?.action.Enable();
    }
    void OnDisable()
    {
        lookAction?.action.Disable();
        zoomAction?.action.Disable();
    }

    void Start()
    {
        if (!target) { Debug.LogWarning("FollowOrbitCamera: target未設定"); enabled = false; return; }
        if (!lookAt) lookAt = target;
        // 初期角度を今の向きから推定
        Vector3 forward = target.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.0001f) yaw = Quaternion.LookRotation(forward).eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- 入力 ---
        Vector2 look = lookAction ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        float zoom = zoomAction ? zoomAction.action.ReadValue<float>() : 0f;

        bool hasLookInput = look.sqrMagnitude > 0.000001f;

        // 右スティックやマウスの感度（秒間角度）
        yaw += look.x * yawSpeed * Time.deltaTime;
        pitch -= look.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // ズーム（スクロールの符号は環境で逆になりがち→好みで反転）
        distance = Mathf.Clamp(distance - zoom * (zoomSpeed * Time.deltaTime), minDistance, maxDistance);

        // 無操作時間の計測
        if (hasLookInput) noLookTimer = 0f; else noLookTimer += Time.deltaTime;

        // --- 自動追従で進行方向に向ける（任意） ---
        if (!hasLookInput && targetRb && noLookTimer > noLookInputTimeToAuto)
        {
            Vector3 v = targetRb.linearVelocity; v.y = 0;
            if (v.sqrMagnitude > autoAlignVelThreshold * autoAlignVelThreshold)
            {
                float targetYaw = Quaternion.LookRotation(v).eulerAngles.y;
                yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, autoAlignSpeed * Time.deltaTime);
            }
        }

        // --- 目標位置計算 ---
        Vector3 pivot = (lookAt ? lookAt.position : target.position) + Vector3.up * height;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredCamPos = pivot + rot * new Vector3(0, 0, -distance);

        // --- カメラ衝突回避（スフィアキャストで手前に寄せる） ---
        Vector3 dir = (desiredCamPos - pivot);
        float dist = dir.magnitude;
        Vector3 safeCamPos = desiredCamPos;
        if (dist > 0.001f)
        {
            dir /= dist;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                safeCamPos = hit.point - dir * collisionBuffer;
            }
        }

        // --- スムーズ追従 ---
        // 位置
        transform.position = Vector3.SmoothDamp(transform.position, safeCamPos, ref vel, 1f / Mathf.Max(0.0001f, posDamping));
        // 回転（ターゲットを向く）
        Quaternion lookRot = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotDamping * Time.deltaTime);
    }

    // Scene上で半径可視化（任意）
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}
