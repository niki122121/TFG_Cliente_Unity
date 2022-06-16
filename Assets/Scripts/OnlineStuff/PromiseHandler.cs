using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


//handle promises with other users upon sending data via webRTC (code to run after the other user sends response)
public class PromiseHandler : MonoBehaviour
{
    class ActionCapsule
    {
        public Action<byte[]> func;
        public byte[] inputBytes;
        public bool complete;
        public ActionCapsule(Action<byte[]> f)
        {
            func = f;
            complete = false;
        }
        public ActionCapsule(byte[] iBytes, Action<byte[]> f, bool comp)
        {
            inputBytes = iBytes;
            func = f;
            complete = comp;
        }
    }
    class Promise
    {
        public Action<byte[]> func;
        public bool orderedAction;
        public Promise(Action<byte[]> a, bool ordA)
        {
            func = a;
            orderedAction = ordA;
        }
    }

    public static PromiseHandler PH;

    List<ActionCapsule> orderedActions;

    Dictionary<uint, Promise> promiseDictionary;
    System.Random promiseRndGen;

    private void Awake()
    {
        //promise handler is singleton, if for whatever reason more than 1 is found its is deleted
        if (PromiseHandler.PH == null)
        {
            PromiseHandler.PH = this;
        }
        else if (PromiseHandler.PH != this)
        {
            Destroy(this.gameObject);
            return;
        }
        orderedActions = new List<ActionCapsule>();
        promiseDictionary = new Dictionary<uint, Promise>();

        promiseRndGen = new System.Random();

        DontDestroyOnLoad(this.gameObject);
    }

    public void createUnsolvedOrderedAction(Action<byte[]> func)
    {
        orderedActions.Add(new ActionCapsule(func));
    }
    public void createOrderedAction(byte[] iBytes, Action<byte[]> func)
    {
        if (orderedActions.Count == 0)
        {
            func(iBytes);
        }
        else
        {
            orderedActions.Add(new ActionCapsule(iBytes, func, true));
        }
    }
    public void solveAction(byte[] iBytes, Action<byte[]> func)
    {
        int actionPos = orderedActions.FindIndex((x) => (x.func.Equals(func)));
        if (actionPos == -1)
        {
            Debug.LogError("ACTION NOT FOUND: SEVER ERROR!");
        }
        else if (actionPos == 0)
        {
            orderedActions[0].complete = true;
            orderedActions[0].inputBytes = iBytes;
            cascadeOrderedActions();
        }
        else
        {
            orderedActions[actionPos].complete = true;
            orderedActions[actionPos].inputBytes = iBytes;
        }
    }

    public void cascadeOrderedActions()
    {
        if (orderedActions.Count > 0 && orderedActions[0].complete)
        {
            orderedActions[0].func(orderedActions[0].inputBytes);
            orderedActions.RemoveAt(0);
            cascadeOrderedActions();
        }
    }

    public uint createSpecificPromise(uint pId, bool orderedAction, Action<byte[]> promiseFunc)
    {
        promiseDictionary.Add(pId, new Promise(promiseFunc, orderedAction));
        if (orderedAction)
        {
            createUnsolvedOrderedAction(promiseFunc);
        }
        return pId;
    }

    //this class can't handle a large number of promises (since while method will run for too long) (example: 1000000 promises starts to be too much)
    public uint createPromiseId()
    {
        uint promiseId = (uint)promiseRndGen.Next();
        while (promiseDictionary.ContainsKey(promiseId))
        {
            promiseId = (uint)promiseRndGen.Next();
        }
        return promiseId;
    }


    /*public uint createPromise(Action<byte[]> promiseFunc)
    {
        uint promiseId = createPromiseId();
        return createSpecificPromise(promiseId, promiseFunc);
    }*/

    public bool resolvePromise(uint promiseId, byte[] promiseBytes)
    {
        Promise promise;
        bool foundId = promiseDictionary.TryGetValue(promiseId, out promise);

        if(!foundId || promise == null)
        {
            return false;
        }
        else
        {
            if (promise.orderedAction)
            {
                solveAction(promiseBytes, promise.func);
            }
            else
            {
                promise.func(promiseBytes);
            }
            promiseDictionary.Remove(promiseId);
            return true;
        }
    }
}
