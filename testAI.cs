using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class testAI : MonoBehaviour
{
    public List<EnemyAI.EnemyAction> actions2 = new List<EnemyAI.EnemyAction>();
    public EnemyAI.EnemyAction action;
    public static testAI oga;
    public EnemyAI AIjson;
    public EnemyData DataJson;
    public GameObject skill2VfxGameObject;
    public Animator skill2VfxAni;
    public CapsuleCollider2D skill2Box;
    public GameObject skill1VfxGameObject;
    public Animator skill1VfxAni;
    public CircleCollider2D skill1Box;
    public GameObject self;

    private void Awake()
    {
        oga = this;
        self = this.gameObject;

        skill2VfxGameObject = self.transform.Find("Skill2Vfx").gameObject;
        skill2VfxAni = skill2VfxGameObject.GetComponent<Animator>();
        skill2Box = skill2VfxGameObject.GetComponent<CapsuleCollider2D>();
        skill1VfxGameObject = self.transform.Find("Skill1Vfx").gameObject;


        skill1VfxAni = skill1VfxGameObject.GetComponent<Animator>();

        skill1Box = skill1VfxGameObject.GetComponent<CircleCollider2D>();

        AIjson = GetComponent<EnemyAI>();
        DataJson = GetComponent<EnemyData>();
        awakeAction();

        //enemyBody = GetComponent<Rigidbody2D>();

        DataJson.baseEnemyValue[0] = 100;
        DataJson.baseEnemyValue[1] = 2;
        DataJson.baseEnemyValue[2] = 3;
        DataJson.baseEnemyValue[3] = 10;
        DataJson.enemyValue[0] = DataJson.baseEnemyValue[0];
        DataJson.enemyValue[1] = DataJson.baseEnemyValue[1];
        DataJson.enemyValue[2] = DataJson.baseEnemyValue[2];
        DataJson.enemyValue[3] = DataJson.baseEnemyValue[3];
    }
    private void Start()
    {
        AIjson.AIawake(actions2);
       
        AIjson.isAwake=true;
        //StartCoroutine(test());
    }
    private void awakeAction()
    {
        DataJson.playerListObject = new List<GameObject>();

        EnemyAI.EnemyAction action =new EnemyAI.EnemyAction();
        action.method = new EnemyAI.enemyAction(Action1);
        action.baseJudgeValue = 3;
        action.actionBox = self.transform.Find("Skill2Vfx/Action1").gameObject.GetComponent<BoxCollider2D>();
        action.attackBox = skill2Box;
        actions2.Add(action);

        EnemyAI.EnemyAction action2 = new EnemyAI.EnemyAction();
        action2.method = new EnemyAI.enemyAction(Action2);
        action2.baseJudgeValue = 4;
        action2.actionBox = null;
        action2.attackBox = skill1Box;
        actions2.Add (action2);
    }
    public void action1Vfx()
    {
        skill2VfxAni.SetInteger("Skill", 2);
        setVfx();
    }
    public void action2Vfx()
    {
        skill1VfxAni.SetInteger("Skill", 1);
    }
    //行为1，大喷火
    public int Action1(int index)
    {
        //if (AIjson.changeAction(AIjson.actions[0].actionBox) == true) { return 0; }
        AIjson.ani.SetInteger("Skill", 2);
        actions2[1].baseJudgeValue = 4;
        actions2[0].baseJudgeValue = 1;
        Debug.Log("行为1:大喷火");
        return 0;
    }
    //行为2，怒吼，对周围玩家造成持续击退
    public int Action2(int index)
    {
        AIjson.ani.SetInteger("Skill", 1);
        actions2[1].baseJudgeValue = 1;
        actions2[0].baseJudgeValue = 3;
        Debug.Log("行为2:怒吼");
        return 0;
    }

    public void setVfx()
    {
        skill2Box.offset = new Vector2(0.45f,-0.31f);
        skill2Box.size = new Vector2(0.29f,0.25f);
        if (AIjson.toward == 0)
        {
            skill2VfxGameObject.transform.localPosition = new Vector3(0, 0, 0);
            skill2VfxGameObject.transform.eulerAngles = new Vector3(0, 0, 0);

        }
        else if(AIjson.toward == 1)
        {
            skill2VfxGameObject.transform.localPosition = new Vector3(0.798f, -0.453f, 0);
            skill2VfxGameObject.transform.localRotation = Quaternion.Euler(0, 0, 0);
            skill2VfxGameObject.transform.eulerAngles = new Vector3(0, 0, 90);
        }
        else if (AIjson.toward == 2)
        {
            skill2VfxGameObject.transform.localPosition = new Vector3(0.576f, -0.043f, 0);
            skill2VfxGameObject.transform.eulerAngles = new Vector3(0, 0, 180);
        }
        else if(AIjson.toward == 3)
        {
            skill2VfxGameObject.transform.localPosition = new Vector3(1.6f, 0.33f, 0);
            skill2VfxGameObject.transform.eulerAngles = new Vector3(0, 0, -180);
            skill2VfxGameObject.transform.localRotation = Quaternion.Euler(0, 180, 0);
        }
    }
}
