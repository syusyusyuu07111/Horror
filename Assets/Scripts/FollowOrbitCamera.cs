using UnityEngine;
using UnityEngine.InputSystem;

public class FollowOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    [Tooltip("�^�[�Q�b�g�̓�������B���w��Ȃ� target ���g��")]
    public Transform lookAt;

    [Header("����/�p�x")]
    public float distance = 4f;
    public float minDistance = 1.2f;
    public float maxDistance = 6f;
    public float height = 1.6f;         // �ڐ��̍���
    public float minPitch = -30f;
    public float maxPitch = 60f;

    [Header("���슴")]
    public float yawSpeed = 180f;       // ������] �p�x/�b
    public float pitchSpeed = 120f;     // ������] �p�x/�b
    public float zoomSpeed = 6f;        // �Y�[������/�b
    public float posDamping = 12f;      // �ʒu�X���[�Y
    public float rotDamping = 18f;      // ��]�X���[�Y

    [Header("�����Ǐ]�i�ړ������ɂ����������j")]
    public Rigidbody targetRb;          // �t���Ȃ��Ă�OK
    public float autoAlignSpeed = 90f;  // �����쎞�ɐi�s�����։�
    public float autoAlignVelThreshold = 0.5f;
    public float noLookInputTimeToAuto = 0.4f;

    [Header("�J�����Փˉ��")]
    public LayerMask collisionMask = ~0;
    public float collisionRadius = 0.2f; // �X�t�B�A�L���X�g���a
    public float collisionBuffer = 0.05f;

    [Header("Input (Input Actions �̎Q��)")]
    public InputActionReference lookAction; // Vector2 (Mouse delta / RightStick)
    public InputActionReference zoomAction; // float (Mouse scroll Y / Gamepad triggers��)

    float yaw, pitch;
    float noLookTimer;
    Vector3 vel; // SmoothDamp�p

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
        if (!target) { Debug.LogWarning("FollowOrbitCamera: target���ݒ�"); enabled = false; return; }
        if (!lookAt) lookAt = target;
        // �����p�x�����̌������琄��
        Vector3 forward = target.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.0001f) yaw = Quaternion.LookRotation(forward).eulerAngles.y;
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- ���� ---
        Vector2 look = lookAction ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;
        float zoom = zoomAction ? zoomAction.action.ReadValue<float>() : 0f;

        bool hasLookInput = look.sqrMagnitude > 0.000001f;

        // �E�X�e�B�b�N��}�E�X�̊��x�i�b�Ԋp�x�j
        yaw += look.x * yawSpeed * Time.deltaTime;
        pitch -= look.y * pitchSpeed * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // �Y�[���i�X�N���[���̕����͊��ŋt�ɂȂ肪�����D�݂Ŕ��]�j
        distance = Mathf.Clamp(distance - zoom * (zoomSpeed * Time.deltaTime), minDistance, maxDistance);

        // �����쎞�Ԃ̌v��
        if (hasLookInput) noLookTimer = 0f; else noLookTimer += Time.deltaTime;

        // --- �����Ǐ]�Ői�s�����Ɍ�����i�C�Ӂj ---
        if (!hasLookInput && targetRb && noLookTimer > noLookInputTimeToAuto)
        {
            Vector3 v = targetRb.linearVelocity; v.y = 0;
            if (v.sqrMagnitude > autoAlignVelThreshold * autoAlignVelThreshold)
            {
                float targetYaw = Quaternion.LookRotation(v).eulerAngles.y;
                yaw = Mathf.MoveTowardsAngle(yaw, targetYaw, autoAlignSpeed * Time.deltaTime);
            }
        }

        // --- �ڕW�ʒu�v�Z ---
        Vector3 pivot = (lookAt ? lookAt.position : target.position) + Vector3.up * height;
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredCamPos = pivot + rot * new Vector3(0, 0, -distance);

        // --- �J�����Փˉ���i�X�t�B�A�L���X�g�Ŏ�O�Ɋ񂹂�j ---
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

        // --- �X���[�Y�Ǐ] ---
        // �ʒu
        transform.position = Vector3.SmoothDamp(transform.position, safeCamPos, ref vel, 1f / Mathf.Max(0.0001f, posDamping));
        // ��]�i�^�[�Q�b�g�������j
        Quaternion lookRot = Quaternion.LookRotation(pivot - transform.position, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, rotDamping * Time.deltaTime);
    }

    // Scene��Ŕ��a�����i�C�Ӂj
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collisionRadius);
    }
}
