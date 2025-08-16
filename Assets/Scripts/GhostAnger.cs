using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class GhostAnger : MonoBehaviour
{
    [Header("怒りパラメータ")]
    public float maxAnger = 100f;
    public float addPerHit = 20f;          // ライト命中ごとの怒り増加
    public float decayPerSec = 8f;         // 通常時の減衰
    public float enragedDecayPerSec = 5f;  // 激怒中の減衰
    [Tooltip("これ以上(=1.0)で激怒に遷移")]
    public float thresholdUp01 = 1.0f;
    [Tooltip("これ未満(=0.6)で鎮静に遷移")]
    public float thresholdDown01 = 0.6f;

    [Header("激怒中の挙動ブースト（任意）")]
    public bool boostMoveSpeed = true;
    [Tooltip("速度をこの倍率で上げる（対象：NavMeshAgent.speed か、反射で見つかった float 速度フィールド/プロパティ）")]
    public float enragedSpeedMultiplier = 1.8f;

    [Tooltip("反射で探す候補名（float）")]
    public string[] speedMemberNames = new[] { "speed", "moveSpeed", "runSpeed", "walkSpeed", "chaseSpeed" };

    public bool IsEnraged { get; private set; }
    public float Anger01 => Mathf.Clamp01(_anger / Mathf.Max(1e-4f, maxAnger));

    float _anger = 0f;

    // 速度制御ターゲット
    NavMeshAgent _agent;
    float _agentSpeedBackup;
    bool _hasAgentBackup;

    Component _speedOwner;             // フィールド/プロパティを保持するインスタンス
    FieldInfo _speedField;
    PropertyInfo _speedProp;
    float _speedBackup;
    bool _hasSpeedBackup;

    void Awake()
    {
        // まず NavMeshAgent
        _agent = GetComponentInParent<NavMeshAgent>() ?? GetComponent<NavMeshAgent>();
        if (_agent)
        {
            _agentSpeedBackup = _agent.speed;
            _hasAgentBackup = true;
        }

        // 次に反射で float 速度を探す（自分と親から）
        var owner = (Component)(GetComponent<MonoBehaviour>()); // 自分
        _speedOwner = owner;

        // 検索対象：自分→親方向で最初に見つかったもの
        Component[] candidates =
        {
            this,                                   // GhostAnger 自身
            GetComponent<MonoBehaviour>(),          // 同じGOの任意のMB
            GetComponentInParent<MonoBehaviour>()   // 親側の任意のMB
        };

        foreach (var comp in candidates)
        {
            if (comp == null) continue;
            var t = comp.GetType();
            // public / non-public 両方探す
            const BindingFlags BF = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // フィールド優先
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
            // 無ければプロパティ
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

    /// FrontLightToggle などから呼ぶ：ライト命中時に怒りを加算
    public void OnLightHit(float scale = 1f)
    {
        _anger = Mathf.Min(maxAnger, _anger + addPerHit * Mathf.Max(0f, scale));
        if (!IsEnraged && Anger01 >= thresholdUp01) EnterEnrage();
    }

    void EnterEnrage()
    {
        IsEnraged = true;
        if (!boostMoveSpeed) return;

        // NavMeshAgent を優先
        if (_agent && _hasAgentBackup)
        {
            _agent.speed = _agentSpeedBackup * Mathf.Max(1f, enragedSpeedMultiplier);
        }
        // 反射で見つけた速度スロット
        else if (_hasSpeedBackup)
        {
            float v = _speedBackup * Mathf.Max(1f, enragedSpeedMultiplier);
            if (_speedField != null) _speedField.SetValue(_speedOwner, v);
            else if (_speedProp != null) _speedProp.SetValue(_speedOwner, v);
        }
        // 見つからなければ何もしない（怒りロジックのみ有効）
    }

    void ExitEnrage()
    {
        IsEnraged = false;

        // 速度を元に戻す
        if (_agent && _hasAgentBackup) _agent.speed = _agentSpeedBackup;
        if (_hasSpeedBackup)
        {
            if (_speedField != null) _speedField.SetValue(_speedOwner, _speedBackup);
            else if (_speedProp != null) _speedProp.SetValue(_speedOwner, _speedBackup);
        }
    }
}
