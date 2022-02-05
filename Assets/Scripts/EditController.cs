using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Linq;

public class EditController : MonoBehaviour
{
    public GameObject HomePanel;
    public PostScreenController PostScreen;
    public ConfirmationController confirmation;

    public TMP_InputField titleText;
    public TMP_InputField contentText;
    public TMP_Dropdown dropdown;
    public TMP_Text errorText;

    public Image gvImage;
    public Image rqImage;

    string pType;
    string area_code;

    HttpClient client = new HttpClient();
    Task<bool> publish_task;

    Post post;

    // Start is called before the first frame update
    void Start()
    {

        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());
    }

    public void SetPost(Post p)
    {
        post = p;
        titleText.text = post.title;
        contentText.text = post.content;
        if(post.post_type == "gv")
        {
            SetGV();
        }
        else
        {
            SetRQ();
        }
        List<string> areas = AppData.areaNamesDict.Values.ToList();
        area_code = post.area;
        dropdown.value = int.Parse(post.area);
    }
    // Update is called once per frame
    void Update()
    {
        if (publish_task != null)
        {
            if (publish_task.IsCompleted)
            {
                if (publish_task.Result)
                {
                    HomePanel.SetActive(true);
                    HomePanel.GetComponent<HomeController>().ReloadPosts();
                    gameObject.SetActive(false);
                    PostScreen.SetPost(post);
                    PostScreen.DisplayPost();
                    publish_task = null;
                }
                else
                {
                    Debug.Log("shit");
                }
            }
        }

    }

    private void OnEnable()
    {
        List<string> areas = AppData.areaNamesDict.Values.ToList();
        dropdown.ClearOptions();
        dropdown.AddOptions(areas);
    }

    public void SelectedArea()
    {
        area_code = dropdown.value.ToString();
    }
    public void SetGV()
    {
        pType = "gv";
        gvImage.color = new Color(0, 215, 255);
        rqImage.color = new Color(176, 176, 176, 255);
    }
    public void SetRQ()
    {
        pType = "rq";
        rqImage.color = new Color(0, 215, 255);
        gvImage.color = new Color(176, 176, 176, 255);
    }
    public void PublishPost()
    {
        string errorMsg = "";
        bool valid = true;
        if (area_code == null)
        {
            valid = false;
            Debug.LogError("area not chosen");
            errorMsg += "area not chosen\n";
        }
        if (pType == null)
        {
            valid = false;
            Debug.LogError("post type not chosen");
            errorMsg += "post type not chosen\n";
        }
        if (titleText.text == "" || contentText.text == "")
        {
            valid = false;
            Debug.LogError("post must have a title and content");
            errorMsg += "post must have a title and content\n";
        }
        if (valid)
        {
            confirmation.DisplayMessage("האם אתה בתוח שאתה רוצה לשמור את השינויים?", confirmEdit);
        }
        else
        {
            errorText.text = errorMsg;
        }

    }
    public void confirmEdit(bool answer)
    {
        if (answer)
        {
            publish_task = publish(titleText.text, contentText.text, pType, area_code);
        }
    }

    async Task<bool> publish(string title, string content, string pType, string area_code)
    {
        var url = "api/edit_post";
        Dictionary<string, string> postData = new Dictionary<string, string>();
        postData.Add("post_type", pType);
        postData.Add("area", area_code);
        postData.Add("title", title);
        postData.Add("content", content);
        postData.Add("pk", post.pk);
        post.post_type = pType;
        post.area = area_code;
        post.area_name = AppData.areaNamesDict[area_code];
        post.title = title;
        post.content = content;
        var data = new FormUrlEncodedContent(postData);
        var response = await client.PostAsync(url, data);
        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        Debug.Log(response.StatusCode);
        return false;
    }
}

