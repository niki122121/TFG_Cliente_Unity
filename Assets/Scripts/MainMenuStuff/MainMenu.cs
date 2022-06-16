using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{

    [SerializeField] GameObject fndBtn;
    [SerializeField] GameObject dropDown;

    void Start()
    {
        var dropdownComp = dropDown.GetComponent<Dropdown>();
        dropdownComp.options.Clear();
        foreach (string option in OnlineHandler.OH.countryList)
        {
            dropdownComp.options.Add(new Dropdown.OptionData(option));
        }
        dropdownComp.onValueChanged.AddListener(delegate {
            OnlineHandler.OH.country = dropdownComp.value;
        });
    }

    public void findGame()
    {
        fndBtn.SetActive(false);
        OnlineHandler.OH.findGame();
    }
    public void queueExit()
    {
        OnlineHandler.OH.queueExit();
        fndBtn.SetActive(true);
    }
}
