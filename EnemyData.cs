using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyData : MonoBehaviour
{
    /// <summary>
    /// Ѫ��������ͷ����Ѫ��UI
    /// </summary>

    public GameObject healthBar;//����ʾ��
    public GameObject healthBarBG;//Ѫ���ı���
    public GameObject healthSign;//ֱ����ʾѪ��Ԥ��λ�ã�����۲�
    public GameObject lockSign;//������ʱ��ʾ�ı�־
    public TextMeshProUGUI nameBar;
    public Coroutine healthBarMoveCoro;
    public Animator vfxAni;
    public List<GameObject> playerListObject;
    public EnemyAI AIjson;
    public EnemyData DataJson;
    public (float, float) Bodysize;
    public BoxCollider2D body;
    public float xSize;
    public float ySize;
    public bool isBeLock;
    public playerControl lockPlayer;

    public SpriteRenderer sp;


    private void Awake()
    {
        enemyValue[hea] = baseEnemyValue[hea];
        healthBar = GameObject.Find(name + "/ShowData/healthBar");
        nameBar = GameObject.Find(name + "/ShowData/nameBar").GetComponent<TextMeshProUGUI>();
        healthBarBG = GameObject.Find(name + "/ShowData");
        lockSign = GameObject.Find(name + "/ShowData/lockSign");
        playerListObject = new List<GameObject>();
        body = GameObject.Find(name + "/Body").GetComponent<BoxCollider2D>();
        xSize = body.size.x;
        ySize = body.size.y;
        AIjson = GetComponent<EnemyAI>();
        isBeLock = false;
        awakeVfx();
        
    }
    // Start is called before the first frame update
    private void Start()
    {
        healthBarBG.GetComponent<CanvasGroup>().alpha = 0.5f;
        lockSign.SetActive(false);
        sceneAwake(0);
        //StartCoroutine(test());
    }
    public void showDetailData(playerControl p)
    {
        healthBarBG.GetComponent<CanvasGroup>().alpha = 1.0f;
        nameBar.text = name;
        lockSign.SetActive(true);
        isBeLock = true;
        lockPlayer = p;
    }
    public void hideDetailData()
    {
        isBeLock=false;
        healthBarBG.GetComponent<CanvasGroup>().alpha = 0.5f;
        nameBar.text = "";
        lockSign.SetActive(false);
        lockPlayer = null;
    }
    /// <summary>
    /// Ѫ���䶯,�������
    /// </summary>
    #region
    private void startHealthBarMoveBar()
    {
        if (healthBarMoveCoro != null)
        {
            StopCoroutine(healthBarMoveCoro);
            healthBarMoveCoro = null;
        }
        healthBarMoveCoro = StartCoroutine(HealthBarMove());
    }
    IEnumerator HealthBarMove()
    {
        float barSize = enemyValue[hea] / baseEnemyValue[hea];

        Vector3 newSize = new Vector3(barSize, 1, 1);
        while (healthBar.transform.localScale.x != barSize)
        {
            healthBar.transform.localScale = Vector3.Lerp(healthBar.transform.localScale, newSize, 0.3f);
            yield return 0;
        }
        yield break;
    }
    public void die()
    {
        if (isBeLock)
        {
            lockPlayer.lockEnemyDied();
        }
        StopAllCoroutines();
        AIjson.StopAllCoroutines();

        new WaitForSeconds(3);
        Destroy(gameObject);
    }
    #endregion

    /// <summary>
    /// ����buffϵͳ����ú�ɾ��buffֱ�ӼӼ���������ֵx����
    /// buf����
    /// ����Ļ������������ﶨ��,�ڹ�����Խű��и�ֵ
    /// </summary>
    #region
    public List<Coroutine> buffList;
    public List<Coroutine> debuffList;
    public Animator buffAni;//����buff�����Ķ�����
    public int[] baseEnemyValue = new int[5];
    public float[] enemyValue = new float[5];
    public int hea = 0;
    public int pow = 1;
    public int def = 2;
    public int speed = 3;
    public int injuryFree = 4;//����

    public void getBuff(float time, int buffType, float value)
    {
        if (value < 0)
        {
            debuffList.Add(StartCoroutine(startBuff(time, buffType, value)));
        }
        else
        {
            buffList.Add(StartCoroutine(startBuff(time, buffType, value)));
        }
    }
    public IEnumerator startBuff(float time, int buffType, float value)
    {
        enemyValue[buffType] += value * baseEnemyValue[buffType];
        yield return new WaitForSeconds(time);
        enemyValue[buffType] -= value * baseEnemyValue[buffType];
        yield break;
    }
    #endregion

    /// <summary>
    /// ��Ч���,�����ͱ�������Ч
    /// �Է�Χ�����maxNum�ĵ�������˺������Ŵ������Ч
    /// 
    /// </summary>
    #region
    public delegate int vfxObject(int j);
    public List<vfxObject> vfxList;
    public float maxRedStrength;
    public Coroutine redCoro;
    public Color rawColor;
    public void awakeVfx()
    {
        vfxList = new List<vfxObject>();
        sp = GetComponent<SpriteRenderer>();
        vfxList.Add(PhyVfx);
        maxRedStrength = 0;
        rawColor = sp.color;
    }
    public Vector3 getVfxPos()
    {
        return new Vector3(UnityEngine.Random.Range(-xSize, xSize)+transform.position.x, UnityEngine.Random.Range(-ySize, ySize)+transform.position.y);
    }
    public int PhyVfx(int layer)
    {
        GameObject obj = Resources.Load<GameObject>("vfx/attackVfxPhy");
        GameObject a=Instantiate(obj, getVfxPos(), Quaternion.identity);
        a.GetComponent<SpriteRenderer>().sortingOrder = layer;
        return 0;
    }
    public void NumVfx(float num,vfxObject v,int layer)
    {
        GameObject obj = Resources.Load<GameObject>("vfx/damageNumVfx");
        GameObject a = Instantiate(obj, getVfxPos(), Quaternion.identity);
        a.GetComponent<TextMeshProUGUI>().text = num.ToString();
    }
    public void takeDamage(float strength, Collider2D box)
    {
        playerListObject.Clear();
        playerListObject = AIjson.getPlayerList(box, 4);
        for (int i = 0; i < playerListObject.Count; i++)
        {
            playerListObject[i].GetComponent<playerControl>().beTakeDamage(strength * enemyValue[0],0);
        }
    }
    public void beTakeDamge(float damage, int vfxIndex)
    {
        enemyValue[hea] -= damage;
        if (enemyValue[hea] > baseEnemyValue[hea])
        {
            enemyValue[hea] = baseEnemyValue[hea];
        }
        else if (enemyValue[hea] <= 0)
        {
            enemyValue[hea] = 0;
            die();
        }
        float s = damage / baseEnemyValue[hea];
        Debug.Log(name + "�ܵ�" + damage + "�˺�" + s + "�ٷֱ�");
        startHealthBarMoveBar();
        startRed(s);
        NumVfx(damage, vfxList[vfxIndex],curLayer+1);
        vfxList[vfxIndex](curLayer+1);
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
        while (time>0)
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
        changeSceneLayer(layer+1);
        spList = new List<SpriteRenderer>();
    }
    public void changeSceneLayer(int layer)//��������ͼ�㣬�Ѿ���һ
    {
        curLayer = layer;
        transform.position = new Vector3(transform.position.x, transform.position.y, layer);
        sp.sortingOrder = layer;
        SpriteRenderer[] sp2 = GetComponentsInChildren<SpriteRenderer>();
        Debug.Log(sp2.Count());
        for(int i=1;i<sp2.Count();i++)
        {
            sp2[i].sortingOrder = layer + 1;
        }
    }
    public void playerLeaveLayer()
    {
        Debug.Log(name + "ֹͣ����");
    }
    #endregion
}
