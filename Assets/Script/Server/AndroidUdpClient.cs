using Mediapipe.Tasks.Vision.FaceLandmarker;
using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using UnityEngine;

public class AndroidUdpClient
{
    // UDP Server를 위해서 준비를 해준다.
    private UdpClient udpClient;

    // 안드로이드여서 컴퓨터의 IP를 받아온다.
    private string serverIP = "192.168.1.102";
    
    // 실행중인 프로그램이 바인딩한 포트 번호
    private int serverPort = 54000;

    // 생성자로 
    public AndroidUdpClient()
    {
        udpClient = new UdpClient();
    }

    // 전달해 줄 데이터를 받아오기 위해서 함수를 만들어 준다.
    public void Send(FaceLandmarkerResult result)
    {
        // 받아온 랜드 마커의 xyz값을 가지고 온다.
        var landmarks = result.faceLandmarks[0].landmarks;

        // 사이즈를 랜드마커의 갯수 * (x,y,z)3 * float크기
        int size = landmarks.Count * 3 * sizeof(float);

        // 사이즈를 만들어 둔다.
        byte[] buffer = new byte[size];

        // 다음 데이터를 어디에 넣어야 할 지 알아야 하기 때문에 필요하다.
        int offset = 0;

        // 이제 모든 값을 넣어준다.
        foreach(var landmark in landmarks)
        {
            WriteFloat(buffer, ref offset, landmark.x);
            WriteFloat(buffer, ref offset, landmark.y);
            WriteFloat(buffer, ref offset, landmark.z);
        }

        // udp에 통신을 해주기 위해서 값을 넣은 Buffer, Buffer의 사이즈, 서버의 Ip와 Port를 넣어준다.
        udpClient.Send(buffer, buffer.Length, serverIP, serverPort);
    }

    // x,y,z의 값을 다 넘겨주기 위해서 설정을 해둔다.
    private void WriteFloat(byte[] buffer, ref int offset, float value)
    {
        // float 1개를 byte 배열로 바꿔준다.
        byte[] bytes = BitConverter.GetBytes(value);

        // 방금 만들어둔 buffer를 현재 offset위치에 복사한다.
        Buffer.BlockCopy(bytes, 0, buffer, offset, sizeof(float));

        // offset 증가
        offset += sizeof(float);
    }

}
