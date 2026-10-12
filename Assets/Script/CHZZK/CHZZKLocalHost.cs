using UnityEngine;

public class CHZZKLocalHost : MonoBehaviour
{
    // 치지직 계정 연동 페이지 주소
    // 치지직과 계정과 외부 서비스/애플리케이션을 연동하는 페이지이다.
    public string authURL = "https://chzzk.naver.com/account-interlock";

    // 어떤 프로그램이 치지직 API를 사용 하는지 구별하는 식별자 역할
    public string clientID = "9cdcc9ac-6877-4e2e-b8cc-4f4078729eb1";

    // 인증이 끝난 후 사용자의 브라우저를 어디로 돌려보낼지
    public string redirectUri = "http://localhost:8080/";

    // 내가 방금 인증을 하면서 보낸 값이랑 돌려 보낼때 나온 값이랑 비교를 해서 맞는지 틀린지
    // 구별하기 위해서 사용을 한다.
    public string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    public string state = "";

    void Start()
    {
        SetupController();
    }

    void Update()
    {

    }

    private void SetupController()
    {
        // 우선 랜덤값 5개를 받아온다.
        state = GetRandomState(5);

        // 치지직에서 인증 코드를 받기 위해서 브라우저를 여는 주소이다.
        string accessCodeRequestURL = $"{authURL}?clientId={clientID}&redirectUri={redirectUri}&state={state}";

        // 이 코드는 Unity에서 웹 브라우저를 저 주소로 열기 위한 함수이다.
        Application.OpenURL(accessCodeRequestURL);
    }

    private string GetRandomState(int index)
    {
        string str = "";

        for (int i = 0; i < index; ++i)
        {
            int idx = Random.Range(0, chars.Length);
            str += chars[idx];
        }

        return str;
    }
}
