using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnHandler : MonoBehaviour
{
    public static TurnHandler TH;

    public enum TurnState {TDraw, TMain, TCombat, TEnemyDrawMain, TEnemyCombat};
    public TurnState state;
    public bool playerTurn = false;
    public bool firstToPlay = false;

    void Awake()
    {
        //turn handler is singleton, if for whatever reason more than 1 is found its is deleted
        if (TurnHandler.TH == null)
        {
            TurnHandler.TH = this;
        }
        else if (TurnHandler.TH != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    public string getPhaseText()
    {
        switch (state)
        {
            case TurnState.TDraw:
                return "DRAW PHASE";
            case TurnState.TMain:
                return "MAIN PHASE";
            case TurnState.TCombat:
                return "COMBAT PHASE";
            case TurnState.TEnemyDrawMain:
                return "ENEMY TURN: MAIN PHASE";
            case TurnState.TEnemyCombat:
                return "ENEMY TURN: COMBAT PHASE";
            default:
                return "MAIN PHASE";
        }
    }

    public void isFirstToPlay(bool ftp)
    {
        firstToPlay = ftp;
        if (firstToPlay)
        {
            playerTurn = true;
            state = TurnState.TMain;
        }
        else
        {
            playerTurn = false;
            state = TurnState.TEnemyDrawMain;
        }
    }

    //FROM turn state TO turn state (p = Player, en = Enemy)

    public void enCOMBAT_to_pDRAW()
    {
        if(!playerTurn && state == TurnState.TEnemyCombat)
        {
            state = TurnState.TDraw;
            playerTurn = true;

            GameHandler.GH.drawNextCardIfReady();
            CardEventHandler.CEH.OnCardDrawStart();

            GameHandler.GH.phaseText.text = getPhaseText();
        }
    }
    public void pDRAW_to_pMAIN()
    {
        if (playerTurn && state == TurnState.TDraw)
        {
            state = TurnState.TMain;
            playerTurn = true;

            GameHandler.GH.phaseText.text = getPhaseText();
        }
    }
    public void pMAIN_to_pCOMBAT() //btn
    {
        if (playerTurn && state == TurnState.TMain)
        {
            state = TurnState.TCombat;
            playerTurn = false;

            CardCombatHandler.CCH.beginCombat();
            EncryptionHandler.EH.genericEndTurn();

            GameHandler.GH.phaseText.text = getPhaseText();
        }
    }
    public void pCOMBAT_to_enDRAWMAIN()
    {
        if (!playerTurn && state == TurnState.TCombat)
        {
            state = TurnState.TEnemyDrawMain;
            playerTurn = false;

            GameHandler.GH.phaseText.text = getPhaseText();
        }
    }
    public void enDRAWMAIN_to_enCOMBAT()
    {
        if (!playerTurn && state == TurnState.TEnemyDrawMain)
        {
            state = TurnState.TEnemyCombat;
            playerTurn = false;

            CardCombatHandler.CCH.beginCombat();

            GameHandler.GH.phaseText.text = getPhaseText();
        }
    }
}
