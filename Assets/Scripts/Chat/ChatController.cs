using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ChatController : MonoBehaviour
{
    GameObject notificationDot;
    TMP_Text username;
    TMP_Text post_title;
    Image user_pic;
    Chat chat;
    bool finished_setup=false;
    // Start is called before the first frame update
    void Start()
    {
        this.notificationDot=transform.GetChild(3).gameObject;
        this.username = transform.GetChild(0).GetComponent<TMP_Text>();
        this.post_title= transform.GetChild(2).GetComponent<TMP_Text>();
        this.user_pic=transform.GetChild(1).GetComponent<Image>();
        finished_setup=true;
    }

    // Update is called once per frame
    void Update()
    {
        if(finished_setup){
            this.username.text= this.chat.GetUsername();
            this.post_title.text=this.chat.GetPostTitle();
            finished_setup=false; //so as to not run this again
        }
        if(this.chat.gotUnreadMessages()){
            notificationDot.SetActive(true);
        }
        else{
            notificationDot.SetActive(false);
        }
    }

    public void SetChat(Chat c){
        this.chat=c;
    }
}
