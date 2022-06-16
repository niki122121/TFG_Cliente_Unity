
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using System.Net.WebSockets;

using Unity.WebRTC;
using UserStateNamespace;
using UnityEngine.SceneManagement;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

public class OnlineHandler : MonoBehaviour
{
    public static OnlineHandler OH;
    public static int testSceneIndex = 1;

    //USER STUFF
    [Header("User Params")]
    public int country = 1;
    [SerializeField] int areaRadInit = 3000;
    [SerializeField] float areaRadIncrease = 1.5f;
    [SerializeField] int mmrDifInit = 100;
    [SerializeField] float mmrDifIncrease = 1.5f;
    public List<string> countryList;
    int currentAreaRad = 0;
    int currentMmrDif = 0;

    //WEBSOCKETS
    [Header("WebSocket Config")]
    ClientWebSocket socket;
    bool isSending = false;
    Queue<ArraySegment<byte>> lefotversQueue;
    public string user = "ni";
    string userPassword = "asd";
    public string selectedUser = "";
    [HideInInspector] public string otherPkHash = "";
    [HideInInspector] public string otherS1Hash = "";
    [HideInInspector] public string otherS2Hash = "";
    LoginSetup loginSetup;
    int socketLeftoverTimerMs = 1000;
    float socketLefoverTimerSec;

    //WEBRTC
    [Header("WebRTC Config")]
    [Tooltip("Time user waits until expanding search parameters and re-requesting game from server.")]
    [SerializeField] float reQueueWait = 20;
    [Tooltip("Time user waits after trying to connect or reconnect and deciding server should get involved.")]
    [SerializeField] float moveOnWaitRec = 46;

    RTCPeerConnection peer;

    Coroutine iniReque;
    Coroutine iniMoveOnRec;

    [Header("DataChannels Config")]
    float distributedDelta = 0.2f;
    public List<DataChannelParams> dataChannelList;
    List<RTCDataChannel> channelSenders;
    List<RTCDataChannel> channelRecievers;
    int senderSetup = 0;
    int recieverSetup = 0;

    [SerializeField] uint maxBytesPerMsg = 16000;        //a bit less than 16 kb per msg (to fit headers and aditioneal info bytes)

    List<Queue<byte[]>> outgoingQList;
    List<uint> outgoingQListSize;

    //delegates
    public delegate void gameStartedDelegate();
    public gameStartedDelegate gameStarted;
    public delegate void gameEndedDelegate();
    public gameStartedDelegate gameEnded;
    public delegate void messageRecievedDelegate(byte[] msg);
    public List<messageRecievedDelegate> msgRecievedEvents;
    public delegate void messageBoutToSendDelegate();       //message will be found in queue
    public List<messageBoutToSendDelegate> msgAboutToSendEvents;
    public delegate void dataChannelsRecreatedDelegate();
    public dataChannelsRecreatedDelegate dataChannelsRecreated;

    //LOGIN REGISTER ...
    UploadHandlerRaw dummyData;

    //prevGame
    bool prevGame = false;
    public int gameNum = 0;

    //structures for sending jsons
    struct loginSuccesResponse
    {
        public int rndSeed;
        public bool newKeys;
    }
    struct userParams
    {
        public string uIdent;
        public string uPass;
        public int mmrDif;
        public int areaId;
        public int areaRadius;
    }
    struct gameFoundData
    {
        public bool strongOffer;
        public int serverSeed;
        public string selectedUser;
        public string otherPkHash;
        public string otherS1Hash;
        public string otherS2Hash;
        public uint[] hashMatrix;
        public uint[] scrambledDeck;
    }
    struct gameRefoundData
    {
        public bool strongOffer;
    }
    struct reconInfoWrapper
    {
        public string uIdent;
        public string trgtIdent;
        public string gamePassword;
    }
    struct socketID
    {
        public string to;
    }
    struct genericData
    {
        public int code;
        public string resultInfo;
    }
    struct genericExchange
    {
        public string from;
        public string to;
    }
    public struct exitQueue
    {
        public string uIdent;
    }
    struct gameExitEmpty
    {
        public string uIdent;
        public string gamePassword;
    }
    struct gameExitCert
    {
        public string uIdent;
        public string gamePassword;
        public int certificateType;
        public string certificate;
    }
    struct gameResult
    {
        public bool playerWon;
    }

    //establish username and password (pass is used every time a player request game from server jic)
    public void setupUser(string usrName, string usrPass)
    {
        user = usrName;
        userPassword = computeSHA256Hash(usrPass);
    }
    void Awake()
    {
        //online handler is singleton, if for whatever reason more than 1 is found its is deleted
        if(OnlineHandler.OH == null)
        {
            OnlineHandler.OH = this;
        }
        else if(OnlineHandler.OH != this)
        {
            Destroy(this.gameObject);
            return;
        }
        //for debug purposes
        setupUser("test", "test123");

        //initialize webrtc
        EncoderType webRTCConfig = 0;
        WebRTC.Initialize(webRTCConfig, true);
        //initialize areas, countries and regions used
        countryList = new List<string>() { "Abkhazia", "Afghanistan", "Albania", "Algeria", "Andorra", "Angola", "Antigua and Barbuda", "Argentina", "Armenia", "Artsakh", "Australia", "Austria", "Azerbaijan", "Bahamas", "Bahrain", "Bangladesh", "Barbados", "Belarus", "Belgium", "Belize", "Benin", "Bhutan", "Bolivia", "Bosnia and Herzegovina", "Botswana", "Brazil", "Brunei", "Bulgaria", "Burkina Faso", "Burundi", "Cambodia", "Cameroon", "Canada", "Cape Verde", "Central African Republic", "Chad", "Chile", "China", "Colombia", "Comoros", "Congo", "Cook Islands", "Costa Rica", "Croatia", "Cuba", "Cyprus", "Czech Republic", "Democratic Republic of Congo", "Denmark", "Djibouti", "Dominica", "Dominican Republic", "Ecuador", "Egypt", "El Salvador", "Equatorial Guinea", "Eritrea", "Estonia", "Eswatini", "Ethiopia", "Fiji", "Finland", "France", "Gabon", "Gambia", "Gaza Strip", "Georgia", "Germany", "Ghana", "Greece", "Grenada", "Guatemala", "Guinea", "Guinea-Bissau", "Guyana", "Haiti", "Honduras", "Hong Kong", "Hungary", "Iceland", "India", "Indonesia", "Iran", "Iraq", "Ireland", "Israel", "Italy", "Ivory Coast", "Jamaica", "Japan", "Jordan", "Kazakhstan", "Kenya", "Kiribati", "Kosovo", "Kuwait", "Kyrgyzstan", "Laos", "Latvia", "Lebanon", "Lesotho", "Liberia", "Libya", "Liechtenstein", "Lithuania", "Luxembourg", "Macedonia", "Madagascar", "Malawi", "Malaysia", "Maldives", "Mali", "Malta", "Marshall Island", "Mauritania", "Mauritius", "Mexico", "Micronesia", "Moldova", "Monaco", "Mongolia", "Montenegro", "Morocco", "Mozambique", "Myanmar", "Namibia", "Nauru", "Nepal", "Netherlands", "New Zealand", "Nicaragua", "Niger", "Nigeria", "Niue", "North Korea", "Northern Cyprus", "Norway", "Oman", "Pakistan", "Palau", "Palestine", "Panama", "Papua New Guinea", "Paraguay", "Peru", "Philippines", "Poland", "Portugal", "Qatar", "Romania", "Russia", "Rwanda", "Sahrawi Arab Democratic Republic", "Saint Kitts and Nevis", "Saint Lucia", "Saint Vincent and the Grenadines", "Samoa", "San Marino", "São Tomé and Príncipe", "Saudi Arabia", "Senegal", "Serbia", "Seychelles", "Sierra Leone", "Singapore", "Slovakia", "Slovenia", "Solomon Islands", "Somalia", "Somaliland", "South Africa", "South Korea", "South Ossetia", "South Sudan", "Spain", "Sri Lanka", "Sudan", "Suriname", "Swaziland", "Sweden", "Switzerland", "Syria", "Taiwan", "Tajikistan", "Tanzania", "Thailand", "The Bahamas", "The Gambia", "Timor-Leste", "Togo", "Tonga", "Transnistria", "Trinidad and Tobago", "Tunisia", "Turkey", "Turkmenistan", "Tuvalu", "Uganda", "Ukraine", "United Arab Emirates", "United Kingdom", "United States", "Uruguay", "Uzbekistan", "Vanuatu", "Vatican City", "Venezuela", "Vietnam", "Yemen", "Zambia", "Zimbabwe" };

        //this is a stupid way of making sure these coroutines are never null (sometimes a stop function is called before they can be properly assigned)
        iniReque = StartCoroutine(nonNullCoroutine());
        iniMoveOnRec = StartCoroutine(nonNullCoroutine());
        //used for request handler of unity webrequest
        byte[] dummyDataArr = new byte[1];
        dummyDataArr[0] = 0;
        dummyData = new UploadHandlerRaw(dummyDataArr);

        //Data channel list and message events setup:
        channelSenders = new List<RTCDataChannel>();
        channelRecievers = new List<RTCDataChannel>();
        outgoingQList = new List<Queue<byte[]>>();
        outgoingQListSize = new List<uint>();

        msgRecievedEvents = new List<messageRecievedDelegate>();
        msgAboutToSendEvents = new List<messageBoutToSendDelegate>();

        lefotversQueue = new Queue<ArraySegment<byte>>();
        socketLefoverTimerSec = (float)socketLeftoverTimerMs / 1000;
    }
    
    private void OnDestroy()
    {
        comprehensiveAppExit();
        WebRTC.Dispose();
    }

    void Start()
    {
        currentAreaRad = areaRadInit;
        currentMmrDif = mmrDifInit;

        //this 2 funcs are called every time game is started and every time user is about to start searching for a new game
        configurePeer();
        UserState.eventChangeDelegate += onUserChange;

        //coroutines used to send and recieve messages from data channels are distributed so they dont overlap with each other (leads to minor performance boost)
        distributedDelta = findGCD() / dataChannelList.Count;
        distributedDelta = distributedDelta / 1000;
        StartCoroutine(distributedChannelCreation());

        //online handler persists through entire game
        DontDestroyOnLoad(this.gameObject);
        SceneManager.LoadScene(testSceneIndex);
    }


    //___________________________________________________________________________________________________________
    //User state methods and coroutines
    //on user change is invoked every time UserState changes (doing nothing, searching for game, playing, in midle of handshake, reconnecting)
    void onUserChange()
    {
        switch (UserState.uState)
        {
            default:
            case UserState.statesEnum.Default:
            break;
            case UserState.statesEnum.Searching:
                iniReque = StartCoroutine(initiateReque(1));
                break;
            case UserState.statesEnum.Reconnecting:
                StopCoroutine(iniReque);
                StopCoroutine(iniMoveOnRec);
                iniMoveOnRec = StartCoroutine(initiateMoveOnRec());
                break;
            case UserState.statesEnum.Negotiating:
                StopCoroutine(iniReque);
                StopCoroutine(iniMoveOnRec);
                iniMoveOnRec = StartCoroutine(initiateMoveOnRec());
            break;
            case UserState.statesEnum.Gaming:
                StopCoroutine(iniReque);
                StopCoroutine(iniMoveOnRec);
                //StartedGaming();
                gameStarted?.Invoke();  //game starts
            break;
        }
    }

    //if X seconds pass with no response from server, expand search params and try again 
    IEnumerator initiateReque(int requestCounter)
    {
        yield return new WaitForSecondsRealtime(reQueueWait);
        currentAreaRad = (int)(currentAreaRad * areaRadIncrease);
        currentMmrDif = (int)(currentMmrDif * mmrDifIncrease);

        var task = Task.Run(async () => await RequestGame(requestCounter));
        yield return new WaitUntil(() => task.IsCompleted);     //Task Coroutine slopy merge

        iniReque = StartCoroutine(initiateReque(requestCounter + 1));
    }

    //if X seconds and no response while connecting or reconnecting request server do something
    IEnumerator initiateMoveOnRec()
    {
        yield return new WaitForSecondsRealtime(moveOnWaitRec);
        RequestReconnect();
    }

    //___________________________________________________________________________________________________________


    //___________________________________________________________________________________________________________
    //General Game Methods

    //initialize or reinitialize everythin properly
    void searchRestartState()
    {
        selectedUser = "";
        otherPkHash = "";
        otherS1Hash = "";
        otherS2Hash = "";
        currentAreaRad = areaRadInit;
        currentMmrDif = mmrDifInit;
        StopCoroutine(iniReque);
        StopCoroutine(iniMoveOnRec);
        UserState.changeState(UserState.statesEnum.Default);
    }

    //stop searching queue
    private void comprehensiveExit(bool queueExit = true)
    {
        disconnectWebRTC();
        searchRestartState();
        if(queueExit)
            RequestQueueExit();

        lefotversQueue.Clear();     //no more messages for server
        prevGame = false;
        gameEnded?.Invoke();        //game Ended
    }

    //exit whole application
    private void comprehensiveAppExit()
    {
        RequestGameExitEmpty();
        comprehensiveExit();
        if (socket != null && (socket.State == WebSocketState.Open || socket.State == WebSocketState.Connecting))
        {
            socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
    }

    //public methods that encapsulate private methods
    public void queueExit()
    {
        comprehensiveExit();
    }
    public void findGame()
    {
        RequestGame(0);
    }
    public void resignGame()
    {
        RequestGameExitEmpty();
    }

    //___________________________________________________________________________________________________________


    //methods for websocket communication
    private string getJsonString(byte[] msgBytes, uint initJsonLeng)
    {
        return Encoding.UTF8.GetString(msgBytes, 8, (int)initJsonLeng);
    }
    /*private byte[] getOtherBytes(byte[] msgBytes, uint initJsonLeng)
    {
        byte[] otherBytes = new byte[msgBytes.Length - 8 - initJsonLeng];
        Array.Copy(msgBytes, 8 + initJsonLeng, otherBytes, 0, otherBytes.Length);
        return otherBytes;
    }*/

    //___________________________________________________________________________________________________________
    //Socket Send methods
    private async Task socketSend(uint header, string msgJson, byte[] msgOtherBytes)
    {
        byte[] msgJsonBytes = Encoding.UTF8.GetBytes(msgJson);
        byte[] completeMsg = new byte[8 + msgJsonBytes.Length + msgOtherBytes.Length];
        byte[] headerBytes = BitConverter.GetBytes(header);
        byte[] jsonLengBytes = BitConverter.GetBytes((uint)msgJsonBytes.Length);
        Array.Copy(headerBytes, 0, completeMsg, 0, 4);
        Array.Copy(jsonLengBytes, 0, completeMsg, 4, 4);
        Array.Copy(msgJsonBytes, 0, completeMsg, 8, msgJsonBytes.Length);
        Array.Copy(msgOtherBytes, 0, completeMsg, 8 + msgJsonBytes.Length, msgOtherBytes.Length);

        if (!isSending)
        {
            isSending = true;
            if (socket.State == WebSocketState.Open)
                await socket.SendAsync(new ArraySegment<byte>(completeMsg, 0, completeMsg.Length), WebSocketMessageType.Binary, true, CancellationToken.None);
            else
                lefotversQueue.Enqueue(new ArraySegment<byte>(completeMsg, 0, completeMsg.Length));
            isSending = false;
        }
        else
            lefotversQueue.Enqueue(new ArraySegment<byte>(completeMsg, 0, completeMsg.Length));
    }

    IEnumerator handleSocketCommunication()
    {
        yield return new WaitForSecondsRealtime(socketLefoverTimerSec);
        int leftoVerQCound = lefotversQueue.Count;
        if (leftoVerQCound > 0)
        {
            isSending = true;
            for (int i = 0; i < leftoVerQCound; i++)
            {
                if (socket.State == WebSocketState.Open)
                {
                    Task t = Task.Run(() => socket.SendAsync(lefotversQueue.Dequeue(), WebSocketMessageType.Binary, true, CancellationToken.None));
                    yield return new WaitUntil(() => t.IsCompleted);
                }   
            }
            isSending = false;
        }
        StartCoroutine(handleSocketCommunication());
    }

    private async Task ConnectionCheck()
    {
        await socketSend(0, JsonConvert.SerializeObject(""), new byte[0]);
    }

    private async Task StartedGaming()
    {
        await socketSend(15, JsonConvert.SerializeObject(""), new byte[0]);
    }

    private async Task RequestGame(int requestCounter)
    {
        Debug.Log("Requesting game from server (attempt: " + requestCounter + ")");
        userParams uParams = new userParams();
        uParams.uIdent = user;
        uParams.uPass = userPassword;
        uParams.mmrDif = currentMmrDif;
        uParams.areaId = country;
        uParams.areaRadius = currentAreaRad;

        if(requestCounter == 0)
        {
            EncryptionHandler.EH.initScramblers();

            int skLeng = EncryptionHandler.EH.secretKeyBytes.Length;
            int s1Leng = EncryptionHandler.EH.scrambler1Bytes.Length;
            int s2Leng = EncryptionHandler.EH.scrambler2Bytes.Length;
            byte[] encryptionDataBytes = new byte[skLeng + s1Leng + s2Leng];

            Array.Copy(EncryptionHandler.EH.secretKeyBytes, 0, encryptionDataBytes, 0, skLeng);
            Array.Copy(EncryptionHandler.EH.scrambler1Bytes, 0, encryptionDataBytes, skLeng, s1Leng);
            Array.Copy(EncryptionHandler.EH.scrambler2Bytes, 0, encryptionDataBytes, skLeng + s1Leng, s2Leng);

            await socketSend(6, JsonConvert.SerializeObject(uParams), encryptionDataBytes);
        }
        else
        {
            await socketSend(6, JsonConvert.SerializeObject(uParams), new byte[0]);
        }

        UserState.changeState(UserState.statesEnum.Searching);
    }
    private async Task RequestReconnect()
    {
        Debug.Log("Atempting reconnect via server:");
        reconInfoWrapper rInfoWrap = new reconInfoWrapper();
        rInfoWrap.uIdent = user;
        rInfoWrap.trgtIdent = selectedUser;
        rInfoWrap.gamePassword = "gamePassword123";
        await socketSend(7, JsonConvert.SerializeObject(rInfoWrap), new byte[0]);
        UserState.changeState(UserState.statesEnum.Reconnecting);
    }

    private async Task RequestQueueExit()
    {
        Debug.Log("Requesting queue exit: ");
        exitQueue eQueue = new exitQueue();
        eQueue.uIdent = user;
        await socketSend(11, JsonConvert.SerializeObject(eQueue), new byte[0]);
    }

    private async Task RequestGameExitEmpty()
    {
        Debug.Log("Requesting game exit empty: ");
        gameExitEmpty gExitEmpty = new gameExitEmpty();
        gExitEmpty.uIdent = user;
        gExitEmpty.gamePassword = "gamePassword123";
        await socketSend(12, JsonConvert.SerializeObject(gExitEmpty), new byte[0]);
    }


    //___________________________________________________________________________________________________________

    void HandleMessageRecieved(byte[] msgBytes)
    {
        uint methodToInvoke = BitConverter.ToUInt32(msgBytes, 0);       //convert first 4 bytes to methodToInvoke
        uint initJsonLeng = BitConverter.ToUInt32(msgBytes, 4);       //convert second 4 bytes to see how long initial data is

        switch (methodToInvoke)
        {
            case 1:
                //OnLoginSuccesfull 
                loginSuccesResponse lSuccesResponse = JsonConvert.DeserializeObject<loginSuccesResponse>(getJsonString(msgBytes, initJsonLeng));
                OnLoginSucces(msgBytes, lSuccesResponse.rndSeed, lSuccesResponse.newKeys, initJsonLeng + 8);
                break;
            case 2:
                //SearchStarted 
                //searchStartedData sStartedData = JsonConvert.DeserializeObject<searchStartedData>(getJsonString(msgBytes, initJsonLeng));
                OnSearchStarted();
                break;
            case 3:
                //GameFound 
                gameFoundData gFoundData = JsonConvert.DeserializeObject<gameFoundData>(getJsonString(msgBytes, initJsonLeng));
                OnGameFound(gFoundData.strongOffer, gFoundData.serverSeed, gFoundData.selectedUser, gFoundData.otherPkHash, gFoundData.otherS1Hash, 
                            gFoundData.otherS2Hash, gFoundData.hashMatrix, gFoundData.scrambledDeck);
                break;
            case 4:
                //GameRefound 
                gameRefoundData gRefoundData = JsonConvert.DeserializeObject<gameRefoundData>(getJsonString(msgBytes, initJsonLeng));
                OnGameRefound(gRefoundData.strongOffer);
                break;
            case 5:
                //KickPlayer 
                //OnKickPlayer();
                break;
            case 8:
                //MediaOffer      PARAMS: byte[] mOffer, string dataFrom, string dataTo
                genericExchange mOfferWrap = JsonConvert.DeserializeObject<genericExchange>(getJsonString(msgBytes, initJsonLeng));
                OnMediaOffer(msgBytes, (int)initJsonLeng, mOfferWrap.from);
                break;
            case 9:
                //MediaAnswer     PARAMS: byte[] mAnswer, string dataFrom, string dataTo
                genericExchange mAnswerWrap = JsonConvert.DeserializeObject<genericExchange>(getJsonString(msgBytes, initJsonLeng));
                OnMediaAnswer(msgBytes, (int)initJsonLeng, mAnswerWrap.from);
                break;
            case 10:
                //IceCandidate    PARAMS: byte[] iCand, string dataTo
                OnIceCandidate(msgBytes, (int)initJsonLeng);
                break;
            case 14:
                //Certificate Recieved (its safe to exit game)
                comprehensiveExit(false);
                gameResult gResult = JsonConvert.DeserializeObject<gameResult>(getJsonString(msgBytes, initJsonLeng));
                Debug.LogError("GAME OVER: YOU " + ((gResult.playerWon) ? "WON" : "LOST"));
                break;
            case 1000:
                //FIX this is for testing purposes only
                /*Debug.LogError("Recieved Data Chunk Num.   Size:  " + msgBytes.Length + " bytes.");
                EncryptionHandler.enHandler.playerStream.Write(msgBytes, 8, msgBytes.Length - 8);*/
                break;
            default:
                Debug.LogError("Method not found:  " + methodToInvoke);
                break;
        }
    }
    //___________________________________________________________________________________________________________
    //On message from socket methods:
    void OnLoginSucces(byte[] msgBytes, int rndSeed, bool newKeys, uint keysStartPos)
    {
        //WE ARE READY AND LISTENING TO SERVER!!

        peer.RestartIce();  //not best place to put this but its fine as long as it is before next create offer     
        loginSetup.handleFinishLoginSucces();

        //FIX possibility of player desconnection that will force recreation of seed
        EncryptionHandler.EH.setupGlobalSeed(rndSeed);
        if (newKeys)
        {
            EncryptionHandler.EH.setKeys(msgBytes, (int)keysStartPos);
        }
        if (prevGame)
        {
            RequestReconnect();
        }
        Debug.Log("LOGIN SUCCES:   " + msgBytes.Length);
    }
    private void OnSearchStarted()
    {
        Debug.Log("Search Started");
        //CardInfoHandle.cHandler.initializeDeck(sUDeck);
    }
    private void OnGameFound(bool strongOffer, int sSeed, string gSelUser, string gOtherPkHash, string gOtherS1Hash, string gOtherS2Hash, uint[] hMatrix, uint[] scrambledDeck)
    {
        UserState.changeState(UserState.statesEnum.Negotiating);
        prevGame = true;
        gameNum++;
        selectedUser = gSelUser;
        otherPkHash = gOtherPkHash;
        otherS1Hash = gOtherS1Hash;
        otherS2Hash = gOtherS2Hash;
        EncryptionHandler.EH.recievedDeckFromServer(sSeed, hMatrix, scrambledDeck);
        //CardInfoHandle.cHandler.initializeDeck(gUDeck);
        Debug.Log("Server found game with selectedUser:  " + selectedUser);
        TurnHandler.TH.isFirstToPlay(!strongOffer);
        if (strongOffer)
        {
            StartCoroutine(createInitialOffer());
        }
    }
    private void OnGameRefound(bool strongOffer)
    {
        peer.RestartIce();
        UserState.changeState(UserState.statesEnum.Negotiating);

        Debug.LogError("Server RE-found game");
        if (strongOffer)
        {
            Debug.Log("Creating Media Offer");
            StartCoroutine(createInitialOffer());
        }
        socketSend(0, JsonConvert.SerializeObject(""), new byte[0]);    //ping server to know your not DCed
    }
    void OnKickPlayer()
    {
        Debug.LogError("SERVER FORCE QUIT");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    private void OnMediaOffer(byte[] msgBytes, int initJsonLeng, string mOFrom)
    {
        if (mOFrom.Equals(selectedUser) && UserState.uState == UserState.statesEnum.Negotiating)
        {
            Debug.Log("mediaOfferRecieved");
            //Starting process for recieving offer (setting descriptions and creating answer)
            string mOffer = Encoding.UTF8.GetString(msgBytes, 8 + initJsonLeng, msgBytes.Length - 8 - initJsonLeng);
            StartCoroutine(onRecievedOffer(JsonConvert.DeserializeObject<RTCSessionDescription>(mOffer), mOFrom));
        }
    }
    private void OnMediaAnswer(byte[] msgBytes, int initJsonLeng, string mAFrom)
    {
        if (mAFrom.Equals(selectedUser) && UserState.uState == UserState.statesEnum.Negotiating)
        {
            //Start setting remote desc when recieving answer
            Debug.Log("mediaAnswerRecieved");
            string mAnswer = Encoding.UTF8.GetString(msgBytes, 8 + initJsonLeng, msgBytes.Length - 8 - initJsonLeng);
            StartCoroutine(onRecievedAnswer(JsonConvert.DeserializeObject<RTCSessionDescription>(mAnswer)));
        }
    }
    private void OnIceCandidate(byte[] msgBytes, int initJsonLeng)
    {
        Debug.Log("IceCandidateRecieved");
        try
        {
            string iCand = Encoding.UTF8.GetString(msgBytes, 8 + initJsonLeng, msgBytes.Length - 8 - initJsonLeng);
            peer.AddIceCandidate(new RTCIceCandidate(JsonConvert.DeserializeObject<RTCIceCandidateInit>(iCand)));
        }
        catch
        {
            //ice candidate got rejected
        }
    }
    //___________________________________________________________________________________________________________

    //create all sender and reciever channels with corresponding message queue and recieve message events
    void createChannels()
    {
        peer.OnDataChannel = (evtChannel) =>
        {
            if (evtChannel.Label.Equals("main")){
                evtChannel.OnMessage = ((msg) => handleReceiveMessage(msg, 0));
            }
            else
            {
                int recieverIndex = getDataChannelIndex(evtChannel.Label);
                evtChannel.OnMessage = ((msg) => handleReceiveMessage(msg, recieverIndex));
            }
            channelRecievers.Add(evtChannel);
            recieverSetup++;
            startGameIfWebRTCReady();
        };
        for (int i = 0; i < dataChannelList.Count; i++)
        {
            int channelIndex = i;   //create hard copy of i (we need i to be a constant value and not go from 0 -> channel number)
            RTCDataChannelInit dataChannelOptions = new RTCDataChannelInit();
            dataChannelOptions.ordered = dataChannelList[channelIndex].ordered;
            if (dataChannelList[channelIndex].maxRetransmits != -1){
                dataChannelOptions.maxRetransmits = dataChannelList[channelIndex].maxRetransmits; }
            channelSenders.Add(peer.CreateDataChannel(dataChannelList[channelIndex].channelName, dataChannelOptions));

            channelSenders[channelIndex].OnOpen = () => {
                channelSenders[channelIndex].Send("Hi");
                senderSetup++;
                startGameIfWebRTCReady();
            };
            outgoingQList.Add(new Queue<byte[]>());
            outgoingQListSize.Add(0);
            msgRecievedEvents.Add(nonNullFunc2);
            msgAboutToSendEvents.Add(nonNullFunc1);
        }
        dataChannelsRecreated?.Invoke();
    }
    void nonNullFunc1() { }
    void nonNullFunc2(byte[] msg) { }
    IEnumerator distributedChannelCreation()
    {
        yield return new WaitForSecondsRealtime(distributedDelta);
        StartCoroutine(handleSocketCommunication());
        for (int i=0; i< dataChannelList.Count; i++)
        {
            yield return new WaitForSecondsRealtime(distributedDelta);
            int channelIndex = i; //hard copy of i
            StartCoroutine(handleOutgoingQueue(channelIndex, (float)dataChannelList[channelIndex].updateTimerMs / 1000, dataChannelList[channelIndex].handleSendQueueOneByOne));
        }
    }

    void configurePeer()
    {
        senderSetup = 0;
        recieverSetup = 0;
        channelSenders.Clear();
        channelRecievers.Clear();
        outgoingQList.Clear();
        outgoingQListSize.Clear();
        msgRecievedEvents.Clear();
        msgAboutToSendEvents.Clear();
        //WRTC: Creating peer with ICE server configuration
        RTCConfiguration config = default;
        config.iceServers = new RTCIceServer[]
        {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302", "stun:stun3.l.google.com:19302", "stun:stun4.l.google.com:19302", "turn:turn.anyfirewall.com:443?transport=tcp" }, credential = "webrtc", username="webrtc" }   //MISSING TURN SERVER CONFIG
        };
        peer = new RTCPeerConnection(ref config);
        
        //when ICE candidate is found send it to other peer (no coroutine needed here)
        peer.OnIceCandidate = async (candidate) =>
        {
            Debug.Log("iceCandidateFound");

            RTCIceCandidateInit initCandidate = new RTCIceCandidateInit();
            initCandidate.candidate = candidate.Candidate;
            initCandidate.sdpMid = candidate.SdpMid;
            initCandidate.sdpMLineIndex = candidate.SdpMLineIndex;

            genericExchange iCandWrap = new genericExchange();
            iCandWrap.from = user;
            iCandWrap.to = selectedUser;
            byte[] initiCandidateBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(initCandidate));
            await socketSend(10, JsonConvert.SerializeObject(iCandWrap), initiCandidateBytes);
        };
        //if the connection drops atemp reconect
        peer.OnIceConnectionChange = (change) =>
        {
            if(change == RTCIceConnectionState.Failed || change == RTCIceConnectionState.Disconnected)
            {
                Debug.LogError("Lost connection to other player");
                RequestReconnect();         //dced players will eventually send this socket meesage through the lefotvers queue
            }
            else if (change == RTCIceConnectionState.Connected && UserState.uState != UserState.statesEnum.Gaming)
            {
                startGameIfWebRTCReady();
            }
        };
        //DATA CHANNELS
        createChannels();
    }

    //when all channels are properly created game starts
    void startGameIfWebRTCReady()
    {
        if (senderSetup >= dataChannelList.Count && recieverSetup >= dataChannelList.Count)
        {
            Debug.Log("GAME STARTING!");
            UserState.changeState(UserState.statesEnum.Gaming);
        }
    }

    //close connection and reconffigure it for another game if needed
    void disconnectWebRTC()
    {
        Debug.Log("Disconecting");
        //close all channels and connecitons
        for (int i = 0; i < channelRecievers.Count; i++)
        {
            if(channelRecievers[i] != null)
                channelRecievers[i].Close();
        }
        for (int i = 0; i < channelSenders.Count; i++)
        {
            if (channelSenders[i] != null)
                channelSenders[i].Close();
        }
        peer.Close();

        //reconfigure webRTC peer (no need to reconfigure Socket)
        configurePeer();
    }

    //WebRTC message code:
    //-------------------------------------------------------------------------------
    //this function is invoked every time non main channels recieves message
    void handleReceiveMessage(byte[] msgBytesComplete, int channelIndex)
    {
        if(msgBytesComplete.Length <= 4)
        {
            return;
        }
        List<uint> headersInt = new List<uint>();
        uint currentPos = 0;
        uint headerInt = 0;

        while (currentPos < msgBytesComplete.Length)
        {
            headerInt = BitConverter.ToUInt32(msgBytesComplete, (int)currentPos);
            //Debug.Log("AAAAAAAAAAAAAAAAA: " + msgBytesComplete[0]+ "   " + msgBytesComplete[1] + "   " + msgBytesComplete[2] + "   " + msgBytesComplete[3]);
            headersInt.Add(headerInt);
            currentPos += 4 + headerInt;
        }

        if(currentPos != msgBytesComplete.Length)
        {
            Debug.LogError("FATAL MESSAGE ERROR: MISSING BYTES!  " + currentPos + "   " + msgBytesComplete.Length);
            return; //put repeat message code here FIX
        }

        currentPos = 4;
        foreach (uint hdr in headersInt)
        {
            byte[] msgBytes = new byte[hdr];
            Array.Copy(msgBytesComplete, currentPos, msgBytes, 0, hdr);
            msgRecievedEvents[channelIndex]?.Invoke(msgBytes);
            currentPos += 4 + hdr;
        }
    }

    void sendMessageToPeer(byte[] msgBytes, int channelIndex)
    {
        if (channelSenders[channelIndex].ReadyState == RTCDataChannelState.Open && msgBytes != null && msgBytes.Length != 0)
        {
            channelSenders[channelIndex].Send(msgBytes);
        }
    }

    //this coroutine is invoked every x miliseconds for each channel and if there are any messages in message queues send them to peer
    IEnumerator handleOutgoingQueue(int channelIndex, float qCheckTimer, bool oneByOne)
    {
        yield return new WaitForSecondsRealtime(qCheckTimer);
        msgAboutToSendEvents[channelIndex]?.Invoke();
        byte[] outGoingMsg;

        //do we send a byte array once per coroutine OR do we merge all arrays in queue into one big msg and send it
        if (oneByOne)
        {
            if(outgoingQList[channelIndex].Count > 0)
            {
                outgoingQListSize[channelIndex] -= (uint)outgoingQList[channelIndex].Peek().Length;
                outGoingMsg = outgoingQList[channelIndex].Dequeue();
                sendMessageToPeer(outGoingMsg, channelIndex);
            }
        }
        else
        {
            outGoingMsg = new byte[outgoingQListSize[channelIndex]];
            int currentPos = 0;

            int messagesToSend = outgoingQList[channelIndex].Count;
            for (int i = 0; i < messagesToSend; i++)
            {
                byte[] partialOutMsg = outgoingQList[channelIndex].Dequeue();
                for (int j = 0; j < partialOutMsg.Length; j++)
                {
                    outGoingMsg[currentPos + j] = partialOutMsg[j];
                }
                currentPos += partialOutMsg.Length;
            }
            sendMessageToPeer(outGoingMsg, channelIndex);
            outgoingQListSize[channelIndex] = 0;
        }

        StartCoroutine(handleOutgoingQueue(channelIndex, qCheckTimer, oneByOne));
    }

    /*public void pushOutgoingMsg(uint action, List<int> blocks, int channelIndex)
    {
        uint size = (uint)blocks.Count * 4 + 4;
        byte[] msgBytes = new byte[size + 4];

        byte[] headerBytes = BitConverter.GetBytes(size);
        Array.Copy(headerBytes, 0, msgBytes, 0, 4);
        byte[] actionBytes = BitConverter.GetBytes(action);
        Array.Copy(actionBytes, 0, msgBytes, 4, 4);

        for (int i = 0; i < blocks.Count; i++)
        {
            byte[] blockBytes = BitConverter.GetBytes(blocks[i]);
            Array.Copy(blockBytes, 0, msgBytes, 8 + i * 4, 4);
        }

        outgoingQList[channelIndex].Enqueue(msgBytes);
        outgoingQListSize[channelIndex] += (size + 4);
    }*/
    public void pushOutgoingMsg(uint action, uint promiseId, List<uint> blocksIDs, List<int> blocksInt, 
                                                             List<float> blocksFloat, List<long> blocksLong, int channelIndex = 0)
    {
        int[] sizes = new int[4];
        sizes[0] = (blocksIDs == null) ? 0 : blocksIDs.Count;
        sizes[1] = (blocksInt == null) ? 0 : blocksInt.Count;
        sizes[2] = (blocksFloat == null) ? 0 : blocksFloat.Count;
        sizes[3] = (blocksLong == null) ? 0 : blocksLong.Count;
        uint totalSize = (uint)((sizes[0] + sizes[1] + sizes[2]) * 4 + sizes[3] * 8);    //total size of 3 lists in bytes
        byte[] msgBytes = new byte[totalSize + 12];

        byte[] headerBytes = BitConverter.GetBytes(totalSize + 8);
        Array.Copy(headerBytes, 0, msgBytes, 0, 4);
        byte[] actionBytes = BitConverter.GetBytes(action);
        Array.Copy(actionBytes, 0, msgBytes, 4, 4);
        byte[] promiseIdBytes = BitConverter.GetBytes(promiseId);
        Array.Copy(promiseIdBytes, 0, msgBytes, 8, 4);

        byte[] blockBytes;
        int lengSoFar = 12;
        for (int i = 0; i < sizes[0]; i++)
        {
            blockBytes = BitConverter.GetBytes(blocksIDs[i]);
            Array.Copy(blockBytes, 0, msgBytes, lengSoFar, 4);
            lengSoFar += blockBytes.Length;
        }
        for (int i = 0; i < sizes[1]; i++)
        {
            blockBytes = BitConverter.GetBytes(blocksInt[i]);
            Array.Copy(blockBytes, 0, msgBytes, lengSoFar, 4);
            lengSoFar += blockBytes.Length;
        }
        for (int i = 0; i < sizes[2]; i++)
        {
            blockBytes = BitConverter.GetBytes(blocksFloat[i]);
            Array.Copy(blockBytes, 0, msgBytes, lengSoFar, 4);
            lengSoFar += blockBytes.Length;
        }
        for (int i = 0; i < sizes[3]; i++)
        {
            blockBytes = BitConverter.GetBytes(blocksLong[i]);
            Array.Copy(blockBytes, 0, msgBytes, lengSoFar, 8);
            lengSoFar += blockBytes.Length;
        }

        outgoingQList[channelIndex].Enqueue(msgBytes);
        outgoingQListSize[channelIndex] += (totalSize + 12);
    }
    public void pushOutgoingMsg(uint action, uint promiseId, byte[] rawBytes, int channelIndex = 0)
    {
        uint size = (uint)rawBytes.Length;
        if(size <= maxBytesPerMsg)
        {
            byte[] msgBytes = new byte[size + 12];

            byte[] headerBytes = BitConverter.GetBytes(size + 8);
            Array.Copy(headerBytes, 0, msgBytes, 0, 4);
            byte[] actionBytes = BitConverter.GetBytes(action);
            Array.Copy(actionBytes, 0, msgBytes, 4, 4);
            byte[] promiseIdBytes = BitConverter.GetBytes(promiseId);
            Array.Copy(promiseIdBytes, 0, msgBytes, 8, 4);

            Array.Copy(rawBytes, 0, msgBytes, 12, size);

            outgoingQList[channelIndex].Enqueue(msgBytes);
            outgoingQListSize[channelIndex] += (size + 12);
        }
        else
        {
            uint subdivisions = (size / maxBytesPerMsg); /* + 1;*/  //last division is smaller so we dont include it here
            byte[] msgBytes;
            byte[] divisionHeaderBytes = BitConverter.GetBytes(maxBytesPerMsg + 8);
            byte[] divisionActionBytes = BitConverter.GetBytes(action);
            byte[] divisionPromiseIdBytes = BitConverter.GetBytes(promiseId);
            uint tracker = 0;
            for (uint i=0; i<subdivisions; i++)
            {
                msgBytes = new byte[maxBytesPerMsg + 12];

                Array.Copy(divisionHeaderBytes, 0, msgBytes, 0, 4);
                Array.Copy(divisionActionBytes, 0, msgBytes, 4, 4);
                Array.Copy(divisionPromiseIdBytes, 0, msgBytes, 8, 4);
                Array.Copy(rawBytes, tracker, msgBytes, 12, maxBytesPerMsg);
                tracker += maxBytesPerMsg;

                outgoingQList[channelIndex].Enqueue(msgBytes);
                outgoingQListSize[channelIndex] += (maxBytesPerMsg + 12);
            }

            //this is for last subdivision (it will always have less bytes than maxBytesPerMsg so we treat it differently
            uint leftover = (uint)rawBytes.Length - tracker;
            msgBytes = new byte[leftover + 12];

            byte[] lastDivisionHeaderBytes = BitConverter.GetBytes(leftover + 8);
            Array.Copy(lastDivisionHeaderBytes, 0, msgBytes, 0, 4);
            byte[] lastDivisionActionBytes = BitConverter.GetBytes(action+1);
            Array.Copy(lastDivisionActionBytes, 0, msgBytes, 4, 4);
            Array.Copy(divisionPromiseIdBytes, 0, msgBytes, 8, 4);
            Array.Copy(rawBytes, tracker, msgBytes, 12, leftover);

            outgoingQList[channelIndex].Enqueue(msgBytes);
            outgoingQListSize[channelIndex] += (leftover + 12);
        }
    }

    void printMsgBytes(byte[] msgBytes)
    {
        string aux = "";
        for(var i=0; i<msgBytes.Length; i++)
        {
            aux += (msgBytes[i].ToString() + ", ");
        }
        Debug.Log("asdasdasd:   " + aux);
    }
    //-------------------------------------------------------------------------------


    //INITIAL OFFER _____________________________________________________________________________________________________________________
    //create offer and when ready set local description
    IEnumerator createInitialOffer()
    {
        var offer = peer.CreateOffer();
        yield return offer;

        if (!offer.IsError)
        {
            StartCoroutine(onCreateOfferSucces(offer.Desc));
        }
    }

    //set local description and when ready send offer via websocket
    IEnumerator onCreateOfferSucces(RTCSessionDescription desc)
    {
        var operation = peer.SetLocalDescription(ref desc);
        yield return operation;

        if (!operation.IsError)
        {
            Debug.Log("ME: => " + user);
            Debug.Log("Sending Media Offer");

            genericExchange mOfferWrap = new genericExchange();
            mOfferWrap.from = user;
            mOfferWrap.to = selectedUser;
            byte[] descBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(desc));

            var task = Task.Run(() => socketSend(8, JsonConvert.SerializeObject(mOfferWrap), descBytes));
            yield return new WaitUntil(() => task.IsCompleted);     //Task Coroutine slopy merge
            Debug.Log("Sending Media Offer Done");
        }
    }
    //INITIAL OFFER _____________________________________________________________________________________________________________________

    //RECIEVE OFFER AND SEND ANSWER _____________________________________________________________________________________________________
    //on recieving offer set remote desc and when done start creating answer
    IEnumerator onRecievedOffer(RTCSessionDescription desc, string from)
    {
        var operation = peer.SetRemoteDescription(ref desc);
        yield return operation;

        if (!operation.IsError)
        {
            StartCoroutine(onRecievedOfferDesctiptionSet(from));
        }

    }

    //create answer and when ready start setting local desc with anser
    IEnumerator onRecievedOfferDesctiptionSet(string from)
    {
        var answer = peer.CreateAnswer();
        yield return answer;

        if (!answer.IsError)
        {
            StartCoroutine(onAnswerCreated(answer.Desc, from));
        }
    }

    //set local desc with answer and when ready send answer via Websocket (to other peer)
    IEnumerator onAnswerCreated(RTCSessionDescription desc, string from)
    {
        var operation = peer.SetLocalDescription(ref desc);
        yield return operation;

        if (!operation.IsError)
        {
            Debug.Log("Sending Answer");
            genericExchange mAnswerWrap = new genericExchange();
            mAnswerWrap.from = user;
            mAnswerWrap.to = selectedUser;
            byte[] descBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(desc));
            
            var task = Task.Run(() => socketSend(9, JsonConvert.SerializeObject(mAnswerWrap), descBytes));
            yield return new WaitUntil(() => task.IsCompleted);     //Task Coroutine slopy merge
        }
    }
    //RECIEVE OFFER AND SEND ANSWER _____________________________________________________________________________________________________

    //RECIEVE ANSWER ____________________________________________________________________________________________________________________
    //when recieving answer set remote description
    IEnumerator onRecievedAnswer(RTCSessionDescription desc)
    {
        var operation = peer.SetRemoteDescription(ref desc);
        yield return operation;
    }
    //RECIEVE ANSWER ____________________________________________________________________________________________________________________


    IEnumerator nonNullCoroutine()
    {
        yield return null;
    }

    int getDataChannelIndex(string chName)
    {
        for(int i=0; i<dataChannelList.Count; i++)
        {
            if (dataChannelList[i].channelName.Equals(chName))
            {
                return i;
            }
        }
        return -1;
    }

    //LOGIN REGISTER SETUP:
    IEnumerator attemptUserExists(RegisterSetup registerSetup, string usrName)
    {
        UnityWebRequest www = new UnityWebRequest("http://darkserver-env.eba-jryyi8dq.eu-west-3.elasticbeanstalk.com/checkUser/" + usrName, "GET", new DownloadHandlerBuffer(), dummyData);

        yield return www.SendWebRequest();
        if (www.error == null)
        {
            var parsedObject = JObject.Parse(www.downloadHandler.text);
            int resultCode = JsonConvert.DeserializeObject<int>(parsedObject["code"].ToString());

            switch (resultCode)
            {
                default:
                case 10:
                    registerSetup.handleFinishUserExistsError();
                    break;
                case 11:
                    registerSetup.handleFinishUserExistsError();
                    Debug.LogError("FATAL DUPLICATION ERROR!!");
                    break;
                case 12:
                    registerSetup.handleFinishUserExistsSucces();
                    break;
                case 13:
                    registerSetup.handleFinishUserExistsAlready();
                    break;
            }
        }
        else
        {
            registerSetup.handleFinishUserExistsError();
        }
    }

    //setup socket connection and all listener methods 
    async Task attemptLogin(string usrName, string usrPass)
    {
        bool validSocketClose = false;
        Debug.Log("Atempting Login...");
        try {
            socket = new ClientWebSocket();
            int newKeys = (EncryptionHandler.EH.newKeys) ? 1 : 0;
            await socket.ConnectAsync(new Uri("ws://darkserver-env.eba-jryyi8dq.eu-west-3.elasticbeanstalk.com/login/" + usrName + "/" + usrPass + "/" + newKeys + "/" + EncryptionHandler.EH.skBytesSHA256 + "/" + EncryptionHandler.EH.pkBytesSHA256 + "/"), CancellationToken.None);

            var buffer = new ArraySegment<byte>(new byte[2048]);
            do
            {
                WebSocketReceiveResult result;
                using (var ms = new MemoryStream())
                {
                    do
                    {
                        result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                        ms.Write(buffer.Array, buffer.Offset, result.Count);
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        validSocketClose = true;
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        string socketReturnMsg = result.CloseStatusDescription;
                        var parsedObject = JObject.Parse(socketReturnMsg);
                        int resultCode = JsonConvert.DeserializeObject<int>(parsedObject["code"].ToString());
                        switch (resultCode)
                        {
                            default:
                            case 10:
                                loginSetup.handleFinishLoginError();
                                break;
                            case 11:
                                loginSetup.handleFinishLoginError();
                                break;
                            case 12:
                                loginSetup.handleFinishLoginCredFail();
                                break;
                            case 13:
                                //nothing went wrong
                                break;
                        }
                        return;
                    }

                    ms.Seek(0, SeekOrigin.Begin);
                    HandleMessageRecieved(ms.ToArray());
                }
            } while (!socket.CloseStatus.HasValue);

        }
        catch (WebSocketException ex)
        {
            validSocketClose = false;
            Debug.LogError(ex);
            loginSetup.handleFinishLoginError();
        }
        finally
        {
            if (Application.isPlaying && !validSocketClose)
            { 
                await Task.Delay(5000);     // try every 5 seconds
                await attemptLogin(usrName, usrPass);
            }
        }
    }
    IEnumerator attemptRegister(RegisterSetup registerSetup, string usrName, string usrPass, string usrEmail)
    {
        UnityWebRequest www = new UnityWebRequest("http://darkserver-env.eba-jryyi8dq.eu-west-3.elasticbeanstalk.com/register/" + usrName + "/" + usrPass + "/" + usrEmail, "GET", new DownloadHandlerBuffer(), dummyData);

        yield return www.SendWebRequest();
        if (www.error == null)
        {
            var parsedObject = JObject.Parse(www.downloadHandler.text);
            int resultCode = JsonConvert.DeserializeObject<int>(parsedObject["code"].ToString());
            switch (resultCode)
            {
                default:
                case 20:
                case 21:
                case 22:
                    registerSetup.handleFinishRegisterError();
                    break;
                case 23:
                    //register succesfull!
                    registerSetup.handleFinishRegisterSucces();
                    break;
            }
        }
        else
        {
            registerSetup.handleFinishRegisterError();
        }
    }

    public void attemptUserExistsStart(RegisterSetup registerSetup, string usrName)
    {
        StartCoroutine(attemptUserExists(registerSetup, usrName));
    }
    public void attempLoginStart(LoginSetup lSetup, string usrName, string usrPass)
    {
        loginSetup = lSetup;
        attemptLogin(usrName, computeSHA256Hash(usrPass));
    }
    public void attempRegisterStart(RegisterSetup registerSetup, string usrName, string usrPass, string usrEmail)
    {
        StartCoroutine(attemptRegister(registerSetup, usrName, computeSHA256Hash(usrPass), computeSHA256Hash(usrEmail)));
    }

    //https://stackoverflow.com/questions/12416249/hashing-a-string-with-sha256
    string computeSHA256Hash(string text)
    {
        using (var sha256 = new SHA256Managed())
        {
            return BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
    }
    string computeSHA256Hash(byte[] bytes, int offset, int count)
    {
        using (var sha256 = new SHA256Managed())
        {
            return BitConverter.ToString(sha256.ComputeHash(bytes, offset, count)).Replace("-", "");
        }
    }

    //find gratest maximum divisor for time distributed coroutine start
    //https://www.geeksforgeeks.org/gcd-two-array-numbers/
    int gcd(int a, int b)
    {
        if (a == 0)
            return b;
        return gcd(b % a, a);
    }
    int findGCD()
    {
        int result = dataChannelList[0].updateTimerMs;
        for (int i = 1; i < dataChannelList.Count; i++)
        {
            result = gcd(dataChannelList[i].updateTimerMs, result);
            if (result == 1)
            {
                break;
            }
        }
        //last check is based on an aditional channel that comunicates with server
        result = gcd(socketLeftoverTimerMs, result);
        return result;
    }



    //setup socket connection and all listener methods (only called once at the starts of game)
}

