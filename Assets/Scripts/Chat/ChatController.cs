using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.AI;

public class ChatController : MonoBehaviour
{
    //handles the chat in the menu and when chat is open and active
    WSController ws;
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

    // holds messages that were sent before user opened the chat screen
    // will create a message controller once chat is opened
    Queue<Message> messagesQueue = new Queue<Message>();  
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

       ws = GameObject.FindObjectOfType<WSController>();
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
        // if(this.chat.gotUnreadMessages()){
        //     notificationDot.SetActive(true);
        // }
        // else{
        //     notificationDot.SetActive(false);
        // }
    }

    public void SetChat(Chat c){
        this.chat=c;
    }
    public Chat GetChat(){
        return this.chat;
    }
    
    public void SendMessage(){
        Debug.Log("sending message in chat: " + this.chat.get_id());
        string messageText = chatInput.GetText();
        Message message = DatabaseController.SaveNewMessage(messageText,chat,AppData.user,DateTime.Now,AppData.requestSN(),false);
        chatInput.ClearInputField();    
        
        //create new message object on screen
        GameObject t= Instantiate<GameObject>(messagePrefab);
        t.transform.SetParent(chatScrollView.transform,false);
        t.transform.SetAsFirstSibling();
        MessageController msg_ctrl = t.GetComponent<MessageController>();
        msg_ctrl.SetMessage(message);
        message_list.Add(msg_ctrl);
    
        ws.sendMessage(message,chat.GetUser().pk,chat.GetPostID());
    }

    public void ReceiveMessage(string new_message,DateTime time_sent,int sn){
        Message message = DatabaseController.SaveNewMessage(new_message,chat,chat.GetUser(),time_sent,sn,true);

        if (chatOpen){
            //create new message object on screen
            GameObject t= Instantiate<GameObject>(messagePrefab);
            t.transform.SetParent(chatScrollView.transform,false);
            t.transform.SetAsFirstSibling();
            MessageController msg_ctrl = t.GetComponent<MessageController>();
            msg_ctrl.SetMessage(message);
            message_list.Add(msg_ctrl);

            //update read in DB
            Dictionary<string,int> msg_dict=new Dictionary<string, int>(){
                {"user",message.GetAuthor().pk},
                {"packet_id",message.GetSN()}
            };
            DatabaseController.ackMessage(msg_dict,true);

            //send read_ack
            ws.read_ackMessage(message);
        }
        else{
            messagesQueue.Enqueue(message);
        }

    }

    public void OpenChat(){
        //show chat screen
        this.ChatScreen.SetActive(true);
        this.ChatScreen.GetComponent<Canvas>().enabled=true;
        ChatScreen.GetComponentInChildren<ScrollRect>().verticalNormalizedPosition=0f;
        this.chatTitle.text = this.chat.GetUsername();
        chatOpen = true;
        
        //remove previous chat messages
        foreach (Transform child in chatScrollView.transform) {
            Debug.Log("destroying " + child.name);
	        GameObject.Destroy(child.gameObject);
        }
        
        //load messages to screen
        List<Message> messages = DatabaseController.GetMessagesFromChat(chat,20,0);

        // convert list of dict to list of Message objects
        for(int i=0;i<messages.Count;i++){
            GameObject t= Instantiate<GameObject>(messagePrefab);
            t.transform.SetParent(chatScrollView.transform,false);
            t.transform.SetAsFirstSibling();
            MessageController msg_ctrl = t.GetComponent<MessageController>();
            msg_ctrl.SetMessage(messages[i]);
            message_list.Add(msg_ctrl);

            //update seen for incoming messages
            if(messages[i].GetAuthor()!=AppData.user){
                if(!messages[i].GetSeen()){
                    //update DB
                    Dictionary<string,int> msg_dict = new Dictionary<string, int>
                    {
                        { "user", messages[i].GetAuthor().pk },
                        { "packet_id", messages[i].GetSN() }
                    };
                    DatabaseController.ackMessage(msg_dict,true);

                    //update message object
                    messages[i].UpdateSeen();

                    //send read ack to server
                    ws.read_ackMessage(messages[i]);
                }
            }
        }

        // for(int i=0;i<messagesQueue.Count;i++){
        //     //create new message object on screen
        //     GameObject t= Instantiate<GameObject>(messagePrefab);
        //     t.transform.SetParent(chatScrollView.transform,false);
        //     t.transform.SetAsFirstSibling();
        //     MessageController msg_ctrl = t.GetComponent<MessageController>();
        //     msg_ctrl.SetMessage(messagesQueue.Dequeue());
        //     message_list.Add(msg_ctrl);
        // }
        //connect the send button to this chat
        chatInput = GameObject.FindObjectOfType<ChatsMenuController>().GetChatInput();
        Button SendButton = chatInput.GetSendButton();
        SendButton.onClick.RemoveAllListeners();
        SendButton.onClick.AddListener(SendMessage);
    }

    public void ackMessage(int sn,bool read){
        foreach (MessageController msg in message_list){
            if (msg.GetAuthorID()==AppData.user.pk){
                if(read){
                    msg.UpdateSeen();
                }
                else{
                    msg.UpdateReceived();
                }
            }
        }
    }
}
