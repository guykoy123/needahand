using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using TMPro;
using System.Linq;
using UnityEngine.UI;

public class GiveController : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public GameObject Content;
    public ScrollRect scrollRect;

    bool firstLoad = false;
    bool loadExtra = false;
    
    GameObject postPrefab;
    
    int offset = 0;
    int count = 8;
    
    HttpClient client = new HttpClient();
    bool got_posts = false;
    Task<List<Post>> get_posts_task;
    List<string> post_pks = new List<string>();//stores the keys of all displayed posts to prevent duplicates
    // Start is called before the first frame update
    void Start()
    {
        string path = Application.persistentDataPath; //change for production, needs to read token from AppData
        AppData.token = new AuthToken(File.ReadAllText(Application.persistentDataPath + "/token.auth"));

        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());

        postPrefab = Resources.Load<GameObject>("post");


    }

    public void OnEnable()
    {
        List<string> areas = AppData.areaNamesDict.Values.ToList();
        dropdown.ClearOptions();
        dropdown.AddOptions(areas);
    }
    public void LoadPosts()
    {
        post_pks.Clear();
        offset = 0;
        got_posts = true;
        get_posts_task = get_posts(dropdown.value.ToString());
        GameObject[] posts = GameObject.FindGameObjectsWithTag("Post");
        for (int i = 0; i < posts.Length; i++)
        {
            Destroy(posts[i]);
        }
        firstLoad = true;
    }
    public void LoadPostsExtra()
    {
        got_posts = true;
        get_posts_task = get_posts(dropdown.value.ToString());
        loadExtra = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (got_posts && get_posts_task.IsCompleted)
        {
            DisplayPosts(get_posts_task.Result);
            offset += count;
            get_posts_task = null;
            got_posts = false;
            loadExtra = false;
        }
        if (scrollRect.verticalNormalizedPosition <= 0.05f && firstLoad && get_posts_task == null && !loadExtra) // scrolled near end
        {
            Debug.Log("scrolled near the end");
            LoadPostsExtra();
        }
    }
    void DisplayPosts(List<Post> posts)
    {

        for (int i = 0; i < posts.Count; i++)
        {
            if (!post_pks.Contains(posts[i].pk))
            {
                GameObject t = Instantiate<GameObject>(postPrefab);
                t.transform.SetParent(Content.transform, false);
                t.GetComponent<PostController>().SetPost(posts[i]);
                post_pks.Add(posts[i].pk);
            }
        }
    }
    async Task<List<Post>> get_posts(string area_code)
    {
        var url = "api/recent_posts";
        Dictionary<string, string> postData = new Dictionary<string, string>();
        postData.Add("post_type", "gv");
        postData.Add("area", area_code);
        postData.Add("count", count.ToString());
        postData.Add("offset", offset.ToString());
        var content = new FormUrlEncodedContent(postData);
        var response = await client.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            var resp = await response.Content.ReadAsStringAsync();
            List<Post> user_posts = JsonConvert.DeserializeObject<List<Post>>(resp);
            return user_posts;
        }

        Debug.Log(response.StatusCode);
        return null;
    }
}
