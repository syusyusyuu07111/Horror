using UnityEngine;
using TMPro;

public class GameFlowWinOnly : MonoBehaviour
{
    [Header("�Q�Ɓi�C�Ӂj")]
    public GhostSpawnManager spawner; // �� �g���܂���i�Q�Ƃ����c����OK�j
    public Transform player;          // �� �g���܂���i�����g���p�j

    [Header("���[��")]
    [Tooltip("�����؂�ׂ��b��")]
    public float surviveSeconds = 30f;

    [Header("UI")]
    [Tooltip("�c�莞�ԕ\���i00:SS�`���j")]
    public TMP_Text timerText;
    [Tooltip("�����e�L�X�g�i�C�Ӂj")]
    public TMP_Text messageText;

    [Header("�V�[���J��")]
    [Tooltip("��Ȃ� BuildSettings ��̎��̃V�[����")]
    public string nextSceneName = "";
    [Tooltip("�����\������J�ڂ܂ł̑҂��b��")]
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

        // �����҂��Ă���V�[���J��
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
