using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyAI : MonoBehaviour
{
    public GameObject enemyObject;
    public BoxCollider2D playerBox;
    public Rigidbody2D enemyBody;
    public BoxCollider2D enemyBox;//用于玩家判定的碰撞箱
    public List<Collider2D> attackBox;
    public CircleCollider2D findBox;//怪物可以发现玩家的范围
    public playerControl.findEnemy enemy;
    public Vector3[] bodyEdgePos=new Vector3[2];
    public float xBodySize;
    public float yBodySize;

    public int level;
    public bool isAwake;//该怪物是否初始化完成

    public Animator ani;
    public int toward;

    bool isPlayerClose;

    public List<Collider2D> playerList = new List<Collider2D>();
    public ContactFilter2D findCondition;
    public LayerMask playerLayer;

    public EnemyData data;

    public void stopAni()
    {
        Debug.Log("动作还原");
        ani.SetInteger("Skill", 0);
        Invoke("startGetAction", 0.1f);
    }

    private void Awake()
    {
        ani = GetComponent<Animator>();
        data=GetComponent<EnemyData>();
        attackBox = new List<Collider2D>();
        enemyBox = GameObject.Find(name + "/Body").GetComponent<BoxCollider2D>();
        enemyBody = GetComponent<Rigidbody2D>();
        findBox=GetComponent<CircleCollider2D>();
        toward = 0;
        
        spawn();
    }
    private void getBodySize()
    {
        bodyEdgePos[0]=new Vector3(transform.position.x+ xBodySize, transform.position.y + yBodySize, 0);//右上角
        bodyEdgePos[1]=new Vector3(transform.position.x- xBodySize, transform.position.y - yBodySize, 0);//左下角
    }
    // Start is called before the first frame update
    public void Start()
    {
        turnFirst = true;
        //speed = 3f;
        //xBodySize = GetComponent<SpriteRenderer>().bounds.size.x;
        //yBodySize = GetComponent<SpriteRenderer>().bounds.size.y;
    }
    public void activeSelf()
    {
        Debug.Log("激活" + name);
        AIawake(actions);
    }

    // Update is called once per frame
    void Update()
    {
        if (toward == left)
        {
            transform.localEulerAngles = new Vector3(0, -180, 0);
        }
        else
        {
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }
    }
    //寻路和发现玩家AI
    /// <summary>
    /// 当玩家进入玩家警觉范围时，在脚本内保存该玩家，同时该怪物会发出一条射线指向玩家
    /// 发现玩家的条件：玩家进入警觉范围后，向玩家发射一条射线，持续检测到该射线不被场景碰撞体阻挡发现玩家
    /// 寻路：射线始终指向玩家，期间如果射线被场景碰撞体或其他怪物阻挡，记录该碰撞体，怪物移动方向随机加减45度，立刻再次检测是否依然被该碰撞体阻挡
    ///       如果依然被阻挡，则再次执行转向，否则依照当前方向移动，移动2秒后再次检测，当射线不被阻挡时恢复指向玩家。
    /// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// 在发现玩家后进行首次AI行为判定，在攻击行为中，如果玩家不处于该行为攻击范围内，则先执行接近协程后再次执行攻击
    /// Boss行为判定：每一次行为结束后进行下一次行为的随机判定，对于判定得到的行为，当锁定的玩家不处于互动范围内时，先执行接近协程后执行该行为协程
    /// 判定方法：通过各技能的数值进行判定，同时在执行完行为之后对各技能数值进行改变，每个技能判定时从技能数值~10进行改变
    /// 希望能够实现的效果：假设Boss拥有普攻1（仅一次攻击），普攻2（仅一次攻击),普攻3（仅一次攻击），技能A，技能B，Boss在释放普攻1后，调整普攻2的数值为7，释放普攻2后调整普攻3为7,保证Boss存在连招
    ///                     但第一次普攻完下一次衔接的可能是技能A，保证战斗的随机性，如果该次普攻1释放完后普攻2没有命中玩家，则减低普攻2的技能数值，给Boss根据战斗修改自己行为的能力。
    /// 除发现玩家外，怪物只能向四个方向移动,当前调整为低等AI，发现玩家直线接近，丢失视野不接近。
    /// 检测近距离：Body下被该怪物所有执行框覆盖的碰撞框，保证至少有一个需要判定技能可以命中
    /// </summary>
    #region
    public RaycastHit2D sightLineHitInfo;
    public Coroutine AItest;
    public Ray2D sightLine;
    public GameObject player;

    public ContactFilter2D sightLineCondition;
    public BoxCollider2D closeDis;

    public static Vector3 findPlayerPos;
    //AI行为使用协程
    #region
    public Coroutine findPlayerCoro;
    public Coroutine closePlayerCoro;
    public Coroutine moveToPosCoro;
    public List<EnemyAction> actions;
    public float sightLineLength;
    #endregion

    //切换协程时暂停协程,关闭除自己以外的协程
    public void stopCoro(Coroutine coro)
    {
        if (coro == closePlayerCoro)
        {
            if (findPlayerCoro != null)
            {
                StopCoroutine(findPlayerCoro);
                findPlayerCoro = null;
            }
            if (moveToPosCoro != null)
            {
                StopCoroutine(moveToPosCoro);
                moveToPosCoro = null;
            }
        }
        else if (coro==findPlayerCoro)
        {
            if (closePlayerCoro != null)
            {
                StopCoroutine(closePlayerCoro);
                closePlayerCoro = null;
            }
            if (moveToPosCoro != null)
            {
                StopCoroutine(moveToPosCoro);
                moveToPosCoro = null;
            }
        }
        else if (coro==moveToPosCoro)
        {
            if (closePlayerCoro != null)
            {
                StopCoroutine(closePlayerCoro);
                closePlayerCoro = null;
            }
            if(findPlayerCoro != null)
            {
                StopCoroutine(findPlayerCoro);
                findPlayerCoro = null;
            }
        }
    }
    public void AIawake(List<EnemyAction> actions1)
    {
        xBodySize = enemyBox.size.x / 2;
        yBodySize = enemyBox.size.y / 2;

        playerLayer = LayerMask.GetMask("Player");
        findCondition = new ContactFilter2D();
        findCondition.ClearDepth();
        findCondition.ClearNormalAngle();
        findCondition.SetLayerMask(playerLayer);
        closeDis=GameObject.Find(name+"/Body/closeDis").GetComponent<BoxCollider2D>();

        sightLineCondition = new ContactFilter2D();
        sightLineCondition.ClearNormalAngle();
        sightLineCondition.ClearDepth();
        sightLineCondition.SetLayerMask(LayerMask.GetMask("SceneData"));
        actions = actions1;
        findPlayerCoro = StartCoroutine(FindPlayer());
        sightLineLength=GetComponent<CircleCollider2D>().radius;

        Physics2D.queriesStartInColliders = false;
        Physics2D.queriesHitTriggers = false;

        //spawn();
    }
    public IEnumerator FindPlayer()
    {
        findBox.OverlapCollider(findCondition, playerList);
        while (playerList.Count == 0)
        {
            findBox.OverlapCollider(findCondition, playerList);
            Debug.Log("未检测到玩家");
            yield return new WaitForSeconds(1);
        }
        Debug.Log(name + "发现玩家" + playerList[0].gameObject.name);

        player = playerList[0].gameObject;
        playerBox = player.GetComponent<BoxCollider2D>();
        
        StartCoroutine(LeavePlayer());

        //持续发出射线检测玩家直到
        //bug:怪物可以360度发现玩家
        while (isBlockWithPlayer())
        {
            yield return new WaitForSeconds(1);
        }
        Debug.Log("玩家未处于掩体，FindPlayer协程结束");
        findPlayerPos = player.transform.position;
        startGetAction();
        yield break;
    }

    //该协程使用情况，玩家不属于传入碰撞体范围或被掩体阻挡时启动
    //该怪物移动时持续判断是否被墙体阻挡
    //先判断怪物和玩家是否有掩体，如果没有，则怪物直接指向玩家移动，如果有，则怪物向可以移动的方向移动，前进到一开始发现玩家的位置后再次检测，如果还是检测不到玩家，则丢失玩家(从斜向移动修改为直角式移动)
    public IEnumerator ClosePlayer(EnemyAction action)
    {
        while (closeDis.IsTouching(playerBox)==false)
        {
            if (isBlockWithPlayer())
            {
                findPlayerCoro=StartCoroutine(FindPlayer());
                stopCoro(findPlayerCoro);
                yield break;
            }
            else
            {
                findPlayerPos=player.transform.position;
                Vector2 dir =(player.transform.position-transform.position).normalized;
                enemyTowardAngle=getAngleWithPlayer();
                changeEnemyToward();
                enemyBody.velocity = dir * data.enemyValue[3];
                yield return 0;
            }
        }
        enemyBody.velocity = Vector2.zero;
        action.method(0);
        yield break;
    }
    public void losePlayer()
    {
        Debug.Log("丢失玩家");
        if (findPlayerCoro != null) { StopCoroutine(findPlayerCoro); findPlayerCoro = null; }
        findPlayerCoro = StartCoroutine(FindPlayer());
        stopCoro(findPlayerCoro);
    }
    //玩家是否离开
    public IEnumerator LeavePlayer()
    {
        while (playerList.Count > 0)
        {
            findBox.OverlapCollider(findCondition, playerList);
            yield return new WaitForSeconds(5);
        }
        Debug.Log("玩家离开范围");
        if (findPlayerCoro != null) StopCoroutine(findPlayerCoro);
        if(closePlayerCoro!= null) {StopCoroutine(closePlayerCoro);closePlayerCoro=null;}
        findPlayerCoro = StartCoroutine(FindPlayer());
        yield break;
    }
    #endregion

    /// <summary>
    /// 闲置，巡逻AI
    /// </summary>
    

    //方向函数，技能判定
    //获取和玩家之间的角度
    //根据和玩家之间的角度修改朝向
    //技能判定：返回判定行为下标
    //AI行为树实现：设计一个类结点，保存抽象方法，每个节点交给生物自身的脚本实现,实现的方法和技能数值保存在结构体Action中,由生物自身脚本实现全部方法类后保存在结构体中的method中
    //子类实现行为方法后，传回该脚本中
    #region
    int down = 0;
    int right = 1;
    int up = 2;
    int left = 3;
    public float enemyTowardAngle;

    public EnemyAction exeAction;

    public class EnemyAction
    {
        public int baseJudgeValue;
        public int judgeValue;
        public enemyAction method;
        public Collider2D actionBox;
        public Collider2D attackBox;
    }
    public delegate int enemyAction(int index);

    //怪物和玩家之间是否被墙体阻挡，如果被阻挡
    public bool isBlockWithPlayer()
    {
        float xSize = player.transform.position.x - transform.position.x;
        float ySize = player.transform.position.y - transform.position.y;
        double distance = ((xSize*xSize)+(ySize*ySize));
        distance=Math.Sqrt(distance);
        getBodySize();
        RaycastHit2D sightLineHitInfoU = Physics2D.Raycast(bodyEdgePos[0], new Vector2(xSize,ySize), (float)distance, LayerMask.GetMask("SceneData"));
        RaycastHit2D sightLineHitInfoD = Physics2D.Raycast(bodyEdgePos[1], new Vector2(xSize, ySize), (float)distance, LayerMask.GetMask("SceneData"));
        if (sightLineHitInfoU.collider == null&&sightLineHitInfoD.collider==null)
        {
            return false;
        }
        Debug.Log(name+"和玩家之间发现掩体"+ sightLineHitInfo.collider);
        return true;
    }
    //怪物前进的方向是否被墙体阻挡,只能朝四个方向移动
    public bool isBlock(float length,int towardX,int towardY,bool canMove)
    {
        getBodySize();
        RaycastHit2D rayHitInfoU = Physics2D.Raycast(bodyEdgePos[0],new Vector2(towardX,towardY), length,LayerMask.GetMask("SceneData"));
        RaycastHit2D rayHitInfoD = Physics2D.Raycast(bodyEdgePos[1], new Vector2(towardX, towardY), length, LayerMask.GetMask("SceneData"));
        if (rayHitInfoU.collider == null&&rayHitInfoD.collider==null)
        {
            return false;
        }
        else if(canMove)
        {
            while(rayHitInfoU.collider != null && rayHitInfoD.collider != null)
            {
                enemyBody.velocity = data.enemyValue[3] * new Vector2(towardX, towardY);
            }
        }
        Debug.Log(name + "前进方向发现掩体" + sightLineHitInfo.collider);
        return true;
    }
    //怪物前进到指定地点执行行动
    public IEnumerator MoveToPos(Vector2 Pos,EnemyAction action)
    {
        Debug.Log(Pos);
        int towardX = 0;
        int towardY = 0;
        float xLength = Mathf.Abs(findPlayerPos.x - transform.position.x);
        float yLength = Mathf.Abs(findPlayerPos.y - transform.position.y);
        if (findPlayerPos.x > transform.position.x) towardX = 1;
        else if (findPlayerPos.x < transform.position.x) towardX = -1;
        else towardX = 0;
        if (findPlayerPos.y > transform.position.y) towardY = 1;
        else if (findPlayerPos.y < transform.position.y) towardY = -1;
        else towardY = 0;
        Debug.Log(towardX + "," + towardY);
        if (towardX != 0 && isBlock(xLength, towardX,0,true) == false)
        {
            while ((int)findPlayerPos.x != (int)transform.position.x)
            {
                enemyBody.velocity = new Vector2(towardX * data.enemyValue[3], 0);
                yield return 0;
            }
            if (towardY != 0 && isBlock(yLength, 0, towardY, true) == false)
            {
                while ((int)findPlayerPos.y != (int)transform.position.y)
                {
                    enemyBody.velocity = new Vector2(0, towardY * data.enemyValue[3]);
                    yield return 0;
                }
            }
        }
        else if (towardY != 0 && isBlock(yLength, 0, towardY, true) == false)
        {
            while ((int)findPlayerPos.y != (int)transform.position.y)
            {
                enemyBody.velocity = new Vector2(0, towardY * data.enemyValue[3]);
                yield return 0;
            }
            if (towardX != 0 && isBlock(xLength, towardX, 0, true) == false)
            {
                while ((int)findPlayerPos.x != (int)transform.position.x)
                {
                    enemyBody.velocity = new Vector2(towardX * data.enemyValue[3], 0);
                    yield return 0;
                }
            }
        }
        if (isBlockWithPlayer())
        {
            losePlayer();
            yield break;
        }
        else
        {
            Debug.Log("移动到玩家消失地点后发现玩家");
            if(action.actionBox.IsTouching(playerBox) == false)
            {
                action.method(0);
            }
            else
            {
                findPlayerCoro = StartCoroutine(FindPlayer());
                stopCoro(findPlayerCoro);
            }
            yield break;
        }
    }
    public List<GameObject> getPlayerList(Collider2D box,int num)
    {
        List<Collider2D> result=new List<Collider2D>();
        box.OverlapCollider(findCondition, result);
        List<GameObject> list=new List<GameObject>();
        for(int i = 0; i < num&&i<result.Count; i++)
        {
            list.Add(result[i].gameObject);
        }
        return list;
    }
    public bool changeAction(Collider2D actionBox)
    {
        if (closePlayerCoro != null) { StopCoroutine(closePlayerCoro); closePlayerCoro = null; }//如果已经存在接近协程，先暂停
        if (actionBox.IsTouching(playerBox) == false)
        {
            Debug.Log(actionBox.name + "无法命中，切换行为");
            startGetAction();
            Debug.Log(getAction());
            return true;
        }
        return false;
    }
    public Coroutine getActionCoro;
    public void startGetAction()
    {
        if (getActionCoro != null)
        {
            StopCoroutine(getActionCoro);
        }
        getActionCoro = StartCoroutine(getAction());
        
        enemyTowardAngle = getAngleWithPlayer();
        changeEnemyToward();
    }
    public IEnumerator getAction()
    {
        int maxValue = 0;
        int maxIndex = 0;
        Debug.Log("获取技能");
        //int maxJudgeValue = 10;
        for (int i = 0; i < actions.Count; i++)
        {
            actions[i].judgeValue = UnityEngine.Random.Range(0, actions[i].baseJudgeValue);
            Debug.Log(actions[i].judgeValue);
            if (actions[i].judgeValue == 10)
            {
                exeAction = actions[i];
                maxIndex = i;
                break;
            }
            else if (actions[i].judgeValue > maxValue)
            {
                maxValue = actions[i].judgeValue;
                maxIndex = i;
            }
        }
        exeAction = actions[maxIndex];
        Debug.Log("当前执行行为" + (maxIndex + 1)+"/"+exeAction);
        if (actions[maxIndex].actionBox != null && closeDis.IsTouching(playerBox) == false)
        {
            Debug.Log("玩家不属于近距离");
            if (closePlayerCoro != null)
            {
                StopCoroutine(closePlayerCoro);
                closePlayerCoro = null;
            }
            closePlayerCoro = StartCoroutine(ClosePlayer(actions[maxIndex]));
        }
        else 
        {
            actions[maxIndex].method(0);
        }
        getActionCoro = null;
        yield break;
    }
    public float getAngleWithPlayer()
    {
        return  -Mathf.Atan2(transform.position.x - player.transform.position.x, transform.position.y - player.transform.position.y) * Mathf.Rad2Deg;
    }
    public void changeEnemyToward()
    {
        if ((enemyTowardAngle > 45 && enemyTowardAngle < 135)&&toward!=right)//右朝向
        {
            Debug.Log("生物" + name + "朝向右");
            towardTurn(right);
        }
        else if ((enemyTowardAngle >= 135 || enemyTowardAngle <= -135) && toward != up)//上朝向
        {
            Debug.Log("生物" + name + "朝向上");
            towardTurn(up);
        }
        else if ((enemyTowardAngle > -135 && enemyTowardAngle < -45) && toward!=left)//左朝向
        {
            Debug.Log("生物" + name + "朝向左");
            towardTurn(left);
        }
        else if ((enemyTowardAngle <= 45 && enemyTowardAngle >= -45)&& toward!=down)//下朝向
        {
            Debug.Log("生物" + name + "朝向下");
            towardTurn(down);
        }
    }
    void towardTurn(int Toward)
    {
        Debug.Log("动画方向改变");
        if (Toward == right)
        {
            ani.SetBool("IsChangeToward", true);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            data.healthBarBG.transform.rotation = Quaternion.Euler(0, 0, 0);
            ani.SetInteger("Toward", 1);
            toward = right;
        }
        else if (Toward == left)
        {
            ani.SetBool("IsChangeToward", true);
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            data.healthBarBG.transform.rotation = Quaternion.Euler(0, 0, 0);
            ani.SetInteger("Toward", 1);
            toward = left;
        }
        else if (Toward == up)
        {
            ani.SetBool("IsChangeToward", true);
            ani.SetInteger("Toward", 2);
            toward = up;
        }
        else if (Toward == down)
        {
            ani.SetBool("IsChangeToward", true);
            ani.SetInteger("Toward", 0);
            toward = down;
        }
    }


    public bool turnFirst;
    public void TurnStop()
    {
        ani.SetBool("IsChangeToward", false);
    }
    #endregion
    public void spawn()
    {
        enemy = new playerControl.findEnemy();
        enemy.level = level;
        enemy.enemy = this.gameObject;
        enemy.box = enemyBox;
    }
}
