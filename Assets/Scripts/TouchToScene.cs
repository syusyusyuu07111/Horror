using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class TouchToScene : MonoBehaviour
{
    [Header("�J�ڐ�V�[�����iBuild Settings �ɓo�^�K�{�j")]
    public string nextSceneName = "Ingame";

    [Header("�I�v�V����")]
    public string playerTag = "Player"; // �G�ꂽ����̃^�O
    public float delay = 0.1f;          // �J�ڂ܂ł̑ҋ@�i���o�p�j

    bool fired;

    void Reset()
    {
        // �G�ꂽ�甽�����₷���悤�Ƀg���K�[�ɂ��Ă����i���ʂ̓����蔻��ł������܂��j
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
