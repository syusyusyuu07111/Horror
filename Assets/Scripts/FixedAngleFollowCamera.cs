using UnityEngine;

public class FixedAngleFollowCamera : MonoBehaviour
{
    [Header("�^�[�Q�b�g")]
    public Transform target;

    [Header("��]�ݒ�")]
    public float yawSmoothTime = 0.12f; // ���E�Ǐ]�̂Ȃ߂炩��
    public float rotateSpeed = 90f;     // ���L�[�ŉ񂷑��x�i�x/�b�j

    // �����ێ�
    Vector3 _initialOffsetWS; // ���[���h��Ԃł�(�J���� - �^�[�Q�b�g)
    float _lockedPitchDeg;    // �N�����̃s�b�`�p
    float _yawVel;
    float _manualYawOffset;   // ���L�[�ŉ�������]�p�x

    void Start()
    {
        if (!target) return;

        // �������΃I�t�Z�b�g���L�^
        _initialOffsetWS = transform.position - target.position;

        // �����s�b�`�����b�N
        var e = transform.rotation.eulerAngles;
        _lockedPitchDeg = Normalize180(e.x);
    }

    void Update()
    {
        // ���L�[���͂ŉ�]�p�����Z
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

        // ���L�[�̓��͂𔽉f�������΃I�t�Z�b�g���v�Z
        Quaternion yawRot = Quaternion.Euler(0f, _manualYawOffset, 0f);
        Vector3 rotatedOffset = yawRot * _initialOffsetWS;

        // �J�����ʒu�X�V
        Vector3 desiredPos = target.position + rotatedOffset;
        transform.position = desiredPos;

        // �J�����̌����i�㉺�Œ�j
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
