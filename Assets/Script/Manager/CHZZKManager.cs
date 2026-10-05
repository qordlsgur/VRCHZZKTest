using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Concurrent;

[System.Serializable]
public class Donation
{
    public string donationType;
    public int payAmount;
    public string donationText;
}

[System.Serializable]
public class Chat
{
    public string nickname;
    public string content;
}

public class CHZZKManager : MonoBehaviour
{
    public VirtualController virtualController;

    private ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();

    private void Update()
    {
        while (chatQueue.TryDequeue(out string message))
        {
            ChatEvent(message);
        }
    }

    public void DonationEvent(string message)
    {
        Donation donation = JsonConvert.DeserializeObject<Donation>(message);

        if(donation.donationType == "CHAT")
        {

        }

        else
        {

        }
    }

    public void ReceiveChat(string message)
    {
        chatQueue.Enqueue(message);
    }

    public void ChatEvent(string message)
    {
        Chat chat = JsonConvert.DeserializeObject<Chat>(message);

        Debug.Log($"채팅 내용: {chat.content}");

        if (chat.content == "ㅎㅇ")
        {
            Debug.Log("ㅎㅇ 감지!");
            virtualController.SetParameter("ParamHappy", 1f);
        }
    }

    public void SubscriptionEvent(string message)
    {

    }

}
