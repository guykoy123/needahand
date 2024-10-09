using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;

public class PostController : MonoBehaviour
{
    Post post;
    bool displayed = false;

    TMP_Text Title;
    TMP_Text Author;
    TMP_Text Time;
    TMP_Text Content;
    TMP_Text Area;
    GameObject GiveImage;
    GameObject RequestImage;

    PostScreenController postScreen;

    // Start is called before the first frame update
    void Start()
    {
        Title = transform.GetChild(0).GetComponent<TMP_Text>();
        Author = transform.GetChild(1).GetComponent<TMP_Text>();
        Time = transform.GetChild(3).GetComponent<TMP_Text>();
        Content = transform.GetChild(2).GetComponent<TMP_Text>();
        Area = transform.GetChild(4).GetComponent<TMP_Text>();

        GiveImage = transform.GetChild(6).gameObject;
        RequestImage = transform.GetChild(5).gameObject;

        postScreen = Resources.FindObjectsOfTypeAll<PostScreenController>()[0];
    }

    // Update is called once per frame
    void Update()
    {
        if (!displayed && post != null)
        {
            DisplayPostData();
            displayed = true;

        }
    }

    void DisplayPostData()
    {
        if (post.post_type == "rq")
        {
            GiveImage.SetActive(false);
            RequestImage.SetActive(true);
        }
        else
        {
            GiveImage.SetActive(true);
            RequestImage.SetActive(false);
        }

        Title.text = post.title;
        Author.text = "User: " + post.username;
        Time.text = post.date_posted.ToString("dd.MM.yy");
        Content.text = post.content;
        Area.text = "אזור: " + AppData.areaNamesDict[post.area];

        //for debugging displays the post and user id
        if (AppData.DebugFlag) {
            Title.text  += "; "+ post.pk.ToString();
            Author.text += "; " + post.author.ToString() ;
        }
    }
    public void SetPost(Post p)
    {
        post = p;
    }

    public bool isDisplayed()
    {
        return displayed;
    }

    public void OpenPost()
    {
        postScreen.gameObject.SetActive(true);
        postScreen.SetPost(post);
    }
}
