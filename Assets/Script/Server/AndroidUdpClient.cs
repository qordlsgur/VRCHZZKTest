using Mediapipe.Tasks.Vision.FaceLandmarker;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;
using static Mediapipe.Unity.Sample.FaceLandmarkDetection.FaceLandmarkerRunner;

public class AndroidUdpClient
{
    // UDP Server를 위해서 준비를 해준다.
    private UdpClient udpClient;

    // 안드로이드여서 컴퓨터의 IP를 받아온다.
    private string serverIP = "192.168.1.102";

    // 실행중인 프로그램이 바인딩한 포트 번호
    private int serverPort = 54000;

    float leftCenterX = 0.6613f;
    float leftCenterY = 0.40927f;
    float rightCenterX = 0.65586f;
    float rightCenterY = 0.61463f;

    // 생성자로 
    public AndroidUdpClient()
    {
        udpClient = new UdpClient();
    }

    // 전달해 줄 데이터를 받아오기 위해서 함수를 만들어 준다.
    public void Send(FaceData result)
    {
        float leftX = Mathf.InverseLerp(
            result.LeftEyeData.Inner.x,
            result.LeftEyeData.Outer.x,
            result.LeftEyeData.IrisCenter.x
        );

        float leftY = Mathf.InverseLerp(
            result.LeftEyeData.Upper.y,
            result.LeftEyeData.Lower.y,
            result.LeftEyeData.IrisCenter.y
        );

        float rightX = Mathf.InverseLerp(
            result.RightEyeData.Inner.x,
            result.RightEyeData.Outer.x,
            result.RightEyeData.IrisCenter.x
        );

        float rightY = Mathf.InverseLerp(
            result.RightEyeData.Upper.y,
            result.RightEyeData.Lower.y,
            result.RightEyeData.IrisCenter.y
        );

        Debug.Log($"leftX: {leftX}, rightX: {rightX}");

        float eyeX = (leftX + rightX) / 2f;
        float eyeY = (leftY + rightY) / 2f;

        byte[] buffer = new byte[8];
        Buffer.BlockCopy(BitConverter.GetBytes(eyeX), 0, buffer, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(eyeY), 0, buffer, 4, 4);
        udpClient.Send(buffer, buffer.Length, serverIP, serverPort);
    }

    public void Close()
    {
        udpClient?.Close();
    }
}
