using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardHand : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Card Info")]
    public string cardName = "asd";
    public uint cardID = 1;
    public uint cardSubID = 0;
    public int hp = 0;
    public int atk = 0;
    public uint cardType = 0;
    public uint cost = 0;
    public uint faction = 0;
    public int specialParam = 0;

    [Header("Card Fixed Info")]
    public int deckRef = -1;

    Vector2 startCardPos;

    public void OnPointerEnter(PointerEventData eventData)
    {
        CardEventHandler.CEH.OnCardHandMouseOverStart();
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        CardEventHandler.CEH.OnCardHandMouseOverEnd();
    }

    //https://docs.unity3d.com/Packages/com.unity.ugui@1.0/api/UnityEngine.EventSystems.IDragHandler.html
    public void OnBeginDrag(PointerEventData eventData)
    {
        CardEventHandler.CEH.OnCardHandDragStart();
        startCardPos = transform.position;
        //diff = new Vector2(transform.position.x, transform.position.y) - eventData.position;
    }
    public void OnDrag(PointerEventData eventData) {
        transform.position = eventData.position/* + diff*/;
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        CardEventHandler.CEH.OnCardHandDragEnd();
        GameHandler.GH.summonCardLocal(this, eventData.position /*+ diff*/, cardID, transform.parent, eventData.pointerCurrentRaycast);
    }
    public void returnToStart()
    {
        transform.position = startCardPos;
        CardEventHandler.CEH.OnCardHandBack();
    }

    public void succesSummon()
    {
        CardEventHandler.CEH.OnCardHandSummon();
        Destroy(gameObject);
    }

    public void assignVariables(uint id, uint subid, int hp, int atck, uint cardtype, uint cost, uint faction, int specialParam, int deckRef)
    {
        this.cardID = id;
        this.cardSubID = subid;
        this.hp = hp;
        this.atk = atck;
        this.cardType = cardtype;
        this.cost = cost;
        this.faction = faction;
        this.specialParam = specialParam;
        this.deckRef = deckRef;
    }
    public void assignVariables(CardField cF)
    {
        this.cardID = cF.cardID;
        this.cardSubID = cF.cardSubID;
        this.hp = cF.hp;
        this.atk = cF.atk;
        this.cardType = cF.cardType;
        this.cost = cF.cost;
        this.faction = cF.faction;
        this.specialParam = cF.specialParam;
        this.deckRef = cF.deckRef;
    }
}
