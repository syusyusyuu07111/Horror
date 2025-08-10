using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PaintingInteraction : MonoBehaviour
{
    [Header("����/UI")]
    [Tooltip("��]�Ɏg���L�[�BInspector�ŕύX��")]
    public KeyCode interactKey = KeyCode.Return;
    [Tooltip("�ڋߎ��ɏo���v�����v�g�i�C�Ӂj")]
    public TMP_Text promptUI;
    [Tooltip("��������Ɏg���v���C���[Transform")]
    public Transform player;
    [Tooltip("���̋����ȓ��ő���\")]
    public float interactRadius = 1.5f;
    [Tooltip("��������ō��������𖳎�����")]
    public bool lockYPlane = true;

    [Header("��]�ݒ�")]
    [Tooltip("1�񉟂���Z�����x�񂷂�")]
    public float rotateStepDegZ = 90f;
    [Tooltip("�����p�x�iZ�j")]
    public float targetZ = 180f;
    [Tooltip("���𔻒�̋��e�덷�i�x�j")]
    public float tolerance = 2f;

    [Header("�p�Y���i����ID�̊G��𑩂˂�j")]
    [Tooltip("����O���[�vID�ŃJ�E���g�����L")]
    public string groupId = "PuzzleA";
    [Tooltip("���̖������������N���A")]
    public int requiredSolved = 2;
    [Tooltip("��������h�APrefab�i�C�Ӂj")]
    public GameObject doorPrefab;
    [Tooltip("�h�A�̐����ʒu�E�����i�C�Ӂj")]
    public Transform doorSpawnPoint;
    [Tooltip("�O���[�v���Ƃ�1�񂾂���������")]
    public bool spawnOncePerGroup = true;

    [Header("�N��/�f�o�b�O")]
    [Tooltip("Play�������ƂɐÓI�J�E���g���N���A�iDomain Reload�����΍�j")]
    public bool resetStaticsOnAwake = true;
    [Tooltip("�񂷂��тɊp�x�┻������O")]
    public bool logOnAngleChange = true;
    [Tooltip("�����B���␶���ۂ����O")]
    public bool logWhenBothSolved = true;

    // ���L�Ǘ��i�O���[�v���Ƃ̒B�����E�����ς݁j
    static readonly Dictionary<string, int> s_solvedCount = new Dictionary<string, int>();
    static readonly HashSet<string> s_spawnedGroup = new HashSet<string>();

    // �N��������̏�ԊǗ�
    bool _isNear;           // �v���C���[�ڋ�
    bool _isSolvedBoot;     // �N�����_�̐�����ԁi�\���̂݁A���_�͂��Ȃ��j
    bool _isSolvedNow;      // ���߂̔���i��]��ɍX�V�j
    bool _hasEverCounted;   // ���̌̂���x�ł����_���ɑJ�ڂ������iOnDisable�ł̌��Z�Ɏg�p�j

    static bool s_initialized; // Domain Reload�������̑��d�������h�~

    void Awake()
    {
        if (!s_initialized || resetStaticsOnAwake)
        {
            s_solvedCount.Clear();
            s_spawnedGroup.Clear();
            s_initialized = true;
            if (logOnAngleChange) Debug.Log("[PUZZLE] �ÓI�J�E���g�����Z�b�g���܂���");
        }
    }

    void Start()
    {
        if (promptUI) promptUI.gameObject.SetActive(false);
        if (!s_solvedCount.ContainsKey(groupId)) s_solvedCount[groupId] = 0;

        // �N�����̊p�x���m�F���邪�A�J�E���g�͂��Ȃ��i������ő������Ƃ��������_�j
        float z = transform.localEulerAngles.z;
        _isSolvedBoot = IsZAtTarget(z, targetZ, tolerance);
        _isSolvedNow = _isSolvedBoot;

        if (logOnAngleChange)
        {
            if (_isSolvedBoot)
                Debug.Log($"[{name}] �N�����ɐ����p�x�i�m�[�J�E���g�j�BZ={z:0.0} �ڕW={targetZ}�}{tolerance}");
            else
                Debug.Log($"[{name}] �N�����͖��B�BZ={z:0.0} �ڕW={targetZ}�}{tolerance}");
        }
    }

    void OnDisable()
    {
        // ���̌̂��g���_�ς݁h�Ȃ�A�j�����ɔO�̂��ߌ��Z�i�V�[���J�ڂ�Destroy�΍�j
        if (_hasEverCounted && _isSolvedNow && s_solvedCount.ContainsKey(groupId))
        {
            s_solvedCount[groupId] = Mathf.Max(0, s_solvedCount[groupId] - 1);
            if (logOnAngleChange)
                Debug.Log($"[{name}] OnDisable�Ō��Z�Bgroup='{groupId}' -> {s_solvedCount[groupId]}/{requiredSolved}");
        }
    }

    void Update()
    {
        // �ڋߕ\���i��������j
        if (player)
        {
            Vector3 a = transform.position;
            Vector3 p = player.position;
            if (lockYPlane) p.y = a.y;
            bool nowNear = Vector3.Distance(a, p) <= interactRadius;
            if (nowNear != _isNear && promptUI) promptUI.gameObject.SetActive(nowNear);
            _isNear = nowNear;
        }

        // ��]���� �� ��ԍX�V
        if (_isNear && Input.GetKeyDown(interactKey))
        {
            transform.Rotate(0f, 0f, rotateStepDegZ, Space.Self);
            UpdateSolvedState(afterRotate: true);
        }
    }

    void UpdateSolvedState(bool afterRotate)
    {
        float z = transform.localEulerAngles.z;
        bool nowSolved = IsZAtTarget(z, targetZ, tolerance);

        if (logOnAngleChange && afterRotate)
            Debug.Log($"[{name}] ��]��Z={z:0.0} ����?={nowSolved} (�ڕW={targetZ}�}{tolerance})");

        // �g�N�����͐������������m�[�J�E���g�h �� �g���B�ɖ߂��h �� �g�ēx�����ցh
        // �̂悤�ȉ����ɂ��Ή��F�J�E���g�́u���B�������v�ɑJ�ڂ���������+1�A
        // �u���������B�v�ɑJ�ڂ���������-1�B
        if (nowSolved == _isSolvedNow) return; // �ω��Ȃ�

        // �O���[�v�J�E���g�̉����Z
        if (!s_solvedCount.ContainsKey(groupId)) s_solvedCount[groupId] = 0;

        if (nowSolved && !_isSolvedNow)
        {
            s_solvedCount[groupId] += 1;
            _hasEverCounted = true; // ��x�ł�+1������
        }
        else if (!nowSolved && _isSolvedNow)
        {
            s_solvedCount[groupId] = Mathf.Max(0, s_solvedCount[groupId] - 1);
        }

        _isSolvedNow = nowSolved;

        if (logOnAngleChange)
            Debug.Log($"[{name}] ��ԕύX �� group='{groupId}' solved={s_solvedCount[groupId]}/{requiredSolved}");

        TrySpawnDoorIfReady();
    }

    void TrySpawnDoorIfReady()
    {
        if (!s_solvedCount.TryGetValue(groupId, out int cnt)) cnt = 0;

        if (cnt >= requiredSolved)
        {
            if (logWhenBothSolved)
                Debug.Log($"[PUZZLE] group='{groupId}' �����B���I�i{cnt}/{requiredSolved}�j");

            // �h�A������
            if (!doorPrefab || !doorSpawnPoint)
            {
                if (logWhenBothSolved)
                    Debug.LogWarning($"[PUZZLE] �h�A�������FdoorPrefab/doorSpawnPoint ���ݒ�igroup='{groupId}'�j");
                return;
            }

            if (spawnOncePerGroup && s_spawnedGroup.Contains(groupId))
            {
                if (logWhenBothSolved)
                    Debug.Log($"[PUZZLE] ���ɐ����ς݂̂��߃X�L�b�v�igroup='{groupId}'�j");
                return;
            }

            Instantiate(doorPrefab, doorSpawnPoint.position, doorSpawnPoint.rotation);
            if (spawnOncePerGroup) s_spawnedGroup.Add(groupId);

            if (logWhenBothSolved)
                Debug.Log($"[PUZZLE] �h�A�����F{doorPrefab.name} @ {doorSpawnPoint.position}�igroup='{groupId}'�j");
        }
    }

    static bool IsZAtTarget(float currentZ, float target, float tol)
    {
        float diff = Mathf.DeltaAngle(currentZ, target);
        return Mathf.Abs(diff) <= tol;
    }

    void OnDrawGizmosSelected()
    {
        if (!player) return;
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Vector3 c = transform.position;
        if (lockYPlane) c.y = transform.position.y;
        Gizmos.DrawWireSphere(c, interactRadius);
    }
}
