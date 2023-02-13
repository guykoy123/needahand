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

public class ChatsMenuController : MonoBehaviour
{
    public GameObject ChatsMenuCanvas;
    public GameObject ContentView; //under this will create the chat objects to appear in the scroll area
    GameObject ChatPrefab;
    //string conn;

    List<Chat> chats = new List<Chat>();

    HttpClient client = new HttpClient();
    bool got_post = false;
    Task<List<Post>> get_post_task;
    bool got_users=false;
    Task<List<User>> get_users_task;

    List<int[]> ActiveChats;
    List<Post> posts;
    List<User> users;
    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());

        ChatPrefab=Resources.Load<GameObject>("Chat");
    }

    // Update is called once per frame
    void Update()
    {
        //display chats
        if(get_post_task != null && get_users_task != null){
            if(get_post_task.IsCompleted && get_users_task.IsCompleted){
                Debug.Log("displaying posts");
                //create chat object and add to list
                posts = get_post_task.Result;
                users = get_users_task.Result;
                get_post_task = null;
                get_users_task =null;
                for(int i=0;i<ActiveChats.Count;i++){
                    Chat c = new Chat(ActiveChats[i][2],posts[i],users[i]);
                    chats.Add(c);
                    Debug.Log("added chat :" + c.get_id());
                }
                Debug.Log(chats);
                Debug.Log(chats.Count);
                InstatiateChatObjectsOnScreen();
            }
        }
    }

    public void OpenChatsMenuScreen(){
        Debug.Log("opening chat menu");
        ChatsMenuCanvas.SetActive(true);
        ActiveChats = GetActiveChats();

        //get seprate id lists for each object type to make a request to server for more info
        int[] post_ids = new int[ActiveChats.Count];
        int[] user_ids = new int[ActiveChats.Count];
        int[] chat_ids = new int[ActiveChats.Count];
        for (int i=0;i<ActiveChats.Count;i++){
            post_ids[i]=ActiveChats[i][1];
            user_ids[i]=ActiveChats[i][0];
            chat_ids[i]=ActiveChats[i][2];
        }
        get_post_task = GetPostAsync(post_ids);
        get_users_task = GetUsersAsync(user_ids);
    }
    void InstatiateChatObjectsOnScreen(){
        Debug.Log("loading chat objects on screen");
        for(int i=0;i<chats.Count;i++){
            GameObject t = Instantiate<GameObject>(ChatPrefab);
            t.transform.SetParent(ContentView.transform,false);
            t.GetComponent<ChatController>().SetChat(chats[i]);
        }
    }

    async Task<List<Post>> GetPostAsync(int[] id)
    {
        Debug.Log("getting active chats posts info");
        var url = "api/get_post_by_id";
        string id_list = "["+string.Join(",", id) +"]";
        Dictionary<string, string> postData = new Dictionary<string, string>();
        postData.Add("pk", id_list);
        var data = new FormUrlEncodedContent(postData);
        var response = await client.PostAsync(url, data);
        if (response.IsSuccessStatusCode)
        {
            Debug.Log("got response about posts");
            var resp = await response.Content.ReadAsStringAsync();
            List<Post> posts = JsonConvert.DeserializeObject<List<Post>>(resp);
            return posts;
        }

        Debug.Log("when trying to get post info fot chat:" + response.StatusCode);
        return null;
    }
    async Task<List<User>> GetUsersAsync(int[] id)
    {
        Debug.Log("getting active chats users info");
        var url = "api/get_user_info";
        string id_list = "["+string.Join(",", id) +"]";
        Dictionary<string, string> POSTData = new Dictionary<string, string>();
        POSTData.Add("pk", id_list);
        var data = new FormUrlEncodedContent(POSTData);
        var response = await client.PostAsync(url, data);
        if(response.IsSuccessStatusCode){
            var resp = await response.Content.ReadAsStringAsync();
            List<User> users = JsonConvert.DeserializeObject<List<User>>(resp);
            return users;
        }
        Debug.Log("when trying to get user info fot chat:" + response.StatusCode);
        return null;
    }
    public void CloseChatsMenuScreen(){
        ChatsMenuCanvas.SetActive(false);
    }
    
     List<int[]> GetActiveChats(){
        Debug.Log("getting active chats");
        List<int[]> ActiveChats = new List<int[]>();
        //setup the connection to the database
        string conn = "URI=file:" + Application.dataPath + "/app_db.db"; //Path to database.
        Debug.Log(conn);
        IDbConnection dbconn;
        dbconn = (IDbConnection) new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database.

        IDbCommand dbcmd = dbconn.CreateCommand();
        string sqlQuery = "SELECT user_id, post_id, chat_id " + "FROM Chats";
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();

        while (reader.Read())
        {
            int user_id = reader.GetInt32(0);
            int post_id = reader.GetInt32(1);
            int chat_id = reader.GetInt32(2);
            
            int[] chat = {chat_id,user_id,post_id};
            ActiveChats.Add(chat);
        }
        reader.Close();
        reader = null;
        dbcmd.Dispose();
        dbcmd = null;
        dbconn.Close();
        dbconn=null;
        return ActiveChats;
    }
}
