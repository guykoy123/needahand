using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.Linq;
public class PublishController : MonoBehaviour
{
    public GameObject HomePanel;
    public GameObject publishPanelButton;

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

    // Start is called before the first frame update
    void Start()
    {

        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());
    }

    // Update is called once per frame
    void Update()
    {
        if(publish_task != null)
        {
            if (publish_task.IsCompleted)
            {
                if (publish_task.Result)
                {
                    HomePanel.SetActive(true);
                    HomePanel.GetComponent<HomeController>().ReloadPosts();
                    gameObject.SetActive(false);
                    publishPanelButton.SetActive(true);
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
        rqImage.color = new Color(176, 176, 176,255);
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
        if(titleText.text == "" || contentText.text == "")
        {
            valid = false;
            Debug.LogError("post must have a title and content");
            errorMsg += "post must have a title and content\n";
        }
        if (valid)
        {
            publish_task = publish(titleText.text, contentText.text, pType, area_code);
        }
        else
        {
            errorText.text = errorMsg;
        }
       
    }

    async Task<bool> publish(string title, string content, string pType, string area_code)
    {
        var url = "api/publish_post";
        Dictionary<string, string> postData = new Dictionary<string, string>
        {
            { "post_type", pType },
            { "area", area_code },
            { "title", title },
            { "content", content }
        };
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
