using UnityEngine;
using UnityEngine.SceneManagement; // シーン遷移

public class GhostChase : MonoBehaviour
{
    public Transform player;            // 追いかける対象
    public float chaseSpeed = 3f;       // 追跡速度
    public string gameOverScene = "GameOver"; // 遷移先シーン名

    Rigidbody rb;
    bool _ending; // 二重遷移防止

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
        if (!player || _ending) return;

        // プレイヤーへの方向
        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f) dir.Normalize();

        // プレイヤーの方向を向く
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        // 移動
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

    // 捕まえた時の共通処理
    void HandleCaught()
    {
        if (_ending) return; // 二重実行防止
        _ending = true;

        // ★ ここで直前のシーン名を保存してからゲームオーバーへ
        SceneTracker.SaveCurrentScene();

        SceneManager.LoadScene(gameOverScene);
    }
}
