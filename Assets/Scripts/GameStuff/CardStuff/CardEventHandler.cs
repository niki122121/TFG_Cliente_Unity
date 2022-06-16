using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardEventHandler : MonoBehaviour
{
    public static CardEventHandler CEH;
    private void Awake()
    {
        //game handler is singleton, if for whatever reason more than 1 is found its is deleted
        if (CardEventHandler.CEH == null)
        {
            CardEventHandler.CEH = this;
        }
        else if (CardEventHandler.CEH != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
    }
    void Start()
    {
        
    }

    //CARD HAND EVENTS
    public void OnCardDrawStart()
    {
        //Debug.LogError("CARD DRAW START");
    }
    public void OnCardDrawEnd()
    {
        //Debug.LogError("CARD DRAW END");
    }
    public void OnCardHandMouseOverStart()
    {
        //Debug.LogError("CARD HAND M OVER START");
    }
    public void OnCardHandMouseOverEnd()
    {
        //Debug.LogError("CARD HAND M OVER END");
    }
    public void OnCardHandDragStart()
    {
        //Debug.LogError("CARD HAND DRAG START");
    }
    public void OnCardHandDragEnd()
    {
        //Debug.LogError("CARD HAND DRAG END");
    }
    public void OnCardHandBack()
    {
        //Debug.LogError("CARD HAND BACK");
    }
    public void OnCardHandSummon()
    {
        //Debug.LogError("CARD HAND SUMMON");
    }

    //CARD FIELD EVENTS
    public void OnCardFieldSummon()
    {
        //Debug.LogError("CARD FIELD SUMMON");
    }
    public void OnCardFieldMouseOverStart()
    {
        //Debug.LogError("CARD FIELD M OVER START");
    }
    public void OnCardFieldMouseOverEnd()
    {
        //Debug.LogError("CARD FIELD M OVER END");
    }
    public void OnCardFieldClickStart()
    {
        //Debug.LogError("CARD FIELD CLICK START");
    }
    public void OnCardFieldClickEnd()
    {
        Debug.LogError("CARD FIELD CLICK END");
    }
    public void OnCardFieldClickOver()
    {
        Debug.LogError("CARD FIELD CLICK OVER");
    }

    //COMBAT EVENTS
    public void OnCardCombatStart()
    {
        //Debug.LogError("CARD COMBAT START");
    }
    public void OnCardCombatEnd()
    {
        //Debug.LogError("CARD COMBAT END");
    }
    public void OnCardInflictDamage()
    {
        //Debug.LogError("CARD INFLICT DMG");
    }
    public void OnCardTakeDamage()
    {
        //Debug.LogError("CARD TAKE DMG");
    }
    public void OnCardDestroy()
    {
        Debug.LogError("CARD DESTROYED");
    }
}
