using UnityEngine;
using UnityEngine.SceneManagement; // �Q�[���I�[�o�[��ʑJ�ڗp

public class GhostChase : MonoBehaviour
{
    public Transform player;      // �ǂ�������Ώ�
    public float chaseSpeed = 3f; // �ǐՑ��x
    public string gameOverScene = "GameOver"; // �J�ڐ�V�[����

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.freezeRotation = true; // ��]�͕����ŕς��Ȃ�
        }
    }

    void Update()
    {
        if (!player) return;

        // �v���C���[�ւ̕���
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f; // �������������ǂ�

        // �v���C���[�̕���������
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        // �ړ�
        if (rb && !rb.isKinematic)
        {
            // Unity 6�`��
            rb.linearVelocity = new Vector3(dir.x * chaseSpeed, rb.linearVelocity.y, dir.z * chaseSpeed);
        }
        else
        {
            transform.position += dir * chaseSpeed * Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            SceneManager.LoadScene(gameOverScene);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(gameOverScene);
        }
    }
}
