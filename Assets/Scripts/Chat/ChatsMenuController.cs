using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//using Mono.Data.Sqlite; 
using System.Data;
using Mono.Data.Sqlite;
using System;
using UnityEngine.UI;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
//using System.Net.WebSockets;
using WebSocketSharp;
using UnityEditor;
using System.Runtime.InteropServices;

public class ChatsMenuController : MonoBehaviour
{
    //general controller for the chat part of the app

    public GameObject ChatsMenuCanvas;//disable and enable to bring chat into view
    public GameObject ChatsContentView; //under this will create the chat objects to appear in the scroll area
    GameObject ChatPrefab; //chat object prefab
    public GameObject MessagesContentView;
    public GameObject NoChatsMessage; //simple text to notify user that there are no active chats
    List<Chat> chats = new List<Chat>(); //list of all active chats
    List<ChatController>  chatControllers = new List<ChatController>();
  
    HttpClient client = new HttpClient();
    bool got_post = false;
    Task<List<Post>> get_post_task;
    bool got_users=false;
    Task<List<User>> get_users_task;

    //lists for loading data from db
    List<int[]> ActiveChats;
    List<Post> posts;
    List<User> users;

    //Flags
    bool FinishedSetup=false; //indicate if chats are displayed on screen and ready for the user
    bool CreatingNewChat=false; //indicate in the process of creating new chat instance to avoid confusion with receiving user and post data from server

    public GameObject ChatScreen;
    public GameObject ChatTitle;

    public ChatInputController chatInput;
    public Task connectionTask;


    // holds messages for newly created chats(by other users)
    // that ChatsMenuController hasn't create a Chat or ChatController object for
    Queue<Dictionary<string,string>> messageQueue=new Queue<Dictionary<string, string>>(); 

    // Start is called before the first frame update
    void Start()
    {
        //setup connection
        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());

        //load chat object prefab
        ChatPrefab=Resources.Load<GameObject>("Chat");

        NoChatsMessage.SetActive(false);
        
        
    }

    // Update is called once per frame
    void Update()
    {
        //display chats
        if(get_post_task != null && get_users_task != null){
            
            if(get_post_task.IsCompleted && get_users_task.IsCompleted) { // \&& !FinishedSetup
                //create chat object and add to list
                posts = get_post_task.Result;
                users = get_users_task.Result;
                //clear tasks for next use
                get_post_task = null;
                get_users_task =null;
                //save chats to the list
                chats=new List<Chat>();
                if (ActiveChats != null){
                    Debug.Log("Found " +ActiveChats.Count.ToString() + " chats");
                    for(int i=0;i<ActiveChats.Count;i++){
                        int k=0;
                        while(ActiveChats[i][1]!=users[k].pk){
                            k++;
                            Debug.Log(k.ToString());
                            }
                        Debug.Log("user index:" +k.ToString());
                        Chat c = new Chat(ActiveChats[i][0],posts[i],users[i]);
                        chats.Add(c);
                        Debug.Log("added chat :" + c.get_id());
                    

                    }
                    //load chat list to screen
                    InstatiateChatObjectsOnScreen();
                    // FinishedSetup=true;
                }

            }
        }
    }

    public void OpenChatsMenuScreen(bool chat_reload_flag){
        
        if (!chat_reload_flag){
            Debug.Log("opening chat menu");
            ChatsMenuCanvas.GetComponent<Canvas>().enabled=true;
            }
        
        
        ActiveChats = DatabaseController.GetActiveChats();
        if (ActiveChats.Count>0){
            //get separate id lists for each object type to make a request to server for more info
            int[] post_ids = new int[ActiveChats.Count];
            int[] user_ids = new int[ActiveChats.Count];
            int[] chat_ids = new int[ActiveChats.Count];
            for (int i=0;i<ActiveChats.Count;i++){
                user_ids[i]=ActiveChats[i][1];
                chat_ids[i]=ActiveChats[i][0];
                post_ids[i]=ActiveChats[i][2];
            }
            // Debug.Log(String.Join("; ", ActiveChats[0]));
            get_post_task = GetPostAsync(post_ids);
            get_users_task = GetUsersAsync(user_ids);
        }
        else if(!chat_reload_flag){
            NoChatsMessage.SetActive(true);
        }

    }
    void InstatiateChatObjectsOnScreen(){
        Debug.Log("loading chat objects on screen");
        //clear screen of old chats
        try{
            Debug.Log("Content view child count: " + ChatsContentView.transform.childCount);
            Transform[] old_chats = ChatsContentView.transform.GetComponentsInChildren<Transform>();
            for(int i =1;i<old_chats.Length;i++){
                Debug.Log("destroyed: " + old_chats[i].name);
                Destroy(old_chats[i].gameObject);
            }
        }
        catch(Exception e){
            Debug.Log(e.ToString());
        }
        //clear chatControllers list
        chatControllers=new List<ChatController>();
        //reload new active chats
        for(int i=0;i<chats.Count;i++){
            GameObject t = Instantiate<GameObject>(ChatPrefab);
            t.transform.SetParent(ChatsContentView.transform,false);
            ChatController controller = t.GetComponent<ChatController>();
            controller.SetChat(chats[i]);
            Debug.Log("created chat: " + chats[i].get_id());

            if(CreatingNewChat && i == chats.Count-1){
                controller.OpenChat();
            }

            chatControllers.Add(controller);
        }

        //after creating all chats, push messages that chats missed
        while(messageQueue.Count!=0){
            Dictionary<string,string> message = messageQueue.Dequeue();
            for(int j=0;j<chatControllers.Count;j++){
                if(chatControllers[j].GetChat().GetPostID()==Int32.Parse(message["post"])){
                    DateTime time_sent = DateTime.ParseExact(message["time_sent"], "yyyy-MM-dd HH:mm",System.Globalization.CultureInfo.InvariantCulture);
                    chatControllers[j].ReceiveMessage(message["contents"],time_sent,Int32.Parse(message["packet_id"]));
                }
            }
        }
    }

    async Task<List<Post>> GetPostAsync(int[] id)
    {
        //return Post objects from the server
        Debug.Log("getting active chats posts info");
        //create request
        var url = "api/get_post_by_id";
        string id_list = "["+string.Join(",", id) +"]";
        Dictionary<string, string> postData = new Dictionary<string, string>
        {
            { "pk", id_list }
        };
        var data = new FormUrlEncodedContent(postData);
        //POST to server
        var response = await client.PostAsync(url, data);
        if (response.IsSuccessStatusCode)
        {
            //read response
            Debug.Log("got response about posts");
            var resp = await response.Content.ReadAsStringAsync();
            List<Post> posts = JsonConvert.DeserializeObject<List<Post>>(resp);
            return posts;
        }

        Debug.Log("error when trying get_post_by_id for chat:" + response.StatusCode);
        Debug.Log(response);
        return null;
    }
    async Task<List<User>> GetUsersAsync(int[] id)
    {
        //get User objects from the server
        Debug.Log("getting active chats users info");
        //create request
        var url = "api/get_user_by_id";
        string id_list = "["+string.Join(",", id) +"]";
        Dictionary<string, string> POSTData = new Dictionary<string, string>
        {
            { "pk", id_list }
        };
        var data = new FormUrlEncodedContent(POSTData);
        //POST to server
        var response = await client.PostAsync(url, data);
        if(response.IsSuccessStatusCode){
            //read response
            var resp = await response.Content.ReadAsStringAsync();
            List<User> users = JsonConvert.DeserializeObject<List<User>>(resp);
            return users;
        }
        Debug.Log("error when trying get_user_by_id for chat:" + response.StatusCode);
        Debug.Log(response);
        return null;
    }
    public void CloseChatsMenuScreen(){
        ChatsMenuCanvas.GetComponent<Canvas>().enabled=false;
    }
    

    public GameObject GetChatScreen(){
        return this.ChatScreen;
    }
    public GameObject GetChatTitle(){
        return this.ChatTitle;
    }

    public ChatInputController GetChatInput(){
        return this.chatInput;
    }

    public void StartNewChat(int post_id,int post_user_id, bool open_chat_screen){
        Debug.Log("starting chat on post:" + post_id.ToString());
        DatabaseController.CreateNewChat(post_id,post_user_id);
        if (open_chat_screen){
            CreatingNewChat=true;
            OpenChatsMenuScreen(false);
        }
        
    }

    public Chat GetChatByPost(int post_id){
        for(int i=0;i<chats.Count;i++){
            if (chats[i].GetPostID()==post_id){
                return chats[i];
            }
        }
        return null;
    }

    public ChatController GetChatController(Chat chat){
        for(int i=0;i<chatControllers.Count;i++){
            if (chatControllers[i].GetChat()==chat){
                return chatControllers[i];
            }
        }
        return null;
    }

    public void PushMessage(Dictionary<string,string> message_data){
        messageQueue.Enqueue(message_data);
        //TODO: add notification for new message
        Debug.Log("pushed message into queue; total: " + messageQueue.Count.ToString());
    }

    public void ackMessage(Dictionary<string,string> data,bool read){
        /*
        handles updating the received/read field based on flag
        for the message both in db and if the chat is currently open updated ChatController
        */
        Dictionary<string,int> db_dict= new Dictionary<string, int>
        {
            { "user", Int32.Parse(data["user"]) }, //set my user id
            { "packet_id", Int32.Parse(data["packet_id"]) }
        };
        int chat_id = DatabaseController.ackMessage(db_dict,read);
        foreach(ChatController chat in chatControllers){
            if (chat.GetChat().get_id()==chat_id){
                chat.ackMessage(Int32.Parse(data["packet_id"]),read);
            }
        }

    }
}
