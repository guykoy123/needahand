using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.Globalization;
using System;

public static class DatabaseController
{
    static IDbConnection dbconn;
    // Start is called before the first frame update
    public static void Start()
    {
        //setup the connection to the database
        string conn = "URI=file:" + Application.dataPath + "/app_db.db"; //Path to database.
        Debug.Log(conn);
        dbconn = (IDbConnection) new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database.
    }

    public static Message SaveNewMessage(string messageText,Chat chat,User author){
        Debug.Log("saving new message");
        //query db for all chats
        IDbCommand dbcmd = dbconn.CreateCommand();
        //insert new chat message into db
        string sqlQuery = "INSERT INTO Messages (chat_id,message_text,author_id,time_sent) ";
        sqlQuery += String.Format("VALUES ({0},'{1}',{2},'{3}')",chat.get_id(),messageText,author.pk,DateTime.Now);
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //get generated id of the message
        sqlQuery = "SELECT seq FROM sqlite_sequence WHERE name='Messages'";
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();
        //create message object
        Message message = new Message(int.Parse(reader.GetValue(0).ToString()),author,messageText,DateTime.Now,false,chat);
        Debug.Log(message.GetText());
        //cleanup
        reader.Close();
        dbcmd.Cancel();

        return message;
    }
    public static List<int[]> GetActiveChats(){
        Debug.Log("getting active chats");
        List<int[]> ActiveChats = new List<int[]>();

        //query db for all chats
        IDbCommand dbcmd = dbconn.CreateCommand();
        string sqlQuery = "SELECT user_id, post_id, chat_id " + "FROM Chats";
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();

        //save data to ActiveChats for further user
        while (reader.Read())
        {
            int user_id = reader.GetInt32(0);
            int post_id = reader.GetInt32(1);
            int chat_id = reader.GetInt32(2);
            
            int[] chat = {chat_id,user_id,post_id};
            ActiveChats.Add(chat);
        }
        //close connection
        reader.Close();
        reader = null;
        dbcmd.Dispose();
        dbcmd = null;
        return ActiveChats;
    }

    public static void CreateNewChat(int post_id,int user_id){
        //create a new chat row

        //insert new chat message into db
        IDbCommand dbcmd = dbconn.CreateCommand();
        string sqlQuery = "INSERT INTO Chats (user_id,post_id) ";
        sqlQuery += String.Format("VALUES ({0},'{1}')",user_id,post_id);
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //cleanup
        dbcmd.Cancel();
    }
}
