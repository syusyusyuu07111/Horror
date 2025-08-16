using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KeyPickup : MonoBehaviour
{
    [Header("�E������")]
    public string playerTag = "Player";  // �v���C���[�̃^�O

    [Header("�V�[���J��")]
    [Tooltip("�����E�����炱�̃V�[���֑J�ڂ��܂��iBuild Settings �ɓo�^�K�{�j")]
    public string nextSceneName = "NextScene";
    [Tooltip("SFX��t�F�[�h�̂��߂ɑJ�ڂ������x�点�����ꍇ�̕b��")]
    public float loadDelay = 0.0f;
    [Tooltip("�񓯊����[�h(���[�h���Ƀt���[�Y���ɂ���)")]
    public bool useAsyncLoad = true;
    [Tooltip("�J�ڒ��O�Ɍ��݂̃V�[������ۑ��������ꍇ�̂�ON�iSceneTracker.SaveCurrentScene() ���Ăԁj")]
    public bool saveCurrentSceneBeforeLoad = false;

    [Header("���o�i�C�Ӂj")]
    public AudioClip pickupSfx;
    [Range(0f, 1f)] public float sfxVolume = 0.9f;
    [Tooltip("�E�����u�ԂɌ����ڂ��������i���b�V��/�R���C�_�[�������j")]
    public bool hideOnPickup = true;
    [Tooltip("�E�������ƃQ�[���I�u�W�F�N�g��j�����邩�ifalse�Ȃ�V�[���J�ڂ܂Ŕ�\���̂܂܎c���j")]
    public bool destroyAfterPickup = false;

    // ����
    Collider _col;
    Renderer _rend;
    bool _loading;

    // �G�f�B�^�ŃA�^�b�`�����u�Ԃ� Reset �ŌĂ΂��
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
            // ��̓I�ȃR���C�_�[��������� BoxCollider ��t�^���ĊT�˃t�B�b�g
            var box = gameObject.AddComponent<BoxCollider>();
            _col = box;

            var r = GetComponentInChildren<Renderer>();
            if (r)
            {
                Bounds b = r.bounds;
                box.center = transform.InverseTransformPoint(b.center);
                // ��]���Ă���ꍇ�͊��S��v�ɂ͂Ȃ�܂��񂪎��p��͏\���ł�
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
            Debug.LogError("[KeyPickup] nextSceneName �����ݒ�ł��iBuild Settings �ɂ��o�^���Ă��������j");
            return;
        }

        _loading = true;

        // ������/������������i�C�Ӂj
        if (hideOnPickup)
        {
            if (_rend) _rend.enabled = false;
            if (_col) _col.enabled = false;
        }

        // SFX �Đ�
        if (pickupSfx)
            AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);

        // �J��
        StartCoroutine(LoadNextSceneRoutine());
    }

    IEnumerator LoadNextSceneRoutine()
    {
        if (saveCurrentSceneBeforeLoad)
        {
            // �g���ꍇ�̓v���W�F�N�g���� SceneTracker.SaveCurrentScene() ��p�ӂ��Ă����Ă�������
            // �i��F���O�̃V�[������ PlayerPrefs �ɕۑ�����Ȃǁj
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
            // �t���[�Y����������ꍇ�� allowSceneActivation �𐧌䂵�ăt�F�[�h������҂��̊g������
            while (!op.isDone) yield return null;
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }

        if (destroyAfterPickup) Destroy(gameObject);
    }
}
