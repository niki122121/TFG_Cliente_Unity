using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameParamHolder : MonoBehaviour
{
    public GraphicRaycaster raycaster;
    public Text phaseText;
    public Button nextPhaseBtn;
    public Button resignBtn;
    public Button sendChatBtn;
    public InputField chatInput;
    public Text chatTextArea;
    public GameObject playerHandObj;
    public RectTransform handStartTransform;
    public GameObject playerField;
    public GameObject neutralField;
    public GameObject enemyField;
    public Transform cardField;

    private void Start()
    {
        GameHandler.GH.setupGameObjects(this);
    }
}
