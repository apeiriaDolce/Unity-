using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gameSystem : MonoBehaviour
{
    public int curLayer;//��ǰ�ṹ����

    public Image screenImg;
    public void Awake()
    {
        screenImg = GameObject.Find("GameUI/screenVfx").GetComponent<Image>();
    }
    public void Start()
    {

    }
}
