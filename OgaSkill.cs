using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OgaSkill : MonoBehaviour
{
    EnemyAI AIjson=null;
    EnemyData data=null;
    testAI oga = null;
    void Start()
    {
        AIjson = GetComponentInParent<EnemyAI>();
        data = GetComponentInParent<EnemyData>();
        oga=GetComponentInParent<testAI>();
    }
    public void StopKeepingAni()
    {
        oga.skill2VfxAni.SetInteger("Skill", 0);
    }
    public void StopAni()
    {
        oga.skill1VfxAni.SetInteger("Skill", 0);
    }
    public void Skill2()//Åç»ð
    {
        data.takeDamage(3, oga.skill2Box);
        oga.skill2Box.offset = new Vector2(oga.skill2Box.offset.x - 0.05f, oga.skill2Box.offset.y - 0.03f);
        oga.skill2Box.size = new Vector2(oga.skill2Box.size.x + 0.04f, oga.skill2Box.size.y+0.02f);
    }
    public void Skill1()//Å­ºð
    {
        data.playerListObject.Clear();
        data.playerListObject=AIjson.getPlayerList(oga.actions2[1].attackBox,4);
        for (int i = 0; i < data.playerListObject.Count; i++)
        {
            Debug.Log("·¢ÏÖÍæ¼Ò"+ data.playerListObject[i].name);
            data.playerListObject[i].GetComponent<playerControl>().controlMove((data.playerListObject[i].transform.position - oga.transform.position).normalized * 18f, 1);
        }
    }
}
