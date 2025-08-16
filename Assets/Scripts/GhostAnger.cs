using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class GhostAnger : MonoBehaviour
{
    [Header("�{��p�����[�^")]
    public float maxAnger = 100f;
    public float addPerHit = 20f;          // ���C�g�������Ƃ̓{�葝��
    public float decayPerSec = 8f;         // �ʏ펞�̌���
    public float enragedDecayPerSec = 5f;  // ���{���̌���
    [Tooltip("����ȏ�(=1.0)�Ō��{�ɑJ��")]
    public float thresholdUp01 = 1.0f;
    [Tooltip("���ꖢ��(=0.6)�Œ��ÂɑJ��")]
    public float thresholdDown01 = 0.6f;

    [Header("���{���̋����u�[�X�g�i�C�Ӂj")]
    public bool boostMoveSpeed = true;
    [Tooltip("���x�����̔{���ŏグ��i�ΏہFNavMeshAgent.speed ���A���˂Ō������� float ���x�t�B�[���h/�v���p�e�B�j")]
    public float enragedSpeedMultiplier = 1.8f;

    [Tooltip("���˂ŒT����▼�ifloat�j")]
    public string[] speedMemberNames = new[] { "speed", "moveSpeed", "runSpeed", "walkSpeed", "chaseSpeed" };

    public bool IsEnraged { get; private set; }
    public float Anger01 => Mathf.Clamp01(_anger / Mathf.Max(1e-4f, maxAnger));

    float _anger = 0f;

    // ���x����^�[�Q�b�g
    NavMeshAgent _agent;
    float _agentSpeedBackup;
    bool _hasAgentBackup;

    Component _speedOwner;             // �t�B�[���h/�v���p�e�B��ێ�����C���X�^���X
    FieldInfo _speedField;
    PropertyInfo _speedProp;
    float _speedBackup;
    bool _hasSpeedBackup;

    void Awake()
    {
        // �܂� NavMeshAgent
        _agent = GetComponentInParent<NavMeshAgent>() ?? GetComponent<NavMeshAgent>();
        if (_agent)
        {
            _agentSpeedBackup = _agent.speed;
            _hasAgentBackup = true;
        }

        // ���ɔ��˂� float ���x��T���i�����Ɛe����j
        var owner = (Component)(GetComponent<MonoBehaviour>()); // ����
        _speedOwner = owner;

        // �����ΏہF�������e�����ōŏ��Ɍ�����������
        Component[] candidates =
        {
            this,                                   // GhostAnger ���g
            GetComponent<MonoBehaviour>(),          // ����GO�̔C�ӂ�MB
            GetComponentInParent<MonoBehaviour>()   // �e���̔C�ӂ�MB
        };

        foreach (var comp in candidates)
        {
            if (comp == null) continue;
            var t = comp.GetType();
            // public / non-public �����T��
            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // �t�B�[���h�D��
            foreach (var name in speedMemberNames)
            {
                var fi = t.GetField(name, BF);
                if (fi != null && fi.FieldType == typeof(float))
                {
                    _speedOwner = comp;
                    _speedField = fi;
                    _speedBackup = (float)_speedField.GetValue(_speedOwner);
                    _hasSpeedBackup = true;
                    return;
                }
            }
            // ������΃v���p�e�B
            foreach (var name in speedMemberNames)
            {
                var pi = t.GetProperty(name, BF);
                if (pi != null && pi.CanRead && pi.CanWrite && pi.PropertyType == typeof(float))
                {
                    _speedOwner = comp;
                    _speedProp = pi;
                    _speedBackup = (float)_speedProp.GetValue(_speedOwner);
                    _hasSpeedBackup = true;
                    return;
                }
            }
        }
    }

    void Update()
    {
        float d = (IsEnraged ? enragedDecayPerSec : decayPerSec) * Time.deltaTime;
        _anger = Mathf.Max(0f, _anger - d);

        if (!IsEnraged && Anger01 >= thresholdUp01)
            EnterEnrage();
        else if (IsEnraged && Anger01 <= thresholdDown01)
            ExitEnrage();
    }

    /// FrontLightToggle �Ȃǂ���ĂԁF���C�g�������ɓ{������Z
    public void OnLightHit(float scale = 1f)
    {
        _anger = Mathf.Min(maxAnger, _anger + addPerHit * Mathf.Max(0f, scale));
        if (!IsEnraged && Anger01 >= thresholdUp01) EnterEnrage();
    }

    void EnterEnrage()
    {
        IsEnraged = true;
        if (!boostMoveSpeed) return;

        // NavMeshAgent ��D��
        if (_agent && _hasAgentBackup)
        {
            _agent.speed = _agentSpeedBackup * Mathf.Max(1f, enragedSpeedMultiplier);
        }
        // ���˂Ō��������x�X���b�g
        else if (_hasSpeedBackup)
        {
            float v = _speedBackup * Mathf.Max(1f, enragedSpeedMultiplier);
            if (_speedField != null) _speedField.SetValue(_speedOwner, v);
            else if (_speedProp != null) _speedProp.SetValue(_speedOwner, v);
        }
        // ������Ȃ���Ή������Ȃ��i�{�胍�W�b�N�̂ݗL���j
    }

    void ExitEnrage()
    {
        IsEnraged = false;

        // ���x�����ɖ߂�
        if (_agent && _hasAgentBackup) _agent.speed = _agentSpeedBackup;
        if (_hasSpeedBackup)
        {
            if (_speedField != null) _speedField.SetValue(_speedOwner, _speedBackup);
            else if (_speedProp != null) _speedProp.SetValue(_speedOwner, _speedBackup);
        }
    }
}
