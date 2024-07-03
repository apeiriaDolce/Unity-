using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class cameraControl : MonoBehaviour
{
    public static cameraControl Instance;
    public float width;
    public float height;
    public float left;
    public float top;
    public float right;
    public float bottom;
    public Vector3 playerPosX;
    public Vector3 playerPosY;
    public float moveSpeedX;
    public float moveSpeedY;
    public Camera thisCamera;
    public bool isFollowPlayer;
    public Coroutine followCoro;
    public GameObject player;
    public Vector3 pos;
    public GameObject backGournd;
    public Coroutine alterSizeCoro;
    float xSize;

    public Transform gameUIPos;

    //进入一个新的场景时获取背景的宽和高
    private void Awake()
    {
        xSize = 1.8f;
        Instance = this;
        thisCamera = GetComponent<Camera>();
        player = GameObject.FindGameObjectWithTag("Player");

        //获取背景的宽和高

        gameUIPos = GameObject.Find("GameUI").GetComponent<Transform>();
        DontDestroyOnLoad(gameObject);
        getSceneSize(GameObject.Find("MapScene/Layer0"));
    }
    // 在Awake之后调用，当物体被屏蔽之后再次激活时也不会再执行，具体啥时候执行我也不知道啊
    void Start()
    {
        playerPosX = playerControl.Instance.player.gameObject.transform.position;
        playerPosY = playerControl.Instance.player.gameObject.transform.position;
        moveSpeedX = 1f;
        moveSpeedY = 0.01f;
        followCoro = StartCoroutine(followPlayer());
    }

    public void reGetSceneSize(float pov)
    {
        left = backGournd.transform.position.x - width / 2 + pov * xSize;
        top = backGournd.transform.position.y + height / 2 - pov;
        right = backGournd.transform.position.x + width / 2 - pov * xSize;
        bottom = backGournd.transform.position.y - height / 2 + pov;
        isFollowPlayer = true;
    }
    public void getSceneSize(GameObject a)
    {
        backGournd = a;
        SpriteRenderer sp = a.transform.Find("map").GetComponent<SpriteRenderer>();
        width = sp.bounds.size.x;
        height = sp.bounds.size.y;
        left = a.transform.position.x - width / 2 + thisCamera.orthographicSize * xSize;
        top = a.transform.position.y + height / 2 - thisCamera.orthographicSize;
        right= a.transform.position.x + width / 2 - thisCamera.orthographicSize * xSize;
        bottom = a.transform.position.y - height / 2 + thisCamera.orthographicSize;
        isFollowPlayer = true;
    }
    //默认所有背景的中心坐标都是（0,0）
    void limit()
    {
            if (transform.position.x > right)
            {
                transform.position = new Vector3(right, transform.position.y, transform.position.z);
            }
            else if (transform.position.x < left)
            {
                transform.position = new Vector3(left, transform.position.y, transform.position.z);
            }
            if (transform.position.y > top)
            {
                transform.position = new Vector3(transform.position.x, top, transform.position.z);
            }
            else if (transform.position.y < bottom)
            {
                transform.position = new Vector3(transform.position.x, bottom, transform.position.z);
            }
    }
    IEnumerator followPlayer()
    {
        while (true)
        {
            pos = new Vector3(player.transform.position.x, player.transform.position.y, -10);
            transform.position = Vector3.Lerp(transform.position, pos, moveSpeedX);
            limit();
            yield return 0;
        }
    }
    void gameUIFollow()
    {
        gameUIPos.position = transform.position;
    }
    public void startAlterCameraSize(float newSize)//只有当游戏内没有同类协程运行时才能生效
    {
        if (alterSizeCoro != null)
        {
            StopCoroutine(alterSizeCoro);
        }
        StartCoroutine(alterCameraSize(newSize));
    }
    IEnumerator alterCameraSize(float s)
    {
        float scale= transform.localScale.x;
        float newScale = s / 5.41f;
        float pov = thisCamera.orthographicSize;
        Debug.Log(newScale);
        Debug.Log(pov);
        if(newScale > scale)
        {
            while (newScale > scale)//放大
            {
                scale += 0.05f;
                pov += 0.25f;
                thisCamera.orthographicSize = pov;
                transform.localScale = new Vector3(scale, scale, scale);
                yield return new WaitForSeconds(0.015f);
            }
        }
        else
        {
            reGetSceneSize(s);
            while (newScale < scale)//缩小
            {
                scale -= 0.05f;
                pov -= 0.25f;
                thisCamera.orthographicSize = pov;
                transform.localScale = new Vector3(scale, scale, scale);
                yield return new WaitForSeconds(0.015f);
            }
        }
        transform.localScale=new Vector3(newScale, newScale, newScale);
        thisCamera.orthographicSize = s;
        reGetSceneSize(thisCamera.orthographicSize);
        yield break;
    }
}
