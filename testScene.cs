using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class testScene : MonoBehaviour
{
    public sceneSystem systemJson;
    sceneSystem.Link testTo0;
    cameraControl cam;
    
    private void Awake()
    {
        testTo0 = new sceneSystem.Link();
        systemJson = GetComponent<sceneSystem>();
        cam=GameObject.FindGameObjectWithTag("MainCamera").GetComponent<cameraControl>();
        
        //systemJson.updateMethodList.Add(updateLayer0);
        //systemJson.updateMethodList.Add(updateLayer1);
        //systemJson.stopLayerList.Add(stopLayer0);
        linkAwake();
    }
    private void Start()
    {
        StartCoroutine(systemJson.linkStart(testTo0));
        updateLayer0(0);
        spwanOga();
    }
    ///<summary>
    ///初始化
    ///</summary>
    #region
    public void linkAwake()
    {
        systemJson.linkList = new List<List<sceneSystem.Link>>();
        systemJson.innerList=new List<List<GameObject>>();

        //第一层连接和内层结构初始化
        List<sceneSystem.Link> linkList0 = new List<sceneSystem.Link>();
        List<GameObject> innerList0=new List<GameObject>();

        testTo0 = new sceneSystem.Link();
        testTo0.a = GameObject.Find("MapScene/InnerLayer/Test");
        testTo0.b = GameObject.Find("MapScene/Layer0");
        testTo0.box = GameObject.Find("MapScene/Link/test").GetComponent<BoxCollider2D>();
        testTo0.intoA = updateInner0;
        testTo0.intoB = updateLayer0;
        testTo0.stopA = stopInner0;
        testTo0.stopB = stopLayer0;
        testTo0.isA = false;
        testTo0.posB = new Vector3(21.56f, 17.43f, 0);
        testTo0.posA = new Vector3(21.56f, 19.18f,0);
        innerList0.Add(GameObject.Find("MapScene/InnerLayer/Test"));

        linkList0.Add(testTo0);
        systemJson.linkList.Add(linkList0);
        systemJson.innerList.Add(innerList0);

        //第一层结构初始化结束
    }
    #endregion

    ///<summary>
    ///结构相关
    ///结构层激活
    ///生物生成
    /// </summary>
    #region

    public void spwanOga()
    {
        GameObject a = Resources.Load<GameObject>("EnemyObject/Oga");
        systemJson.spawn(a, Vector3.zero);
    }
    public int updateLayer0(int layer)
    {
        Debug.Log("激活Layer0");
        systemJson.layerList[0].transform.Find("StopObject").gameObject.SetActive(true);
        systemJson.activeEnemy(systemJson.layerList[0].transform.Find("StopObject/Enemy").gameObject);
        systemJson.curLayer = systemJson.layerList[0];
        StartCoroutine(systemJson.setLayerAlpha(systemJson.layerList[0], 1));
        if (systemJson.isAwake == false)
        {
            cam.startAlterCameraSize(10.82f);
        }
        return 0;
    }
    public int updateInner0(int layer)
    {
        Debug.Log("激活Test");
        systemJson.innerList[0][0].transform.Find("StopObject").gameObject.SetActive(true);
        systemJson.activeEnemy(systemJson.innerList[0][0].transform.Find("StopObject/Enemy").gameObject);
        systemJson.curLayer = systemJson.innerList[0][0];
        StartCoroutine(systemJson.setLayerAlpha(systemJson.innerList[0][0], 1));
        if (systemJson.isAwake == false)
        {
            cam.startAlterCameraSize(5.41f);
        }
        return 0;
    }
    public int stopLayer0(int layer)
    {
        Debug.Log("暂停Layer0");
        StartCoroutine(systemJson.setLayerAlpha(systemJson.layerList[0], 0));
        systemJson.layerList[0].transform.Find("StopObject").gameObject.SetActive(false);
        return 0;
    }
    public int stopInner0(int layer)
    {
        Debug.Log("暂停Test");
        StartCoroutine(systemJson.setLayerAlpha(systemJson.innerList[0][0], 0));
        systemJson.innerList[0][0].transform.Find("StopObject").gameObject.SetActive(false);
        return 0;
    }
    public int stopLayer1(int layer)
    {
        return 0;
    }
    #endregion
}
