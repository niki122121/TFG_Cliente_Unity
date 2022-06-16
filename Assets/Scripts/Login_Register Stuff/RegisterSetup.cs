using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class RegisterSetup : MonoBehaviour
{
    [SerializeField] GameObject userInput;
    [SerializeField] GameObject emailInput;
    [SerializeField] GameObject passInput;
    [SerializeField] GameObject confirmPassInput;

    [SerializeField] GameObject userCheckIcon;
    [SerializeField] List<GameObject> correctIcons;
    [SerializeField] List<GameObject> correctionErrorTexts;

    [SerializeField] GameObject registerBtn;
    [SerializeField] LogRegSwitcher switcher;

    int validUser = -1;
    bool validEmail = false;
    bool validPassword = false;
    bool validConfirmPass = false;

    bool canRegister = false;

    const string MatchEmailPattern =
            @"^(([\w-]+\.)+[\w-]+|([a-zA-Z]{1}|[\w-]{2,}))@"
     + @"((([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])\."
     + @"([0-1]?[0-9]{1,2}|25[0-5]|2[0-4][0-9])\.([0-1]?
				[0-9]{1,2}|25[0-5]|2[0-4][0-9])){1}|"
     + @"([a-zA-Z0-9]+[\w-]+\.)+[a-zA-Z]{1}[a-zA-Z0-9-]{1,23})$";


    //Code from: https://www.codeproject.com/Articles/22777/Email-Address-Validation-Using-Regular-Expression
    bool IsEmail(string email)
    {
        if (email != null) return Regex.IsMatch(email, MatchEmailPattern);
        else return false;
    }

    private void Start()
    {
        registerBtn.GetComponent<Button>().onClick.AddListener(register);
        userInput.GetComponent<InputField>().onEndEdit.AddListener(delegate (string text) { performCheckUser(false); });
        emailInput.GetComponent<InputField>().onEndEdit.AddListener(delegate (string text) { performCheckEmail(); });
        passInput.GetComponent<InputField>().onEndEdit.AddListener(delegate (string text) { performCheckPass(); });
        confirmPassInput.GetComponent<InputField>().onEndEdit.AddListener(delegate (string text) { performCheckConfirmPass(); });
    }

    void performCheckUser(bool finalCheck)
    {
        string userToCheck = userInput.GetComponent<InputField>().text;

        userCheckIcon.SetActive(false);
        if (userToCheck.Length >= 4 && userToCheck.Length <= 30)
        {
            if (finalCheck)
            {
                validUser = 1;
                correctIcons[0].SetActive(true);
                correctIcons[0].GetComponent<Image>().color = new Color(0, 1, 0);
                correctionErrorTexts[0].SetActive(false);
                userCheckIcon.SetActive(false);
            }
            else
            {
                validUser = 0;
                correctIcons[0].SetActive(false);
                userCheckIcon.SetActive(true);
                registerBtn.GetComponent<ButtonSetup>().buttonUnavailable();
                userExists();
            }
        }
        else
        {
            validUser = -1;
            correctIcons[0].SetActive(true);
            correctIcons[0].GetComponent<Image>().color = new Color(1, 0, 0);
            correctionErrorTexts[0].SetActive(true);
            correctionErrorTexts[0].GetComponent<Text>().text = "Username must be between 4 and 30 characters long.";
        }
    }
    void performCheckEmail()
    {
        string emailToCehck = emailInput.GetComponent<InputField>().text;
        validEmail = IsEmail(emailToCehck);

        correctIcons[1].SetActive(true);
        if (validEmail)
        {
            correctIcons[1].GetComponent<Image>().color = new Color(0,1,0);
            correctionErrorTexts[1].SetActive(false);
        }
        else
        {
            correctIcons[1].GetComponent<Image>().color = new Color(1, 0, 0);
            correctionErrorTexts[1].SetActive(true);
        }
    }
    void performCheckPass()
    {
        string passwordToCheck = passInput.GetComponent<InputField>().text;

        correctIcons[2].SetActive(true);
        if (passwordToCheck.Length >= 6 && passwordToCheck.Length  <= 30 && passwordToCheck.Any(char.IsDigit) && passwordToCheck.Any(char.IsLetter))
        {
            validPassword = true;
            correctIcons[2].GetComponent<Image>().color = new Color(0, 1, 0);
            correctionErrorTexts[2].SetActive(false);
        }
        else
        {
            validPassword = false;
            correctIcons[2].GetComponent<Image>().color = new Color(1, 0, 0);
            correctionErrorTexts[2].SetActive(true);
        }
    }
    void performCheckConfirmPass()
    {
        string confirmPassToCheck = confirmPassInput.GetComponent<InputField>().text;

        correctIcons[3].SetActive(true);
        if (confirmPassToCheck.Equals(passInput.GetComponent<InputField>().text))
        {
            validConfirmPass = true;
            correctIcons[3].GetComponent<Image>().color = new Color(0, 1, 0);
            correctionErrorTexts[3].SetActive(false);
        }
        else
        {
            validConfirmPass = false;
            correctIcons[3].GetComponent<Image>().color = new Color(1, 0, 0);
            correctionErrorTexts[3].SetActive(true);
        }
    }

    public void register()
    {
        performCheckUser(true);
        performCheckEmail();
        performCheckPass();
        performCheckConfirmPass();
        if (validUser==1 && validEmail && validPassword && validConfirmPass)
        {
            registerBtn.GetComponent<ButtonSetup>().buttonWaiting();
            OnlineHandler.OH.attempRegisterStart(this, userInput.GetComponent<InputField>().text, passInput.GetComponent<InputField>().text, emailInput.GetComponent<InputField>().text);
        }
    }

    public void handleFinishRegisterSucces()
    {
        switcher.showLog();
        registerBtn.GetComponent<ButtonSetup>().buttonReady();
    }

    public void handleFinishRegisterError()
    {
        Debug.LogError("Registration Error");
        //registerBtn.GetComponent<ButtonSetup>().buttonReady();
    }

    public void userExists()
    {
        OnlineHandler.OH.attemptUserExistsStart(this, userInput.GetComponent<InputField>().text);
    }

    public void handleFinishUserExistsSucces()
    {
        registerBtn.GetComponent<ButtonSetup>().buttonReady();
        validUser = 1;
        correctIcons[0].SetActive(true);
        correctIcons[0].GetComponent<Image>().color = new Color(0, 1, 0);
        correctionErrorTexts[0].SetActive(false);
        userCheckIcon.SetActive(false);
    }
    public void handleFinishUserExistsAlready()
    {
        registerBtn.GetComponent<ButtonSetup>().buttonReady();
        validUser = -1;
        correctIcons[0].SetActive(true);
        correctIcons[0].GetComponent<Image>().color = new Color(1, 0, 0);
        correctionErrorTexts[0].GetComponent<Text>().text = "User already taken";
        correctionErrorTexts[0].SetActive(true);
        userCheckIcon.SetActive(false);
    }
    public void handleFinishUserExistsError()
    {
        registerBtn.GetComponent<ButtonSetup>().buttonReady();
        validUser = -1;
        correctIcons[0].SetActive(true);
        correctIcons[0].GetComponent<Image>().color = new Color(1, 0, 0);
        correctionErrorTexts[0].GetComponent<Text>().text = "Error connecting to server";
        correctionErrorTexts[0].SetActive(true);
        userCheckIcon.SetActive(false);
    }
}
