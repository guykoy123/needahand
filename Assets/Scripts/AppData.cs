using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

public static class AppData
{
    public static bool DebugFlag=true;
    public static AuthToken token;
    public static User user;
    public static Dictionary<string, string> areaNamesDict;
    
    //url of the server, currently set to the development server
    public static readonly string APIaddress = "http://127.0.0.1:8000/";
    public static readonly string WSaddress = "ws://127.0.0.1:8000/";

     public static HttpClient client = new HttpClient();

     public static void Setup(){
        client.BaseAddress = new System.Uri(APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token.get());
     }

    
}

public class AuthToken
{
    protected string auth_token;
    public AuthToken(string token)
    {
        this.auth_token = token;
    }

    public string get()
    {
        return this.auth_token;
    }
}

public class Post
{
    public string title { get; set; }
    public string content { get; set; }
    public int author { get; set; }
    public string post_type { get; set; }
    public DateTime date_posted { get; set; }
    public string area { get; set; }
    public string pk { get; set; }
    public string username { get; set; }
    public string area_name { get; set; }
}

public class User
{
    public string username { get; set; }
    public string email { get; set; }
    public string image { get; set; }
    public int pk { get; set; }
    
}

public class Chat
{
    int chat_id;
    Post post;
    User user;
    List<Message> Messages;
    public Chat(int id,Post post,User user){
        //constructor for loading from db
        this.chat_id=id;
        this.post=post;
        this.user=user;
    }

    public int get_id(){
        return this.chat_id;
    }

    public string GetUsername(){
        return this.user.username;
    }
    public string GetPostTitle(){
        return this.post.title;
    }
    public bool gotUnreadMessages(){
        if (this.Messages == null){
            return false;
        }
        for(int i=0; i<this.Messages.Count; i++){
            if(this.Messages[i].GetAuthor()!=AppData.user){
                if(!this.Messages[i].GetSeen()){
                    return true;
                }
            }
        }
        return false;
    }
}

public class Message
{
    int id;
    User author;
    string text;
    DateTime time_sent;
    bool seen=false;
    Chat chat;
    public Message(int id,User author,string text,DateTime time, bool seen, Chat chat)
    {
        //constructor for loading a message from db
        this.id =id;
        this.author=author;
        this.text=text;
        this.time_sent=time;
        this.seen=seen;
        this.chat=chat;
    }

    public string GetText(){
        return this.text;
    }
    public DateTime GetTimeSent(){
        return this.time_sent;
    }
    public bool GetSeen(){
        return this.seen;
    }
    public void UpdateSeen(){
        this.seen=true;
    }
    public string GetUsername(){
        return author.username;
    }
    public User GetAuthor(){
        return author;
    }

}