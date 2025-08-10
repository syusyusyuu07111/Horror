using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class TouchToScene : MonoBehaviour
{
    [Header("遷移先シーン名（Build Settings に登録必須）")]
    public string nextSceneName = "Ingame";

    [Header("オプション")]
    public string playerTag = "Player"; // 触れた相手のタグ
    public float delay = 0.1f;          // 遷移までの待機（演出用）

    bool fired;

    void Reset()
    {
        // 触れたら反応しやすいようにトリガーにしておく（普通の当たり判定でも動きます）
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (other.CompareTag(playerTag))
            StartCoroutine(LoadAfter(delay));
    }

    void OnCollisionEnter(Collision collision)
    {
        if (fired) return;
        if (collision.collider.CompareTag(playerTag))
            StartCoroutine(LoadAfter(delay));
    }

    System.Collections.IEnumerator LoadAfter(float t)
    {
        fired = true;
        if (t > 0f) yield return new WaitForSeconds(t);
        SceneManager.LoadScene(nextSceneName);
    }
}
