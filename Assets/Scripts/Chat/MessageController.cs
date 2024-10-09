using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MessageController : MonoBehaviour
{
    TMP_Text messageText; 
    Image messageBubble;
    Message message;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetMessage(Message msg){
        this.message=msg;
        messageText = transform.GetComponentInChildren<TMP_Text>();
        messageBubble = transform.GetComponent<Image>();
        if (AppData.user.pk == message.GetAuthor().pk){
            messageBubble.color = new Color(95,255,166); //my message color
        }
        else{
            messageBubble.color = new Color(167,255,255); //other person message color
        }
        messageText.text=message.GetText();
        messageText.ForceMeshUpdate();
        // Bounds textBounds = messageText.textBounds;

        // float size = messageBubble.GetComponent<Renderer> ().bounds.size.y;
        // Vector3 rescale = messageBubble.transform.localScale;
        // rescale.x = (Screen.width*0.5f) * rescale.x / size;
        // rescale.y = (textBounds.extents.y * 2 + 40) * rescale.y / size;
        // messageBubble.transform.localScale = rescale;

    }

    public void UpdateSeen(){
        this.message.UpdateSeen();
        //TODO: add visual que
        //TODO: update db
    }
}
