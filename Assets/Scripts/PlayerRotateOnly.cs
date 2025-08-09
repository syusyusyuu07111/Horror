using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerRotateOnly : MonoBehaviour
{
    [Tooltip("true: �J����� / false: ���[���h�")]
    public bool cameraRelative = true;
    [Tooltip("�J������ŉ�]�������ꍇ�͎w��i���ݒ�Ȃ� Camera.main�j")]
    public Transform cam;

    Vector2 moveInput;

    void Awake()
    {
        if (!cam && Camera.main) cam = Camera.main.transform;
    }

    // Input System�R�[���o�b�N
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    void Update()
    {
        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        if (input.sqrMagnitude < 0.001f) return; // ���͂Ȃ��Ȃ牽�����Ȃ�

        Vector3 dir = input.normalized;

        // �J������ɕϊ�
        if (cameraRelative && cam)
        {
            Vector3 fwd = cam.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.right; right.y = 0f; right.Normalize();
            dir = (right * input.x + fwd * input.z).normalized;
        }

        // �����𑦃X�i�b�v
        transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
