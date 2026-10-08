using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading.Tasks;

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
    public VtuberStodioLoaclHost vtuberStodioLoaclHost;

    private ConcurrentQueue<string> chatQueue = new ConcurrentQueue<string>();

    private void Update()
    {
        while (chatQueue.TryDequeue(out string message))
        {
            ChatEvent(message);
        }
    }

    public async Task DonationEvent(string message)
    {
        Donation donation = JsonConvert.DeserializeObject<Donation>(message);

        if(donation.donationType == "CHAT")
        {
            if(donation.payAmount <= 10000)
            {
                await vtuberStodioLoaclHost.SetHotKey("HAMMER");
            }
        }

        else
        {

        }
    }

    public void ReceiveChat(string message)
    {
        chatQueue.Enqueue(message);
    }

    public async Task ChatEvent(string message)
    {
        Chat chat = JsonConvert.DeserializeObject<Chat>(message);

        Debug.Log($"채팅 내용: {chat.content}");

        if (chat.content == "ㅎㅇ")
        {
            Debug.Log("ㅎㅇ 감지!");
            await vtuberStodioLoaclHost.SetHotKey("Close eye");
        }
    }

    public void SubscriptionEvent(string message)
    {

    }

}
