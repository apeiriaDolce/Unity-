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
    public BoxCollider2D enemyBox;//��������ж�����ײ��
    public List<Collider2D> attackBox;
    public CircleCollider2D findBox;//������Է�����ҵķ�Χ
    public playerControl.findEnemy enemy;
    public Vector3[] bodyEdgePos=new Vector3[2];
    public float xBodySize;
    public float yBodySize;

    public int level;
    public bool isAwake;//�ù����Ƿ��ʼ�����

    public Animator ani;
    public int toward;

    bool isPlayerClose;

    public List<Collider2D> playerList = new List<Collider2D>();
    public ContactFilter2D findCondition;
    public LayerMask playerLayer;

    public EnemyData data;

    public void stopAni()
    {
        Debug.Log("������ԭ");
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
        bodyEdgePos[0]=new Vector3(transform.position.x+ xBodySize, transform.position.y + yBodySize, 0);//���Ͻ�
        bodyEdgePos[1]=new Vector3(transform.position.x- xBodySize, transform.position.y - yBodySize, 0);//���½�
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
        Debug.Log("����" + name);
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
    //Ѱ·�ͷ������AI
    /// <summary>
    /// ����ҽ�����Ҿ�����Χʱ���ڽű��ڱ������ң�ͬʱ�ù���ᷢ��һ������ָ�����
    /// ������ҵ���������ҽ��뾯����Χ������ҷ���һ�����ߣ�������⵽�����߲���������ײ���赲�������
    /// Ѱ·������ʼ��ָ����ң��ڼ�������߱�������ײ������������赲����¼����ײ�壬�����ƶ���������Ӽ�45�ȣ������ٴμ���Ƿ���Ȼ������ײ���赲
    ///       �����Ȼ���赲�����ٴ�ִ��ת�򣬷������յ�ǰ�����ƶ����ƶ�2����ٴμ�⣬�����߲����赲ʱ�ָ�ָ����ҡ�
    /// -------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    /// �ڷ�����Һ�����״�AI��Ϊ�ж����ڹ�����Ϊ�У������Ҳ����ڸ���Ϊ������Χ�ڣ�����ִ�нӽ�Э�̺��ٴ�ִ�й���
    /// Boss��Ϊ�ж���ÿһ����Ϊ�����������һ����Ϊ������ж��������ж��õ�����Ϊ������������Ҳ����ڻ�����Χ��ʱ����ִ�нӽ�Э�̺�ִ�и���ΪЭ��
    /// �ж�������ͨ�������ܵ���ֵ�����ж���ͬʱ��ִ������Ϊ֮��Ը�������ֵ���иı䣬ÿ�������ж�ʱ�Ӽ�����ֵ~10���иı�
    /// ϣ���ܹ�ʵ�ֵ�Ч��������Bossӵ���չ�1����һ�ι��������չ�2����һ�ι���),�չ�3����һ�ι�����������A������B��Boss���ͷ��չ�1�󣬵����չ�2����ֵΪ7���ͷ��չ�2������չ�3Ϊ7,��֤Boss��������
    ///                     ����һ���չ�����һ���νӵĿ����Ǽ���A����֤ս��������ԣ�����ô��չ�1�ͷ�����չ�2û��������ң�������չ�2�ļ�����ֵ����Boss����ս���޸��Լ���Ϊ��������
    /// ����������⣬����ֻ�����ĸ������ƶ�,��ǰ����Ϊ�͵�AI���������ֱ�߽ӽ�����ʧ��Ұ���ӽ���
    /// �������룺Body�±��ù�������ִ�п򸲸ǵ���ײ�򣬱�֤������һ����Ҫ�ж����ܿ�������
    /// </summary>
    #region
    public RaycastHit2D sightLineHitInfo;
    public Coroutine AItest;
    public Ray2D sightLine;
    public GameObject player;

    public ContactFilter2D sightLineCondition;
    public BoxCollider2D closeDis;

    public static Vector3 findPlayerPos;
    //AI��Ϊʹ��Э��
    #region
    public Coroutine findPlayerCoro;
    public Coroutine closePlayerCoro;
    public Coroutine moveToPosCoro;
    public List<EnemyAction> actions;
    public float sightLineLength;
    #endregion

    //�л�Э��ʱ��ͣЭ��,�رճ��Լ������Э��
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
            Debug.Log("δ��⵽���");
            yield return new WaitForSeconds(1);
        }
        Debug.Log(name + "�������" + playerList[0].gameObject.name);

        player = playerList[0].gameObject;
        playerBox = player.GetComponent<BoxCollider2D>();
        
        StartCoroutine(LeavePlayer());

        //�����������߼�����ֱ��
        //bug:�������360�ȷ������
        while (isBlockWithPlayer())
        {
            yield return new WaitForSeconds(1);
        }
        Debug.Log("���δ�������壬FindPlayerЭ�̽���");
        findPlayerPos = player.transform.position;
        startGetAction();
        yield break;
    }

    //��Э��ʹ���������Ҳ����ڴ�����ײ�巶Χ�������赲ʱ����
    //�ù����ƶ�ʱ�����ж��Ƿ�ǽ���赲
    //���жϹ��������Ƿ������壬���û�У������ֱ��ָ������ƶ�������У������������ƶ��ķ����ƶ���ǰ����һ��ʼ������ҵ�λ�ú��ٴμ�⣬������Ǽ�ⲻ����ң���ʧ���(��б���ƶ��޸�Ϊֱ��ʽ�ƶ�)
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
        Debug.Log("��ʧ���");
        if (findPlayerCoro != null) { StopCoroutine(findPlayerCoro); findPlayerCoro = null; }
        findPlayerCoro = StartCoroutine(FindPlayer());
        stopCoro(findPlayerCoro);
    }
    //����Ƿ��뿪
    public IEnumerator LeavePlayer()
    {
        while (playerList.Count > 0)
        {
            findBox.OverlapCollider(findCondition, playerList);
            yield return new WaitForSeconds(5);
        }
        Debug.Log("����뿪��Χ");
        if (findPlayerCoro != null) StopCoroutine(findPlayerCoro);
        if(closePlayerCoro!= null) {StopCoroutine(closePlayerCoro);closePlayerCoro=null;}
        findPlayerCoro = StartCoroutine(FindPlayer());
        yield break;
    }
    #endregion

    /// <summary>
    /// ���ã�Ѳ��AI
    /// </summary>
    

    //�������������ж�
    //��ȡ�����֮��ĽǶ�
    //���ݺ����֮��ĽǶ��޸ĳ���
    //�����ж��������ж���Ϊ�±�
    //AI��Ϊ��ʵ�֣����һ�����㣬������󷽷���ÿ���ڵ㽻����������Ľű�ʵ��,ʵ�ֵķ����ͼ�����ֵ�����ڽṹ��Action��,����������ű�ʵ��ȫ��������󱣴��ڽṹ���е�method��
    //����ʵ����Ϊ�����󣬴��ظýű���
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

    //��������֮���Ƿ�ǽ���赲��������赲
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
        Debug.Log(name+"�����֮�䷢������"+ sightLineHitInfo.collider);
        return true;
    }
    //����ǰ���ķ����Ƿ�ǽ���赲,ֻ�ܳ��ĸ������ƶ�
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
        Debug.Log(name + "ǰ������������" + sightLineHitInfo.collider);
        return true;
    }
    //����ǰ����ָ���ص�ִ���ж�
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
            Debug.Log("�ƶ��������ʧ�ص�������");
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
        if (closePlayerCoro != null) { StopCoroutine(closePlayerCoro); closePlayerCoro = null; }//����Ѿ����ڽӽ�Э�̣�����ͣ
        if (actionBox.IsTouching(playerBox) == false)
        {
            Debug.Log(actionBox.name + "�޷����У��л���Ϊ");
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
        Debug.Log("��ȡ����");
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
        Debug.Log("��ǰִ����Ϊ" + (maxIndex + 1)+"/"+exeAction);
        if (actions[maxIndex].actionBox != null && closeDis.IsTouching(playerBox) == false)
        {
            Debug.Log("��Ҳ����ڽ�����");
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
        if ((enemyTowardAngle > 45 && enemyTowardAngle < 135)&&toward!=right)//�ҳ���
        {
            Debug.Log("����" + name + "������");
            towardTurn(right);
        }
        else if ((enemyTowardAngle >= 135 || enemyTowardAngle <= -135) && toward != up)//�ϳ���
        {
            Debug.Log("����" + name + "������");
            towardTurn(up);
        }
        else if ((enemyTowardAngle > -135 && enemyTowardAngle < -45) && toward!=left)//����
        {
            Debug.Log("����" + name + "������");
            towardTurn(left);
        }
        else if ((enemyTowardAngle <= 45 && enemyTowardAngle >= -45)&& toward!=down)//�³���
        {
            Debug.Log("����" + name + "������");
            towardTurn(down);
        }
    }
    void towardTurn(int Toward)
    {
        Debug.Log("��������ı�");
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
