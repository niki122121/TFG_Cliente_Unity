using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.Research.SEAL;
using System.IO;
using SealUtilites;
using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

public class EncryptionHandler : MonoBehaviour
{
    public static EncryptionHandler EH;

    int cardParamCount = 8;
    int cardCopies = 4;

    //random generation
    [HideInInspector] public int rndSeed;
    System.Random rndGenerator;
    int serverSeed;

    //homomorfic variables and objects
    [SerializeField] ulong polyModDegree = 8192;    //4096 8192
    int rowSize;
    [SerializeField] int plainModBits = 22;         //16 20
    int plainModVal;
    SEALContext sealContext;
    BatchEncoder universalBatchEncoder;
    Evaluator universalEvaluator;

    //PLAYER STUFF:
    //matrix values:
    List<ulong> scrambler1;
    List<ulong> scrambler2;
    uint[] hashMatrix;

    //encryption stuff:
    Decryptor decryptor;
    SecretKey secretKey;
    PublicKey publicKey;

    //serialized byte data:
    [HideInInspector] public byte[] secretKeyBytes;
    [HideInInspector] public byte[] publicKeyBytes;
    [HideInInspector] public byte[] scrambler1Bytes;
    [HideInInspector] public byte[] scrambler2Bytes;

    //OTHER STUFF (encrypted with other players Secret Key)
    Encryptor otherEncryptor;
    PublicKey otherPublicKey;
    Ciphertext scrambledDeckCipher;
    Ciphertext otherS1Cipher;
    Ciphertext otherS2Cipher;
    [HideInInspector] public MemoryStream playerRecieverStream;

    //key generation variables
    [HideInInspector] public string skBytesSHA256 = "0000000000000000000000000000000000000000000000000000000000000000";
    [HideInInspector] public string pkBytesSHA256 = "0000000000000000000000000000000000000000000000000000000000000000";
    public bool newKeys = true;

    enum ActionType { Draw, DrawTurnStart, SummonH, SummonD};
    void Awake()
    {
        //encryption handler is singleton, if for whatever reason more than 1 is found its is deleted
        if (EncryptionHandler.EH == null)
        {
            EncryptionHandler.EH = this;
        }
        else if (EncryptionHandler.EH != this)
        {
            Destroy(this.gameObject);
            return;
        }

        EncryptionParameters parms = new EncryptionParameters(SchemeType.BFV);
        parms.PolyModulusDegree = polyModDegree;
        parms.CoeffModulus = CoeffModulus.Create(polyModDegree, new int[] { 50, 35, 35, 35, 50 });
        parms.PlainModulus = PlainModulus.Batching(polyModDegree, plainModBits);
        plainModVal = (int)parms.PlainModulus.Value;
        sealContext = new SEALContext(parms);

        rowSize = (int)polyModDegree / 2;

        Debug.Log(plainModVal); // 1032193

        universalBatchEncoder = new BatchEncoder(sealContext);
        universalEvaluator = new Evaluator(sealContext);
        //load keys
        getKeys();

        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        playerRecieverStream = new MemoryStream();
        OnlineHandler.OH.gameStarted += onGameStarted;
        OnlineHandler.OH.msgRecievedEvents[0] += ((rawBytes) => recieveEncryptionStuff(rawBytes));
        OnlineHandler.OH.gameEnded += resetGameVariables;

        OnlineHandler.OH.dataChannelsRecreated += resetChannelStuff;

        scrambler1Bytes = null;
        scrambler2Bytes = null;
    }

    //when data channels are recreated this listeners are lost so we have to reset them
    public void resetChannelStuff()
    {
        OnlineHandler.OH.msgRecievedEvents[0] -= ((rawBytes) => recieveEncryptionStuff(rawBytes));
        OnlineHandler.OH.msgRecievedEvents[0] += ((rawBytes) => recieveEncryptionStuff(rawBytes));
    }

    struct jsonBytes
    {
        public byte[] bytes;
    }

    //seed used for scrambler generation (reamins until users logs out)
    public void setupGlobalSeed(int seed)
    {
        Debug.Log("RANDOM SEED FROM SERVER:  " + seed);
        rndSeed = seed;
    }

    int getGameRndSeed(int gameNum)
    {
        rndGenerator = new System.Random(rndSeed);
        int seedToReturn = 0;
        for (int i = 0; i < gameNum; i++)
        {
            seedToReturn = rndGenerator.Next();
        }
        return seedToReturn;
    }

    //get sk and pk from player prefs
    public void getKeys()
    {
        string secretKeyString = PlayerPrefs.GetString("sKey", "");
        string publicKeyString = PlayerPrefs.GetString("pKey", "");

        if (string.IsNullOrEmpty(secretKeyString) || string.IsNullOrEmpty(publicKeyString))
        {
            newKeys = true;
            skBytesSHA256 = "0000000000000000000000000000000000000000000000000000000000000000";
            pkBytesSHA256 = "0000000000000000000000000000000000000000000000000000000000000000";
            Debug.LogError("MISSING KEYS");
        }
        else
        {
            try
            {
                newKeys = false;

                jsonBytes skJson = JsonConvert.DeserializeObject<jsonBytes>(secretKeyString);
                jsonBytes pkJson = JsonConvert.DeserializeObject<jsonBytes>(publicKeyString);
                secretKeyBytes = skJson.bytes;
                publicKeyBytes = pkJson.bytes;


                using MemoryStream secretKeyStream = new MemoryStream();
                using MemoryStream publicKeyStream = new MemoryStream();
                secretKeyStream.Write(secretKeyBytes, 0, secretKeyBytes.Length);
                publicKeyStream.Write(publicKeyBytes, 0, publicKeyBytes.Length);

                secretKey = new SecretKey();
                publicKey = new PublicKey();
                secretKeyStream.Seek(0, SeekOrigin.Begin);
                publicKeyStream.Seek(0, SeekOrigin.Begin);
                secretKey.Load(sealContext, secretKeyStream);
                publicKey.Load(sealContext, publicKeyStream);

                decryptor = new Decryptor(sealContext, secretKey);

                //compute hashes of keys
                skBytesSHA256 = computeSHA256Hash(secretKeyBytes, 0, secretKeyBytes.Length);
                pkBytesSHA256 = computeSHA256Hash(publicKeyBytes, 0, publicKeyBytes.Length);
            }
            catch (Exception ex)
            {
                newKeys = true;
                skBytesSHA256 = "0000000000000000000000000000000000000000000000000000000000000000";
                pkBytesSHA256 = "0000000000000000000000000000000000000000000000000000000000000000";
                Debug.LogError("MISSING KEYS    " + ex.Message);
            }
        }
    }

    //turn byte array into sk and pk and save into player prefs
    public void setKeys(byte[] msgBytes, int startPos)
    {
        newKeys = false;
        int sKeyLeng = (int)BitConverter.ToUInt64(msgBytes, startPos + 8);

        using MemoryStream secretKeyStream = new MemoryStream();
        using MemoryStream publicKeyStream = new MemoryStream();
        secretKeyStream.Write(msgBytes, startPos, sKeyLeng);
        publicKeyStream.Write(msgBytes, startPos + sKeyLeng, msgBytes.Length - startPos - sKeyLeng);

        secretKey = new SecretKey();
        publicKey = new PublicKey();
        secretKeyStream.Seek(0, SeekOrigin.Begin);
        publicKeyStream.Seek(0, SeekOrigin.Begin);
        secretKey.Load(sealContext, secretKeyStream);
        publicKey.Load(sealContext, publicKeyStream);

        decryptor = new Decryptor(sealContext, secretKey);

        secretKeyBytes = secretKeyStream.ToArray();
        publicKeyBytes = publicKeyStream.ToArray();

        //save keys in player prefs
        jsonBytes skJson = new jsonBytes();
        jsonBytes pkJson = new jsonBytes();
        skJson.bytes = secretKeyBytes;
        pkJson.bytes = publicKeyBytes;
        string secretKeyString = JsonConvert.SerializeObject(skJson);
        string publicKeyString = JsonConvert.SerializeObject(pkJson);
        PlayerPrefs.SetString("sKey", secretKeyString);
        PlayerPrefs.SetString("pKey", publicKeyString);

        //compute hashes of keys
        skBytesSHA256 = computeSHA256Hash(secretKeyBytes, 0, secretKeyBytes.Length);
        pkBytesSHA256 = computeSHA256Hash(publicKeyBytes, 0 , publicKeyBytes.Length);
    }

    //generate random scramblers and their ciphers from random seed
    public void initScramblers()
    {
        scrambler1 = new List<ulong>();
        scrambler2 = new List<ulong>();

        System.Random localRandScrambler = new System.Random(getGameRndSeed(OnlineHandler.OH.gameNum));
        for (int i = 0; i < (int)polyModDegree; i++)
        {
            scrambler1.Add((ulong)localRandScrambler.Next(0, plainModVal));
            scrambler2.Add((ulong)localRandScrambler.Next(0, plainModVal));
        }

        using Encryptor encryptor = new Encryptor(sealContext, publicKey);

        using Plaintext plainTScrambler1 = new Plaintext();
        using Plaintext plainTScrambler2 = new Plaintext();
        universalBatchEncoder.Encode(scrambler1, plainTScrambler1);
        universalBatchEncoder.Encode(scrambler2, plainTScrambler2);

        using Serializable<Ciphertext> scrambler1Cipher = encryptor.Encrypt(plainTScrambler1);
        using Serializable<Ciphertext> scrambler2Cipher = encryptor.Encrypt(plainTScrambler2);

        using MemoryStream ms1 = new MemoryStream();
        using MemoryStream ms2 = new MemoryStream();
        scrambler1Cipher.Save(ms1, ComprModeType.ZSTD);
        scrambler2Cipher.Save(ms2, ComprModeType.ZSTD);

        scrambler1Bytes = ms1.ToArray();
        scrambler2Bytes = ms2.ToArray();
    }

    //recieved scrambled deck from server (deck scrambled by the other players s1 scrambler)
    public void recievedDeckFromServer(int sSeed, uint[] hMatrix, uint[] scrambledDeck)
    {
        serverSeed = sSeed;
        hashMatrix = hMatrix;
        List<ulong> scrambledDeckList = new List<ulong>();
        for (int i = 0; i < (int)polyModDegree; i++)
        {
            scrambledDeckList.Add((ulong)scrambledDeck[i]);
        }

        Plaintext plainTSscrambledDeck = new Plaintext();
        universalBatchEncoder.Encode(scrambledDeckList, plainTSscrambledDeck);

        scrambledDeckCipher = new Ciphertext();
        
        if(otherEncryptor != null)
        {
            otherEncryptor.Encrypt(plainTSscrambledDeck, scrambledDeckCipher);
            plainTSscrambledDeck.Dispose();
            onGameStartedAndProperInit();
        }
        else
        {
            PromiseHandler.PH.createSpecificPromise(1, false, (promiseBytes) =>
            {
                otherEncryptor.Encrypt(plainTSscrambledDeck, scrambledDeckCipher);
                plainTSscrambledDeck.Dispose();
                onGameStartedAndProperInit();
            });
        }
    }

    //game started
    void onGameStarted()
    {
        //send pk is bytes to enemy
        OnlineHandler.OH.pushOutgoingMsg(1000, 0, publicKeyBytes);
        OnlineHandler.OH.pushOutgoingMsg(1002, 0, scrambler1Bytes);
        OnlineHandler.OH.pushOutgoingMsg(1004, 1, scrambler2Bytes);
        scrambler1Bytes = null;
        scrambler2Bytes = null;

        //create promise that enemy will eventually ask for initial draw 
        Action<byte[]> promiseFunc = onDirDRequest(ActionType.Draw, GameHandler.GH.initialCardDraw, 1008, 2);
        PromiseHandler.PH.createSpecificPromise(2, false, promiseFunc);
    }


    //game started AND all encryption parameters have been properly initialized
    void onGameStartedAndProperInit()
    {
        int[] initialDrawArray = new int[GameHandler.GH.initialCardDraw];
        for (int i = 0; i < GameHandler.GH.initialCardDraw; i++)
        {
            initialDrawArray[i] = i;
        }
        prepareCipher_InHand(scrambledDeckCipher, 1, initialDrawArray);

        byte[] dd_Bytes = requestDirD_Bytes(ActionType.Draw, 3, false, initialDrawArray, null);
        OnlineHandler.OH.pushOutgoingMsg(1006, 3, dd_Bytes);
    }

    //main recieve method for channel 0 (data channel for encryption data)
    void recieveEncryptionStuff(byte[] rawBytes)
    {
        uint action = System.BitConverter.ToUInt32(rawBytes, 0);
        uint promiseId = System.BitConverter.ToUInt32(rawBytes, 4);
        switch (action)
        {
            //pk is initialization
            case 1000:
            case 1001:
            case 1002:
            case 1003:
            case 1004:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                break;
            case 1005:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                onPlayerInit();
                PromiseHandler.PH.resolvePromise(1, null);
                break;

            //initial draw response
            case 1006:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                break;
            case 1007:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                PromiseHandler.PH.resolvePromise(2, null);
                break;
            //after initial draw response 
            case 1008:
                PromiseHandler.PH.resolvePromise(3, rawBytes);
                break;
            
            //enemy solicits draw
            /*case 2010:
                Action<byte[]> promiseFunc_draw = onDirDRequest(ActionType.Draw, 1, 1012, promiseId);
                PromiseHandler.pHandler.createSpecificPromise(promiseId, promiseFunc_draw);
                break;
            //generic draw response
            case 1010:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                break;
            case 1011:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                PromiseHandler.pHandler.resolvePromise(promiseId, null);
                break;
            //after generic draw response
            case 1012:
                PromiseHandler.pHandler.resolvePromise(promiseId, rawBytes);
                break;*/


            //enemy summons card (FIX: can olny summon on player turn)
            case 2014:
                int cardNum_summon = (rawBytes.Length - 8) / 28;       //divide by 4 bytes per param and 7 (only 1/7 of bytes reference card ids)
                int[] cardOrders_summon = new int[cardNum_summon];
                for (int i = 0; i < cardNum_summon; i++)
                {
                    cardOrders_summon[i] = BitConverter.ToInt32(rawBytes, i * 4 + 8) + GameHandler.GH.deckSize;
                }
                byte[] dd_Bytes_summon = requestDirD_Bytes(ActionType.SummonH, promiseId, true, cardOrders_summon, rawBytes);
                OnlineHandler.OH.pushOutgoingMsg(1014, promiseId, dd_Bytes_summon);
                break;
            case 1014:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                break;
            case 1015:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                PromiseHandler.PH.resolvePromise(promiseId, null);
                break;
            //after generic draw response
            case 1016:
                PromiseHandler.PH.resolvePromise(promiseId, rawBytes);
                break;


            //enemy ends turn
            case 2018:
                PromiseHandler.PH.createOrderedAction(null, (oActionBytes) =>
                {
                    if (!TurnHandler.TH.playerTurn)
                    {
                        TurnHandler.TH.enDRAWMAIN_to_enCOMBAT();

                        int[] cardOrders_endT = new int[1] { GameHandler.GH.nextCardToDraw };
                        prepareCipher_InHand(scrambledDeckCipher, 1, cardOrders_endT);
                        byte[] dd_Bytes_endT = requestDirD_Bytes(ActionType.DrawTurnStart, promiseId, true, cardOrders_endT, new byte[0]);
                        OnlineHandler.OH.pushOutgoingMsg(1018, promiseId, dd_Bytes_endT);
                    }
                });
                break;
            case 1018:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                break;
            case 1019:
                playerRecieverStream.Write(rawBytes, 8, rawBytes.Length - 8);
                PromiseHandler.PH.resolvePromise(promiseId, null);
                break;
            case 1020:
                PromiseHandler.PH.resolvePromise(promiseId, rawBytes);
                break;

            default:
                Debug.LogError("RECIEVED DATA WITH INVALID ACTION");    //FIX contact server (not sure if necesary)
                break;
        }


    }

    private void onPlayerInit()
    {
        byte[] recievedData = playerRecieverStream.ToArray();

        Debug.Log("LAST data chunk, initialization between players complete!");

        int recievedPkLeng = (int)BitConverter.ToUInt64(recievedData, 8);
        int recievedS1Leng = (int)BitConverter.ToUInt64(recievedData, 8 + recievedPkLeng);
        int recievedS2Leng = (int)BitConverter.ToUInt64(recievedData, 8 + recievedPkLeng + recievedS1Leng);


        string recievedPkHash = computeSHA256Hash(recievedData, 0, recievedPkLeng);
        string recievedS1Hash = computeSHA256Hash(recievedData, recievedPkLeng, recievedS1Leng);
        string recievedS2Hash = computeSHA256Hash(recievedData, recievedPkLeng + recievedS1Leng, recievedS2Leng);


        if (!recievedPkHash.Equals(OnlineHandler.OH.otherPkHash))
        {
            Debug.LogError("KEY HASH MISMATCH, CONTACT SERVER");
            //FIX!!!!!!!!!!  PLAYER WINS BY DEFAULT SINCE OTHER PLAYER IS TRYING TO SEND WRONG PKEY
        }
        if (!recievedS1Hash.Equals(OnlineHandler.OH.otherS1Hash))
        {
            Debug.LogError("S1 HASH MISMATCH, CONTACT SERVER");
            //FIX
        }
        if (!recievedS2Hash.Equals(OnlineHandler.OH.otherS2Hash))
        {
            Debug.LogError("S2 HASH MISMATCH, CONTACT SERVER");
            //FIX
        }

        playerRecieverStream.Seek(0, SeekOrigin.Begin);

        otherPublicKey = new PublicKey();
        otherS1Cipher = new Ciphertext();
        otherS2Cipher = new Ciphertext();
        otherPublicKey.Load(sealContext, playerRecieverStream);

        otherS1Cipher.Load(sealContext, playerRecieverStream);
        otherS2Cipher.Load(sealContext, playerRecieverStream);

        otherEncryptor = new Encryptor(sealContext, otherPublicKey);

        playerRecieverStream.Seek(0, SeekOrigin.Begin);
    }

    //DRAW FUNCTION:
    public void genericDraw()
    {
        /*uint promiseId = PromiseHandler.pHandler.createPromiseId();
        int[] cardOrders = new int[1] { GameHandler.gHandler.nextCardToDraw };

        prepareCipher_InHand(scrambledDeckCipher, 1, cardOrders);

        byte[] dd_Bytes = requestDirD_Bytes(ActionType.Draw, promiseId, cardOrders, null);
        OnlineHandler.oHandler.pushOutgoingMsg(2010, promiseId, new byte[0]);
        OnlineHandler.oHandler.pushOutgoingMsg(1010, promiseId, dd_Bytes);*/
    }

    void setupTurnPromise(uint promiseId)
    {
        //FIX this is need to have correct turns (where every player expects the actions of the other)
        /*PromiseHandler.PH.createSpecificPromise(4, (promiseBytes) =>
        {
            if (!TurnHandler.TH.playerTurn)
            {
                TurnHandler.TH.nextPhase();

                int[] cardOrders_endT = new int[1] { GameHandler.GH.nextCardToDraw };
                prepareCipher_InHand(scrambledDeckCipher, 1, cardOrders_endT);
                byte[] dd_Bytes_endT = requestDirD_Bytes(ActionType.DrawTurnStart, promiseId, cardOrders_endT, new byte[0]);
                OnlineHandler.OH.pushOutgoingMsg(1018, promiseId, dd_Bytes_endT);
            }
        });*/
    }

    //SUMMON CARD FUCNTION
    public void genericSummon(List<int> cardDRefs ,List<float> cardPos, List<long> standartCardPos)
    {
        int cardAmount = cardDRefs.Count;
        uint promiseId = PromiseHandler.PH.createPromiseId();
        Action<byte[]> promiseFunc = onDirDRequest(ActionType.SummonH, cardAmount, 1016, promiseId);
        PromiseHandler.PH.createSpecificPromise(promiseId, false, promiseFunc);

        //init msg
        OnlineHandler.OH.pushOutgoingMsg(2014, promiseId, null, cardDRefs, cardPos, standartCardPos);
    }

    //END TURN FUNCTION:
    public void genericEndTurn()
    {
        int cardAmount = 1;
        uint promiseId = PromiseHandler.PH.createPromiseId();
        Action<byte[]> promiseFunc = onDirDRequest(ActionType.DrawTurnStart, cardAmount, 1020, promiseId);
        PromiseHandler.PH.createSpecificPromise(promiseId, false, promiseFunc);

        //init msg
        OnlineHandler.OH.pushOutgoingMsg(2018, promiseId, new byte[0]);
    }

    byte[] requestDirD_Bytes(ActionType actionT, uint promiseId, bool orderedRequest, int[] cardOrders, byte[] aditionalInfo)
    {
        int cardNum = cardOrders.Length;
        int rowSize = (int)polyModDegree / 2;

        using Ciphertext deck = new Ciphertext(scrambledDeckCipher);
        universalEvaluator.SubInplace(deck, otherS1Cipher);
        List<ulong> redundancyMatrtix;
        List<ulong> aditionalSMatrix;
        prepareCipher_DirD(deck, cardOrders, out redundancyMatrtix, out aditionalSMatrix);

        universalEvaluator.ModSwitchToInplace(deck, sealContext.LastParmsId);
        using MemoryStream ms = new MemoryStream();
        deck.Save(ms, ComprModeType.ZSTD);

        //create positions to send to other player and keep a copy of unsorted positions
        int[,] returnPos = new int[cardNum, cardParamCount * cardCopies];
        for (int cOrd = 0; cOrd < cardNum; cOrd++)
        {
            int[] positions = new int[cardParamCount * cardCopies];
            for (int i = 0; i < cardParamCount * cardCopies; i++)
            {
                int pos_adjusted = cardOrders[cOrd] * cardParamCount * cardCopies + i;
                positions[i] = (int)hashMatrix[pos_adjusted];
                returnPos[cOrd, i] = (int)hashMatrix[pos_adjusted];
            }
            Array.Sort(positions);
            byte[] positionsBytes = new byte[cardParamCount * cardCopies * 4];
            for (int i = 0; i < cardParamCount * cardCopies; i++)
            {
                returnPos[cOrd, i] = findFirstPosition(returnPos[cOrd, i], positions);

                byte[] auxByteArray = BitConverter.GetBytes(positions[i]);
                Array.Copy(auxByteArray, 0, positionsBytes, i * 4, 4);
            }
            ms.Write(positionsBytes, 0, positionsBytes.Length);
        }

        Action<byte[]> promiseFunc = onDirDResponse(actionT, cardNum, cardOrders, returnPos, aditionalSMatrix, redundancyMatrtix, aditionalInfo);
        PromiseHandler.PH.createSpecificPromise(promiseId, orderedRequest, promiseFunc);

        return ms.ToArray();
    }

    //ACTION GENERATION FUNCTIONS

    //what to do when oponent requests a direct decipher
    Action<byte[]> onDirDRequest(ActionType actionT, int cardNum, uint actionId, uint promiseId)
    {
        Action<byte[]> promiseFunc = (promiseBytes) =>
        {
            playerRecieverStream.Seek(0, SeekOrigin.Begin);
            using Ciphertext otherPlayerCipher = new Ciphertext();
            otherPlayerCipher.Load(sealContext, playerRecieverStream);

            using Plaintext decryptedOtherPlayerDeck = new Plaintext();
            List<ulong> decodedOtherPlayerDeck = new List<ulong>();

            //decrypt and decode scrambler ciphers
            decryptor.Decrypt(otherPlayerCipher, decryptedOtherPlayerDeck);
            universalBatchEncoder.Decode(decryptedOtherPlayerDeck, decodedOtherPlayerDeck);

            byte[] returnPromiseBytes = new byte[cardParamCount * cardCopies * 4 * cardNum];
            byte[] cipherPositionHash = new byte[cardParamCount * cardCopies];

            for (int cOrd = 0; cOrd < cardNum; cOrd++)
            {
                byte[] positionsBytes = new byte[cardParamCount * cardCopies * 4];
                playerRecieverStream.Read(positionsBytes, 0, positionsBytes.Length);

                string paramText = "";
                paramText += serverSeed;

                for (int i = 0; i < cardParamCount * cardCopies; i++)
                {
                    int cParam = BitConverter.ToInt32(positionsBytes, i * 4);
                    paramText += cParam;

                    int adjustedPos = cOrd * cardParamCount * cardCopies + i;
                    Array.Copy(BitConverter.GetBytes((uint)decodedOtherPlayerDeck[cParam]), 0, returnPromiseBytes, adjustedPos * 4, 4);
                    cipherPositionHash[i] = (byte)decodedOtherPlayerDeck[cParam + rowSize];
                }

                byte[] recievedPositionHash = computeSHA256HashRaw(paramText);
                if (!recievedPositionHash.SequenceEqual(cipherPositionHash))
                {
                    Debug.LogError("POSITIONS MISMATCH CONTACT SERVER");    //FIX contact server
                }
            }

            if (actionT == ActionType.Draw || actionT == ActionType.DrawTurnStart)
            {
                int[] newlyAdded = new int[cardNum];
                for(int i = 0; i < cardNum; i++)
                {
                    newlyAdded[i] = GameHandler.GH.nextCardToDraw_Enemy;
                    GameHandler.GH.nextCardToDraw_Enemy++;
                }
                prepareCipher_InHand(scrambledDeckCipher, 1, newlyAdded);
            }

            OnlineHandler.OH.pushOutgoingMsg(actionId, promiseId, returnPromiseBytes);
            playerRecieverStream.Seek(0, SeekOrigin.Begin);
        };
        return promiseFunc;
    }

    //what to do after direct decipher request has been answered
    Action<byte[]> onDirDResponse(ActionType actionT, int cardNum, int[] cardOrders, int[,] returnPos, 
                                    List<ulong> aditionalSMatrix, List<ulong> redundancyMatrtix, byte[] aditionalInfo)
    {
        Action<byte[]> promiseFunc = (promiseBytes) =>
        {
            bool allOk = true;
            uint[] decryptedResult = new uint[cardParamCount * cardCopies * cardNum];
            uint[,] prepedResult = new uint[cardNum, cardParamCount * cardCopies];

            if (promiseBytes.Length != decryptedResult.Length * 4 + 8)
            {
                allOk = false;
            }
            else
            {
                for (int i = 0; i < cardParamCount * cardCopies * cardNum; i++)
                {
                    decryptedResult[i] = BitConverter.ToUInt32(promiseBytes, i * 4 + 8);                //+8 since we recieve some extra header bytes
                }
                //decrypted result is send with its positions ordered from smallest to largest (we need to reverse it to its original order)
                for (int cOrd = 0; cOrd < cardNum; cOrd++)
                {
                    for (int i = 0; i < cardParamCount * cardCopies; i++)
                    {
                        prepedResult[cOrd, i] = decryptedResult[cOrd * cardParamCount * cardCopies + returnPos[cOrd, i]];
                    }
                }

                for (int cOrd = 0; cOrd < cardNum; cOrd++)
                {
                    for (int i = 0; i < cardParamCount * cardCopies; i++)
                    {
                        int adjustedGlobalPos = (int)hashMatrix[cardOrders[cOrd] * cardParamCount * cardCopies + i];

                        if (prepedResult[cOrd, i] >= (uint)aditionalSMatrix[adjustedGlobalPos])
                        {
                            prepedResult[cOrd, i] = prepedResult[cOrd, i] - (uint)aditionalSMatrix[adjustedGlobalPos];                        //a = pM*0 + res - n
                        }
                        else
                        {
                            prepedResult[cOrd, i] = (uint)plainModVal + prepedResult[cOrd, i] - (uint)aditionalSMatrix[adjustedGlobalPos];    //a = pM*1 + res - n  (find value before adition matrix)
                        }
                        if (prepedResult[cOrd, i] >= plainModVal || prepedResult[cOrd, i] % (uint)redundancyMatrtix[adjustedGlobalPos] != 0)
                        {
                            allOk = false;
                        }
                        prepedResult[cOrd, i] = prepedResult[cOrd, i] / (uint)redundancyMatrtix[adjustedGlobalPos];
                        if (i >= cardParamCount * (cardCopies - 1))
                        {
                            if (prepedResult[cOrd, i] != prepedResult[cOrd, i - cardParamCount] ||
                                prepedResult[cOrd, i] != prepedResult[cOrd, i - cardParamCount * 2] ||
                                prepedResult[cOrd, i] != prepedResult[cOrd, i - cardParamCount * 3])
                            {
                                allOk = false;
                            }
                        }
                    }
                }
            }
            if (allOk)
            {
                if (actionT == ActionType.Draw)
                {
                    GameHandler.GH.drawCards(cardNum, cardOrders, prepedResult);
                }
                else if (actionT == ActionType.DrawTurnStart)
                {
                    GameHandler.GH.setupNextCardDraw(cardNum, cardOrders, prepedResult);
                }
                else if(actionT == ActionType.SummonH)
                {
                    GameHandler.GH.summonCards(cardNum, cardOrders, prepedResult, aditionalInfo);
                }
            }
            else
            {
                Debug.LogError("DETECTED RESULT DIFF, CONTACT SERVER");  //FIX contact server
            }
        };

        return promiseFunc;
    }


    //PREPARE CIPHER FUNCTIONS: 
    //FIX allow negative adder params (ulong => long)
    void prepareCipher_InHand(Ciphertext cipher, ulong adderValue, int[] cardOrders)
    {
        int cardNum = cardOrders.Length;
        int rowSize = (int)polyModDegree / 2;

        List<ulong> adder = new List<ulong>();

        for (int i = 0; i < (int)polyModDegree; i++)
        {
            adder.Add(0);
        }
        for (int cOrd = 0; cOrd < cardNum; cOrd++)
        {
            for (int i = 0; i < cardParamCount * cardCopies; i++)
            {
                int hashedPosition = (int)hashMatrix[cardParamCount * cardCopies * cardOrders[cOrd] + i];

                if ((i + 1) % cardParamCount == 0)
                {
                    adder[hashedPosition] = adderValue;              //add +1 (or something else to in-hand parameter)
                }
                else
                {
                    adder[hashedPosition] = 0;
                }
                adder[hashedPosition + rowSize] = 0;
            }
        }

        using Plaintext plainTAdder = new Plaintext();
        universalBatchEncoder.Encode(adder, plainTAdder);

        using Ciphertext adderCipher = new Ciphertext();
        otherEncryptor.Encrypt(plainTAdder, adderCipher);

        universalEvaluator.AddInplace(cipher, adderCipher);
    }

    void prepareCipher_DirD(Ciphertext cipher, int[] cardOrders, out List<ulong> redundancyMatrtix, out List<ulong> aditionalSMatrix)
    {
        int cardNum = cardOrders.Length;
        int rowSize = (int)polyModDegree / 2;

        System.Random localRandScrambler = new System.Random(getGameRndSeed(1000000 + OnlineHandler.OH.gameNum));

        List<ulong> eliminator = new List<ulong>();
        redundancyMatrtix = new List<ulong>();
        aditionalSMatrix = new List<ulong>();
        for (int i = 0; i < (int)polyModDegree; i++)
        {
            eliminator.Add(0);
            redundancyMatrtix.Add(0);
            aditionalSMatrix.Add(0);
        }

        for (int cOrd = 0; cOrd < cardNum; cOrd++)
        {
            for (int i = 0; i < cardParamCount * cardCopies; i++)
            {
                int hashedPosition = (int)hashMatrix[cardParamCount * cardCopies * cardOrders[cOrd] + i];

                eliminator[hashedPosition] = 1;
                eliminator[hashedPosition + rowSize] = 1;

                redundancyMatrtix[hashedPosition] = (ulong)localRandScrambler.Next(1, plainModVal / 1000);
                redundancyMatrtix[hashedPosition + rowSize] = 1;

                aditionalSMatrix[hashedPosition] = (ulong)localRandScrambler.Next(0, plainModVal);
                aditionalSMatrix[hashedPosition + rowSize] = 0;
            }
        }

        using Plaintext plainTEliminator = new Plaintext();
        using Plaintext plainTRedundancy = new Plaintext();
        using Plaintext plainTAditioanlS = new Plaintext();
        universalBatchEncoder.Encode(eliminator, plainTEliminator);
        universalBatchEncoder.Encode(redundancyMatrtix, plainTRedundancy);
        universalBatchEncoder.Encode(aditionalSMatrix, plainTAditioanlS);

        using Ciphertext eliminatorCipher = new Ciphertext();
        using Ciphertext redundancyCipher = new Ciphertext();
        using Ciphertext aditionalSCipher = new Ciphertext();
        otherEncryptor.Encrypt(plainTEliminator, eliminatorCipher);
        otherEncryptor.Encrypt(plainTRedundancy, redundancyCipher);
        otherEncryptor.Encrypt(plainTAditioanlS, aditionalSCipher);

        universalEvaluator.MultiplyInplace(cipher, eliminatorCipher);
        universalEvaluator.MultiplyInplace(cipher, redundancyCipher);
        universalEvaluator.AddInplace(cipher, aditionalSCipher);
    }

    public void resetGameVariables()
    {
        scrambledDeckCipher = null;
        otherEncryptor = null;
        otherPublicKey = null;
        otherS1Cipher = null;
        otherS2Cipher = null;
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
    byte[] computeSHA256HashRaw(string text)
    {
        using (var sha256 = new SHA256Managed())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }
    byte[] arrComputeSHA256(int[] array)
    {
        string text = "";
        for (int i = 0; i < array.Length; i++)
        {
            text += array[i];
        }
        using (var sha256 = new SHA256Managed())
        {
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        }
    }

    string arrayToText(byte[] array, int maxLeng = -1)
    {
        string text = "";
        if (maxLeng >= 0)
        {
            for (int i = 0; i < maxLeng; i++)
            {
                text += array[i] + ", ";
            }
        }
        else
        {
            for (int i = 0; i < array.Length; i++)
            {
                text += array[i] + ", ";
            }
        }
        return text;
    }
    string arrayToText(uint[] array, int maxLeng = -1)
    {
        string text = "";
        if(maxLeng >= 0)
        {
            for (int i = 0; i < maxLeng; i++)
            {
                text += array[i] + ", ";
            }
        }
        else
        {
            for (int i = 0; i < array.Length; i++)
            {
                text += array[i] + ", ";
            }
        }
        return text;
    }
    string arrayToText(int[] array, int maxLeng = -1)
    {
        string text = "";
        if (maxLeng >= 0)
        {
            for (int i = 0; i < maxLeng; i++)
            {
                text += array[i] + ", ";
            }
        }
        else
        {
            for (int i = 0; i < array.Length; i++)
            {
                text += array[i] + ", ";
            }
        }
        return text;
    }
    string arrayToTextUint(byte[] array)
    {
        string text = "";
        for (int i = 0; i < array.Length; i+=4)
        {
            text += BitConverter.ToUInt32(array, i) + ", ";
        }
        return text;
    }

    int findFirstPosition(int par, int[] array)
    {
        for(int i = 0; i < array.Length; i++)
        {
            if (array[i] == par)
            {
                return i;
            }
        }
        return -1;
    }
}
