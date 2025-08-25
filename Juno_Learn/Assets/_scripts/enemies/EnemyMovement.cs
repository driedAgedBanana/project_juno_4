using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Dead
    }

    [SerializeField] private EnemyState _currentState;


    public NavMeshAgent agent;

    public Transform centerPoint;

    [SerializeField] private float _walkTimeCount;

    [SerializeField] private bool _isAllowedToWalk;

    public float targetSpeed;

    public float patrolSpeed = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        _isAllowedToWalk = true;

        _currentState = EnemyState.Patrol;
    }

    // Update is called once per frame
    void Update()
    {
        agent.speed = targetSpeed;
        switch (_currentState)
        {
            case EnemyState.Idle:
                Debug.Log("Do I even need this?");
                break;

            case EnemyState.Patrol:
                Patrolling();
                break;

            case EnemyState.Chase:
                Debug.Log("Chase player");
                break;

            case EnemyState.Attack:
                Debug.Log("Attack Player");
                break;
        }
    }

    private void Patrolling()
    {
        if (_isAllowedToWalk)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                Vector3 point;
                targetSpeed = patrolSpeed;
                float range = Random.Range(3f, 15f);
                if (RandomPoint(centerPoint.position, range, out point))
                {
                    Debug.Log("Enemy walking range will be " + range);
                    Debug.DrawRay(point, Vector3.up, Color.blue, 1.0f);
                    agent.SetDestination(point);
                    _walkTimeCount++;

                    if (_walkTimeCount >= 3)
                    {
                        _isAllowedToWalk = false;
                        StartCoroutine(WaitThenPatrol());
                    }
                }
            }
        }

        else
        {
            return;
        }
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

    private IEnumerator WaitThenPatrol()
    {
        float waitTime = Random.Range(3f, 15f);

        Debug.Log("Enemy will be waiting for " + waitTime + " seconds!");
        yield return new WaitForSeconds(waitTime);

        _walkTimeCount = 0f;
        if (_walkTimeCount <= 0)
        {
            _isAllowedToWalk = true;
            Patrolling();
        }

    }
}
