using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardField : MonoBehaviour
{
    [Header("Card Info")]
    public string cardName = "asd";
    public uint cardID;
    public uint cardSubID = 0;
    public int hp = 0;
    public int atk = 0;
    public uint cardType = 0;
    public uint cost = 0;
    public uint faction = 0;
    public int specialParam = 0;

    [Header("Card Fixed Info")]
    public int deckRef = -1;
    public Transform parentHand;
    [SerializeField] GameObject cardContainer;
    [SerializeField] SpriteRenderer artRef;
    [SerializeField] SpriteRenderer atkSymbRef;
    [SerializeField] SpriteRenderer hpSymbRef;
    [SerializeField] MeshRenderer atkRef;
    [SerializeField] MeshRenderer hpRef;
    public ParticleSystem onHitPS;
    public ParticleSystem onDeathPS;

    [Header("Card Standart Position")]
    public long stX = 0;
    public long stY = 0;

    [HideInInspector] public CardField closestOponent;
    [HideInInspector] public long opDistSquared = long.MaxValue;
    [HideInInspector] GameObject currentWeapon;

    float weaponSpeed = 0.2f;
    float weaponTurnSpeed = 2f;

    private void OnMouseEnter()
    {
        CardEventHandler.CEH.OnCardFieldMouseOverStart();
    }
    private void OnMouseExit()
    {
        CardEventHandler.CEH.OnCardFieldMouseOverEnd();
    }
    private void OnMouseDown()
    {
        CardEventHandler.CEH.OnCardFieldClickStart();
    }
    private void OnMouseUp()
    {
        CardEventHandler.CEH.OnCardFieldClickEnd();
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

        updateValues();
    }
    public void assignVariables(CardHand cH)
    {
        this.cardID = cH.cardID;
        this.cardSubID = cH.cardSubID;
        this.hp = cH.hp;
        this.atk = cH.atk;
        this.cardType = cH.cardType;
        this.cost = cH.cost;
        this.faction = cH.faction;
        this.specialParam = cH.specialParam;
        this.deckRef = cH.deckRef;

        updateValues();
    }

    //visual function
    void updateValues()
    {
        atkRef.GetComponent<TMP_Text>().SetText(atk+"");
        hpRef.GetComponent<TMP_Text>().SetText(hp + "");
        //FIX cost is also numeric value taht needs uptading
    }

    public void updateHp()
    {
        hpRef.GetComponent<TMP_Text>().SetText(hp + "");
    }

    public void initAttackAnimation(bool playerOwned)
    {
        GameObject weaponPrefab = CardInfoHandle.CH.getCardPrefab(cardID).GetComponent<CardPrefab>().cardWeapon;
        currentWeapon = Instantiate(weaponPrefab, transform.position, weaponPrefab.transform.rotation, GameHandler.GH.cardField);
        currentWeapon.GetComponent<WeaponController>().setPointDir(playerOwned);
        if (closestOponent != null)
        {
            currentWeapon.GetComponent<WeaponController>().setTarget(closestOponent.transform.position, weaponSpeed, weaponTurnSpeed, playerOwned);
        }
    }

    public void beginAttackAnimation()
    {
        currentWeapon.GetComponent<WeaponController>().shoot();

        StartCoroutine(attackAnimation(currentWeapon.GetComponent<WeaponController>().getDistance() / weaponSpeed * Time.fixedDeltaTime));
    }

    IEnumerator attackAnimation(float delay)
    {
        if(closestOponent != null) {
            yield return new WaitForSeconds(delay);
            closestOponent.onCardTakeDmg(atk);
        }
        else
        {
            yield return null;
        }

        Destroy(currentWeapon);
        CardCombatHandler.CCH.fightResolved();
    }

    public void assignStandartPos(long standartX, long standartY)
    {
        stX = standartX;
        stY = standartY;
    }

    public void succesSummon()
    {
        CardEventHandler.CEH.OnCardFieldSummon();
    }

    public void sortLayerOrder(int lOrder)
    {
        cardContainer.GetComponent<SpriteRenderer>().sortingOrder = lOrder;
        artRef.sortingOrder = lOrder + 1;
        atkSymbRef.sortingOrder = lOrder + 2;
        hpSymbRef.sortingOrder = lOrder + 2;
        atkRef.sortingOrder = lOrder + 3;
        hpRef.sortingOrder = lOrder + 3;
    }

    void onCardTakeDmg(int dmg)
    {
        hp -= dmg;
        updateHp();
        CardEventHandler.CEH.OnCardTakeDamage();
        onHitPS.Play();
    }

    public float onCardDestroyed()
    {
        CardEventHandler.CEH.OnCardDestroy();
        cardContainer.SetActive(false);
        GetComponent<BoxCollider2D>().enabled = false;
        onDeathPS.Play();
        return onDeathPS.main.duration;
        //Specific death particles here
    }
    
}
