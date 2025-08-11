using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationControl : MonoBehaviour
{
    public Animator animator;              // Animator ‚ğ Inspector ‚Åİ’è
    public string moveParam = "isMoving";  // Animator ‚Ì Bool ƒpƒ‰ƒ[ƒ^–¼

    Vector2 moveInput; // “ü—Íó‘Ô‚ğ•Û‘¶

    // Input System ‚©‚çŒÄ‚Î‚ê‚é
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();

        // “ü—Í‚ªƒ[ƒ‚©‚Ç‚¤‚©‚Å”»’è
        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        animator.SetBool(moveParam, isMoving);
    }
}
