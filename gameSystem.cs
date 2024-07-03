using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class gameSystem : MonoBehaviour
{
    public int curLayer;//当前结构层数

    public Image screenImg;
    public void Awake()
    {
        screenImg = GameObject.Find("GameUI/screenVfx").GetComponent<Image>();
    }
    public void Start()
    {

    }
}
