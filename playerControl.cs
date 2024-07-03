using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class playerControl : MonoBehaviour
{
    public static playerControl Instance;
    public GameObject player;
    public Coroutine testCoro;
    public Camera cam;
    public playerData dataJson;
    public float xSize;
    public float ySize;
    public playerControl thisJson;

    private void Awake()
    {
        Instance = this;
        dataJson = GetComponent<playerData>();
        player = this.gameObject;
        aniObjectAwake();
        AwakeAttackObject();
        moveObjectAwake();
        awakeState();
        awakeVfx();
        thisJson = GetComponent<playerControl>();
    }
    // Start is called before the first frame update
    void Start()
    {
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        xSize = playerBox.size.x / 2;
        ySize = playerBox.size.y / 2;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (canAttack)
        {
            attackDown();
            if (Input.GetKeyDown(KeyCode.Space))
            {
                getFindEnemy();
                autoLockEnemyCoro = StartCoroutine(autoChangeLockEnemy());
            }
            //IsAttackEnd();
        }
    }

    //角色状态,属性栏，攻击力buff协程，防御力buff协程等
    //获得buff时，将buff的持续时间和数值记录并根据数值启动协程。
    //buff显示,分为常规显示和详细显示。
    #region
    public float basePower;//角色基础攻击力
    public float power;//角色攻击力
    public float baseMoveSpeed;
    public float moveSpeed;
    public float baseHealth;
    public float health;
    public List<BUFF> BuffList;
    public List<Coroutine> BuffCoroList;
    public List<string> BuffNameList;

    public struct BUFF
    {
        public float keepingTime;
        public float vlaue;
        public string name;
        public string buffIntroduction;
        public int bufftype;//0为攻击，1为防御，2为移速
    };
    public void awakeState()
    {
        baseHealth = 100;
        health = baseHealth;
        BuffList = new List<BUFF>();
        basePower = 1f;
        power = basePower;
        baseMoveSpeed = 20f;
        moveSpeed = baseMoveSpeed;
    }

    public void addAttackBuffCoro(BUFF buff, string name)
    {
        BuffList.Add(buff);
        BuffNameList.Add(name);
        StartCoroutine(buffState(buff.keepingTime, name));
        if (buff.bufftype == 0)
        {
            power += basePower * buff.vlaue;
        }
    }
    IEnumerator buffState(float keepingTime, string name)
    {
        float time = keepingTime;
        int buffCoroListIndex;
        yield return new WaitForSeconds(time);
        buffCoroListIndex = findBuffIndex(name);
        if (buffCoroListIndex < 0)
        {
            yield break;
        }
        StopCoroutine(BuffCoroList[buffCoroListIndex]);
        BuffCoroList.RemoveAt(buffCoroListIndex);
        yield break;
    }
    public int findBuffIndex(string name)
    {
        for (int i = 0; i < BuffNameList.Count; i++)
        {
            if (name == BuffNameList[i])
            {
                return i;
            }
        }
        return -1;
    }
    public void deleteBuff(int index)
    {
        if (BuffList[index].bufftype == 0)
        {
            power -= basePower * BuffList[index].vlaue;
        }
        BuffList.RemoveAt(index);
    }
    #endregion

    /// <summary>攻击部分
    ///角色普攻,释放技能，该部分主要被动画帧调用
    ///普攻或技能只有四个朝向，部分技能和普攻包含自动追击效果，含有追击效果的技能会在技能方向范围内追击锁定的敌人，否则正常释放。技能方向优先副方向，当副方向不存在时，优先副方向改为主方向，在释放技能动画结束前禁用移动。bug记录：禁用攻击的协程时可以左右移动
    ///角色朝向修改，45~135为向右，135~225为向上，225~315为向左，315~45为向下，朝向修改函数在角色移动代码中实现,修改朝向时同时修改角色GameObject朝向
    ///修改普通攻击框为方形攻击框，不再进行角度计算，始终保持攻击框在角色前方
    ///获取攻击和被攻击相关的对象变量
    ///造成伤害时按基础攻击力x倍率
    ///为了保证攻击判定不止有一帧，A动画有三帧攻击动画，前两帧启用startAttack记录命中怪物，第三帧造成伤害，在造成伤害时清空记录的怪物列表(删除）
    ///bug：攻击判定延迟：动画事件执行isAttackEnd后延迟两帧才结束动画：协程延迟：切换到update结束动画：update结束动画过快，跳过二次动画判定：调整到FixUpdate判定动画是否结束，修改isAttackEnd为直接结束函数
    ///解决方案：在update中添加动画切换条件:添加动画条件后出现执行过快无法判定二段攻击的情况，修改攻击协程到update中：普攻手感非常怪：
    /// </summary>
    public bool isAttackBtnDown;
    AttackType[] attackType = new AttackType[4];
    public int nowAttackType;
    public Coroutine playerAttackCoro;
    public int attackCount;
    public List<ContactPoint2D> p;

    public ContactFilter2D filterUseless;

    public class AttackType
    {
        public float attackSizeX;
        public float attackSizeY;
        public attackAction action;

        public void setAttackBoxSize()
        {
            playerControl.Instance.attackBox.size = new Vector2(attackSizeX, attackSizeY);
        }
    }
    public void attackTypeAwake()
    {
        p = new List<ContactPoint2D>();
        filterUseless = new ContactFilter2D();
        //初始化武器信息
        #region
        attackType = new AttackType[4];
        attackType[0] = new AttackType();
        attackType[1] = new AttackType();
        attackType[2] = new AttackType();
        attackType[3] = new AttackType();

        //初始化攻击范围，为了方便玩家计算，正常攻击过程中不变化攻击范围
        attackType[0].attackSizeX = 3;
        attackType[0].attackSizeY = 2.3f;
        attackType[1].attackSizeX = 10;
        attackType[1].attackSizeY = 10;
        attackType[2].attackSizeX = 10;
        attackType[2].attackSizeY = 10;
        attackType[3].attackSizeX = 10;
        attackType[3].attackSizeY = 10;

        attackType[0].action = new attackAction(twinSwordAttack);

        #endregion

        //以下为测试信息
        attackType[0].setAttackBoxSize();
        nowAttackType = 0;

        //初始化武器信息，玩家初始武器为单手剑和双剑
        attackCount = 0;
    }
    public void nomalAttack()
    {
        attackType[nowAttackType].action(attackCount);
    }
    public void attackDown()
    {
        if (Input.GetButtonDown("Attack"))
        {
            if (attackCount == 0)
            {
                disableMove();
                isAttackBtnDown = true;
                ani.SetBool("Attack", true);
                ani.SetBool("StopAni", false);
                attackCount++;
                return;
            }
            attackCount++;
        }
    }
    public bool IsAttackEnd()
    {
        if (attackCount <= 1)
        {
            ani.SetBool("StopAni", true);
            ani.SetBool("Attack", false);
            enableMove();
            attackCount = 0;
            return true;
        }
        attackCount = 0;
        return false;
    }

    /// <summary>
    /// 特效相关
    /// 对范围内最多maxNum的敌人造成伤害并播放传入的特效
    /// </summary>
    #region
    public delegate int vfxObject(int layer);
    public List<vfxObject> vfxList;
    public float maxRedStrength;
    public Coroutine redCoro;
    public Color rawColor;
    public SpriteRenderer sp;
    public void awakeVfx()
    {
        vfxList = new List<vfxObject>();
        vfxList.Add(PhyVfx);
        sp = GetComponent<SpriteRenderer>();
        maxRedStrength = 0;
        rawColor = sp.color;
    }
    public Vector3 getVfxPos()
    {
        return new Vector3(UnityEngine.Random.Range(-xSize, xSize) + transform.position.x, UnityEngine.Random.Range(-ySize, ySize) + transform.position.y);
    }
    public int PhyVfx(int layer)
    {
        GameObject obj = Resources.Load<GameObject>("vfx/attackVfxPhy");
        GameObject a = Instantiate(obj, getVfxPos(), Quaternion.identity);
        a.GetComponent<SpriteRenderer>().sortingOrder = layer;
        return 0;
    }
    public void NumVfx(float num, vfxObject v, int layer)
    {
        GameObject obj = Resources.Load<GameObject>("vfx/damageNumVfx");
        GameObject a = Instantiate(obj, getVfxPos(), Quaternion.identity);
        a.GetComponent<TextMeshProUGUI>().text = num.ToString();
        v(layer);
    }
    public void takeDamage(float attackStrength, int maxNum, int vfxIndex)
    {
        attackEnemyList.Clear();
        attackBox.OverlapCollider(findContactFilter, attackEnemyList);
        float damage = attackStrength * power; ;
        for (int i = 0; i < attackEnemyList.Count && i < maxNum; i++)
        {
            attackEnemyList[i].gameObject.GetComponentInParent<EnemyData>().beTakeDamge(damage, vfxIndex);
        }
        attackEnemyList.Clear();
    }
    //角色受击
    //屏幕抖动效果
    //受控制
    #region
    public Coroutine camShakeCoro;
    public float maxShake = 0;
    public void beTakeDamage(float damage, int vfxIndex)
    {
        health -= damage;
        Debug.Log("相机晃动");
        float strength = damage / baseHealth;
        NumVfx(damage, vfxList[vfxIndex],curLayer+1);
        //相机晃动
        startCamShake(strength * 0.1f, strength * 0.02f);
        startRed(strength);
    }
    public void startCamShake(float duration, float magnitude)
    {
        if (camShakeCoro != null && maxShake < magnitude)
        {
            StopCoroutine(camShakeCoro);
            camShakeCoro = StartCoroutine(CamShake(duration, magnitude));
            maxShake = magnitude;
            return;
        }
        StartCoroutine(CamShake(duration, magnitude));
    }
    public IEnumerator CamShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = UnityEngine.Random.Range(-1f, 1f) * magnitude;
            float y = UnityEngine.Random.Range(-1f, 1f) * magnitude;

            cam.transform.localPosition = cam.transform.position + new Vector3(x, y, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }
        maxShake = 0;
        camShakeCoro = null;
        yield break;
    }
    public void startRed(float strength)
    {
        if (strength > maxRedStrength)
        {
            if (redCoro != null)
            {
                StopCoroutine(redCoro);
                sp.color = rawColor;
            }
            StartCoroutine(spriteBecomingRed(strength));
        }
    }

    //受伤颜色逐渐变红，时间固定，但根据传入的数值调整变红的速度,和玩家相机机制相同，只有存在高等级时刷新该协程
    public IEnumerator spriteBecomingRed(float strength)
    {
        int time;
        float s = strength * 1.25f;
        time = 10;
        while (time > 0)
        {
            sp.color = new Color(sp.color.r, sp.color.g - s, sp.color.b - s);
            time--;
            yield return new WaitForSeconds(0.01f);
        }
        sp.color = rawColor;
        maxRedStrength = 0;
        redCoro = null;
        yield break;
    }
    #endregion
    #endregion

    ///<summary>
    ///场景相关,每次进入或出生在场景时调用
    ///修改当前生物所在层,并应用到子类
    /// </summary>
    #region
    public int curLayer;
    public List<SpriteRenderer> spList;
    public SpriteRenderer[] sp2;
    public void sceneAwake(int layer)
    {
        changeSceneLayer(layer + 1);
        spList = new List<SpriteRenderer>();
    }
    public void changeSceneLayer(int layer)//设置生物图层，已经加一
    {
        curLayer = layer;
        transform.position = new Vector3(transform.position.x, transform.position.y, layer);
        sp.sortingOrder = layer;
        SpriteRenderer[] sp2 = GetComponentsInChildren<SpriteRenderer>();
        Debug.Log(sp2.Count());
        for (int i = 1; i < sp2.Count(); i++)
        {
            sp2[i].sortingOrder = layer + 1;
        }
    }
    public void leaveLayer()
    {
        lockEnemyDied();
    }
    #endregion

    //攻击详细函数
    #region
    public delegate int attackAction(int index);
    public int singleSwordAttack(int index)
    {
        takeDamage(power, 10, 0);
        isPurseAttack();
        return index;
    }
    public int twinSwordAttack(int index)
    {
        takeDamage(power, 10, 0);
        isPurseAttack();
        return index;
    }
    public int pikeAttack(int index)
    {
        return index;
    }
    public int bowAttack(int index)
    {
        return index;
    }
    #endregion


    //技能详细函数
    #region
    #endregion

    ///<summary>索敌相关
    ///获取与锁定敌人相关的角度
    ///追击协程(执行时间，移动速度，执行对象）,执行时间为动画中位移动画结束的时间，如第三帧结束，则追击协程在第三帧结束，移动速度和技能原定的移动速度相同，追击效果：技能位移速度乘算Cos，Sin角度，追击开始时调整技能朝向
    ///是否追击判定,判定成功清除旧追击协程，开启新协程
    ///获取锁定敌人,在findLockEnemyRange圆形范围内获取可锁定的敌人。
    ///主动锁定刷新机制：玩家自行切换锁定等级和目标
    ///被动锁定刷新机制：自动选择同等级内最最近的目标
    ///代码中的特殊情况1：如果刷新后被锁定的怪物不在怪物列表中，依然可以执行锁定，锁定对象不会刷新，除非玩家主动刷新或高等级对象出现时自动刷新
    ///代码中的特殊情况2：如果怪物离开的玩家锁定范围，在下一次刷新前该怪物依然存在于怪物列表中
    ///锁定敌人切换，在敌人列表中切换
    ///</summary>
    #region
    public BoxCollider2D playerBox;
    public CircleCollider2D playerFindBox;

    public int singleSword = 0;
    public int twinSword = 1;
    public int pike = 2;
    public int bow = 3;

    public findEnemy lockEnemy;
    public GameObject lockSign;//锁定标志
    public CircleCollider2D purseAttackRange;

    public List<Collider2D> hadFindEnemy;//进入锁定范围的所有敌人
    public List<findEnemy>[] hadFindEnemyList = new List<findEnemy>[4];
    public int lockEnemyIndex;//玩家当前锁定的怪物在列表中的下标
    public int maxEnemyLevel;//获取的敌人列表中最高的等级，当比该数值高的敌人出现后，自动切换锁定为该目标
    public int lockLevel;
    public bool isAutoLevel;//当该标志为false时，获取敌人协程按最高等级执行，为true时按玩家指定等级执行
    public bool autoChangeLock;//自动切换锁定是否启用

    public List<Collider2D> attackEnemyList;//进入攻击范围的敌人
    public BoxCollider2D lockEnemyBox;
    public ContactFilter2D findContactFilter;//寻找接触筛选器
    public Coroutine purseAttackCoro;
    public Coroutine autoLockEnemyCoro;
    public LayerMask EnemyLayer;
    public bool isHaveLockEnemy;

    public bool canAttack;

    public BoxCollider2D attackBox;
    public GameObject attackBoxObject;

    public struct findEnemy
    {
        public int level;//锁定优先级
        public GameObject enemy;
        public Collider2D box;
    };
    public void lockEnemyDied()
    {
        lockEnemy.enemy = null;
        if (autoLockEnemyCoro != null) StopCoroutine(autoChangeLockEnemy());
        getFindEnemy();
        autoLockEnemyCoro = StartCoroutine(autoChangeLockEnemy());
    }

    public void AwakeAttackObject()
    {
        attackBoxObject = GameObject.Find("Player/attackRange");
        attackBox = GameObject.Find("Player/attackRange").GetComponent<BoxCollider2D>();


        //自动锁定设置
        isAutoLevel = true;
        attackTypeAwake();

        lockLevel = 1;
        playerBox = GetComponent<BoxCollider2D>();
        EnemyLayer = LayerMask.GetMask("Enemy");
        findContactFilter = new ContactFilter2D();
        findContactFilter.ClearDepth();
        findContactFilter.ClearNormalAngle();
        findContactFilter.SetLayerMask(EnemyLayer);

        canAttack = true;
        playerFindBox = GameObject.Find("Player/findLockEnemyRange").GetComponent<CircleCollider2D>();
        purseAttackRange = GameObject.Find("Player/purseAttackRange").GetComponent<CircleCollider2D>();
        purseAttackCoro = null;

        hadFindEnemyList[0] = new List<findEnemy>();
        hadFindEnemyList[1] = new List<findEnemy>();
        hadFindEnemyList[2] = new List<findEnemy>();
        hadFindEnemyList[3] = new List<findEnemy>();
        isAttackBtnDown = false;
    }

    //获取怪物列表,1为小兵，2为精英怪，3为Boss,0为找不到怪物
    public void getFindEnemy()
    {
        maxEnemyLevel = 0;
        hadFindEnemy.Clear();
        hadFindEnemyList[1].Clear();
        hadFindEnemyList[2].Clear();
        hadFindEnemyList[3].Clear();
        playerFindBox.OverlapCollider(findContactFilter, hadFindEnemy);
        for (int i = 0; i < hadFindEnemy.Count; i++)
        {
            Debug.Log(hadFindEnemy[i].name);
        }
        for (int i = 0; i < hadFindEnemy.Count; i++)
        {
            if (hadFindEnemy[i].GetComponentInParent<EnemyAI>().level == 2 && hadFindEnemy[i].GetComponentInParent<EnemyAI>().isAwake == true)
            {
                hadFindEnemyList[1].Add(hadFindEnemy[i].GetComponentInParent<EnemyAI>().enemy);

                if (maxEnemyLevel < 1) { maxEnemyLevel = 1; lockLevel = 1; }
                if (isAutoLevel == false) { lockLevel = 1; }
            }
            else if (hadFindEnemy[i].GetComponentInParent<EnemyAI>().level == 1 && hadFindEnemy[i].GetComponentInParent<EnemyAI>().isAwake == true)
            {
                hadFindEnemyList[2].Add(hadFindEnemy[i].GetComponentInParent<EnemyAI>().enemy);
                if (maxEnemyLevel < 2) { maxEnemyLevel = 2; lockLevel = 2; }
                if (isAutoLevel == false) { lockLevel = 1; }
            }
            else if (hadFindEnemy[i].GetComponentInParent<EnemyAI>().level == 3 && hadFindEnemy[i].GetComponentInParent<EnemyAI>().isAwake == true)
            {
                hadFindEnemyList[3].Add(hadFindEnemy[i].GetComponentInParent<EnemyAI>().enemy);
                if (maxEnemyLevel < 3) { maxEnemyLevel = 3; lockLevel = 3; }
                if (isAutoLevel == false) { lockLevel = 1; }
            }
        }
        Debug.Log("最高等级为" + maxEnemyLevel);
    }
    public void changeLockEnemy(findEnemy LE)
    {
        if (lockEnemy.enemy != null)
        {
            lockEnemy.enemy.GetComponent<EnemyData>().hideDetailData();
        }
        lockEnemy = LE;
        lockEnemy.enemy.GetComponent<EnemyData>().showDetailData(thisJson);
        Debug.Log("切换对象为" + LE.enemy.name);
    }
    public void changeLockEnemyLevel(int level)
    {

    }
    //获取锁定等级中最近的对象
    //当附近没有怪物时或没有指定等级的怪物时，不执行代码，3秒后再次执行
    //当附近存在多个指定等级怪物时，自动锁定最近敌人
    public IEnumerator autoChangeLockEnemy()
    {
        Debug.Log("当前锁定等级" + lockLevel);
        while (true)
        {
            getFindEnemy();
            if (maxEnemyLevel == 0 || hadFindEnemyList[lockLevel].Count == 0)
            {
                Debug.Log("怪物列表为空");
                yield return new WaitForSeconds(3f);
                continue;
            }
            GameObject lockE = hadFindEnemyList[lockLevel][0].enemy;
            int minDisIndex = 0;

            //从第一个可以被计算的碰撞体开始循环
            while (hadFindEnemyList[lockLevel][minDisIndex].box.Distance(playerBox).isValid == false)
            {
                Debug.Log(hadFindEnemyList[lockLevel][minDisIndex].enemy.name + "可以被计算");
                minDisIndex++;
            }
            //如果循环中出现了不可计算的碰撞体，则跳过
            for (int i = 0; i < hadFindEnemyList[lockLevel].Count; i++)
            {
                if (hadFindEnemyList[lockLevel][i].box.Distance(playerBox).isValid == false)
                {
                    continue;
                }
                if (hadFindEnemyList[lockLevel][i].box.Distance(playerBox).distance < hadFindEnemyList[lockLevel][minDisIndex].box.Distance(playerBox).distance)
                {
                    minDisIndex = i;
                }
            }
            changeLockEnemy(hadFindEnemyList[lockLevel][minDisIndex]);
            yield return new WaitForSeconds(3f);
        }
    }

    //获取与锁定对象的角度
    public float getAngleWithEnmey()
    {
        return -Mathf.Atan2(player.transform.position.x - lockEnemy.enemy.transform.position.x, player.transform.position.y - lockEnemy.enemy.transform.position.y) * Mathf.Rad2Deg;
    }
    public void changePurseAttackAngle()
    {
        float angle = getAngleWithEnmey();
        if (angle > 45 && angle < 135)//右朝向
        {
            playerTurn(right);
        }
        else if (angle >= 135 || angle <= -135)//上朝向
        {
            playerTurn(up);
        }
        else if (angle > -135 && angle < -45)//左朝向
        {
            playerTurn(left);
        }
        else if (angle <= 45 || angle >= -45)//下朝向
        {
            playerTurn(down);
        }
    }
    //旧追击
    #region
    //追击协程(执行时间，移动速度，执行对象）,执行时间为动画中位移动画结束的时间，如第三帧结束，则追击协程在第三帧结束，移动速度和技能原定的移动速度相同
    //bug：如攻击动画在第三帧，但攻击速度被增加至第二帧位置触发攻击，此时追击协程还在执行，导致未追击至对象而提前触发攻击
    //解决方案：将动画事件内的函数都在这里执行，动画速度的增加和减少影响协程执行时间
    //当出现追击或移动协程执行时出现第二个追击或移动协程时，清除第一个协程,立即执行新出现的协程
    IEnumerator purseAttack(int exeTime, float exeSpeed, GameObject exeObject)
    {
        int time = exeTime;
        float angle = getAngleWithEnmey();
        Vector2 dir = (lockEnemy.enemy.transform.position - transform.position).normalized;
        /*float xSpeed = (float)Math.Sin(angle / (180 / Math.PI));
        float ySpeed = -(float)Math.Cos(angle / (180 / Math.PI));*/
        Rigidbody2D exeObjectBody = exeObject.GetComponent<Rigidbody2D>();
        //调整动画方向
        if (angle > 45 && angle < 135)//右朝向
        {
            playerTurn(right);
        }
        else if (angle >= 135 || angle <= -135)//上朝向
        {
            playerTurn(up);
        }
        else if (angle > -135 && angle < -45)//左朝向
        {
            playerTurn(left);
        }
        else if (angle <= 45 || angle >= -45)//下朝向
        {
            playerTurn(down);
        }

        //开始追击
        while (time > 0 && playerBox.IsTouching(lockEnemyBox) == false)
        {
            exeObjectBody.velocity = dir * moveSpeed;
            time--;
            yield return 0;
        }
        exeObjectBody.velocity = new Vector2(0, 0);
        purseAttackCoro = null;
        yield break;
    }
    //非追击协程
    IEnumerator moveAttack(int exeTime, float exeSpeed, GameObject exeObject)
    {
        Rigidbody2D exeObjectBody = exeObject.GetComponent<Rigidbody2D>();
        int time = exeTime;
        if (toward == up)
        {
            while (time > 0)
            {
                time--;
                yield return 0;
            }
        }
        else if (toward == down)
        {
            while (time > 0)
            {
                time--;
                yield return 0;
            }
        }
        else if (toward == left)
        {
            while (time > 0)
            {
                time--;
                yield return 0;
            }
        }
        else if (toward == right)
        {
            while (time > 0)
            {
                time--;
                yield return 0;
            }
        }
        yield break;
    }
    /*public void isPurseAttack(int exeTime, bool isMove)
    {
        if (lockEnemy.enemy != null && purseAttackRange.IsTouching(lockEnemyBox))
        {
            if (purseAttackCoro != null)
            {
                StopCoroutine(purseAttackCoro);
                purseAttackCoro = null;
            }
            purseAttackCoro = StartCoroutine(purseAttackCo(exeTime, moveSpeed, this.gameObject));
        }
        else if (isMove)
        {

            if (purseAttackCoro != null)
            {
                StopCoroutine(purseAttackCoro);
                purseAttackCoro = null; ;
            }
            purseAttackCoro = StartCoroutine(moveAttack(exeTime, moveSpeed, this.gameObject));
        }
    }*/
    /*public void isPurseAttack(float purseSize, int exeTime, float exeSpeed, GameObject exeObject, bool isMove)
 {
     purseAttackRange.radius = purseSize;
     if (lockEnemy.enemy != null && purseAttackRange.IsTouching(lockEnemyBox))
     {
         if (purseAttackCoro != null)
         {
             StopCoroutine(purseAttackCoro);
             purseAttackCoro = null;
         }
         purseAttackCoro = StartCoroutine(purseAttack(exeTime, exeSpeed, exeObject));
     }
     else if (isMove)
     {
         if (purseAttackCoro != null)
         {
             StopCoroutine(purseAttackCoro);
             purseAttackCoro = null; ;
         }
         purseAttackCoro = StartCoroutine(moveAttack(exeTime, exeSpeed, exeObject));
     }
 }*/
    #endregion
    public void purseAttack(float strength)
    {
        changePurseAttackAngle();
        controlMove((lockEnemy.enemy.transform.position - player.transform.position).normalized * moveSpeed * strength, moveLevel);
    }
    public void isPurseAttack()
    {
        if (lockEnemy.enemy != null && purseAttackRange.IsTouching(lockEnemy.box))
        {
            purseAttack(3);
        }
    }
    #endregion

    //角色移动，可以进行斜角移动,当角色修改朝向时，同时修改技能朝向（完成）
    //获取角色移动相关的对象变量
    //当角色移动时，当玩家按下按键时，记录进列表,当玩家松开某个按键时，将该按键从列表中删除,优先显示主朝向动画(完成)
    //朝向动画切换，当玩家朝向发生改变时，将isChangToward改为true,如果玩家松开的是列表中第一个按键，则视为主方向更改
    //朝向处理Toward为0进入Down动画，2进入Up动画，非0非1进入Right动画，主朝向从左转到右时或从右转到左时，不改变主朝向动画，而是人物转向
    //移动优先级：分为控制级，移动级。同等级之间直接覆盖原来移速，当控制级出现时修改当前优先级为控制级，覆盖移动级，期间移动级仅在控制的速度上做加减。
    //控制级出现0.1秒后修改当前优先级为移动级
    #region
    public Rigidbody2D playerBody;
    public Coroutine playerMoveCoro;
    public Coroutine recardPlayerTowardCoro;
    public List<int> playerToward;
    public GameObject playerActionToward;
    public string downToward = "S";
    public string upToward = "W";
    public string leftToward = "A";
    public string rightToward = "D";
    public string[] towardName = new string[4];
    public int toward;
    #region
    public int controlLevel = 1;//控制级
    public int moveLevel = 0;//移动级
    public int curMoveLevel = 0;//当前等级
    public Coroutine recoverMoveLevelCoro;
    #endregion
    //定义常量,逆时针
    int down = 0;
    int right = 1;
    int up = 2;
    int left = 3;

    public void moveObjectAwake()
    {
        playerBody = GetComponent<Rigidbody2D>();
        playerActionToward = GameObject.Find("Player/PlayerActionToward");
        playerMoveCoro = StartCoroutine(playerMove());
        recardPlayerTowardCoro = StartCoroutine(recardPlayerToward());
        toward = -1;
        curMoveLevel = 0;
        recoverMoveLevelCoro = null;
    }
    void moveH()
    {
        float moveH = Input.GetAxis("Horizontal");
        controlMove(new Vector2((moveSpeed * moveH), playerBody.velocity.y), moveLevel);//不设置另一个方向速度为零，方便斜角移动
        ani.SetBool("Walk", true);
    }
    void moveV()
    {
        float moveV = Input.GetAxis("Vertical");
        controlMove(new Vector2(playerBody.velocity.x, (moveSpeed * moveV)), moveLevel);//不设置另一个方向速度为零，方便斜角移动
        ani.SetBool("Walk", true);
    }
    public void controlMove(Vector2 v, int level)
    {
        if (level < curMoveLevel)
        {
            playerBody.velocity += v * Time.deltaTime;
        }
        else if (level == curMoveLevel)
        {
            playerBody.velocity = v;
        }
        else
        {
            curMoveLevel = level;
            playerBody.velocity = v;
            if (recoverMoveLevelCoro != null)
            {
                StopCoroutine(recoverMoveLevelCoro);
            }
            StartCoroutine(recoverMoveLevel());
        }
    }
    IEnumerator recoverMoveLevel()
    {
        yield return new WaitForSeconds(0.2f);
        curMoveLevel = 0;
        recoverMoveLevelCoro = null;
        yield break;
    }
    //角色行走主副朝向记录
    void playerTurnRecord()
    {
        if (Input.GetButtonDown("Left"))
        {
            playerTurn(left);
        }
        if (Input.GetButtonDown("Down"))
        {
            playerTurn(down);
        }
        if (Input.GetButtonDown("Right"))
        {
            playerTurn(right);
        }
        if (Input.GetButtonDown("Up"))
        {
            playerTurn(up);
        }
        //保证玩家已经按下方向键的情况不会改变方向，松开第一次按下的方向键才会改变
        if (Input.GetButtonDown("Left") && playerToward.IndexOf(left) == -1)
        {
            playerToward.Add(left);
            if (playerToward.IndexOf(left) == 0) { playerTurn(playerToward[0]); }
        }
        if (Input.GetButtonDown("Down") && playerToward.IndexOf(down) == -1)
        {
            playerToward.Add(down);
            if (playerToward.IndexOf(down) == 0) { playerTurn(playerToward[0]); }
        }
        if (Input.GetButtonDown("Right") && playerToward.IndexOf(right) == -1)
        {
            playerToward.Add(right);
            if (playerToward.IndexOf(right) == 0) { playerTurn(playerToward[0]); }
        }
        if (Input.GetButtonDown("Up") && playerToward.IndexOf(up) == -1)
        {
            playerToward.Add(up);
            if (playerToward.IndexOf(up) == 0) { playerTurn(playerToward[0]); }
        }
        if (Input.GetButtonUp("Left"))
        {
            if (playerToward.IndexOf(left) == 0 && playerToward.Count > 1) { playerTurn(playerToward[1]); }
            playerToward.Remove(left);
        }
        if (Input.GetButtonUp("Down"))
        {
            if (playerToward.IndexOf(down) == 0 && playerToward.Count > 1) { playerTurn(playerToward[1]); }
            playerToward.Remove(down);
        }
        if (Input.GetButtonUp("Up"))
        {
            if (playerToward.IndexOf(up) == 0 && playerToward.Count > 1) { playerTurn(playerToward[1]); }
            playerToward.Remove(up);
        }
        if (Input.GetButtonUp("Right"))
        {
            if (playerToward.IndexOf(right) == 0 && playerToward.Count > 1) { playerTurn(playerToward[1]); }
            playerToward.Remove(right);
        }
    }
    //角色朝向修改函数
    void playerTurn(int Toward)
    {
        if (Toward == right && toward != right)
        {
            ani.SetBool("IsChangeToward", true);
            transform.localRotation = Quaternion.Euler(0, 0, 0);
            attackBoxObject.transform.eulerAngles = new Vector3(0, 0, 90);
            ani.SetInteger("Toward", 1);
            toward = right;
        }
        else if (Toward == left && toward != left)
        {
            ani.SetBool("IsChangeToward", true);
            transform.localRotation = Quaternion.Euler(0, 180, 0);
            attackBoxObject.transform.eulerAngles = new Vector3(0, 0, -90);
            ani.SetInteger("Toward", 1);
            toward = left;
        }
        else if (Toward == up && toward != up)
        {
            ani.SetBool("IsChangeToward", true);
            ani.SetInteger("Toward", 2);
            attackBoxObject.transform.eulerAngles = new Vector3(0, 0, 180);
            toward = up;
        }
        else if (Toward == down && toward != down)
        {
            ani.SetBool("IsChangeToward", true);
            ani.SetInteger("Toward", 0);
            attackBoxObject.transform.eulerAngles = new Vector3(0, 0, 0);
            toward = down;
        }
    }
    public void TurnStop()
    {
        ani.SetBool("StopAni", false);
        ani.SetBool("IsChangeToward", false);
    }
    IEnumerator playerMove()
    {
        while (true)
        {
            if (Input.GetButton("Horizontal"))
            {
                moveH();
            }
            if (Input.GetButton("Vertical"))
            {
                moveV();
            }
            if (Input.GetButton("Vertical") == false && Input.GetButton("Horizontal") == false)
            {
                ani.SetBool("Walk", false);
            }
            else if (Input.GetButton("Vertical") == false)
            {
                controlMove(new Vector2(playerBody.velocity.x, 0), moveLevel);
            }
            else if (Input.GetButton("Horizontal") == false)
            {
                controlMove(new Vector2(0, playerBody.velocity.y), moveLevel);
            }
            yield return 0;
        }
    }
    IEnumerator recardPlayerToward()
    {
        while (true)
        {
            playerTurnRecord();
            if (toward == left)
            {
                player.transform.localEulerAngles = new Vector3(0, -180, 0);
            }
            else
            {
                player.transform.localEulerAngles = new Vector3(0, 0, 0);
            }
            yield return 0;
        }
    }
    public void disableMove()
    {
        StopCoroutine(playerMoveCoro);
        //StopCoroutine(recardPlayerTowardCoro);
        ani.SetBool("Walk", false);
    }
    public void enableMove()
    {
        playerMoveCoro = StartCoroutine(playerMove());
        //recardPlayerTowardCoro = StartCoroutine(recardPlayerToward());
    }
    #endregion
    public void actionFailed()
    {
        Debug.Log("该操作是不被允许的");
    }

    //切换角色动画
    //切换技能状态机中的动画
    //动画相关变量初始化
    #region
    public Animator ani;
    public static AnimatorOverrideController aniContorl;
    public static string aniPath;
    public void aniObjectAwake()
    {
        ani = GetComponent<Animator>();
        aniContorl = new AnimatorOverrideController(ani.runtimeAnimatorController);
        ani.runtimeAnimatorController = aniContorl;
        aniPath = "ani/playerAni/";
    }
    public void aniSwitch(string name, bool isTrue)
    {
        ani.SetBool(name, isTrue);
    }
    public void aniSwitch(string name, int num)
    {
        ani.SetInteger(name, num);
    }
    public void aniSwitch(string name, bool isTrue, float speed)
    {
        ani.SetBool(name, isTrue);
        ani.speed = speed;
    }
    public void aniClipSwitch(string aniName, string aniName2, string aniPath)
    {
        aniContorl[aniName] = Resources.Load<AnimationClip>(aniPath + aniName2);
    }
    #endregion
}
