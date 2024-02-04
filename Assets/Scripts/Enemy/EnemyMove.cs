using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyMove : MonoBehaviour
{


    [Header("Movement Attributes")]
    public List<PathNode> pathNodes; //This may be empty if our enemy is not going to be patroling.
    
    private PathNode tempTarget=null;//This is our current target that we will work towards getting to.
    
    public MoveState moveState;
    public bool ShouldFly;//True if this enemy should not experience gravity.
    SpriteRenderer spriteRenderer;
    Rigidbody2D rig;
    GameObject player;

    private Coroutine last_idlewait;
    [Min(0)]
    public float Speed=1;
    [Header("Data Attributes")]
    [Min(0)]
    public float minArriveDist = 0.5f;//How close the enemy has to get to be said to "arrive" somewhere
    [Min(0)]
    public float minWaitWander=1;

    [Min(0)]
    public float maxWaitWander=2;

    [Min(0)]
    public float minWaitPatrol=1;

    [Min(0)]
    public float maxWaitPatrol=2;

    [System.Serializable]
    public class PathNode 
    {
        //This class if for our patrols, where we will want to have a set of locations that are our patrol route.
        public PathNode()
        {

        }
        public PathNode(Vector2 pos)
        {
            Position = pos;
        }
        public PathNode(GameObject targ)
        {
            SnapToTarget = targ;
            Position= targ.transform.position;  
        }

        public Vector2 Position;

        [Header("Override Position To Target:")]
        public GameObject SnapToTarget;
        public Vector2 getPos()
        {
            if(SnapToTarget!=null)
            {
                return SnapToTarget.transform.position;
            }
            else 
            { 
                return Position; 
            }
        }
    }

    public enum MoveState
    {
        Idle,
        Wander,
        Patrol,
        Chase
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (pathNodes==null)
        {
            pathNodes=new List<PathNode>();
        }

        if (pathNodes.Count > 0)
        {
            Vector2 lastPos = pathNodes[pathNodes.Count - 1].getPos();
            foreach (PathNode node in pathNodes)
            {
                if (node.SnapToTarget != null)
                {
                    node.Position = node.SnapToTarget.transform.position;
                }
                Gizmos.DrawLine(lastPos, node.getPos());
                lastPos = node.Position;
            }
        }
        Gizmos.color = Color.green;
        if(tempTarget!=null)
        {
            Gizmos.DrawLine(transform.position, tempTarget.getPos());
        }
    }

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rig = GetComponent<Rigidbody2D>();
        tempTarget = null;
        //Replace with a better way to locate the player
        player = GameObject.FindGameObjectWithTag("Player");
        //
        if (pathNodes == null)
        {
            pathNodes = new List<PathNode>();
        }
    }

    void Update()
    {
        Animate();


        switch (moveState)
        {
            case MoveState.Idle:
                Idle();
                break;
            case MoveState.Wander:
                Wander();
                break;
            case MoveState.Patrol:
                Patrol();
                break;
            case MoveState.Chase:
                Chase();
                break;
        }
    }
    private IEnumerator WaitIdle(float duration,MoveState nextState) //Waits idle for duration seconds and then sets the movestate to nextState
    {
        yield return new WaitForSeconds(duration);
        moveState = nextState;
        last_idlewait = null;
    }
    public bool isCurrentlyWaitingIdle() //Returns true if this enemy is currenlty waiting in WaitIdle()
    {
        return last_idlewait != null;
    }
    public void WaitIdleForABit(float duration,MoveState nextState) //Starts a WaitIdle Coroutine, overrides preexisting WaitIdles
    {
        if(isCurrentlyWaitingIdle())
        {
            StopCoroutine(last_idlewait);
        }
        last_idlewait = StartCoroutine(WaitIdle(duration, nextState));
    }
    public void AbortIdleWait()//Aborts the current WaitIdle Coroutine if applicable.
    {
        if (isCurrentlyWaitingIdle())
        {
            StopCoroutine(last_idlewait);
            last_idlewait = null;
        }
    }

    public void Animate() //Updates the Animation for the Enemey based on movement.
    {
        //Replace this with enemy animations \
        spriteRenderer.flipX = rig.velocity.x > 0;
        //Replace this with enemy animations /
    }
    public bool CheckShouldChase() //Snaps the Enemy into a chase state if it should chase, returns true/false depending on whether or not chasing should be occuring now.
    {
        //To Do: Implement check if I can see the player.
        if(!CanReachPosition(new PathNode(player)))
        {
            return false;
        }
        //I did not find the player, and I am chasing, cancel time!
        if (moveState==MoveState.Chase)
        {
            moveState = MoveState.Idle;
            WaitIdleForABit(1, MoveState.Idle);
        }
        //I found the player, chase time!
        if (moveState != MoveState.Chase)
        {
            AbortIdleWait();
            moveState = MoveState.Chase;
        }
        return true;
    }
    public void Idle()
    {
        if (CheckShouldChase())
            return;

        if(!isCurrentlyWaitingIdle())
        {
            //The wait timer has expired, so we should exit Idle and enter Wander/Patrol now.
            if(pathNodes.Count== 0)
            {
                moveState = MoveState.Wander;
            }
            else
            {
                moveState = MoveState.Patrol;
            }
        }
    }
    public bool CanReachPosition(PathNode pos)
    {
        //ToDo: Implement Me (Returns true if a clear raycast line exists between current positon and pos)
        return false;
    }
    public PathNode genNextWanderPos()
    {
        //ToDo: Implement Me (So that its not just up right by 5 units but is random and respects flight limits
       PathNode pos= new PathNode(new Vector2(transform.position.x + 5, transform.position.y + 5));

     /*  while (!CanReachPosition(pos))
       {

       }
     */
       return pos;
    }
    public void Wander()
    {
        if (CheckShouldChase())
            return;


        if(tempTarget==null||Vector2.Distance(transform.position,tempTarget.getPos())<=minArriveDist)
        {
            //Find me a new target location to wander towards!
            tempTarget = genNextWanderPos();




            WaitIdleForABit(Random.Range(minWaitWander, maxWaitPatrol), MoveState.Wander);
        }
        else
        {//tempTarget != null and distance > 1f

            //Move me towards the target location!
            Vector2 dir = (tempTarget.getPos()-new Vector2(transform.position.x,transform.position.y)).normalized;

            rig.AddForce(dir*Speed,ForceMode2D.Force);

        }
    }

    public void Patrol()
    {
        if (CheckShouldChase())
            return;

        //We can assume pathNodes is not empty if we are here. (Error elsewise)

        if (tempTarget == null || Vector2.Distance(transform.position, tempTarget.getPos()) <= minArriveDist)
        {
            //Take the next pathNode and set it as my target, then shift it to the bottom of the list.
            tempTarget = pathNodes[0];
            pathNodes.RemoveAt(0);
            pathNodes.Add(tempTarget);
            WaitIdleForABit(Random.Range(minWaitWander, maxWaitPatrol), MoveState.Patrol);
        }
        else
        {//tempTarget != null and distance > 1f

            //Move me towards the target location!
            Vector2 dir = (tempTarget.getPos() - new Vector2(transform.position.x, transform.position.y)).normalized;

            rig.AddForce(dir * Speed, ForceMode2D.Force);

        }
    }

    public void Chase()
    {
        //ToDo: Implement Me (So that I beeline towards the player, up to a min distance)

        Debug.LogWarning("Chases have not been implemented yet!");
        if (!CheckShouldChase())
            return;
        
    }

}
