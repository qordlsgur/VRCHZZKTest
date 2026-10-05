using System.Threading.Tasks;
using UnityEngine;
using NativeWebSocket;

[System.Serializable]
public class MassageType
{
    public string messageType;
}

[System.Serializable]
public class AuthenticationTokenResponse
{
    public string messageType;
    public AuthenticationData data;
}
[System.Serializable]
public class AuthenticationData
{
    public string authenticationToken;
}

[System.Serializable]
public class AuthenticationResponse
{
    public string messageType;
    public Authenticationdata data;
}

[System.Serializable]
public class Authenticationdata
{
    public bool authenticated;
    public string reason;
}

public class VtuberStodioLoaclHost : MonoBehaviour
{
    private WebSocket VtuberStudioSoc;
    private string authenticationToken;

    void Start()
    {
        SetupController();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //Setexpressions("exp_05.exp3.json");
            SetHotKey("Close eye");
        }
    }

    private async Task SetupController()
    {
        // websocket을 이용하여서 접속을 해준다.
        // SDK NativeWebSocket을 이용하여서 바로 열어준다.
        VtuberStudioSoc = new WebSocket("ws://127.0.0.1:8001");

        // NativeWebSocket에 있는 함수 OnMessage를 이용해서
        // WebSocket에 메세지가 들어오면 그 값을 받아둔다.
        VtuberStudioSoc.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);

            Debug.Log("VTube Studio 응답 : " + message);

            MassageType Type = JsonUtility.FromJson<MassageType>(message);

            if (Type.messageType == "AuthenticationTokenResponse")
            {
                AuthenticationTokenResponse response =
                        JsonUtility.FromJson<AuthenticationTokenResponse>(message);

                authenticationToken = response.data.authenticationToken;

                Debug.Log("인증 토큰 저장 완료");

                ReqeustYoken();
            }

            if (Type.messageType == "AuthenticationResponse")
            {
                AuthenticationResponse response =
                       JsonUtility.FromJson<AuthenticationResponse>(message);

                if (response.data.authenticated == true)
                    Debug.Log("인증 성공");

                else
                    Debug.Log("인증 실패");
            }
        };

        VtuberStudioSoc.Connect();

        Debug.Log("Connect");

        await Task.Delay(1000);

        await RequestVtuberStdio();
    }

    private async Task RequestVtuberStdio()
    {
        Debug.Log("RequestVtuberStdio 진입");

        string request = @"
        {
        ""apiName"" : ""VTubeStudioPublicAPI"",
        ""apiVersion"" : ""1.0"",
        ""requestID"" : ""SomeID"",
        ""messageType"" : ""AuthenticationTokenRequest"",
        ""data"" : {
            ""pluginName"" : ""VRCHZZKTest"",
            ""pluginDeveloper"" : ""Baek""
        }}";

        await VtuberStudioSoc.SendText(request);

        Debug.Log("인증 토큰 요청 전송 완료");
    }

    private async Task ReqeustYoken()
    {
        Debug.Log("Token 전달");

        string request = $@"
        {{
        ""apiName"" : ""VTubeStudioPublicAPI"",
        ""apiVersion"" : ""1.0"",
        ""requestID"" : ""SomeID"",
        ""messageType"" : ""AuthenticationRequest"",
        ""data"" : {{
            ""pluginName"" : ""VRCHZZKTest"",
            ""pluginDeveloper"" : ""Baek"",
            ""authenticationToken"" : ""{authenticationToken}""
            }}
        }}";

        Debug.Log("전송 직전");

        await VtuberStudioSoc.SendText(request);

        Debug.Log("인증 토큰 전달 완료");
    }

    private async Task Setexpressions(string TextName)
    {
        Debug.Log($"{TextName}");

        string request = $@"
        {{
            ""apiName"": ""VTubeStudioPublicAPI"",
	        ""apiVersion"": ""1.0"",
	        ""requestID"": ""SomeID"",
	        ""messageType"": ""ExpressionActivationRequest"",
	        ""data"": {{
		    ""expressionFile"": ""{TextName}"",
		    ""fadeTime"": 0.5,
		    ""active"": true
            }} 
        }}";

        await VtuberStudioSoc.SendText(request);
    }

    private async Task SetHotKey(string HotKey)
    {
        Debug.Log($"{HotKey}");

        string request = $@"
        {{
        	""apiName"": ""VTubeStudioPublicAPI"",
	        ""apiVersion"": ""1.0"",
	        ""requestID"": ""SomeID"",
	        ""messageType"": ""HotkeyTriggerRequest"",
	        ""data"": {{
		    ""hotkeyID"": ""{HotKey}""
	        }}
        }}";

        await VtuberStudioSoc.SendText(request);
    }
}
