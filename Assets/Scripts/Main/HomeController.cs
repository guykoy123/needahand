using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
public class HomeController : MonoBehaviour
{
    public GameObject homeContent;
    GameObject postPrefab;

    HttpClient client = new HttpClient();
    bool got_user_posts = false;
    Task<List<Post>> get_user_posts_task;
    // Start is called before the first frame update
    void Start()
    {
        string path = Application.persistentDataPath; //change for production, needs to read token from AppData
        AppData.token = new AuthToken(File.ReadAllText(Application.persistentDataPath + "/token.auth"));

        client.BaseAddress = new System.Uri(AppData.APIaddress);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", AppData.token.get());

        got_user_posts = true;
        get_user_posts_task = get_user_posts();

        postPrefab = Resources.Load<GameObject>("post");
    }

    // Update is called once per frame
    void Update()
    {
        if (got_user_posts && get_user_posts_task.IsCompleted)
        {
            DisplayHomePosts(get_user_posts_task.Result);
            get_user_posts_task = null;
            got_user_posts = false;
        }
    }
    public void ReloadPosts()
    {
        got_user_posts = true;
        get_user_posts_task = get_user_posts();
        GameObject[] posts = GameObject.FindGameObjectsWithTag("Post");
        for(int i = 0; i < posts.Length; i++)
        {
            Destroy(posts[i]);
        }
    }

    void DisplayHomePosts(List<Post> posts)
    {

        for (int i = 0; i < posts.Count; i++)
        {
            GameObject t = Instantiate<GameObject>(postPrefab);
            t.transform.SetParent(homeContent.transform,false);
            t.GetComponent<PostController>().SetPost(posts[i]);
        }
    }
    async Task<List<Post>> get_user_posts()
    {
        var url = "api/get_user_posts";
        Debug.Log(client.BaseAddress + url);
        var response = await client.GetAsync(url);
        if(response.IsSuccessStatusCode){
            var resp = await response.Content.ReadAsStringAsync();
            List<Post> user_posts = JsonConvert.DeserializeObject<List<Post>>(resp);
            return user_posts;
        }
        else{
            Debug.Log("Error loading user's posts");
            Debug.Log(response);
            return null;
        }
    }
}
