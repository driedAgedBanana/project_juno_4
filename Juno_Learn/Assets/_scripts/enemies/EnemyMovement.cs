using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [SerializeField] private EnemyState _currentState;

    [Header("Basic setup")]
    public NavMeshAgent agent;
    public float targetSpeed;

    [Header("Line of sight")]
    public GameObject player;
    public float visionDegree;

    [Header("Patrolling")]
    public float stopSafeDistance;
    public int maxWalkCount;
    private int _randomWalkCount;
    public Transform centerPoint;
    private float _walkTimeCount;
    private bool _isAllowedToWalk;

    [Header("Chasing")]
    public Animator enemyAnimator;
    public float chaseRange;
    public float loseSightRange;
    private bool _isScreaming = false;
    public float screamDuration = 2f;

    [Header("Attacking")]
    public float bufferDistance;
    public float attackRange;
    public BoxCollider attackCollider;
    private bool _isPlayerInAttackZone = false;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyAnimator = GetComponent<Animator>();

        // Disable agent auto-movement, use root motion instead
        agent.updatePosition = false;
        agent.updateRotation = false;

        agent.stoppingDistance = stopSafeDistance;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _isAllowedToWalk = true;
        _currentState = EnemyState.Patrol;

        Debug.Log("Enemy will walk for " + maxWalkCount + " times!");
    }

    // Update is called once per frame
    void Update()
    {
        agent.speed = targetSpeed;


        switch (_currentState)
        {
            case EnemyState.Patrol:
                Patrolling();
                CheckForPlayer();
                break;

            case EnemyState.Chase:
                ChasingPlayer();
                CheckForPlayer();
                break;

            case EnemyState.Attack:
                AttackPlayer();
                break;
        }
    }

    #region Checking for player
    private void CheckForPlayer()
    {
        if (_isPlayerInAttackZone)
        {
            Debug.Log("Player is attacking!");
            _currentState = EnemyState.Attack;
            return;
        }

        Vector3 direction = player.transform.position - transform.position;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        float stopDistance = Vector3.Distance(transform.position, player.transform.position);

        if (Mathf.Abs(Vector3.Angle(transform.forward, direction)) < visionDegree)
        {
            if (distance <= chaseRange)
            {
                enemyAnimator.SetBool("isChasingPlayer", true);
                _currentState = EnemyState.Chase;
            }
            else if (distance > loseSightRange)
            {
                enemyAnimator.SetBool("isChasingPlayer", false);
                _currentState = EnemyState.Patrol;
            }
        }
    }
    #endregion

    #region Patrolling

    private void Patrolling()
    {
        if (!_isAllowedToWalk) return;

        if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
        {
            // Pick a new random destination
            Vector3 point;
            float range = Random.Range(6f, 20f);
            if (RandomPoint(centerPoint.position, range, out point))
            {
                agent.SetDestination(point);
                Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
                _walkTimeCount++;

                if (_walkTimeCount >= _randomWalkCount)
                {
                    _isAllowedToWalk = false;
                    StartCoroutine(WaitBeforePatrol());
                }
            }
        }

        // Calculate velocity and set animator
        Vector3 velocity = agent.desiredVelocity;
        float speed = velocity.magnitude;
        enemyAnimator.SetFloat("Speed", speed);

        // Smooth rotation toward agent desired velocity
        if (velocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }
    }

    private void OnAnimatorMove()
    {
        // Apply root motion movement
        Vector3 rootPos = enemyAnimator.rootPosition;
        rootPos.y = agent.nextPosition.y; // keep synced with navmesh height
        transform.position = rootPos;

        transform.rotation = enemyAnimator.rootRotation;

        // Tell agent where we ended up
        agent.nextPosition = transform.position;
    }

    private bool RandomPoint(Vector3 center, float range, out Vector3 result)
    {
        Vector3 randomPoint = center + Random.insideUnitSphere * range; // Random point in a shpere
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = Vector3.zero;
        return false;
    }

    private IEnumerator WaitBeforePatrol()
    {
        float waitTime = Random.Range(3f, 10f);

        Debug.Log("Enemy will be waiting for " + waitTime + " seconds!");
        yield return new WaitForSeconds(waitTime);

        _randomWalkCount = Random.Range(3, 10);
        maxWalkCount = _randomWalkCount;

        Debug.Log("Enemy will walk for " + _randomWalkCount + " times!");

        _walkTimeCount = 0f;
        if (_walkTimeCount <= 0)
        {
            _isAllowedToWalk = true;
            Patrolling();
        }

    }

    #endregion

    #region Chasing
    private void ChasingPlayer()
    {
        // Only start scream if we're not already screaming
        if (!_isScreaming)
        {
            StartCoroutine(ScreamThenRunTowardsPlayer());
            return; // skip chasing this frame
        }
        else
        {
            // Chasing logic only runs when _isScreaming is false
            agent.SetDestination(player.transform.position);

            Vector3 velocity = agent.desiredVelocity;
            float speed = velocity.magnitude;
            enemyAnimator.SetFloat("chaseSpeed", speed);

            if (velocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
        }

    }

    private IEnumerator ScreamThenRunTowardsPlayer()
    {
        _isScreaming = true;

        // Trigger scream animation and stop movement
        enemyAnimator.SetTrigger("Screaming");

        // Stop NavMesh movement while screaming
        agent.isStopped = true;
        agent.ResetPath();

        // Wait for the duration of the scream
        yield return new WaitForSeconds(screamDuration);

        // Resume chasing
        _isScreaming = false;
    }
    #endregion


    #region Attacking
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInAttackZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            _isPlayerInAttackZone = false;

            // if player is still within the chase range, go back to chasing
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= chaseRange)
            {
                _currentState = EnemyState.Chase;
            }
            else
            {
                _currentState = EnemyState.Patrol;
            }
        }
    }


    private void AttackPlayer()
    {
        Debug.Log("Attack Player");
        agent.ResetPath();
    }

    #endregion
}
