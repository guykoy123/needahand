using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using System.IO;

public class LoginManager : MonoBehaviour
{
    public GameObject LoginPanel;
    public GameObject RegisterPanel;

    public TMP_InputField login_username;
    public TMP_InputField login_password;
    public Toggle remember_toggle;

    public TMP_InputField register_username;
    public TMP_InputField register_email;
    public TMP_InputField register_pass1;
    public TMP_InputField register_pass2;

    public GameObject LoginError;
    public GameObject usernameError;
    public GameObject emailError;
    public GameObject password1Error;
    public GameObject password2Error;

    HttpClient client = new HttpClient();
    Task<AuthToken> login_task;
    Task<bool> register_task;
    bool started_login_task = false;
    bool started_register_task = false;
    RegistrationError regErr;

    // Start is called before the first frame update
    void Start()
    {
        //TODO: add token check
        string path = Application.persistentDataPath;
        if (File.Exists(path + "/token.auth"))
        {
            AppData.token = new AuthToken(File.ReadAllText(Application.persistentDataPath + "/token.auth"));
            SceneManager.LoadScene("MainScene");
        }
        
        LoginPanel.SetActive(true);
        RegisterPanel.SetActive(false);
        LoginError.SetActive(false);

        usernameError.SetActive(false);
        emailError.SetActive(false);
        password1Error.SetActive(false);
        password2Error.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(started_login_task){
            if(login_task.IsCompleted){
                if(login_task.Result ==null){
                    LoginError.SetActive(true);
                    started_login_task = false;
                }
                else{
                    Debug.Log("logged in");
                    AuthToken t = login_task.Result;
                    if (remember_toggle.isOn)
                    {
                        string path = Application.persistentDataPath;
                        File.WriteAllText(path + "/token.auth", t.get());
                    }
                    AppData.token = t;
                    SceneManager.LoadScene("MainScene");
                    started_login_task=false;
                }
            }
        }
        else if (started_register_task)
        {
            if (register_task.IsCompleted)
            {
                if (register_task.Result)
                {
                    LoginPanel.SetActive(true);
                    RegisterPanel.SetActive(false);
                    LoginError.SetActive(true);
                    LoginError.GetComponent<TMP_Text>().text = "registered successfuly";
                    started_register_task = false;
                    started_login_task = false;
                }

                else
                {
                    usernameError.SetActive(false);
                    emailError.SetActive(false);
                    password1Error.SetActive(false);
                    password2Error.SetActive(false);
                    try
                    {
                        if (regErr.username[0][0] != null)
                        {
                            usernameError.SetActive(true);
                            usernameError.GetComponent<TMP_Text>().text = regErr.username[0][0];
                        }
                    }
                    catch (NullReferenceException e){

                    }
                    try
                    {
                        if (regErr.password1[0][0] != null)
                        {
                            password1Error.SetActive(true);
                            password1Error.GetComponent<TMP_Text>().text = regErr.password1[0][0];
                        }
                    }

                    catch (NullReferenceException e)
                    {

                    }
                    try
                    {
                        if (regErr.password2[0][0] != null)
                        {
                            password2Error.SetActive(true);
                            password2Error.GetComponent<TMP_Text>().text = regErr.password2[0][0];
                        }
                    }

                    catch (NullReferenceException e)
                    {

                    }
                    try
                    {
                        if (regErr.email[0][0] != null)
                        {
                            emailError.SetActive(true);
                            emailError.GetComponent<TMP_Text>().text = regErr.email[0][0];
                        }
                    }

                    catch (NullReferenceException e)
                    {

                    }
                    started_register_task = false;
                }
            }


        }
    }




    async Task<AuthToken> post_login(string username, string password)
    {
        var values = new Dictionary<string, string>
        {
            { "username", username },
            { "password", password }
        };

        var content = new FormUrlEncodedContent(values);   
        var response = await client.PostAsync(AppData.APIaddress+"api/auth/token/login/", content);
        if (response.IsSuccessStatusCode == true)
        {
            string res = await response.Content.ReadAsStringAsync();
            AuthToken token = JsonConvert.DeserializeObject<AuthToken>(res);
            return token;
        }
        else
        {
            Debug.Log(response.StatusCode);
            return null;
        }
    }

    public void Login()
    {
        started_login_task = true;
        login_task = post_login(login_username.text, login_password.text);
    }

    public void Register()
    {
        started_register_task = true;
        register_task = post_register(register_username.text, register_pass1.text, register_pass2.text, register_email.text);
    }

    async Task<bool> post_register(string username, string password1, string password2, string email)
    {
        var values = new Dictionary<string, string>
        {
            { "username", username },
            { "email", email },
            { "password1", password1 },
            { "password2", password2 }
        };

        var content = new FormUrlEncodedContent(values);

        var response = await client.PostAsync(AppData.APIaddress + "api/register", content);
        if (response.IsSuccessStatusCode == true)
        {
            Debug.Log("created user " + response.Content);
            return true;
        }
        else
        {
            Debug.Log("could not create user");
            string res = await response.Content.ReadAsStringAsync();
            regErr = JsonConvert.DeserializeObject<RegistrationError>(res);

            return false;
        }
    }


}

public class RegistrationError
{
    public List<List<string>> username { get; set; }
    public List<List<string>> email { get; set; }
    public List<List<string>> password1 { get; set; }
    public List<List<string>> password2 { get; set; }

    public override string ToString()
    {
        string message = "";
        if (this.username != null)
        {
            message += this.username[0][0];
        }
        if (this.password1 != null)
        {
            message += this.password1[0][0];
        }
        if (this.password2 != null)
        {
            message += this.password2[0][0];
        }
        if (this.email != null)
        {
            message += this.email[0][0];
        }
        return message;
    }
}
