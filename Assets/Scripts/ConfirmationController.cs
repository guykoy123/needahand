using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class ConfirmationController : MonoBehaviour
{
    public GameObject panel;
    public TMP_Text confirmationMessage;

    Action<bool> callback;

    // Start is called before the first frame update
    void Start()
    {
        panel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void DisplayMessage(string message, Action<bool> action)
    {
        if(callback!= null)
        {
            Debug.LogError("couldn't open confirmation message, has one open from " + callback.ToString());
        }
        panel.SetActive(true);
        confirmationMessage.text = message;
        callback = action;
    }

    public void Yes()
    {
        callback(true);
        callback = null;
        panel.SetActive(false);
    }

    public void No()
    {
        callback(false);
        callback = null;
        panel.SetActive(false);
    }
}
