using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardCombatHandler : MonoBehaviour
{
    public static CardCombatHandler CCH;
    public int cardFightsToResolve = 0;

    private void Awake()
    {
        //game handler is singleton, if for whatever reason more than 1 is found its is deleted
        if (CardCombatHandler.CCH == null)
        {
            CardCombatHandler.CCH = this;
        }
        else if (CardCombatHandler.CCH != this)
        {
            Destroy(this.gameObject);
            return;
        }

        DontDestroyOnLoad(this.gameObject);
    }

    public void setupClosestOponent()
    {
        foreach(CardField cPlayer in GameHandler.GH.cardsOnField_Player)
        {
            foreach (CardField cEnemy in GameHandler.GH.cardsOnField_Enemy)
            { 
                long distSq = squaredDist(cPlayer, cEnemy);
                if(cPlayer.closestOponent == null || cPlayer.opDistSquared > distSq)
                {
                    cPlayer.closestOponent = cEnemy;
                    cPlayer.opDistSquared = distSq;
                }
                if (cEnemy.closestOponent == null || cEnemy.opDistSquared > distSq)
                {
                    cEnemy.closestOponent = cPlayer;
                    cEnemy.opDistSquared = distSq;
                }
            }
        }
    }

    long squaredDist(CardField cPlayer, CardField cEnemy)
    {
        long diffX = cEnemy.stX - cPlayer.stX;
        long diffY = cEnemy.stY - cPlayer.stY;
        return diffX * diffX + diffY * diffY;
    }

    public void beginCombat()
    {
        CardEventHandler.CEH.OnCardCombatStart();

        if(GameHandler.GH.cardsOnField_Player.Count != 0 && GameHandler.GH.cardsOnField_Enemy.Count != 0)
        {
            cardFightsToResolve = GameHandler.GH.cardsOnField_Player.Count + GameHandler.GH.cardsOnField_Enemy.Count;

            setupClosestOponent();
            
            StartCoroutine(beginCombatAnimations());
        }
        else
        {
            cardFightsToResolve = 0;
            StartCoroutine(endCombat());
        }

    }

    IEnumerator beginCombatAnimations()
    {
        int cardAmount = GameHandler.GH.cardsOnField_Player.Count + GameHandler.GH.cardsOnField_Enemy.Count;
        float waitAmount = Mathf.Min(0.2f, 0.4f / cardAmount);
        foreach (CardField cPlayer in GameHandler.GH.cardsOnField_Player)
        {
            yield return new WaitForSeconds(waitAmount);
            cPlayer.initAttackAnimation(true);
        }
        foreach (CardField cEnemy in GameHandler.GH.cardsOnField_Enemy)
        {
            yield return new WaitForSeconds(waitAmount);
            cEnemy.initAttackAnimation(false);
        }

        yield return new WaitForSeconds(0.75f);
        foreach (CardField cPlayer in GameHandler.GH.cardsOnField_Player)
        {
            cPlayer.beginAttackAnimation();
        }
        foreach (CardField cEnemy in GameHandler.GH.cardsOnField_Enemy)
        {
            cEnemy.beginAttackAnimation();
        }
    }

    public void fightResolved()
    {
        cardFightsToResolve--;
        if (cardFightsToResolve == 0)
        {
            StartCoroutine(endCombat());
        }
        else if (cardFightsToResolve < 0)
        {
            Debug.LogError("FATAL ERROR MORE CARDS ON FIELD AFTER COMBAT INITIALIZATION");
        }
    }

    IEnumerator endCombat()
    {
        yield return new WaitForSeconds(0.75f);
        float destroyDelay = 0;
        for (int i=0; i< GameHandler.GH.cardsOnField_Player.Count; i++)
        {
            if (GameHandler.GH.cardsOnField_Player[i].hp <= 0)
            {
                CardField cPlayer = GameHandler.GH.cardsOnField_Player[i];
                destroyDelay = cPlayer.onCardDestroyed();
                GameHandler.GH.cardsOnField_Player.RemoveAt(i);
                Destroy(cPlayer.gameObject, destroyDelay);
                i--;
            }
        }
        for (int i = 0; i < GameHandler.GH.cardsOnField_Enemy.Count; i++)
        {
            if (GameHandler.GH.cardsOnField_Enemy[i].hp <= 0)
            {
                CardField cEnemy = GameHandler.GH.cardsOnField_Enemy[i];
                destroyDelay = cEnemy.onCardDestroyed();
                GameHandler.GH.cardsOnField_Enemy.RemoveAt(i);
                Destroy(cEnemy.gameObject, destroyDelay);
                i--;
            }
        }

        yield return new WaitForSeconds(1);
        CardEventHandler.CEH.OnCardCombatEnd();

        if (TurnHandler.TH.state == TurnHandler.TurnState.TCombat)
        {
            TurnHandler.TH.pCOMBAT_to_enDRAWMAIN();
        }
        else if(TurnHandler.TH.state == TurnHandler.TurnState.TEnemyCombat)
        {
            TurnHandler.TH.enCOMBAT_to_pDRAW();
        }
    }
}
