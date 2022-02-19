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
    public RawImage ProfileImage;
    public TMP_Text username;
    public TMP_Text email;
    public TMP_Text score;
    public RawImage new_pic;
    public TMP_Text file_name;
    HttpClient client = new HttpClient();
    Task<byte[]> image_task;
    Task<bool> upload_task;

    public ConfirmationController confirmation;
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

    public void DisplayNewPic(string path){
        Texture2D tex = new Texture2D(2, 2);
        ImageConversion.LoadImage(tex,File.ReadAllBytes(path));
        new_pic.texture = tex;
        string[] path_parts = path.Split('/');
		file_name.text=path_parts[path_parts.Length-1];
    }
    public void UploadProfilePicture(){
        string message = "האם את/ה בטוח/ה שאת/ה רוצה להעלות את תמונת הפרופיל הזו?";
        confirmation.DisplayMessage(message, ConfirmUpload);

    }

    public void ConfirmUpload(bool answer){
        if(answer){
            upload_task = upload_profile_picture();
        }
    }

    async Task<bool> upload_profile_picture(){
        FilePicker fPicker = gameObject.GetComponent<FilePicker>();
        string[] path = fPicker.get_file_path().Split('.');
        byte[] pic_bytes = File.ReadAllBytes(fPicker.get_file_path());  
        string base64String = Convert.ToBase64String(pic_bytes);
        var url = "api/upload_profile_pic";
        var values = new Dictionary<string, string>
        {
            { "file_type", path[path.Length-1] },
            { "encoded_data", base64String }
        };

        var content = new FormUrlEncodedContent(values);   
        var response = await client.PostAsync(url, content);
        if(response.IsSuccessStatusCode){
            return true;
        }

        return false;
    }

    // Update is called once per frame
    void Update()
    {
        if(image_task != null){
            if (image_task.IsCompleted)
            {
                Texture2D tex = new Texture2D(2, 2);
                ImageConversion.LoadImage(tex,image_task.Result);
                ProfileImage.texture = tex;
                image_task = null;
            }
        }
        if(upload_task != null && upload_task.IsCompleted){
            if(upload_task.Result){
                image_task = get_profile_pic();
            }
            upload_task = null;
        }
    }
}
