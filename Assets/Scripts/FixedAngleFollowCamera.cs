using UnityEngine;

public class FixedAngleFollowCamera : MonoBehaviour
{
    [Header("�^�[�Q�b�g")]
    public Transform target;

    [Header("��]�i�ʒu�͏������΃I�t�Z�b�g���ێ��j")]
    public float yawSmoothTime = 0.12f; // ���E�Ǐ]�̂Ȃ߂炩��

    // �����ێ��F�N�����̑��΃I�t�Z�b�g���s�b�`
    Vector3 _initialOffsetWS; // ���[���h��Ԃł�(�J���� - �^�[�Q�b�g)
    float _lockedPitchDeg;    // �N�����̃s�b�`�p
    float _yawVel;

    void Start()
    {
        if (!target) return;

        // �������Łg���̌����ځh���L�^����
        _initialOffsetWS = transform.position - target.position;

        // �s�b�`(�㉺)�����b�N�F���݊p�x�����̂܂܎g��
        var e = transform.rotation.eulerAngles;
        _lockedPitchDeg = Normalize180(e.x);
    }

    void LateUpdate()
    {
        if (!target) return;

        // �ʒu�F��Ɂu�N�����̑��΃I�t�Z�b�g�v���ێ��i�����E�������Œ�j
        Vector3 desiredPos = target.position + _initialOffsetWS;
        transform.position = desiredPos;

        // ���E�����^�[�Q�b�g�֌�����iXZ���ʂɓ��e�j
        Vector3 dirPlanar = target.position - transform.position;
        dirPlanar.y = 0f;
        if (dirPlanar.sqrMagnitude < 1e-6f) return;

        float desiredYaw = Mathf.Atan2(dirPlanar.x, dirPlanar.z) * Mathf.Rad2Deg;

        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.forward;
        float currYaw = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;

        float nextYaw = Mathf.SmoothDampAngle(currYaw, desiredYaw, ref _yawVel, yawSmoothTime);

        // �㉺�͌Œ�A���[����0
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
