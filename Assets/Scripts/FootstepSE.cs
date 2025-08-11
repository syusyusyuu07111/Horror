using UnityEngine;

/// <summary>
/// 水平移動距離ベースで足音を鳴らす。Rigidbody前提。
/// ・移動している間だけ鳴る
/// ・接地していない間は鳴らない
/// ・速度に応じて間隔が自動で変わる（距離トリガー）
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FootstepSE : MonoBehaviour
{
    [Header("参照")]
    public Rigidbody rb;                    // プレイヤーのRigidbody（未設定なら自動取得）

    [Header("再生設定")]
    public AudioClip[] clips;               // 足音クリップ（複数あると自然）
    [Tooltip("1歩の間隔（m）。小さいほど頻度が上がる。歩き=0.7〜0.9、走り=0.5〜0.7 が目安")]
    public float stepDistance = 0.8f;
    [Tooltip("ごく小さな揺れを無視するための速度しきい値")]
    public float speedThreshold = 0.1f;

    [Header("音質のゆらぎ")]
    [Range(0f, 1f)] public float volume = 0.9f;
    [Tooltip("再生毎のピッチばらつき幅（±）")]
    public float pitchJitter = 0.05f;

    [Header("接地判定")]
    public LayerMask groundLayers = ~0;     // 既定は全部
    public float groundCheckRadius = 0.15f; // 足元の球判定
    public float groundCheckOffsetY = 0.1f; // コライダー中心からの下オフセット
    [Tooltip("接地外になってから最低この時間は足音を鳴らさない（ジャンプ直後の誤爆防止）")]
    public float minAirTime = 0.05f;

    private AudioSource source;
    private float movedSinceLastStep;       // 前回からの水平移動距離
    private float lastUngroundedTime;       // 最後に接地を失った時刻
    private Vector3 lastPosXZ;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;

        if (!rb) rb = GetComponentInParent<Rigidbody>() ?? GetComponent<Rigidbody>();
        lastPosXZ = GetXZ(transform.position);
    }

    void Update()
    {
        if (!rb || clips == null || clips.Length == 0) return;

        // 接地判定
        bool grounded = IsGrounded();

        // 水平速度
#if UNITY_6000_0_OR_NEWER
        Vector3 vel = rb.linearVelocity;
#else
        Vector3 vel = rb.velocity;
#endif
        Vector2 velXZ = new Vector2(vel.x, vel.z);
        float speed = velXZ.magnitude;

        // 移動していない/未接地なら距離を貯めない
        if (!grounded || speed < speedThreshold)
        {
            if (!grounded) lastUngroundedTime = Time.time;
            lastPosXZ = GetXZ(transform.position);
            return;
        }

        // 一定時間以上の未接地直後は抑制
        if (Time.time - lastUngroundedTime < minAirTime)
        {
            lastPosXZ = GetXZ(transform.position);
            return;
        }

        // 水平移動距離を加算してしきい値に達したら再生
        Vector3 nowXZ = GetXZ(transform.position);
        movedSinceLastStep += Vector3.Distance(nowXZ, lastPosXZ);
        lastPosXZ = nowXZ;

        if (movedSinceLastStep >= Mathf.Max(0.05f, stepDistance))
        {
            PlayOneFootstep();
            movedSinceLastStep = 0f;
        }
    }

    private void PlayOneFootstep()
    {
        var clip = clips[Random.Range(0, clips.Length)];
        float pitch = 1f + Random.Range(-pitchJitter, pitchJitter);

        // 一瞬だけピッチ変更して再生
        float prevPitch = source.pitch;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume);
        source.pitch = prevPitch;
    }

    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.down * groundCheckOffsetY;
        return Physics.CheckSphere(origin, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private static Vector3 GetXZ(Vector3 v) => new Vector3(v.x, 0f, v.z);

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 origin = transform.position + Vector3.down * groundCheckOffsetY;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
#endif
}
