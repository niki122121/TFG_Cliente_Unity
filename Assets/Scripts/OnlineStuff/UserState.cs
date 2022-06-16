using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UserStateNamespace
{
    public class UserState
    {
        public enum statesEnum { Default, Searching, Reconnecting, Negotiating, Gaming };
        public static statesEnum uState = statesEnum.Default;

        public static bool firstConnection = true;

        public delegate void eventChangeFunc();
        public static eventChangeFunc eventChangeDelegate;

        public static void changeState(statesEnum stateParam)
        {
            //only use this when a different state from the previous one is provided
            if(stateParam != UserState.uState)
            {
                UserState.uState = stateParam;
                UserState.eventChangeDelegate?.Invoke();
            }
        }

    }
}
