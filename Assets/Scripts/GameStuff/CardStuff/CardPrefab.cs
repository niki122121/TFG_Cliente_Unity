using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CardPrefab : MonoBehaviour
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
    public string keyWords = "";
    public Sprite spriteArt;

    public Gradient impactCol1;
    public Gradient impactCol2;

    [Header("Card Fixed Info")]
    public int deckRef = -1;

    [Header("Card References")]
    public GameObject cardHand;
    public GameObject cardField;
    public GameObject cardWeapon;

    [Header("Card Buttons")]
    [SerializeField] bool commitChanges;
    public bool everyThingOK = false;

#if UNITY_EDITOR
    [CustomEditor(typeof(CardPrefab))]
    public class ObjectBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CardPrefab cardPrefabScript = (CardPrefab)target;
            if (GUILayout.Button("Assign Variables"))
            {
                cardPrefabScript.assignVars();
            }

            if (GUILayout.Button("Check Result"))
            {
                cardPrefabScript.checkResult();
            }
        }
    }
#endif

    void assignVars()
    {
        everyThingOK = false;

        cardHand.GetComponent<CardHand>().cardName = cardName;
        cardHand.GetComponent<CardHand>().cardID = cardID;
        cardHand.GetComponent<CardHand>().cardSubID = cardSubID;
        cardHand.GetComponent<CardHand>().hp = hp;
        cardHand.GetComponent<CardHand>().atk = atk;
        cardHand.GetComponent<CardHand>().cardType = cardType;
        cardHand.GetComponent<CardHand>().cost = cost;
        cardHand.GetComponent<CardHand>().faction = faction;
        cardHand.GetComponent<CardHand>().specialParam = specialParam;
        cardHand.GetComponent<CardHand>().deckRef = deckRef;

        cardField.GetComponent<CardField>().cardName = cardName;
        cardField.GetComponent<CardField>().cardID = cardID;
        cardField.GetComponent<CardField>().cardSubID = cardSubID;
        cardField.GetComponent<CardField>().hp = hp;
        cardField.GetComponent<CardField>().atk = atk;
        cardField.GetComponent<CardField>().cardType = cardType;
        cardField.GetComponent<CardField>().cost = cost;
        cardField.GetComponent<CardField>().faction = faction;
        cardField.GetComponent<CardField>().specialParam = specialParam;
        cardField.GetComponent<CardField>().deckRef = deckRef;

        var col1 = cardField.GetComponent<CardField>().onHitPS.colorOverLifetime;
        col1.color = new ParticleSystem.MinMaxGradient(impactCol1, impactCol2);
        var col2 = cardField.GetComponent<CardField>().onDeathPS.colorOverLifetime;
        col2.color = new ParticleSystem.MinMaxGradient(impactCol1, impactCol2);

        cardHand.transform.Find("Name").GetComponent<Text>().text = cardName;
        cardHand.transform.Find("ATK").GetComponent<Text>().text = atk.ToString();
        cardHand.transform.Find("HP").GetComponent<Text>().text = hp.ToString();
        cardHand.transform.Find("CostFaction").transform.Find("CostNum").GetComponent<Text>().text = cost.ToString();
        cardHand.transform.Find("KeyWords").GetComponent<Text>().text = keyWords;

        cardField.transform.Find("CardContainer").Find("ATK").GetComponent<TMP_Text>().text = atk.ToString();
        cardField.transform.Find("CardContainer").Find("HP").GetComponent<TMP_Text>().text = hp.ToString();

        cardHand.transform.Find("Art").GetComponent<Image>().sprite = spriteArt;
        cardField.transform.Find("CardContainer").Find("Art").GetComponent<SpriteRenderer>().sprite = spriteArt;

        cardField.transform.Find("CardContainer").Find("ATK").GetComponent<MeshRenderer>().sortingLayerName = "Cards";
        cardField.transform.Find("CardContainer").Find("HP").GetComponent<MeshRenderer>().sortingLayerName = "Cards";
        cardField.transform.Find("CardContainer").Find("ATK").GetComponent<MeshRenderer>().sortingOrder = 3;
        cardField.transform.Find("CardContainer").Find("HP").GetComponent<MeshRenderer>().sortingOrder = 3;

        commitChanges = false;
    }
    void checkResult()
    {
        bool everythinOK = true;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().cardName == cardName;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().cardID == cardID;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().cardSubID == cardSubID;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().hp == hp;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().atk == atk;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().cardType == cardType;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().cost == cost;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().faction == faction;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().specialParam == specialParam;
        everythinOK = everythinOK && cardHand.GetComponent<CardHand>().deckRef == deckRef;

        everythinOK = everythinOK && cardField.GetComponent<CardField>().cardName == cardName;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().cardID == cardID;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().cardSubID == cardSubID;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().hp == hp;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().atk == atk;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().cardType == cardType;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().cost == cost;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().faction == faction;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().specialParam == specialParam;
        everythinOK = everythinOK && cardField.GetComponent<CardField>().deckRef == deckRef;

        everythinOK = everythinOK && cardHand.transform.Find("Name").GetComponent<Text>().text.Equals(cardName);
        everythinOK = everythinOK && cardHand.transform.Find("ATK").GetComponent<Text>().text.Equals(atk.ToString());
        everythinOK = everythinOK && cardHand.transform.Find("HP").GetComponent<Text>().text.Equals(hp.ToString());
        everythinOK = everythinOK && cardHand.transform.Find("CostFaction").transform.Find("CostNum").GetComponent<Text>().text.Equals(cost.ToString());
        everythinOK = everythinOK && cardHand.transform.Find("KeyWords").GetComponent<Text>().text.Equals(keyWords.ToString());

        everythinOK = everythinOK && cardField.transform.Find("CardContainer").Find("ATK").GetComponent<TMP_Text>().text.Equals(atk.ToString());
        everythinOK = everythinOK && cardField.transform.Find("CardContainer").Find("HP").GetComponent<TMP_Text>().text.Equals(hp.ToString());

        everythinOK = everythinOK && cardHand.transform.Find("Art").GetComponent<Image>().sprite.Equals(spriteArt);
        everythinOK = everythinOK && cardField.transform.Find("CardContainer").Find("Art").GetComponent<SpriteRenderer>().sprite.Equals(spriteArt);

        if (everythinOK)
        {
            everyThingOK = true;
            Debug.Log(cardName + ":  OK");
        }
        else
        {
            everyThingOK = false;
            Debug.LogError(cardName + ":  NOT GOOD!!!!!!!!!!!");
        }
    }
}
