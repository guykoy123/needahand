using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine.UI;
using System;
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
    bool my_post = false;

    HttpClient client = new HttpClient();
    Task<bool> deleteTask;

    public HomeController homeScreen;
    public ConfirmationController confirmation;
    public EditController editScreen;

    public ChatsMenuController ChatMenu;

    // Start is called before the first frame update
    void Start()
    {
        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());
        ChatMenu = GameObject.FindObjectOfType<ChatsMenuController>();
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
        my_post = (post.username == AppData.user.username);
    }
    public void DisplayPost()
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
        if (my_post)
        {
            MyButtons.SetActive(true);
            OthersButtons.SetActive(false);
        }
        else
        {
            MyButtons.SetActive(false);
            OthersButtons.SetActive(true);
            Button ContactButton = OthersButtons.GetComponentInChildren<Button>();
            ContactButton.onClick.AddListener(delegate { ChatMenu.StartNewChat(Int32.Parse(this.post.pk),this.post.author,true); });
        }
    }

    public void EditPost()
    {
        editScreen.gameObject.SetActive(true);
        editScreen.SetPost(post);
    }
    public void DeletePost()
    {
        if (post != null)
        {
            string message = "האם אתה בטוח שאתה רוצה למחוק את הפוסט?";
            confirmation.DisplayMessage(message, ConfirmDelete);

        }
        
    }
    public void ConfirmDelete(bool answer)
    {
        if (answer)
        {
            deleteTask = delete(post.pk);
            homeScreen.ReloadPosts();
            gameObject.SetActive(false);
        }
    }
    async Task<bool> delete(string id)
    {
        string url = "api/delete_post";
        var values = new Dictionary<string, string> { { "post_id", id } };
        var content = new FormUrlEncodedContent(values);
        var response = await client.PostAsync(url, content);
        if (response.IsSuccessStatusCode == true)
        {
            Debug.Log("deleted post");
            return true;
        }
        Debug.Log("failed to delet post " + response.StatusCode);
        return false;
        
    }
}
