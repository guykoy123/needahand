using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TMPro;
using Newtonsoft.Json;
public class AboutController : MonoBehaviour
{
    Task<bool> about_task;
    Dictionary<string,string> aboutData;
    public TMP_Text aboutText;
    public TMP_Text creditsText;

    // Start is called before the first frame update
    void Start()
    {
        about_task =GetAbout();
    }

    // Update is called once per frame
    void Update()
    {
        if (about_task != null && about_task.IsCompleted){
            if(about_task.Result){
                aboutText.text = aboutData["about"];
                creditsText.text = aboutData["credits"];
                about_task = null;
            }
            else{
                Debug.LogError("couldn't load about page");
                about_task=null;
            }
        }
    }

    async Task<bool> GetAbout(){
        var url = "api/about";
        var response =await AppData.client.GetAsync(url);
        if(response.IsSuccessStatusCode){
            var data = await response.Content.ReadAsStringAsync();
            aboutData = JsonConvert.DeserializeObject<Dictionary<string,string>>(data);
            return true;
        }
        else{
            return false;
        }
       
    }
}
