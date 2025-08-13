using UnityEngine;
using UnityEngine.InputSystem;

public class InstantKeyFace : MonoBehaviour
{
    [Header("�J�����ʒu��Ō�����ς���")]
    public bool cameraRelative = false;
    public Transform cam; // cameraRelative=true �̂Ƃ��ɎQ�Ɓi���ݒ�Ȃ� Camera.main�j

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

        // �΂ߓ��́i���������Ή��j
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
            // �J�����ʒu���猩���u�v���C���[���牓����������v��O�Ƃ���
            Vector3 fwd = (transform.position - cam.position);
            fwd.y = 0f;
            return fwd.sqrMagnitude > 0f ? fwd.normalized : Vector3.forward;
        }
        return Vector3.forward; // ���[���h+Z�
    }

    Vector3 GetRight()
    {
        if (cameraRelative && cam)
        {
            // ������ƑO��������E�������v�Z
            Vector3 fwd = GetForward();
            return Vector3.Cross(Vector3.up, fwd).normalized;
        }
        return Vector3.right; // ���[���h+X�
    }
}
