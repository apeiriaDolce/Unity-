using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.Diagnostics;

public class sceneSystem : MonoBehaviour
{
    public gameSystem gs;
    public Collider2D playerBox;
    public GameObject player;
    cameraControl cam;
    public List<Vector3> spawnPos;
    public bool isAwake;
    public struct layer
    {
        float time;//�ϴ�����˳���ʱ��
    }
    //������ʼ��
    private void Awake()
    {
        isAwake = true;
        updateMethodList = new List<updateLayer>();
        stopLayerList=new List<stopLayer>();
        awakeLayer();
        playerBox=GameObject.FindGameObjectWithTag("Player").GetComponent<BoxCollider2D>();
        player = GameObject.FindGameObjectWithTag("Player");
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<cameraControl>();
        gs=GameObject.Find("GameSystem").GetComponent<gameSystem>();
        spawnPos=new List<Vector3>();
    }
    //��������ʱִ��
    public void Start()
    {
        isAwake = false;
        updateLayerAll();
        
    }
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && first == false)
        {
            isTp = true;
        }
    }
    ///<summary>
    ///ϵͳ��Ҫ�������
    ///��������ڵ�ǰ����ṹ����������
    ///����뿪����ʱ����ó�����Ϣ
    ///</summary>
    #region
    public void spawn(GameObject a, Vector3 pos)
    {
        GameObject b=Instantiate(a,pos, Quaternion.identity, curLayer.transform.Find("StopObject/Enemy"));
        b.GetComponent<EnemyData>().sceneAwake(curLayerNum);
    }
    public void saveSceneData()
    {
        Debug.Log("����" + name + "��Ϣ");
    }
    #endregion
    ///<summary>
    ///�ṹ���
    ///���ӽṹ�壺Ŀ�Ľṹ��Դ�ṹ,����
    ///�ڲ�ṹ�л������վ�ڸò����ӽṹ��ʱ����͸��Ԥ��������ո��л�����ڲ�ṹ��ɽ�������ݵȡ�
    ///�����ڲ�ṹ����Ϊ�뿪��ǰ�ṹ�㣬�ⲿ�ṹ��Ȼ�������У��������Ϊ�������Χ
    ///</summary>
    #region
    public List<GameObject> layerList;
    public List<GameObject> beStopObjectList;
    public List<updateLayer> updateMethodList;
    public List<stopLayer> stopLayerList;
    public List<Coroutine> linkCoro;//��ǰ����ִ�е����ӽṹ���˳���ǰ���ȫ��ֹͣ����
    public List<List<GameObject>> innerList;
    public List<List<Link>> linkList;
    public int maxLayer;
    public GameObject curLayer;
    public int curLayerNum;

    public Coroutine screenVfxCoro;

    public bool first;
    public bool isTp;

    public delegate int updateLayer(int layer);//�������������Ϣ����ʱ���
    public delegate int stopLayer(int layer);
    public Coroutine preView(GameObject a)//վ�����ӽṹ�Ͽ���Ԥ��Ҫ����Ľṹ���������ӽṹͨ��
    {
        return StartCoroutine(setLayerAlpha(a, 0.25f));
    }
    public void stopPreView(GameObject a)//������ͣЭ�̣������������ṹ�ı仯��ͣ
    {
        StartCoroutine(setLayerAlpha(a, 0));
    }
    public IEnumerator setLayerAlpha(GameObject a,float e)
    {
        SpriteRenderer[] sp2 = a.GetComponentsInChildren<SpriteRenderer>();
        for (int i = 0; i < sp2.Count(); i++)
        {
            sp2[i].color = new Color(sp2[i].color.r, sp2[i].color.g, sp2[i].color.b, e);
        }
        Debug.Log(a + "͸����Ϊ" + e);
        yield break;
        #region
        /*float o = sp2[0].color.a;
        if (o > e)//�䵭
        {
            float al = o;
            while (al > e)
            {
                al -= 0.1f;
                for (int i = 0; i < sp2.Count(); i++)
                {
                    sp2[i].color = new Color(sp2[i].color.r, sp2[i].color.g, sp2[i].color.b, al);
                }
                yield return new WaitForSeconds(0.05f);
            }
            yield break;
        }
        if (o < e)//������
        {
            float al = o;
            while (al < e)
            {
                al += 0.1f;
                for (int i = 0; i < sp2.Count(); i++)
                {
                    sp2[i].color = new Color(sp2[i].color.r, sp2[i].color.g, sp2[i].color.b, al);
                }
                yield return new WaitForSeconds(0.05f);
            }
            yield break;
        }
        yield break;*/
        #endregion
    }

    public delegate int intoLayer(GameObject a, GameObject b);//���뷽��

    public struct Link
    {
        public GameObject a;
        public GameObject b;
        public BoxCollider2D box;
        public updateLayer intoA;
        public updateLayer intoB;
        public stopLayer stopA;
        public stopLayer stopB;
        public bool isA;//����״̬����A��������B����
        public Vector2 posA;
        public Vector2 posB;
    }

    public void changeLayer(int i)
    {
        updateMethodList[i](0);
    }
    public void awakeLayer()
    {
        int i = 0;
        curLayerNum = 0;
        layerList = new List<GameObject>();
        layerList.Add(GameObject.Find("MapScene/Layer" + i));
        beStopObjectList = new List<GameObject>();
        beStopObjectList.Add(GameObject.Find("MapScene/Layer/StopObject" + i));
        while (layerList[i] != null)
        {
            i++;
            layerList.Add(GameObject.Find("MapScene/Layer" + i));
            beStopObjectList.Add(GameObject.Find("MapScene/Layer/StopObject" + i));
        }
        maxLayer = i;
    }
    public void updateLayerAll()
    {
        for(int i = 0;i<updateMethodList.Count;i++)
        {
            updateMethodList[i](0);

        }

    }
    public void activeEnemy(GameObject a)
    {
        EnemyAI[] list = a.GetComponentsInChildren<EnemyAI>();
        int l = list.Length;
        for(int i = 0; i < l; i++)
        {
            if (list[i].isAwake == true)
            {
                list[i].activeSelf();
            }
            else
            {
                Destroy(list[i].gameObject);
            }
        }
    }
    public void startScreenVfx()
    {
        if (screenVfxCoro!=null)
        {
            StopCoroutine(screenVfxCoro);
        }
        screenVfxCoro = StartCoroutine(changeLayerVfx());
    }
    public IEnumerator changeLayerVfx()
    {
        Color a=gs.screenImg.color;
        float ap = 0.5f;
        a.a = ap;
        gs.screenImg.color = a;
        while(a.a>=0)
        {
            ap -= 0.1f;
            a.a = ap;
            gs.screenImg.color = a;
            yield return new WaitForSeconds(0.1f);
        }
        yield break;
    }
    //���ӽṹЭ�̣����ֽṹͨ��
    public IEnumerator linkStart(Link l)
    {
        first=true;
        Coroutine showCoro = null;
        while (true)
        {
            if (l.box.IsTouching(playerBox))
            {
                if (first)
                { 
                    first = false;
                    if (l.isA)
                    {
                        showCoro=preView(l.b);
                    }
                    else
                    {
                        showCoro = preView(l.a);
                    }
                }
                if (isTp)
                {
                    if (l.isA)
                    {
                        isTp = false;
                        l.stopA(0);
                        l.intoB(0);
                        cam.getSceneSize(l.b);
                        l.isA = false;
                        //StopCoroutine(showCoro);
                        player.transform.position = l.posB;
                        l.box.gameObject.transform.position = l.posB;
                        l.box.gameObject.transform.localRotation=new Quaternion(0,0,0,0);
                        startScreenVfx();
                    }
                    else
                    {
                        isTp = false;
                        l.stopB(0);
                        l.intoA(0);
                        cam.getSceneSize(l.a);
                        l.isA = true;
                        //StopCoroutine(showCoro);
                        player.transform.position = l.posA;
                        l.box.gameObject.transform.position = l.posA;
                        l.box.gameObject.transform.localRotation = new Quaternion(0, 0, 180, 0);
                        startScreenVfx();
                    }
                }
            }
            else
            {
                if (first == false) 
                {
                    if (l.isA)
                    {
                        stopPreView(l.b);
                        //StopCoroutine(showCoro);
                    }
                    else
                    {
                        stopPreView(l.a);
                        //StopCoroutine(showCoro);
                    }
                }
                first = true;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    #endregion
}
