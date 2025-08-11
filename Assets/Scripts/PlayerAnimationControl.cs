using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationControl : MonoBehaviour
{
    public Animator animator;              // Animator �� Inspector �Őݒ�
    public string moveParam = "isMoving";  // Animator �� Bool �p�����[�^��

    Vector2 moveInput; // ���͏�Ԃ�ۑ�

    // Input System ����Ă΂��
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        // ���͂��[�����ǂ����Ŕ���
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetBool(moveParam, isMoving);
    }
}
