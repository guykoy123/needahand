using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using System;

public class ProfileController : MonoBehaviour
{
    public Image ProfileImage;
    public TMP_Text username;
    public TMP_Text email;
    public TMP_Text score;
    public UnityEngine.UI.Image new_pic;

    HttpClient client = new HttpClient();
    Task<byte[]> image_task;
    // Start is called before the first frame update
    void Start()
    {
        username.text = AppData.user.username;
        email.text = AppData.user.email;
        Debug.Log(AppData.user.image);
        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());
        image_task = get_profile_pic();
    }

    async Task<byte[]> get_profile_pic()
    {
        var url = "api/get_my_pic";
        var response = await client.GetAsync(url);
        string base64image = response.Content.ReadAsStringAsync().Result;
        byte[] imageBytes = Convert.FromBase64String(base64image);
        return imageBytes;
    }

    // Update is called once per frame
    void Update()
    {
        if(image_task != null){
            if (image_task.IsCompleted)
            {
                string path = Application.dataPath;
                Debug.Log("saved to " + path);
                //System.IO.Directory.CreateDirectory(path+"/Resources");
                File.WriteAllBytes(path + "/Resources/profile_pic.png",image_task.Result);
                Sprite pic = Resources.Load<Sprite>("profile_pic");
                ProfileImage.sprite = pic;
                image_task = null;
                Debug.Log(pic);
            }
        }

        

    }

    /*public Image byteArrayToImage(byte[] byteArrayIn)
    {
        MemoryStream ms = new MemoryStream(byteArrayIn);
        Image returnImage = Image.FromStream(ms);
        return returnImage;
    }*/
}
