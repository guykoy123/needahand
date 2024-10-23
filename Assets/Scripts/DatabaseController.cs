using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data;
using Mono.Data.Sqlite;
using System.Globalization;
using System;
using UnityEngine.XR.WSA;

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

    public static Message SaveNewMessage(string messageText,Chat chat,User author,DateTime time_sent,int sn,bool received){
        // Debug.Log("saving new message");

        int receive_flag=0;
        if (received){
            receive_flag=1;
        }
        //query db for all chats
        IDbCommand dbcmd = dbconn.CreateCommand();
        //insert new chat message into db
        string sqlQuery = "INSERT INTO Messages (chat_id,message_text,author_id,time_sent,sn,received) ";
        sqlQuery += String.Format("VALUES ({0},'{1}',{2},'{3}',{4},{5})",chat.get_id(),messageText,author.pk,time_sent.ToString("yyyy-MM-dd HH:mm"),sn,receive_flag);
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //get generated id of the message
        sqlQuery = "SELECT seq FROM sqlite_sequence WHERE name='Messages'";
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();
        //create message object
        Message message = new Message(int.Parse(reader.GetValue(0).ToString()),author,messageText,time_sent,false,chat,sn,received);
        // Debug.Log(message.GetText());
        //cleanup
        reader.Close();
        dbcmd.Cancel();

        return message;
    }

    public static List<Message> GetMessagesFromChat(Chat chat,int limit, int offset){ 
        /*
        db query response format:
            chat_id (int), message_id (int), message_text (string), author_id (int), time_sent (string), seen (int)
        */
        //query db for all chats
        IDbCommand dbcmd = dbconn.CreateCommand();
        string sqlQuery = string.Format("SELECT * FROM Messages WHERE chat_id = {0} ORDER BY message_id LIMIT {1} OFFSET {2} ",chat.get_id(),limit,offset);
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();
        List<Message> messages = new List<Message>();
        while (reader.Read())
        {
            User author;
            int author_id=reader.GetInt32(3);
            if (AppData.user.pk==author_id){author = AppData.user;}
            else{author = chat.GetUser();}

            DateTime time_sent = DateTime.ParseExact(reader.GetString(4), "yyyy-MM-dd HH:mm",System.Globalization.CultureInfo.InvariantCulture);
            bool received;
            if (reader.GetInt32(7)==0){
                received = false;
            }
            else{
                received = true;
            }
            Message msg = new Message(reader.GetInt32(1),author,reader.GetString(2),time_sent,Convert.ToBoolean(reader.GetInt32(5)),chat,reader.GetInt32(6),received);
            messages.Add(msg);
        }
        return messages;
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

    public static bool DoesChatExist(int post_id){
        //insert new chat message into db
        IDbCommand dbcmd = dbconn.CreateCommand();
        string sqlQuery = String.Format("SELECT 1 FROM Chats WHERE post_id = {0};",post_id.ToString());
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();
        try{
            if (reader.Read()){
                int data=reader.GetInt32(0);
                if(data==1){
                    //cleanup
                    dbcmd.Cancel();
                    return true;
                }
            }
        }
        catch(Exception ex){
            Debug.Log("couldn't find chat for post: " + post_id.ToString());
            Debug.Log(ex.ToString()); 
        }
        //cleanup
        dbcmd.Cancel();
        return false;
        
    }

    public static int ackMessage(Dictionary<string,int> data,bool read){
        /*
        data:
            {
            "user":user id,
            "packet_id": serial number
            }
        sets the received/read field to true based on flag
        returns the chat id
        */
        IDbCommand dbcmd = dbconn.CreateCommand();
        string sqlQuery = String.Format("SELECT chat_id, message_id FROM Messages WHERE author_id = {0} AND sn={1};",data["user"],data["packet_id"]);
        dbcmd.CommandText = sqlQuery;
        IDataReader reader = dbcmd.ExecuteReader();
        int chat_id=0;
        int message_id=0;
        while(reader.Read()){
            chat_id = reader.GetInt32(0);
            message_id = reader.GetInt32(1);
        }
        dbcmd.Cancel();
        if(message_id==0){
            throw new Exception("When trying to update ack for message couldn't find message "+string.Join(Environment.NewLine, data) );
        }
        dbcmd = dbconn.CreateCommand();
        if(read){
            sqlQuery = String.Format("UPDATE Messages SET seen=1 WHERE message_id={0};",message_id);
        }
        else{
            sqlQuery = String.Format("UPDATE Messages SET received=1 WHERE message_id={0};",message_id);
        }
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();
        //cleanup
        dbcmd.Cancel();
        if (chat_id==0){
            throw new Exception("When trying to update ack for message "+string.Join(Environment.NewLine, data) );
        }
        return chat_id;

    } 
}
