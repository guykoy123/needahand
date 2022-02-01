using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class PostScreenController : MonoBehaviour
{
    Post post;
    public TMP_Text title;
    public TMP_Text content;
    public TMP_Text area;
    public TMP_Text author;
    public TMP_Text date;
    public GameObject gvImg;
    public GameObject rqImg;

    public GameObject MyButtons;
    public GameObject OthersButtons;

    bool displayed_info = false;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!displayed_info && post!=null)
        {
            DisplayPost();
            displayed_info = true;
        }
    }

    public void OnDisable()
    {
        displayed_info = false;
        post = null;
    }

    public void SetPost(Post p)
    {
        post = p;
    }
    void DisplayPost()
    {
        if (post.post_type == "rq")
        {
            gvImg.SetActive(false);
            rqImg.SetActive(true);
        }
        else
        {
            gvImg.SetActive(true);
            rqImg.SetActive(false);
        }

        title.text = post.title;
        author.text = "User: " + post.username;
        date.text = post.date_posted.ToString("dd.MM.yy");
        content.text = post.content;
        area.text = "אזור: " + AppData.areaNamesDict[post.area];
    }
}
