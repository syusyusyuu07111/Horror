using UnityEngine;
using UnityEngine.InputSystem;

public class InstantKeyFace : MonoBehaviour
{
    [Header("�J������ɂ���ꍇ��ON")]
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

        // �����ꂽ�g�u�ԁh�ɂ��������i�A�ł�f�����ؑւɋ����j
        if (kb.wKey.wasPressedThisFrame) dir = GetForward();
        if (kb.sKey.wasPressedThisFrame) dir = -GetForward();
        if (kb.dKey.wasPressedThisFrame) dir = GetRight();
        if (kb.aKey.wasPressedThisFrame) dir = -GetRight();

        // �΂߂ɂ��Ή��������ꍇ�i����t���[����2�L�[���������獇���j
        // ��: W �� D �𓯃t���[���ŉ������� 45�� �E�O��
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
        return Vector3.forward; // ���[���h+Z�
    }

    Vector3 GetRight()
    {
        if (cameraRelative && cam)
        {
            Vector3 r = cam.right; r.y = 0f; return r.sqrMagnitude > 0f ? r.normalized : Vector3.right;
        }
        return Vector3.right; // ���[���h+X�
    }
}
