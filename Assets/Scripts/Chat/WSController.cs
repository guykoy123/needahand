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
using UnityEngine.Rendering;
// websocket controller
public class WSController:MonoBehaviour
{
    public bool keep_open=true;
    bool connected_flag=false; 

    Uri uri = new Uri(AppData.WSaddress);
    ClientWebSocket ws = new ClientWebSocket();
    Task<bool> connect_task;
    //had problem when receiving multiple messages rapidly so converted from task to queue of task
    //each time checks which ones have completed, handles the messages and removes the task
    //if not yet completed keeps it
    //Queue<Task<Dictionary<string,string>>> receive_tasks=new Queue<Task<Dictionary<string, string>>>();
    Task<Dictionary<string,string>> receive_tasks;
    public ChatsMenuController chatMenu;
    Queue<Dictionary<string,string>> send_data_queue=new Queue<Dictionary<string, string>>();
    Task send_task;
    // Start is called before the first frame update
    void Start()
    {
        // Debug.Log("WSController");
    }

    // Update is called once per frame
    async void Update()
    {
        if (AppData.token!=null && connect_task==null){ // runs once to setup connection

            Debug.Log("trying ws connection");
            connect_task = Connect();
        }
        if (connect_task.IsCompleted && !connected_flag){
            if (connect_task.Result){
                connected_flag=true;
                receive_tasks= receive(); //start the receive task
            }
        }
        if (connected_flag){ //after setup is complete
            // if (receive_tasks.IsCanceled || receive_tasks.IsFaulted){
            //     Debug.Log("receive task faulted/canceled");
            //     receive_tasks=null;
            //     receive_tasks=receive();
            // }
            if(receive_tasks.IsCompleted){
                Dictionary<string,string> data = receive_tasks.Result;
                if (data!=null){
                    incoming_message_handler(data);
                }
                
                receive_tasks=null;
                receive_tasks=receive();
            }



            //if websocket connection gets dropped, retry connecting
            if (ws.State!= WebSocketState.Open && ws.State!=WebSocketState.Connecting){
                Debug.Log("websocket connection dropped, reopening");
                if (ws.State==WebSocketState.Aborted){
                    ws.Dispose();
                }
                else{
                    ws.CloseOutputAsync(WebSocketCloseStatus.Empty,"",CancellationToken.None);
                    ws.CloseAsync(WebSocketCloseStatus.ProtocolError,"",CancellationToken.None);
                }
                

                ws = new ClientWebSocket();
                connect_task=null;
                connect_task = Connect();
                connected_flag=false;

            }

            //while connected send all awaiting messages (because can only use one instance of ws.SendAsync at once)
            while(send_data_queue.Count>0 && ws.State==WebSocketState.Open){
                if(send_task== null || send_task.IsCompleted){
                    Dictionary<string,string> message_data=send_data_queue.Dequeue();
                    string message_json = JsonConvert.SerializeObject(message_data);
                    var buffer = Encoding.UTF8.GetBytes(message_json);
                    try{
                        send_task=ws.SendAsync(new ArraySegment<byte>(buffer),WebSocketMessageType.Text,true,CancellationToken.None);;
                    }
                    catch(Exception e){
                        Debug.Log("couldn't send message, " + e.ToString());
                        send_data_queue.Enqueue(message_data);
                    }
                }
                else{
                    break;
                }
                
                
            }
        }

    }
    // private async Task sendTask(byte[] byte_data){
    //     await 
    // }
    public async void incoming_message_handler(Dictionary<string,string> message_data){
    /*
    handles incoming messages 
    if a chat for that post already exists:
        try to get the chat controller and set a new message
            if can't get chatController just push message into queue to be sent to the controller once it's open 
    if no chat exist in the db:
        create a new chat in db and push message into the queue
    */
        try{
            if(message_data.TryGetValue("type",out string type)){
                if (type=="message"){
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

                else if (type=="ack"){
                    chatMenu.ackMessage(message_data,false);
                }
                else if(type=="read_ack"){
                    chatMenu.ackMessage(message_data,true);
                }
            }
            else{
                string exception_str="";
                foreach (KeyValuePair<string, string> kvp in message_data)
                {
                    exception_str += string.Format("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                }
                Debug.Log("incoming message does not contain key: type. \r\n" + exception_str);
            }
        }
        catch (Exception e) { 
            
            Debug.Log("exception when checking message type:\r\n"+e.ToString());
        }      
    }
    async Task<bool> Connect(){
        /*
        sets up the websocket with the users token in the header
        then connects to server
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
        try{
            if(ws.State==WebSocketState.Open){
                
                var receiveBuffer = new byte[1024];
                var receiveResult = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                string receivedMessage = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                Debug.Log("received: " + receivedMessage);
                Dictionary<string,string> message_data=JsonConvert.DeserializeObject<Dictionary<string, string>>(receivedMessage);
                return message_data;
            }
            else{
                Debug.Log("Can't receive data, WebSocket state: " + ws.State.ToString());
                
                return null;
            }

        }
        catch(Exception e){
            Debug.Log(e);
            Debug.Log("WebSocket state: " + ws.State.ToString());
            
            return null;
        }

        
    }

    void send(Dictionary<string,string> message_data){
        send_data_queue.Enqueue(message_data);
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
