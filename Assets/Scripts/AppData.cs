using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Http;
using System.Net.Http.Headers;

public static class AppData
{
    public static AuthToken token;
    public static User user;
    public static Dictionary<string, string> areaNamesDict;
    public static readonly string APIaddress = "http://10.12.1.11/";
    /*
     * workurl = "http://10.12.1.11/";
     * homeurl = "http://192.168.1.119/";
     */

     public static HttpClient client = new HttpClient();

     public static void Setup(){
        client.BaseAddress = new System.Uri(APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token.get());
     }

}

public class AuthToken
{
    public string auth_token;
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
    public int user { get; set; }
    
}