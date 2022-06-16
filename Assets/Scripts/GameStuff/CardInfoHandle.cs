using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CardInfoHandle : MonoBehaviour
{
    public static CardInfoHandle CH;
    [SerializeField] List<GameObject> allCardsList;
    Dictionary<uint, GameObject> allCardsDictionary;

#if UNITY_EDITOR
    [SerializeField] string allCardText = "";

    [CustomEditor(typeof(CardInfoHandle))]
    public class ObjectBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            CardInfoHandle cardInfoHandleScript = (CardInfoHandle)target;
            if (GUILayout.Button("Card MySQL insert"))
            {
                cardInfoHandleScript.cardInsertText();
            }
            if (GUILayout.Button("Clear MySQL insert"))
            {
                cardInfoHandleScript.clearCardText();
            }
        }
    }

    private void cardInsertText()
    {
        string cardText = "REPLACE INTO cardTable VALUES \n";
        for (int i = 0; i < allCardsList.Count; i++)
        {
            CardPrefab card = allCardsList[i].GetComponent<CardPrefab>();
            cardText += ("(" + card.cardID + ",\"" + card.cardName + "\"," + 0 + "," + card.hp + "," + card.atk + "," + 0 + "," +
                        card.cost + "," + 0 + "," + 0 + "),\n");
        }
        int lastComa = cardText.LastIndexOf(',');
        cardText = cardText.Remove(lastComa, 1);
        cardText = cardText.Insert(lastComa, ";");
        allCardText = cardText;
    }

    private void clearCardText()
    {
        allCardText = "";
    }

#endif

    private void Awake()
    {
        //card handler is singleton, if for whatever reason more than 1 is found its deleted
        if (CardInfoHandle.CH == null)
        {
            CardInfoHandle.CH = this;
        }
        else if (CardInfoHandle.CH != this)
        {
            Destroy(this.gameObject);
            return;
        }
        allCardsDictionary = new Dictionary<uint, GameObject>();
        GameObject aux = null;

        for (int i=0; i< allCardsList.Count; i++)
        {
            if (allCardsDictionary.TryGetValue(allCardsList[i].GetComponent<CardPrefab>().cardID, out aux))
            {
                Debug.LogError("CARD WITH DUPLICATE ID: GAME BREAKING ERROR   =>    " + aux.GetComponent<CardPrefab>().cardName + " AND " + allCardsList[i].GetComponent<CardPrefab>().cardName);
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                return;
            }
            allCardsDictionary.Add(allCardsList[i].GetComponent<CardPrefab>().cardID, allCardsList[i]);
        }
        allCardsList.Clear();

        DontDestroyOnLoad(this.gameObject);
    }

    public GameObject getCardPrefab(uint cardID)
    {
        GameObject cardToReturn = null;
        allCardsDictionary.TryGetValue(cardID, out cardToReturn);
        return cardToReturn;
    }

    public int allCardsLeng()
    {
        return allCardsDictionary.Count;
    }
}
