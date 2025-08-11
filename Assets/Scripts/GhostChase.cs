using UnityEngine;
using UnityEngine.SceneManagement; // �V�[���J��

public class GhostChase : MonoBehaviour
{
    public Transform player;            // �ǂ�������Ώ�
    public float chaseSpeed = 3f;       // �ǐՑ��x
    public string gameOverScene = "GameOver"; // �J�ڐ�V�[����

    Rigidbody rb;
    bool _ending; // ��d�J�ږh�~

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
        if (!player || _ending) return;

        // �v���C���[�ւ̕���
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

        // �v���C���[�̕���������
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        // �ړ�
        if (rb && !rb.isKinematic)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector3(dir.x * chaseSpeed, rb.linearVelocity.y, dir.z * chaseSpeed);
#else
            rb.velocity = new Vector3(dir.x * chaseSpeed, rb.velocity.y, dir.z * chaseSpeed);
#endif
        }
        else
        {
            transform.position += dir * chaseSpeed * Time.deltaTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) HandleCaught();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) HandleCaught();
    }

    // �߂܂������̋��ʏ���
    void HandleCaught()
    {
        if (_ending) return; // ��d���s�h�~
        _ending = true;

        // �� �����Œ��O�̃V�[������ۑ����Ă���Q�[���I�[�o�[��
        SceneTracker.SaveCurrentScene();

        SceneManager.LoadScene(gameOverScene);
    }
}
