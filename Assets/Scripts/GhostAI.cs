using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class GhostAI : MonoBehaviour
{
    public Transform player;
    public float sightRange = 10f;      // �T�m�͈�
    public float attackRange = 1.2f;    // �Q�[���I�[�o�[���苗��
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float wanderRadius = 5f;     // �p�j�͈�
    public float wanderDelay = 2f;      // �����_���ړ��̊Ԋu

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
            // �v���C���[������������p�g���[���ɖ߂�
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

        // �U���͈͂ɓ�������Q�[���I�[�o�[
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
