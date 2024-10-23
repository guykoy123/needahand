using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.WebSockets;
using Newtonsoft.Json;
using System.Threading;
using System.Resources;
using System.Threading.Tasks;
using System.Text;
using System.Linq.Expressions;
// websocket controller
public class WSController : MonoBehaviour
{
    public bool keep_open=true;
    bool connected_flag=false; 

    Uri uri = new Uri(AppData.WSaddress);
    ClientWebSocket ws = new ClientWebSocket();
    Task<bool> connect_task;
    Task<Dictionary<string,string>> receive_task;
    public ChatsMenuController chatMenu;
    // Start is called before the first frame update
    void Start()
    {
        // Debug.Log("WSController");
    }

    // Update is called once per frame
    void Update()
    {
        if (AppData.token!=null && connect_task==null){ // runs once to setup connection

            Debug.Log("trying ws connection");
            connect_task = Connect();
        }
        if (connect_task.IsCompleted && !connected_flag){
            if (connect_task.Result){
                connected_flag=true;
                receive_task = receive(); //start the receive task
            }
        }
        if (connected_flag){ //after setup is complete
            if (receive_task.IsCompleted){ //if receive data handle it
                // try{
                Dictionary<string,string> data = receive_task.Result;
                receive_task=null;
                receive_task=receive(); //restart listening

                if (data["type"]=="message"){
                    // new Task(() => { chatMenu.incoming_message_handler(data);}).Start();
                    incoming_message_handler(data);
                }
                // }
                // catch (Exception e){
                //     Debug.Log("receive: no type key in data dict");
                //     Debug.Log(e);
                // }
               

                // Debug.Log("received: " + receive_task.Result.ToString());
                
            }
        }
    }
    public void incoming_message_handler(Dictionary<string,string> message_data){
    /*
    handles incoming messages 
    if a chat for that post already exists:
        try to get the chat controller and set a new message
            if can't get chatController just push message into queue to be sent to the controller once it's open 
    if no chat exist in the db:
        create a new chat in db and push message into the queue
    */
        if (message_data["type"]=="message"){
            if(DatabaseController.DoesChatExist(Int32.Parse(message_data["post"]))){
                Chat chat = chatMenu.GetChatByPost(Int32.Parse(message_data["post"]));
                if (chat!=null){ //chat exists and is loaded in ChatsMenuController
                    ChatController controller = chatMenu.GetChatController(chat);
                    DateTime time_sent = DateTime.ParseExact(message_data["time_sent"], "yyyy-MM-dd HH:mm",System.Globalization.CultureInfo.InvariantCulture);
                    controller.ReceiveMessage(message_data["contents"],time_sent,Int32.Parse(message_data["packet_id"]));
                }
                else{ //chat exists but isn't loaded in ChatsMenuController
                    // this.OpenChatsMenuScreen(true);
                    chatMenu.PushMessage(message_data);
                    Debug.Log("received message for a chat in DB but ChatMenuController doesn't have it");
                }
            }
            else{ //chat doesn't exist 
                chatMenu.StartNewChat(Int32.Parse(message_data["post"]),Int32.Parse(message_data["from"]),false);
                chatMenu.PushMessage(message_data);
                
            }
            //send acknowledge of message
            Dictionary<string,string> ack_data= new Dictionary<string, string>
            {
                { "type", "ack" },
                { "user", message_data["from"] },
                { "packet_id", message_data["packet_id"] }
            };
            send(ack_data);
        }

        else if (message_data["type"]=="ack"){
            chatMenu.ackMessage(message_data,false);
        }
        else if(message_data["type"]=="read_ack"){
            chatMenu.ackMessage(message_data,true);
        }
        
    }
    async Task<bool> Connect(){
        /*
        sets up the websocket with the users token in the header
        then connects to server
        TODO: handle event of failed connection
        */
        ws.Options.SetRequestHeader("token",AppData.token.get());
        // ws.Options.Cookies = new CookieContainer();
        // ws.Options.Cookies.Add(uri,new Cookie("token",AppData.token.get()));
        ws.Options.KeepAliveInterval = TimeSpan.Zero; // keep ws open forever
        try{
            await ws.ConnectAsync(uri,CancellationToken.None);
            Debug.Log(ws.State);
            Debug.Log("connected to ws: " + ws.ToString());
            return true;
        }
        catch (Exception e){
            Debug.Log(e.ToString());
            return false;
        }
        
    }

    async Task<Dictionary<string,string>> receive(){
        /*
        receive data from server
        deserialize json to dict and return it
        */
        var receiveBuffer = new byte[1024];
        var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
        string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
        Debug.Log("received: " + receivedMessage);
        Dictionary<string,string> message_data=JsonConvert.DeserializeObject<Dictionary<string, string>>(receivedMessage);
        return message_data;
    }

    async Task send(Dictionary<string,string> message_data){
        string message_json = JsonConvert.SerializeObject(message_data);
        var buffer = Encoding.UTF8.GetBytes(message_json);

        await ws.SendAsync(new ArraySegment<byte>(buffer),WebSocketMessageType.Text,true,CancellationToken.None);
    }

    public async Task read_ackMessage(Message msg){
        //send acknowledge of message
        Dictionary<string,string> ack_data= new Dictionary<string, string>
        {
            { "type", "read_ack" },
            { "user", msg.GetAuthor().pk.ToString() },
            { "packet_id", msg.GetSN().ToString() }
        };
        send(ack_data);
    }

    public async Task sendMessage(Message msg,int target_user_id,int post_id){
        Dictionary<string,string> data= new Dictionary<string, string>
        {
            { "type", "message" },
            { "user", target_user_id.ToString() },
            { "contents", msg.GetText() },
            { "post", post_id.ToString() },
            { "time_sent", msg.GetTimeSent().ToString("yyyy-MM-dd HH:mm") },
            { "packet_id", msg.GetSN().ToString() }
        };
        send(data);


    }
}
