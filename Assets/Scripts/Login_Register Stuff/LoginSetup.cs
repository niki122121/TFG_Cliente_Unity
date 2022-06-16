using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginSetup : MonoBehaviour
{
    [SerializeField] GameObject userInput;
    [SerializeField] GameObject passInput;

    [SerializeField] GameObject loginBtn;

    [SerializeField] GameObject correctionIcon;
    [SerializeField] Text correcitonText;
 
    void Start()
    {
        loginBtn.GetComponent<Button>().onClick.AddListener(login);
    }

    public void login()
    {
        loginBtn.GetComponent<ButtonSetup>().buttonWaiting();
        OnlineHandler.OH.attempLoginStart(this, userInput.GetComponent<InputField>().text, passInput.GetComponent<InputField>().text);

        correctionIcon.SetActive(false);
    }

    public void handleFinishLoginSucces()
    {
        if (SceneManager.GetActiveScene().name.Equals("loginMenu"))
        {
            OnlineHandler.OH.setupUser(userInput.GetComponent<InputField>().text, passInput.GetComponent<InputField>().text);
            //loginBtn.GetComponent<ButtonSetup>().buttonReady();
            SceneManager.LoadScene(2);
        }
    }

    public void handleFinishLoginCredFail()
    {
        if (SceneManager.GetActiveScene().name.Equals("loginMenu"))
        {
            loginBtn.GetComponent<ButtonSetup>().buttonReady();

            correctionIcon.SetActive(true);
            correcitonText.text = "Invalid credentials, try again.";
        }
    }

    public void handleFinishLoginError()
    {

        if (SceneManager.GetActiveScene().name.Equals("loginMenu"))
        {
            loginBtn.GetComponent<ButtonSetup>().buttonReady();

            correctionIcon.SetActive(true);
            correcitonText.text = "Error login in, try again.";
        }

    }
}
