using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ChatController : MonoBehaviour
{
    //handles the chat in the menu and when chat is open and active

    //UI elements
    GameObject notificationDot;
    TMP_Text username;
    TMP_Text post_title;
    Image user_pic;

    Chat chat;
    
    bool finished_setup=false;
    
    GameObject ChatScreen;
    TMP_Text chatTitle; //title on chat screen
    ChatInputController chatInput;
    bool chatOpen=false;

    GameObject chatScrollView;
    public GameObject messagePrefab;

    List<MessageController> message_list = new List<MessageController>();
    // Start is called before the first frame update
    void Start()
    {
        //get UI elements
        this.notificationDot=transform.GetChild(3).gameObject;
        this.username = transform.GetChild(0).GetComponent<TMP_Text>();
        this.post_title= transform.GetChild(2).GetComponent<TMP_Text>();
        this.user_pic=transform.GetChild(1).GetComponent<Image>();
        finished_setup=true;

       ChatsMenuController chatsMenu = GameObject.FindObjectOfType<ChatsMenuController>();
       this.ChatScreen = chatsMenu.GetChatScreen();
       this.chatTitle = chatsMenu.GetChatTitle().GetComponent<TMP_Text>();
       chatScrollView = chatsMenu.MessagesContentView;
    }

    // Update is called once per frame
    void Update()
    {
        if(finished_setup){ 
            //display the info on screen
            this.username.text= this.chat.GetUsername();
            this.post_title.text=this.chat.GetPostTitle();
            finished_setup=false; //so as to not run this again
        }
        //notification dot handler
        if(this.chat.gotUnreadMessages()){
            notificationDot.SetActive(true);
        }
        else{
            notificationDot.SetActive(false);
        }
    }

    public void SetChat(Chat c){
        this.chat=c;
    }
    
    public void SendMessage(){
        Debug.Log("sending message in chat: " + this.chat.get_id());
        string messageText = chatInput.GetText();
        Message message = DatabaseController.SaveNewMessage(messageText,chat,AppData.user);
        chatInput.ClearInputField();    
        
        //create new message object on screen
        GameObject t= Instantiate<GameObject>(messagePrefab);
        t.transform.SetParent(chatScrollView.transform,false);
        t.transform.SetAsFirstSibling();
        MessageController msg_ctrl = t.GetComponent<MessageController>();
        msg_ctrl.SetMessage(message);
        message_list.Add(msg_ctrl);
        
        //TODO: send message to server
    }

    public void OpenChat(){
        //show chat screen
        this.ChatScreen.SetActive(true);
        ChatScreen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0f;
        this.chatTitle.text = this.chat.GetUsername();
        chatOpen = true;
        
        //remove previous chat messages
        foreach (Transform child in chatScrollView.transform) {
            Debug.Log("destroying " + child.name);
	        GameObject.Destroy(child.gameObject);
        }

        //connect the send button to this chat
        chatInput = GameObject.FindObjectOfType<ChatsMenuController>().GetChatInput();
        Button SendButton = chatInput.GetSendButton();
        SendButton.onClick.RemoveAllListeners();
        SendButton.onClick.AddListener(SendMessage);
    }

}
