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
// websocket controller
public class WSController : MonoBehaviour
{
    public bool keep_open=true;
    bool connected_flag=false; 

    Uri uri = new Uri(AppData.WSaddress);
    ClientWebSocket ws = new ClientWebSocket();
    Task<bool> connect_task;
    Task<Dictionary<string,string>> receive_task;

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
                receive_task = receive(); //start the receive task
            }
        }
        if (connected_flag){ //after setup is complete
            if (receive_task.IsCompleted){ //if receive data handle it
                
                // Debug.Log("received: " + receive_task.Result.ToString());
                receive_task=receive(); //restart listening
            }
        }
    }

    async Task<bool> Connect(){
        /*
        sets up the websocket with the users token in the header
        then connects to server
        TODO: check if can remove cookie
        TODO: handle event of failed connection
        */
        ws.Options.SetRequestHeader("token",AppData.token.get());
        ws.Options.Cookies = new CookieContainer();
        ws.Options.Cookies.Add(uri,new Cookie("token",AppData.token.get()));
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

    
}
