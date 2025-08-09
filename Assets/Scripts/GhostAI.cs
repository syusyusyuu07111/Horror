using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GhostAI : MonoBehaviour
{
    public Transform player;
    public float sightRange = 10f;      // 探知範囲
    public float attackRange = 1.2f;    // ゲームオーバー判定距離
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float wanderRadius = 5f;     // 徘徊範囲
    public float wanderDelay = 2f;      // ランダム移動の間隔

    private NavMeshAgent agent;
    private Vector3 spawnPos;
    private float wanderTimer;
    private enum State { Patrol, Chase }
    private State currentState = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        spawnPos = transform.position;
        wanderTimer = wanderDelay;
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= sightRange)
        {
            currentState = State.Chase;
        }
        else if (currentState == State.Chase && distanceToPlayer > sightRange * 1.5f)
        {
            // プレイヤーを見失ったらパトロールに戻る
            currentState = State.Patrol;
        }

        if (currentState == State.Patrol)
        {
            PatrolUpdate();
        }
        else if (currentState == State.Chase)
        {
            ChaseUpdate();
        }

        // 攻撃範囲に入ったらゲームオーバー
        if (distanceToPlayer <= attackRange)
        {
            SceneManager.LoadScene("GameOver");
        }
    }

    void PatrolUpdate()
    {
        wanderTimer += Time.deltaTime;
        if (wanderTimer >= wanderDelay)
        {
            Vector3 newPos = RandomNavPosAround(spawnPos, wanderRadius);
            GoTo(newPos, patrolSpeed);
            wanderTimer = 0f;
        }
    }

    void ChaseUpdate()
    {
        GoTo(player.position, chaseSpeed);
    }

    void GoTo(Vector3 pos, float speed)
    {
        if (agent.destination != pos)
        {
            agent.speed = speed;
            agent.SetDestination(pos);
        }
    }

    Vector3 RandomNavPosAround(Vector3 center, float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += center;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return center;
    }
}
