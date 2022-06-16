using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonSetup : MonoBehaviour
{
    [SerializeField] GameObject loading;
    [SerializeField] Text text;
    Color textColor;
    // Start is called before the first frame update
    void Start()
    {
        textColor = text.color;
        loading.SetActive(false);
    }
    public void buttonUnavailable()
    {
        GetComponent<Button>().interactable = false;
        text.color = Color.grey;
    }
    public void buttonWaiting()
    {
        GetComponent<Button>().interactable = false;
        text.gameObject.SetActive(false);
        loading.SetActive(true);
    }

    public void buttonReady()
    {
        GetComponent<Button>().interactable = true;
        loading.SetActive(false);
        text.color = textColor;
        text.gameObject.SetActive(true);
    }
}
