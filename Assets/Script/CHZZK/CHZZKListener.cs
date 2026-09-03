using Newtonsoft.Json.Linq;
using SocketIOClient;
using SocketIOClient.Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;


// 치지직 Access Token이 요구하는 값
[System.Serializable]
public class TokenRequest
{
    public string grantType;
    public string clientId;
    public string clientSecret;
    public string code;
    public string state;
}

// 받아온 값 Json 구조
[System.Serializable]
public class AccessTokenResult
{
    public Content content;
}

// 치지직 Access Token이 반환 해주는 값
[System.Serializable]
public class Content
{
    public string accessToken;
    public string refreshToken;
    public string tokenType;
    public int expiresIn;
}

// 세션 생성을 한 후 받은 url을 저장 해준다.
[System.Serializable]
public class SessionAuthResult
{
    public int code;
    public string message;
    public SessionContent content;
}

// Json 구조로 url을 받아온다.
[System.Serializable]
public class SessionContent
{
    public string url;
}

public class CHZZKListener : MonoBehaviour
{
    public CHZZKLocalHost chzzkLocalHost;

    public CHZZKManager chzzkManager;

    // SocketIO의 버전 문제로 인하여 유니티를 위한 SocketIO를 가지고 온다.
    private SocketIOUnity socket;

    // 프로그램이 HTTP 요청을 직접 받아서 서버 역할을 하게 해주는 C# 클래스이다.
    // 마든 이유는 치지직에서 인증이 끝나면 치지직이 사용자가 지정한 브라우저로
    // 돌려보내는데 요청을 받아줄 프로그램이 없어서 만들어 준다.
    private HttpListener listener;

    private string redirectUrl = "http://localhost:8080/";
    private string baseURL = "https://openapi.chzzk.naver.com";
    private string clientSecret = "2qXaknxusO_DTPjx8s0eddsIC9wyUnZ2gpS0a_9fOs4";
    private string ContentType = "application/json";
    private bool isRunning = false;
    private string accessToken;
    private string refreshToken;
    private string TokenType;
    private int expiresIn;
    private string sessionURL;
    private string sessionKey;
    private string URL_SubscribeChat = "/open/v1/sessions/events/subscribe/chat";
    private string URL_SubscribeDonation = "/open/v1/sessions/events/subscribe/donation";
    private string URL_SubscribeSubscription = "/open/v1/sessions/events/subscribe/subscription";

    private readonly Queue<Action> _mainThreadActions = new Queue<Action>();

    public string code = null;
    private bool tokenRequested = false;
    void Start()
    {
        chzzkManager = GetComponent<CHZZKManager>();

        // 객체를 새로 만들고 그 객체의 주소를 지정 해준다.
        listener = new HttpListener();
        listener.Prefixes.Add(chzzkLocalHost.redirectUri);
        StartListen();
    }

    void Update()
    {
        if(!string.IsNullOrEmpty(code) && !tokenRequested)
        {
            tokenRequested = true;
            StartCoroutine(PostAccessTooken());
        }

        while(true)
        {
            Action action;

            lock (_mainThreadActions)
            {
                if (_mainThreadActions.Count == 0)
                    break;

                action = _mainThreadActions.Dequeue();
            }

            action?.Invoke();
        }

    }

    private void StartListen()
    {
        code = null;
        // 만들어둔 Listener를 시작한다.
        listener.Start();
        Debug.Log("Listenning on: " + chzzkLocalHost.redirectUri);

        // HttpListener의 비동기 요청 대기를 별도 스레드에서 처리하기 위해 Task.Run()을 사용함
        Task.Run(() => ListenForRedirect());
    }

    private async Task ListenForRedirect()
    {
        // listener가 실행 중인지 확인하는 값으로 bool값이다.
        while(listener.IsListening)
        {
            // listener로 HTTP 요청이 들어올 때까지 비동기로 기다린다.
            // await은 요청이 올 때까지 다음 코드 실행을 중단한다.
            // 하지만 스레드를 붙잡고 기다리는 방식은 아님
            var context = await listener.GetContextAsync();
            
            // 받아온 값에서 URL을 추출한다.
            // request를 만든 이유는 code와 state를 꺼내오려고 하기 위해
            var request = context.Request;
            string redirectUrl = request.Url.ToString();

            if (request.Url != null)
            {
                Debug.Log($"Received Request : {request.Url.AbsoluteUri}");
                
                // 엑세스 토큰을 받기 위해 code를 가지고 온다.
                var querieCode = request.QueryString["code"];
                code = querieCode;
            }

            // code를 받아 왔으니 liistener를 멈춰준다.
            listener.Stop();
        }
    }

    IEnumerator PostAccessTooken()
    {
        // 치지직 Access Token 발급을 받아온다.
        string accessTokenURL = $"{baseURL}/auth/v1/token";

        // Json 문자열로 변환한 결과를 저장 해준다.
        string accessTokenJson = JsonUtility.ToJson(new TokenRequest
        {
            // authorization_code 고정이다.
            grantType = "authorization_code",
            // 치지직 애플리케이션에서 받아온 clientID
            clientId = chzzkLocalHost.clientID,
            // 치지직 애플리케이션에서 받은 clientSecret
            clientSecret = clientSecret,
            // 위에서 받은 code
            code = this.code,
            // 인증할 때 만든 state값
            state = chzzkLocalHost.state
        });

        // 문자열로 되어있는 Json을 HTTP로 전송할 수 있는 데이터로 변환 해준다.
        byte[] bodyRaw = Encoding.UTF8.GetBytes(accessTokenJson);

        // using은 2가지가 있지만 여기서 사용하는 using은 사용이 끝난 객체를 자동으로 정리 해주는 역할이다.
        // UnityWebRequest는 사용이 끝나면 정리 작업을 해줘야 하는데 이때 자동으로 정리를 해준다.
        // 무조건 즉시 삭제 하는게 아닌 해당 객체가 제공하는 Dispose를 호출해서 정리를 하는 문법이다.
        // 외부 리소스를 사용하기 나두면 계속 메모리나 리소스를 낭비하기 때문에 지워줘야함
        using (UnityWebRequest request = new UnityWebRequest(accessTokenURL, "POST"))
        {
            // 서버로 보낼 데이터를 POST Body에 넣어서 보낸다.
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            // 서버에서 보내주는 응답을 받을 준비를 한다.
            request.downloadHandler = new DownloadHandlerBuffer();
            // 우리가 보낸 데이터가 어떤 형식인지 서버에 알려주는 HTTP 헤더인데
            // 지금 보내는건 JSON이므로 보통 ContentType은 application/json이다.
            request.SetRequestHeader("Content-Type", ContentType);

            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                AccessTokenResult accessTokenResult = JsonUtility.FromJson<AccessTokenResult>(request.downloadHandler.text);
                Debug.Log("===Get Access Token===");
                // 받아온 결과를 저장 해준다.
                refreshToken = accessTokenResult.content.refreshToken;
                accessToken = accessTokenResult.content.accessToken;
                TokenType = accessTokenResult.content.tokenType;
                expiresIn = accessTokenResult.content.expiresIn;

                StartCoroutine(GetSessionAuth());

            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    IEnumerator GetSessionAuth()
    {
        // 토큰을 통하여 소켓 연결을 위한 URL을 받아온다.
        string url = $"{baseURL}/open/v1/sessions/auth";

        // 세션 인증 API에 GET 요청을 보내 소켓 연결에 사용할 URL을 받아온다.
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Access Token을 Authorization 헤더에 담아 인증한다.
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);

            // 서버에 요청을 한다.
            yield return request.SendWebRequest();

            // 만약 성공하면 이제 값을 받아온다.
            if(request.result == UnityWebRequest.Result.Success)
            {
                // 받아온 Json값을 저장 해주고 거기에서 필요한 url을 저장 해준다.
                SessionAuthResult result = JsonUtility.FromJson<SessionAuthResult>(request.downloadHandler.text);
                sessionURL = result.content.url;

                Debug.Log("=== Get Session Auth ===");
                Debug.Log(request.downloadHandler.text);

                StartCoroutine(ConnectSocket());
            }
            else
            {
                Debug.LogError("Session Error : " + request.error);
                Debug.LogError(request.downloadHandler.text);
            }
        }
    }

    IEnumerator ConnectSocket()
    {
        // 연결을 해주는데 URL이 없으면 멈춘다.
        if (string.IsNullOrEmpty(sessionURL))
        {
            Debug.LogError("sessionURL이 없습니다.");
            yield break;
        }

        Debug.Log("Socket URL : " + sessionURL);

        Uri uri = new Uri(sessionURL);

        // Uri에서 Scheme, Host, Port를 추출하여 Socket.IO 서버 주소를 만든다.
        // 서버 주소는 고정하지 않고 치지직에서 전달받은 URL을 사용한다.
        string serverUrl = uri.Scheme + "://" + uri.Host + ":" + uri.Port;

        // URL 뒤에 붙어있는 Query string을 잘라서 각각의 값을 쉽게 꺼내기 위해서 받아온다.
        var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);

        // Query String에서 auth 값을 가져온다.
        string authToken = queryParams["auth"];

        if (string.IsNullOrEmpty(authToken))
        {
            Debug.LogError("auth 토큰이 없습니다.");
            yield break;
        }

        // 세션 URL에서 추출한 authToken을 사용하여 Socket.IO 연결을 설정한다.
        // serverUrl을 Uri 객체로 변환한 후 SocketIOUnity 객체를 생성한다.
        socket = new SocketIOUnity(new Uri(serverUrl), new SocketIOOptions
        {
            // 위에서 받아온 authToken을 SOcket.IO 연결 요청에 얺어준다.
            Query = new Dictionary<string, string> { {"auth", authToken } },
            // Socket.IO는 내부적으로 Engine.IO라는 통신 계층을 사용 하는데 여기서 v3를 사용해줌
            EIO = SocketIOClient.EngineIO.V3,
            // 소켓 방식을 WebSocket으로 지정을 해준다.
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
            // 연결이 끊어질떄 자동으로 연결해주는걸 꺼준다.
            Reconnection = false,
            // 3초동안 연결하고 안되면 실패처리
            ConnectionTimeout = TimeSpan.FromMilliseconds(3000)
        });

        // 이제 Socket.IO에서 주고받는 Json 데이터를 처리할 떄 NewTonsogt.Json
        // 기반 Serializer를 사용하도록 지정 해준다.
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

        // Socket.IO로 치지직 세션에 연결한 다음 SYSTEM 이벤트를 받았을 때
        // 바로 처리 해주기 위해서 람다식으로 받아온다.
        socket.On("SYSTEM", ev =>
        {
            try
            {
                // ev의 Value를 string으로 받아온다.
                string evString = ev.GetValue<string>();
                Debug.LogWarning($"[SYSTEM Event] Raw message received: {evString}");

                // 그리고 받아온 값을 Json으로 파싱해서 사용한다.
                JObject json = JObject.Parse(evString);

                // 우선 타입을 가져온다.
                string type = json["type"]?.ToString() ?? "";
                Debug.LogWarning($"[SYSTEM Event] Parsed type: {type}, Full message: {evString}");

                // 타입에 따라서
                switch (type)
                {
                    // 연결 완료
                    case "connected":
                        {
                            // sessionKey값을 꺼내서 문자열로 만들어 준다.
                            string parsedSessionkey = json["data"]?["sessionKey"]?.ToString();

                            if (!string.IsNullOrEmpty(parsedSessionkey))
                            {
                                // 키 값을 저장 해준다.
                                sessionKey = parsedSessionkey;

                                Debug.Log($"[SYSTEM] sessionKey: {sessionKey}");

                                // 여러 스레드가 Queue에 동시에 접근하는 것을 보호 해준다.
                                lock (_mainThreadActions)
                                {
                                    // 이벤트를 바로 실행하는 것이 아닌 Queue에 저장을 해준다.
                                    // StartCoroutine()은 Unity 메인 스레드에서 호출해야 하는데
                                    // 메인 스레드가 아닌 곳에서 호출하면 런타임에서 문제가 발생할 수 있다.
                                    // 그래서 Queue에 등록해 두고 메인 스레드에서 실행할 수 있도록 해준다.
                                    _mainThreadActions.Enqueue(() =>
                                    {
                                        StartCoroutine(SubscribeChat());
                                        StartCoroutine(SubscribeDonation());
                                        StartCoroutine(SubscribeSubscription());
                                    });
                                }
                            }

                            break;
                        }
                    case "subscribed":
                        Debug.Log("[SYSTEM] 이벤트 구독 완료");
                        break;

                    case "unsubscribed":
                        Debug.Log("[SYSTEM] 이벤트 구독 해제");
                        break;
                    case "revoked":
                        Debug.LogWarning("[SYSTEM] 이벤트 구독 또는 권한이 취소되었습니다.");
                        break;
                    default:
                        Debug.LogWarning($"[SYSTEM] 알 수 없는 타입: {type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SYSTEM Event Handler] 예외 발생: {ex.Message}\nReceived data: {ev}\nStackTrace: {ex.StackTrace}");
            }
        });

        socket.OnConnected += (sender, e) =>
        {
            Debug.LogWarning("[ConnectSocket] Successfully connected to Socket.IO server!");
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.LogWarning("[ConnectSocket] Disconnected from server");
        };

        // 서버에서 전달되는 채팅 이벤트를 받아준다.
        socket.On("CHAT", (ev) =>
        {
            try
            {
                string message = ev.GetValue<string>();
                chzzkManager.ReceiveChat(message);
                Debug.Log($"[ConnectSocket] Received CHAT message: {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CHAT Event Handler] 예외 발생: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        });

        // 서버에서 전달되는 도네이션 이벤트를 받아준다.
        socket.On("DONATION", (ev) =>
        {
            try
            {
                string message = ev.GetValue<string>();
                chzzkManager.DonationEvent(message);
                Debug.Log($"[ConnectSocket] Received DONATION message: {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DONATION Event Handler] 예외 발생: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        });

        // 서버에서 전달되는 구독 이벤트를 받아준다.
        socket.On("SUBSCRIPTION", (ev) =>
        {
            try
            {
                string message = ev.GetValue<string>();
                chzzkManager.SubscriptionEvent(message);
                Debug.Log($"[ConnectSocket] Received SUBSCRIPTION message: {message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Subscription Event Handler] 예외 발생: {ex.Message}\nStackTrace: {ex.StackTrace}");
            }
        });

        // 만들어 놓은 SOcket.IO 객체로 실제 서버 연결을 시도한다.
        try
        {
            socket.Connect();
        }
        catch (Exception ex)
        {
            Debug.LogError($"연결 실패: {ex.Message}\nStackTrace: {ex.StackTrace}");
            yield break;
        }
    }

    // 치지직 서버에서 CHAT 이벤트 구독을 요청하는 코드이다.
    IEnumerator SubscribeChat()
    {
        string subscribeChatURL = $"{baseURL}{URL_SubscribeChat}?sessionKey={sessionKey}";
        using (UnityWebRequest subscribeChatRequest = new UnityWebRequest(subscribeChatURL, "POST"))
        {
            subscribeChatRequest.SetRequestHeader("Authorization", $"{TokenType} {accessToken}");
            subscribeChatRequest.SetRequestHeader("Content-Type", ContentType);

            yield return subscribeChatRequest.SendWebRequest();
            if (subscribeChatRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[CHAT] subscribeChat Succeeded");
            }
            else
            {
                Debug.LogWarning(
                          $"[CHAT] subscribeChat Failed\n" +
                          $"Result: {subscribeChatRequest.result}\n" +
                          $"ResponseCode: {subscribeChatRequest.responseCode}\n" +
                          $"Error: {subscribeChatRequest.error}\n" +
                          $"Response: {subscribeChatRequest.downloadHandler?.text}"
                                );
            }
        }
    }

    // 치지직 서버에서 Donation 이벤트 구독을 요청하는 코드이다.
    IEnumerator SubscribeDonation()
    {
        string subscribeDonationURL = $"{baseURL}{URL_SubscribeDonation}?sessionKey={sessionKey}";
        using (UnityWebRequest subscribeDonationRequest = new UnityWebRequest(subscribeDonationURL, "POST"))
        {
            subscribeDonationRequest.SetRequestHeader("Authorization", $"{TokenType} {accessToken}");
            subscribeDonationRequest.SetRequestHeader("Content-Type", ContentType);
            yield return subscribeDonationRequest.SendWebRequest();
            if (subscribeDonationRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[DONATION] subscribeChat Succeeded");
            }
            else
            {
                Debug.LogWarning($"[DONATION] subscribeChat Failed");
            }
        }
    }

    // 치지직 서버에서 Subscription 이벤트 구독을 요청하는 코드이다.
    IEnumerator SubscribeSubscription()
    {
        string subscribeSubscriptionURL = $"{baseURL}{URL_SubscribeSubscription}?sessionKey={sessionKey}";
        using (UnityWebRequest subscribeSubscriptionRequest = new UnityWebRequest(subscribeSubscriptionURL, "POST"))
        {
            subscribeSubscriptionRequest.SetRequestHeader("Authorization", $"{TokenType} {accessToken}");
            subscribeSubscriptionRequest.SetRequestHeader("Content-Type", ContentType);
            yield return subscribeSubscriptionRequest.SendWebRequest();
            if (subscribeSubscriptionRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[Subscription] subscribeChat Succeeded");
            }
            else
            {
                Debug.LogWarning($"[Subscription] subscribeChat Failed");
            }
        }
    }
}