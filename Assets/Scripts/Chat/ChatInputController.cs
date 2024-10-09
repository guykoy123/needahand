using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Numerics;
using Vector3 = UnityEngine.Vector3;

public class ChatInputController : MonoBehaviour
{
    public TMP_InputField input;
    public Button SendButton;
    public Transform ChatScrollView;
    public Transform InputRow;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(TouchScreenKeyboard.visible){
            Debug.Log("keyboard open");
            // Vector3 chatScrollPosition = ChatScrollView.position;
            // chatScrollPosition.y = 40;
            // ChatScrollView.position = chatScrollPosition;
            ChatScrollView.GetComponent<RectTransform>().sizeDelta=new UnityEngine.Vector2(0,-130);
            Vector3 inputRowPos = InputRow.position;
            inputRowPos.y +=130;
            InputRow.position = inputRowPos;


            //TODO: make sure the screen is scaled correctly
        }
        //TODO: close keyboard on tap of back button
    }

    public string GetText(){
        return this.input.text;
    }
    public void ClearInputField(){
        this.input.text="";
    }

    public Button GetSendButton(){
        return this.SendButton;
    }

    public void OpenKeyboard(){
        Debug.Log("opening keyboard");
        TouchScreenKeyboard.Open("Enter message...",TouchScreenKeyboardType.Default);
    }
}
