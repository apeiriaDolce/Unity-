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

    //��ɫ״̬,��������������buffЭ�̣�������buffЭ�̵�
    //���buffʱ����buff�ĳ���ʱ�����ֵ��¼��������ֵ����Э�̡�
    //buff��ʾ,��Ϊ������ʾ����ϸ��ʾ��
    #region
    public float basePower;//��ɫ����������
    public float power;//��ɫ������
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
        public int bufftype;//0Ϊ������1Ϊ������2Ϊ����
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

    /// <summary>��������
    ///��ɫ�չ�,�ͷż��ܣ��ò�����Ҫ������֡����
    ///�չ�����ֻ���ĸ����򣬲��ּ��ܺ��չ������Զ�׷��Ч��������׷��Ч���ļ��ܻ��ڼ��ܷ���Χ��׷�������ĵ��ˣ����������ͷš����ܷ������ȸ����򣬵������򲻴���ʱ�����ȸ������Ϊ���������ͷż��ܶ�������ǰ�����ƶ���bug��¼�����ù�����Э��ʱ���������ƶ�
    ///��ɫ�����޸ģ�45~135Ϊ���ң�135~225Ϊ���ϣ�225~315Ϊ����315~45Ϊ���£������޸ĺ����ڽ�ɫ�ƶ�������ʵ��,�޸ĳ���ʱͬʱ�޸Ľ�ɫGameObject����
    ///�޸���ͨ������Ϊ���ι����򣬲��ٽ��нǶȼ��㣬ʼ�ձ��ֹ������ڽ�ɫǰ��
    ///��ȡ�����ͱ�������صĶ������
    ///����˺�ʱ������������x����
    ///Ϊ�˱�֤�����ж���ֹ��һ֡��A��������֡����������ǰ��֡����startAttack��¼���й������֡����˺���������˺�ʱ��ռ�¼�Ĺ����б�(ɾ����
    ///bug�������ж��ӳ٣������¼�ִ��isAttackEnd���ӳ���֡�Ž���������Э���ӳ٣��л���update����������update�����������죬�������ζ����ж���������FixUpdate�ж������Ƿ�������޸�isAttackEndΪֱ�ӽ�������
    ///�����������update����Ӷ����л�����:��Ӷ������������ִ�й����޷��ж����ι�����������޸Ĺ���Э�̵�update�У��չ��ָзǳ��֣�
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
        //��ʼ��������Ϣ
        #region
        attackType = new AttackType[4];
        attackType[0] = new AttackType();
        attackType[1] = new AttackType();
        attackType[2] = new AttackType();
        attackType[3] = new AttackType();

        //��ʼ��������Χ��Ϊ�˷�����Ҽ��㣬�������������в��仯������Χ
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

        //����Ϊ������Ϣ
        attackType[0].setAttackBoxSize();
        nowAttackType = 0;

        //��ʼ��������Ϣ����ҳ�ʼ����Ϊ���ֽ���˫��
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
    /// ��Ч���
    /// �Է�Χ�����maxNum�ĵ�������˺������Ŵ������Ч
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
    //��ɫ�ܻ�
    //��Ļ����Ч��
    //�ܿ���
    #region
    public Coroutine camShakeCoro;
    public float maxShake = 0;
    public void beTakeDamage(float damage, int vfxIndex)
    {
        health -= damage;
        Debug.Log("����ζ�");
        float strength = damage / baseHealth;
        NumVfx(damage, vfxList[vfxIndex],curLayer+1);
        //����ζ�
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

    //������ɫ�𽥱�죬ʱ��̶��������ݴ������ֵ���������ٶ�,��������������ͬ��ֻ�д��ڸߵȼ�ʱˢ�¸�Э��
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
    ///�������,ÿ�ν��������ڳ���ʱ����
    ///�޸ĵ�ǰ�������ڲ�,��Ӧ�õ�����
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
    public void changeSceneLayer(int layer)//��������ͼ�㣬�Ѿ���һ
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

    //������ϸ����
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


    //������ϸ����
    #region
    #endregion

    ///<summary>�������
    ///��ȡ������������صĽǶ�
    ///׷��Э��(ִ��ʱ�䣬�ƶ��ٶȣ�ִ�ж���,ִ��ʱ��Ϊ������λ�ƶ���������ʱ�䣬�����֡��������׷��Э���ڵ���֡�������ƶ��ٶȺͼ���ԭ�����ƶ��ٶ���ͬ��׷��Ч��������λ���ٶȳ���Cos��Sin�Ƕȣ�׷����ʼʱ�������ܳ���
    ///�Ƿ�׷���ж�,�ж��ɹ������׷��Э�̣�������Э��
    ///��ȡ��������,��findLockEnemyRangeԲ�η�Χ�ڻ�ȡ�������ĵ��ˡ�
    ///��������ˢ�»��ƣ���������л������ȼ���Ŀ��
    ///��������ˢ�»��ƣ��Զ�ѡ��ͬ�ȼ����������Ŀ��
    ///�����е��������1�����ˢ�º������Ĺ��ﲻ�ڹ����б��У���Ȼ����ִ���������������󲻻�ˢ�£������������ˢ�»�ߵȼ��������ʱ�Զ�ˢ��
    ///�����е��������2����������뿪�����������Χ������һ��ˢ��ǰ�ù�����Ȼ�����ڹ����б���
    ///���������л����ڵ����б����л�
    ///</summary>
    #region
    public BoxCollider2D playerBox;
    public CircleCollider2D playerFindBox;

    public int singleSword = 0;
    public int twinSword = 1;
    public int pike = 2;
    public int bow = 3;

    public findEnemy lockEnemy;
    public GameObject lockSign;//������־
    public CircleCollider2D purseAttackRange;

    public List<Collider2D> hadFindEnemy;//����������Χ�����е���
    public List<findEnemy>[] hadFindEnemyList = new List<findEnemy>[4];
    public int lockEnemyIndex;//��ҵ�ǰ�����Ĺ������б��е��±�
    public int maxEnemyLevel;//��ȡ�ĵ����б�����ߵĵȼ������ȸ���ֵ�ߵĵ��˳��ֺ��Զ��л�����Ϊ��Ŀ��
    public int lockLevel;
    public bool isAutoLevel;//���ñ�־Ϊfalseʱ����ȡ����Э�̰���ߵȼ�ִ�У�Ϊtrueʱ�����ָ���ȼ�ִ��
    public bool autoChangeLock;//�Զ��л������Ƿ�����

    public List<Collider2D> attackEnemyList;//���빥����Χ�ĵ���
    public BoxCollider2D lockEnemyBox;
    public ContactFilter2D findContactFilter;//Ѱ�ҽӴ�ɸѡ��
    public Coroutine purseAttackCoro;
    public Coroutine autoLockEnemyCoro;
    public LayerMask EnemyLayer;
    public bool isHaveLockEnemy;

    public bool canAttack;

    public BoxCollider2D attackBox;
    public GameObject attackBoxObject;

    public struct findEnemy
    {
        public int level;//�������ȼ�
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


        //�Զ���������
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

    //��ȡ�����б�,1ΪС����2Ϊ��Ӣ�֣�3ΪBoss,0Ϊ�Ҳ�������
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
        Debug.Log("��ߵȼ�Ϊ" + maxEnemyLevel);
    }
    public void changeLockEnemy(findEnemy LE)
    {
        if (lockEnemy.enemy != null)
        {
            lockEnemy.enemy.GetComponent<EnemyData>().hideDetailData();
        }
        lockEnemy = LE;
        lockEnemy.enemy.GetComponent<EnemyData>().showDetailData(thisJson);
        Debug.Log("�л�����Ϊ" + LE.enemy.name);
    }
    public void changeLockEnemyLevel(int level)
    {

    }
    //��ȡ�����ȼ�������Ķ���
    //������û�й���ʱ��û��ָ���ȼ��Ĺ���ʱ����ִ�д��룬3����ٴ�ִ��
    //���������ڶ��ָ���ȼ�����ʱ���Զ������������
    public IEnumerator autoChangeLockEnemy()
    {
        Debug.Log("��ǰ�����ȼ�" + lockLevel);
        while (true)
        {
            getFindEnemy();
            if (maxEnemyLevel == 0 || hadFindEnemyList[lockLevel].Count == 0)
            {
                Debug.Log("�����б�Ϊ��");
                yield return new WaitForSeconds(3f);
                continue;
            }
            GameObject lockE = hadFindEnemyList[lockLevel][0].enemy;
            int minDisIndex = 0;

            //�ӵ�һ�����Ա��������ײ�忪ʼѭ��
            while (hadFindEnemyList[lockLevel][minDisIndex].box.Distance(playerBox).isValid == false)
            {
                Debug.Log(hadFindEnemyList[lockLevel][minDisIndex].enemy.name + "���Ա�����");
                minDisIndex++;
            }
            //���ѭ���г����˲��ɼ������ײ�壬������
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

    //��ȡ����������ĽǶ�
    public float getAngleWithEnmey()
    {
        return -Mathf.Atan2(player.transform.position.x - lockEnemy.enemy.transform.position.x, player.transform.position.y - lockEnemy.enemy.transform.position.y) * Mathf.Rad2Deg;
    }
    public void changePurseAttackAngle()
    {
        float angle = getAngleWithEnmey();
        if (angle > 45 && angle < 135)//�ҳ���
        {
            playerTurn(right);
        }
        else if (angle >= 135 || angle <= -135)//�ϳ���
        {
            playerTurn(up);
        }
        else if (angle > -135 && angle < -45)//����
        {
            playerTurn(left);
        }
        else if (angle <= 45 || angle >= -45)//�³���
        {
            playerTurn(down);
        }
    }
    //��׷��
    #region
    //׷��Э��(ִ��ʱ�䣬�ƶ��ٶȣ�ִ�ж���,ִ��ʱ��Ϊ������λ�ƶ���������ʱ�䣬�����֡��������׷��Э���ڵ���֡�������ƶ��ٶȺͼ���ԭ�����ƶ��ٶ���ͬ
    //bug���繥�������ڵ���֡���������ٶȱ��������ڶ�֡λ�ô�����������ʱ׷��Э�̻���ִ�У�����δ׷�����������ǰ��������
    //����������������¼��ڵĺ�����������ִ�У������ٶȵ����Ӻͼ���Ӱ��Э��ִ��ʱ��
    //������׷�����ƶ�Э��ִ��ʱ���ֵڶ���׷�����ƶ�Э��ʱ�������һ��Э��,����ִ���³��ֵ�Э��
    IEnumerator purseAttack(int exeTime, float exeSpeed, GameObject exeObject)
    {
        int time = exeTime;
        float angle = getAngleWithEnmey();
        Vector2 dir = (lockEnemy.enemy.transform.position - transform.position).normalized;
        /*float xSpeed = (float)Math.Sin(angle / (180 / Math.PI));
        float ySpeed = -(float)Math.Cos(angle / (180 / Math.PI));*/
        Rigidbody2D exeObjectBody = exeObject.GetComponent<Rigidbody2D>();
        //������������
        if (angle > 45 && angle < 135)//�ҳ���
        {
            playerTurn(right);
        }
        else if (angle >= 135 || angle <= -135)//�ϳ���
        {
            playerTurn(up);
        }
        else if (angle > -135 && angle < -45)//����
        {
            playerTurn(left);
        }
        else if (angle <= 45 || angle >= -45)//�³���
        {
            playerTurn(down);
        }

        //��ʼ׷��
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
    //��׷��Э��
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

    //��ɫ�ƶ������Խ���б���ƶ�,����ɫ�޸ĳ���ʱ��ͬʱ�޸ļ��ܳ�����ɣ�
    //��ȡ��ɫ�ƶ���صĶ������
    //����ɫ�ƶ�ʱ������Ұ��°���ʱ����¼���б�,������ɿ�ĳ������ʱ�����ð������б���ɾ��,������ʾ�����򶯻�(���)
    //���򶯻��л�������ҳ������ı�ʱ����isChangToward��Ϊtrue,�������ɿ������б��е�һ������������Ϊ���������
    //������TowardΪ0����Down������2����Up��������0��1����Right���������������ת����ʱ�����ת����ʱ�����ı������򶯻�����������ת��
    //�ƶ����ȼ�����Ϊ���Ƽ����ƶ�����ͬ�ȼ�֮��ֱ�Ӹ���ԭ�����٣������Ƽ�����ʱ�޸ĵ�ǰ���ȼ�Ϊ���Ƽ��������ƶ������ڼ��ƶ������ڿ��Ƶ��ٶ������Ӽ���
    //���Ƽ�����0.1����޸ĵ�ǰ���ȼ�Ϊ�ƶ���
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
    public int controlLevel = 1;//���Ƽ�
    public int moveLevel = 0;//�ƶ���
    public int curMoveLevel = 0;//��ǰ�ȼ�
    public Coroutine recoverMoveLevelCoro;
    #endregion
    //���峣��,��ʱ��
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
        controlMove(new Vector2((moveSpeed * moveH), playerBody.velocity.y), moveLevel);//��������һ�������ٶ�Ϊ�㣬����б���ƶ�
        ani.SetBool("Walk", true);
    }
    void moveV()
    {
        float moveV = Input.GetAxis("Vertical");
        controlMove(new Vector2(playerBody.velocity.x, (moveSpeed * moveV)), moveLevel);//��������һ�������ٶ�Ϊ�㣬����б���ƶ�
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
    //��ɫ�������������¼
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
        //��֤����Ѿ����·�������������ı䷽���ɿ���һ�ΰ��µķ�����Ż�ı�
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
    //��ɫ�����޸ĺ���
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
        Debug.Log("�ò����ǲ��������");
    }

    //�л���ɫ����
    //�л�����״̬���еĶ���
    //������ر�����ʼ��
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
