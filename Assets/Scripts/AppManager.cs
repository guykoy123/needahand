using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;

public class AppManager : MonoBehaviour
{
    enum Panels
    {
        Home,
        Give,
        Request
    }

    private Panels currentPanel;
    public GameObject homePanel;
    public GameObject givePanel;
    public GameObject requestPanel;
    public GameObject menuPanel;
    public GameObject publishPanel;
    public GameObject publishPanelButton;

    public GameObject homeSelected;
    public GameObject giveSelected;
    public GameObject requestSelected;

    private Vector3 fp;   //First touch position
    private Vector3 lp;   //Last touch position
    private float dragDistance;  //minimum distance for a swipe to be registered

    HttpClient client = new HttpClient();


    bool got_area_names = false;
    Task<bool> area_names_task;
    Task<bool> user_info_task;

    // Start is called before the first frame update
    void Start()
    {
        AppData.Setup();
        string path = Application.persistentDataPath; //change for production, needs to read token from AppData
        AppData.token = new AuthToken(File.ReadAllText(Application.persistentDataPath + "/token.auth"));

        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());

        homePanel.SetActive(true);
        givePanel.SetActive(false);
        requestPanel.SetActive(false);
        menuPanel.SetActive(false);
        publishPanel.SetActive(false);
        publishPanelButton.SetActive(true);

        homeSelected.SetActive(true);
        giveSelected.SetActive(false);
        requestSelected.SetActive(false);

        currentPanel = Panels.Home;
        dragDistance = Screen.height * 15 / 100; //dragDistance is 15% height of the screen

        area_names_task = get_area_names();
        user_info_task = get_user_info();
    }
    async Task<bool> get_area_names()
    {
        var url = "api/get_areas_dict";
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var resp = await response.Content.ReadAsStringAsync();
            var area_dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp);
            AppData.areaNamesDict = area_dict;
            return true;
        }
        return false;
    }

    async Task<bool> get_user_info()
    {
        var url = "api/get_my_info";
        var response = await client.GetAsync(url);
        if (response.IsSuccessStatusCode)
        {
            var resp = await response.Content.ReadAsStringAsync();
            AppData.user = JsonConvert.DeserializeObject<User>(resp);
            return true;
        }
        return false;
    }
    void Update()
    {

        if (area_names_task.IsCompleted && !got_area_names)
        {
            got_area_names = true;
        }
        
        if (Input.touchCount == 1) // user is touching the screen with a single touch
        {
            Touch touch = Input.GetTouch(0); // get the touch
            if (touch.phase == TouchPhase.Began) //check for the first touch
            {
                fp = touch.position;
                lp = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved) // update the last position based on where they moved
            {
                lp = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended) //check if the finger is removed from the screen
            {
                lp = touch.position;  //last touch position. Ommitted if you use list

                //Check if drag distance is greater than 15% of the screen height
                if (Mathf.Abs(lp.x - fp.x) > dragDistance || Mathf.Abs(lp.y - fp.y) > dragDistance)
                {//It's a drag
                 //check if the drag is vertical or horizontal
                    if (Mathf.Abs(lp.x - fp.x) > Mathf.Abs(lp.y - fp.y))
                    {   //If the horizontal movement is greater than the vertical movement...
                        if ((lp.x > fp.x))  //If the movement was to the right)
                        {   //Right swipe
                            Debug.Log("Right Swipe");
                            swipeRight();
                        }
                        else
                        {   //Left swipe
                            Debug.Log("Left Swipe");
                            swipeLeft();
                        }
                    }
                    else
                    {   //the vertical movement is greater than the horizontal movement
                        if (lp.y > fp.y)  //If the movement was up
                        {   //Up swipe
                            Debug.Log("Up Swipe");
                        }
                        else
                        {   //Down swipe
                            Debug.Log("Down Swipe");
                        }
                    }
                }
                else
                {   //It's a tap as the drag distance is less than 15% of the screen height
                    Debug.Log("Tap");
                }
            }
        }
    }


    void swipeRight()
    {
        switch (currentPanel)
        {
            case Panels.Home:
                openGivePanel();
                break;
            case Panels.Request:
                openHomePanel();
                break;
        }
    }

    void swipeLeft()
    {
        switch (currentPanel)
        {
            case Panels.Home:
                openRequestPanel();
                break;
            case Panels.Give:
                openHomePanel();
                break;
        }
    }

    public void openHomePanel()
    {
        homePanel.SetActive(true);
        givePanel.SetActive(false);
        requestPanel.SetActive(false);

        homeSelected.SetActive(true);
        giveSelected.SetActive(false);
        requestSelected.SetActive(false);

        currentPanel = Panels.Home;
    }

    public void openGivePanel()
    {
        homePanel.SetActive(false);
        givePanel.SetActive(true);
        requestPanel.SetActive(false);

        homeSelected.SetActive(false);
        giveSelected.SetActive(true);
        requestSelected.SetActive(false);

        currentPanel = Panels.Give;
    }

    public void openRequestPanel()
    {
        homePanel.SetActive(false);
        givePanel.SetActive(false);
        requestPanel.SetActive(true);

        homeSelected.SetActive(false);
        giveSelected.SetActive(false);
        requestSelected.SetActive(true);   

        currentPanel = Panels.Request;
    }
}
