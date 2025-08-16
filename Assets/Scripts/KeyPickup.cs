using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyPickup : MonoBehaviour
{
    [Header("拾得条件")]
    public string playerTag = "Player";  // プレイヤーのタグ

    [Header("シーン遷移")]
    [Tooltip("鍵を拾ったらこのシーンへ遷移します（Build Settings に登録必須）")]
    public string nextSceneName = "NextScene";
    [Tooltip("SFXやフェードのために遷移を少し遅らせたい場合の秒数")]
    public float loadDelay = 0.0f;
    [Tooltip("非同期ロード(ロード中にフリーズしにくい)")]
    public bool useAsyncLoad = true;
    [Tooltip("遷移直前に現在のシーン名を保存したい場合のみON（SceneTracker.SaveCurrentScene() を呼ぶ）")]
    public bool saveCurrentSceneBeforeLoad = false;

    [Header("演出（任意）")]
    public AudioClip pickupSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Tooltip("拾った瞬間に見た目を消すか（メッシュ/コライダー無効化）")]
    public bool hideOnPickup = true;
    [Tooltip("拾ったあとゲームオブジェクトを破棄するか（falseならシーン遷移まで非表示のまま残す）")]
    public bool destroyAfterPickup = false;

    // 内部
    Collider _col;
    Renderer _rend;
    bool _loading;

    // エディタでアタッチした瞬間や Reset で呼ばれる
    void Reset()
    {
        EnsureTriggerCollider();
    }

    void Awake()
    {
        EnsureTriggerCollider();
        _rend = GetComponentInChildren<Renderer>();
    }

    void EnsureTriggerCollider()
    {
        if (!TryGetComponent(out _col) || _col == null)
        {
            // 具体的なコライダーが無ければ BoxCollider を付与して概ねフィット
            var box = gameObject.AddComponent<BoxCollider>();
            _col = box;

            var r = GetComponentInChildren<Renderer>();
            if (r)
            {
                Bounds b = r.bounds;
                box.center = transform.InverseTransformPoint(b.center);
                // 回転している場合は完全一致にはなりませんが実用上は十分です
                Vector3 sizeLocal = transform.InverseTransformVector(b.size);
                box.size = new Vector3(Mathf.Abs(sizeLocal.x), Mathf.Abs(sizeLocal.y), Mathf.Abs(sizeLocal.z));
            }
        }
        _col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_loading) return;
        if (!other.CompareTag(playerTag)) return;
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[KeyPickup] nextSceneName が未設定です（Build Settings にも登録してください）");
            return;
        }

        _loading = true;

        // 見た目/当たりを消す（任意）
        if (hideOnPickup)
        {
            if (_rend) _rend.enabled = false;
            if (_col) _col.enabled = false;
        }

        // SFX 再生
        if (pickupSfx)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);

        // 遷移
        StartCoroutine(LoadNextSceneRoutine());
    }

    IEnumerator LoadNextSceneRoutine()
    {
        if (saveCurrentSceneBeforeLoad)
        {
            // 使う場合はプロジェクト側に SceneTracker.SaveCurrentScene() を用意しておいてください
            // （例：直前のシーン名を PlayerPrefs に保存するなど）
            var trackerType = System.Type.GetType("SceneTracker");
            if (trackerType != null)
            {
                var mi = trackerType.GetMethod("SaveCurrentScene", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (mi != null) mi.Invoke(null, null);
            }
        }

        if (loadDelay > 0f) yield return new WaitForSeconds(loadDelay);

        if (useAsyncLoad)
        {
            var op = SceneManager.LoadSceneAsync(nextSceneName);
            // フリーズを避けたい場合は allowSceneActivation を制御してフェード完了を待つ等の拡張も可
            while (!op.isDone) yield return null;
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }

        if (destroyAfterPickup) Destroy(gameObject);
    }
}
