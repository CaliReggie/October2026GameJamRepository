using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Cat : MonoBehaviour
{
    public enum CatState { Patrol, Chasing, Attacking, Waiting}

    [Header("Stats")]
    [SerializeField] float patrolSpeed;
    [SerializeField] float chaseSpeed;
    [SerializeField] float lineOfSightDist;
    [SerializeField] float spherecastRadius;

    [SerializeField] float attackDist = 1;

    [SerializeField] float searchDuration;
    [SerializeField] float searchTimer;

    [SerializeField] LayerMask spherecastLayers;
    [SerializeField] LayerMask targetLayers;

    [Header("References")]
    [SerializeField] NavMeshAgent agent;
    [SerializeField] Vector3 target;

    [SerializeField] GameObject targetedPlayer;

    [SerializeField] Animator animator;

    [SerializeField] bool canSeePlayer = false;

    [Header("Patrol Points")]
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] int currentPointIndex;
    [SerializeField] float timeBeforeNextPoint;

    [SerializeField] CatState state;

    Coroutine waitAtPointRoutine;
    Coroutine searchRoutine;

    private void Update()
    {
        StateMachine();
        LineOfSight();

        if (searchTimer > 0)
        {
            searchTimer -= Time.deltaTime;
        }
    }

    public void ChangeStates(CatState newState)
    {
        state = newState;

        switch (newState)
        {
            case CatState.Attacking: agent.isStopped = true;
                Vector3 lookPos = targetedPlayer.transform.position;
                lookPos.y = transform.position.y;
                lookPos.z = transform.position.z;
                transform.LookAt(lookPos);
                animator.SetTrigger("Swipe");
                StartCoroutine(AttackRoutine());
                break;

            case CatState.Patrol: agent.isStopped = false;
                animator.SetFloat("Vert", 1);
                animator.SetFloat("State", 0);
                break;

            case CatState.Chasing: agent.isStopped = false;
                animator.SetFloat("Vert", 1);
                animator.SetFloat("State", 1);
                break;

            case CatState.Waiting: agent.isStopped = true;
                animator.SetFloat("Vert", 0);
                animator.SetFloat("State", 0);
                break;
        }
    }

    void StateMachine()
    {
        switch (state)
        {
            case CatState.Patrol: Patrol();
                break;

            case CatState.Chasing: Chase();
                break;
        }
    }

    void Chase()
    {
        if (targetedPlayer != null && canSeePlayer)
        {
            targetedPlayer.TryGetComponent<PlayerObjectPioComponent>(out var player);
            if (player != null && player.IsIncapacitated) ChangeStates(CatState.Patrol);
            agent.speed = chaseSpeed;
            agent.SetDestination(targetedPlayer.transform.position);
        }

        if (!canSeePlayer)
        {
            if (searchRoutine == null) searchRoutine = StartCoroutine(SearchRoutine());
        }

        if (Vector3.Distance(transform.position, targetedPlayer.transform.position) < attackDist)
        {
            ChangeStates(CatState.Attacking);
        }
    }

    IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(1);
        if (canSeePlayer) ChangeStates(CatState.Chasing);
        else ChangeStates(CatState.Patrol);
    }

    void Patrol()
    {
        agent.speed = patrolSpeed;

        if (Vector3.Distance(transform.position, patrolPoints[currentPointIndex].transform.position) < 3)
        {
            Debug.Log("Arrived At Point");
            ChangeStates(CatState.Waiting);
            if (waitAtPointRoutine == null) waitAtPointRoutine = StartCoroutine(WaitAtPatrolPoint());
        }
        else
        {
            agent.SetDestination(patrolPoints[currentPointIndex].transform.position);

        }
    }

    void NewPatrolPoint()
    {
        int newIndex = Random.Range(0, patrolPoints.Length);
        Mathf.RoundToInt(newIndex);
        currentPointIndex = newIndex;
    }

    void LineOfSight()
    {
        if (Physics.SphereCast(transform.position, spherecastRadius, transform.forward, out RaycastHit hit, lineOfSightDist, spherecastLayers))
        {
            Ray ray = new Ray(transform.position, hit.transform.position - transform.position);
            Debug.DrawLine(transform.position, hit.transform.position);
            if (Physics.Raycast(ray, out RaycastHit hit2, lineOfSightDist, targetLayers))
            {
                if (hit2.collider.gameObject.CompareTag("Player"))
                {
                    canSeePlayer = true;
                    targetedPlayer = hit2.collider.gameObject;
                    ChangeStates(CatState.Chasing);
                }
                else
                {
                    canSeePlayer = false;
                }
            }
        }
    }

    IEnumerator WaitAtPatrolPoint()
    {
        yield return new WaitForSeconds(timeBeforeNextPoint);
        NewPatrolPoint();
        ChangeStates(CatState.Patrol);
        waitAtPointRoutine = null;
    }

    IEnumerator SearchRoutine()
    {
        yield return new WaitForSeconds(searchDuration);
        if (!canSeePlayer)
        {
            ChangeStates(CatState.Patrol);
        }

        searchRoutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((transform.position + transform.forward * lineOfSightDist), spherecastRadius);
    }
}
