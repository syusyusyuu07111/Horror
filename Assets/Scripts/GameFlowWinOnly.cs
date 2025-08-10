using UnityEngine;
using TMPro;

public class GameFlowWinOnly : MonoBehaviour
{
    [Header("参照（任意）")]
    public GhostSpawnManager spawner; // ← 使いません（参照だけ残してOK）
    public Transform player;          // ← 使いません（将来拡張用）

    [Header("ルール")]
    [Tooltip("逃げ切るべき秒数")]
    public float surviveSeconds = 30f;

    [Header("UI")]
    [Tooltip("残り時間表示（00:SS形式）")]
    public TMP_Text timerText;
    [Tooltip("勝利テキスト（任意）")]
    public TMP_Text messageText;

    [Header("シーン遷移")]
    [Tooltip("空なら BuildSettings 上の次のシーンへ")]
    public string nextSceneName = "";
    [Tooltip("勝利表示から遷移までの待ち秒数")]
    public float winSceneLoadDelay = 1.0f;

    float _timeLeft;
    bool _finished;

    void Start()
    {
        _timeLeft = surviveSeconds;
        _finished = false;
        if (messageText) messageText.text = "";
        UpdateTimerUI();
    }

    void Update()
    {
        if (_finished) return;

        _timeLeft -= Time.deltaTime;
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            Win();
        }

        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        if (!timerText) return;
        int sec = Mathf.CeilToInt(_timeLeft);
        timerText.text = $"00:{Mathf.Clamp(sec, 0, 99):00}";
    }

    void Win()
    {
        _finished = true;

        if (messageText) messageText.text = "YOU WIN!";

        // 少し待ってからシーン遷移
        Invoke(nameof(LoadNextScene), winSceneLoadDelay);
    }

    void LoadNextScene()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            var cur = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex;
            UnityEngine.SceneManagement.SceneManager.LoadScene(cur + 1);
        }
    }
}
