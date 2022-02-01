using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public TMP_Text usernameText;

    // Start is called before the first frame update
    void Start()
    {
        usernameText.text = AppData.username;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void logout()
    {
        File.Delete(Application.persistentDataPath + "/token.auth");
        SceneManager.LoadScene("LoginScene");
    }
}
