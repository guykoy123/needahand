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

    WebSocket ws;
    // Start is called before the first frame update
    void Start()
    {
        //setup connection
        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());

        //load chat object prefab
        ChatPrefab=Resources.Load<GameObject>("Chat");

        ws = new WebSocket(AppData.WSaddress);
        ws.SetCookie(new WebSocketSharp.Net.Cookie("token",AppData.token.get()));
        
        Debug.Log("trying to connect");
        ws.OnOpen += (sender, e) => {
            Dictionary<string,string> d = new Dictionary<string, string>();
            d.Add("type","test");
            ws.Send(JsonConvert.SerializeObject(d));
            Debug.Log("sent test");
        };    
        ws.OnMessage += (sender, e) => {
            Debug.Log("received: "+e.Data);
        };
        ws.Connect();

        NoChatsMessage.SetActive(false);
        
        /*ClientWebSocket webSocket = new ClientWebSocket();
        CancellationToken cancellationToken = new CancellationToken();
        WebSocketContext context = new WebSocketContext();
        connectionTask = webSocket.ConnectAsync(new Uri(AppData.WSaddress),cancellationToken);*/
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

    public void OpenChatsMenuScreen(){
        Debug.Log("opening chat menu");
        ChatsMenuCanvas.SetActive(true);
        
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
        else{
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
        //reload new active chats
        for(int i=0;i<chats.Count;i++){
            GameObject t = Instantiate<GameObject>(ChatPrefab);
            t.transform.SetParent(ChatsContentView.transform,false);
            t.GetComponent<ChatController>().SetChat(chats[i]);
            Debug.Log("created chat: " + chats[i].get_id());

            if(CreatingNewChat && i == chats.Count-1){
                t.GetComponent<ChatController>().OpenChat();
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
        Dictionary<string, string> postData = new Dictionary<string, string>();
        postData.Add("pk", id_list);
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
        Dictionary<string, string> POSTData = new Dictionary<string, string>();
        POSTData.Add("pk", id_list);
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
        ChatsMenuCanvas.SetActive(false);
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

    public void StartNewChat(Post post){
        Debug.Log("starting chat on post:" + post.pk);
        DatabaseController.CreateNewChat(Int32.Parse(post.pk),post.author);
        CreatingNewChat=true;
        OpenChatsMenuScreen();
    }
}
