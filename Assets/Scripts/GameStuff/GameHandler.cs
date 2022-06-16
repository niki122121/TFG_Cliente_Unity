using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHandler : MonoBehaviour
{
    public static GameHandler GH;

    //[SerializeField] GameObject otherMouse;
    [HideInInspector] public GraphicRaycaster raycaster;
    [HideInInspector] public Text phaseText;
    Button nextPhaseBtn;
    Button resignBtn;
    Button sendChatBtn;
    InputField chatInput;
    Text chatTextArea;
    GameObject playerHandObj;
    RectTransform handStartTransform;
    GameObject playerField;
    GameObject neutralField;
    GameObject enemyField;
    [HideInInspector] public Transform cardField;
    [HideInInspector] public List<GameObject> cardsInHand;
    [HideInInspector] public List<CardField> cardsOnField_Player;
    [HideInInspector] public List<CardField> cardsOnField_Enemy;

    public RaycastResult mouseTarget;

    public int initialCardDraw = 5;
    public int deckSize = 40;
    [HideInInspector] public int nextCardToDraw = 0;
    [HideInInspector] public int nextCardToDraw_Enemy;

    public int availableLayer = -32767;

    //encoder
    UTF8Encoding utf8;

    [HideInInspector] public bool nextDrawReady = false;
    int nextCardAmount;
    int[] nextDeckRef;
    uint[,] nextParamsArray;

    private void Awake()
    {
        //game handler is singleton, if for whatever reason more than 1 is found its is deleted
        if (GameHandler.GH == null)
        {
            GameHandler.GH = this;
        }
        else if (GameHandler.GH != this)
        {
            Destroy(this.gameObject);
            return;
        }

        cardsInHand = new List<GameObject>();
        cardsOnField_Player = new List<CardField>();
        cardsOnField_Enemy = new List<CardField>();
        nextCardToDraw_Enemy = deckSize;

        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        OnlineHandler.OH.gameStarted += (() => SceneManager.LoadScene(3));
        OnlineHandler.OH.gameStarted += (() => setupchannelStuff());
        OnlineHandler.OH.gameEnded += (() => removeChannelStuff());
        OnlineHandler.OH.gameEnded += (() => SceneManager.LoadScene(2));

        utf8 = new UTF8Encoding();

        availableLayer = -32767;
    }

    void setupchannelStuff()
    {
        //OnlineHandler.oHandler.msgAboutToSendEvents[2] += sendMouse;
        OnlineHandler.OH.msgRecievedEvents[1] += ((rawChat) => recieveChat(rawChat));
        //OnlineHandler.oHandler.msgRecievedEvents[2] += ((rawCommand) => recieveMouse(rawCommand));
    }
    void removeChannelStuff()
    {
        //OnlineHandler.oHandler.msgAboutToSendEvents[2] -= sendMouse;
        OnlineHandler.OH.msgRecievedEvents[1] -= ((rawChat) => recieveChat(rawChat));
        //OnlineHandler.oHandler.msgRecievedEvents[2] -= ((rawCommand) => recieveMouse(rawCommand));
    }

    public void setupGameObjects(GameParamHolder paramHolder)
    {

        this.raycaster = paramHolder.raycaster;
        this.phaseText = paramHolder.phaseText;
        this.nextPhaseBtn = paramHolder.nextPhaseBtn;
        this.resignBtn = paramHolder.resignBtn;
        this.sendChatBtn = paramHolder.sendChatBtn;
        this.chatInput = paramHolder.chatInput;
        this.chatTextArea = paramHolder.chatTextArea;
        this.playerHandObj = paramHolder.playerHandObj;
        this.handStartTransform = paramHolder.handStartTransform;
        this.playerField = paramHolder.playerField;
        this.neutralField = paramHolder.neutralField;
        this.enemyField = paramHolder.enemyField;
        this.cardField = paramHolder.cardField;

        this.nextPhaseBtn.onClick.RemoveAllListeners();
        this.nextPhaseBtn.onClick.AddListener(nextPhase);

        this.resignBtn.onClick.RemoveAllListeners();
        this.resignBtn.onClick.AddListener(resignGame);

        this.sendChatBtn.onClick.RemoveAllListeners();
        this.sendChatBtn.onClick.AddListener(sendChat);

        phaseText.text = TurnHandler.TH.getPhaseText();
    }

    public void resignGame()
    {
        OnlineHandler.OH.resignGame();
    }
    public void nextPhase()
    {
        TurnHandler.TH.pMAIN_to_pCOMBAT();
    }

    public void drawCardBtn()
    {
        //EncryptionHandler.enHandler.genericDraw();
    }

    public void setupNextCardDraw(int cardAmount, int[] deckRef, uint[,] paramsArray)
    {
        if(TurnHandler.TH.state == TurnHandler.TurnState.TDraw)
        {
            nextDrawReady = false;
            drawCards(cardAmount, deckRef, paramsArray);
            TurnHandler.TH.pDRAW_to_pMAIN();
        }
        else if (TurnHandler.TH.state == TurnHandler.TurnState.TEnemyCombat)
        {
            nextDrawReady = true;
            nextCardAmount = cardAmount;
            nextDeckRef = deckRef;
            nextParamsArray = paramsArray;
        }
    }

    public void drawNextCardIfReady()
    {
        if (nextDrawReady)
        {
            nextDrawReady = false;
            drawCards(nextCardAmount, nextDeckRef, nextParamsArray);
            TurnHandler.TH.pDRAW_to_pMAIN();
        }
        else
        {
            //Here we wait for draw information to arrive
        }
    }

    public void drawCards(int cardAmount, int[] deckRef, uint[,] paramsArray)
    {
        nextCardToDraw += cardAmount;
        for (int i = 0; i < cardAmount; i++)
        {
            var cardInHand = Instantiate(CardInfoHandle.CH.getCardPrefab(paramsArray[i,0]).GetComponent<CardPrefab>().cardHand, Vector2.zero, Quaternion.identity, playerHandObj.transform);
            cardInHand.GetComponent<CardHand>().assignVariables(paramsArray[i, 0], paramsArray[i, 1], (int)paramsArray[i, 2], (int)paramsArray[i, 3],
                                                                paramsArray[i, 4], paramsArray[i, 5], paramsArray[i, 6], (int)paramsArray[i, 7], deckRef[i]);
            cardsInHand.Add(cardInHand);
        }

        adjustHand();
        CardEventHandler.CEH.OnCardDrawEnd();
    }

    public void adjustHand()
    {
        if (cardsInHand.Count == 0) return;
        int space = 180;
        int maxSpace = space;
        int maxCards = 4;
        int totalSpace = space * maxCards;
        space = space - ((cardsInHand.Count + 1) * space - totalSpace) / cardsInHand.Count;
        space = Mathf.Clamp(space, 10, maxSpace);
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            cardsInHand[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(handStartTransform.anchoredPosition.x, handStartTransform.anchoredPosition.y - i * space);
        }
    }

    public void summonCardLocal(CardHand cH, Vector2 dragEndPos, uint cardHandID, Transform parentHand, RaycastResult fieldRayCast)
    {
        if(fieldRayCast.gameObject == playerField && TurnHandler.TH.state == TurnHandler.TurnState.TMain)
        {
            Vector2 worldPosition = Camera.main.ScreenToWorldPoint(dragEndPos);
            GameObject cardOnFieldObj = Instantiate(CardInfoHandle.CH.getCardPrefab(cardHandID).GetComponent<CardPrefab>().cardField, worldPosition, Quaternion.identity, cardField);
            CardField cardOnField = cardOnFieldObj.GetComponent<CardField>();

            cardOnField.assignVariables(cH);
            cardOnField.assignStandartPos(standartizeFloat(worldPosition.x), standartizeFloat(worldPosition.y));
            cardOnField.parentHand = parentHand;

            cardOnField.sortLayerOrder(availableLayer);
            availableLayer += 4;

            cardsOnField_Player.Add(cardOnField);

            EncryptionHandler.EH.genericSummon(new List<int> { cH.deckRef },
                                                      new List<float> { worldPosition.x, worldPosition.y },
                                                      new List<long> { standartizeFloat(worldPosition.x), standartizeFloat(worldPosition.y) });

            cH.succesSummon();
            cardOnField.succesSummon();

            cardsInHand.Remove(cH.gameObject);
            adjustHand();
        }
        else
        {
            cH.returnToStart();
        }
    }

    public void summonCards(int cardAmount, int[] deckRef, uint[,] paramsArray, byte[] aditionalInfo)
    {
        int byteOffset1 = 8 + cardAmount * 4;
        int byteOffset2 = cardAmount * 8;
        bool everythinOk = true;
        for (int i = 0; i < cardAmount; i++)
        {
            everythinOk = everythinOk && paramsArray[i, 7] == 1;

            Vector2 recievedPos = new Vector2(BitConverter.ToSingle(aditionalInfo, byteOffset1 + 4 * (i * 2)), 
                                              BitConverter.ToSingle(aditionalInfo, byteOffset1 + 4 * (i * 2 + 1)));

            long standart_X_Mine = standartizeFloat(recievedPos.x);
            long standart_Y_Mine = standartizeFloat(recievedPos.y);
            long standart_X_Other = BitConverter.ToInt64(aditionalInfo, byteOffset1 + byteOffset2 + 8 * (i * 2));
            long standart_Y_Other = BitConverter.ToInt64(aditionalInfo, byteOffset1 + byteOffset2 + 8 * (i * 2 + 1));

            everythinOk = everythinOk && checkStandart(standart_X_Mine, standart_X_Other);
            everythinOk = everythinOk && checkStandart(standart_Y_Mine, standart_Y_Other);

            GameObject cardOnFieldObj = Instantiate(CardInfoHandle.CH.getCardPrefab(paramsArray[i, 0]).GetComponent<CardPrefab>().cardField, flipPosition(recievedPos), Quaternion.identity, cardField);
            CardField cardOnField = cardOnFieldObj.GetComponent<CardField>();
            
            cardOnField.assignVariables(paramsArray[i, 0], paramsArray[i, 1], (int)paramsArray[i, 2], (int)paramsArray[i, 3],
                                                                    paramsArray[i, 4], paramsArray[i, 5], paramsArray[i, 6], (int)paramsArray[i, 7], 
                                                                    deckRef[i]);
            cardOnField.assignStandartPos(standart_X_Other, flipPositionSt(standart_Y_Other));

            cardOnField.sortLayerOrder(availableLayer);
            availableLayer += 4;

            cardsOnField_Enemy.Add(cardOnField);

            cardOnField.succesSummon();
        }
        if (!everythinOk)
        {
            Debug.LogError("IN HAND PARAMETER ERROR CONTACT SERVER");
        }
    }

    void recieveChat(byte[] rawChat)
    {
        if (BitConverter.ToUInt32(rawChat, 0) == 1)
        {
            //this message will include 4 uselss bytes so we ignore them
            chatTextArea.text += "\n" + OnlineHandler.OH.selectedUser + ":  " + System.Text.Encoding.UTF8.GetString(rawChat, 8, rawChat.Length - 8);
        }
    }
    void sendChat()
    {
        //byte[] textBytes = utf8.GetBytes(text);

        chatTextArea.text += "\n" + OnlineHandler.OH.user + ":  " + chatInput.text;
        OnlineHandler.OH.pushOutgoingMsg(1, 0, utf8.GetBytes(chatInput.text), 1);                       
        chatInput.text = "";
    }

    /*void recieveMouse(byte[] rawCommand)
    {
        if (BitConverter.ToUInt32(rawCommand, 0) == 2)
        {
            otherMouse.GetComponent<RectTransform>().position =  new Vector2(BitConverter.ToSingle(rawCommand, 8) * Screen.width, BitConverter.ToSingle(rawCommand, 12) * Screen.height);
        }
    }
    void sendMouse()
    {
        OnlineHandler.oHandler.pushOutgoingMsg(2, 0, null , null, new List<float> {Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height }, 2);
    }*/

    byte[] turnToBytes(string input)
    {
        return Encoding.UTF8.GetBytes(input);
    }

    Vector2 flipPosition(Vector2 vec)
    {
        /*Vector2 worldCenter = Camera.main.ScreenToWorldPoint(neutralField.transform.position);
        Debug.LogError(worldCenter.y);
        Vector2 newVec = new Vector2(vec.x, vec.y + 2*(worldCenter.y - vec.y));*/
        return new Vector2(vec.x, -vec.y);
    }
    long flipPositionSt(long stY)
    {
        return -stY;
    }

    long standartizeFloat(float value)
    {
        return (long)(value * 1000);

    }

    bool checkStandart(long stValueMine, long stValueOther)
    {
        if(Math.Abs(stValueMine - stValueOther) > 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    
}
