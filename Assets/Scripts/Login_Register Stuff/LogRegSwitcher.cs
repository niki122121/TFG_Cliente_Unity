using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogRegSwitcher : MonoBehaviour
{
    [SerializeField] GameObject log;
    [SerializeField] GameObject reg;

    public void showLog()
    {
        reg.SetActive(false);
        log.SetActive(true);
    }
    public void showReg()
    {
        log.SetActive(false);
        reg.SetActive(true);
    }
}
