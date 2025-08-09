using UnityEngine;
using UnityEngine.SceneManagement; // ゲームオーバー画面遷移用

public class GhostChase : MonoBehaviour
{
    public Transform player;      // 追いかける対象
    public float chaseSpeed = 3f; // 追跡速度
    public string gameOverScene = "GameOver"; // 遷移先シーン名

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.freezeRotation = true; // 回転は物理で変えない
        }
    }

    void Update()
    {
        if (!player) return;

        // プレイヤーへの方向
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f; // 水平方向だけ追う

        // プレイヤーの方向を向く
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        // 移動
        if (rb && !rb.isKinematic)
        {
            // Unity 6形式
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
