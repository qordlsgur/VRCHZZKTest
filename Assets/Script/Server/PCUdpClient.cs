using System;
using System.Net.Sockets;
using UnityEngine;
using static Mediapipe.Unity.Sample.FaceLandmarkDetection.FaceLandmarkerRunner;

public class PCUdpClient : MonoBehaviour
{
    private UdpClient udpClient;

    public float eyeX;
    public float eyeY;

    async public void Start()
    {
        udpClient = new UdpClient(50001);

        while (udpClient != null)
        {
            UdpReceiveResult result = await udpClient.ReceiveAsync();

            byte[] data = result.Buffer;

            int offset = 0;

            eyeX = BitConverter.ToSingle(data, offset);
            offset += 4;

            eyeY = BitConverter.ToSingle(data, offset);

            Debug.Log($"eyeX: {eyeX}, eyeY: {eyeY}");
        }
    }

    private void ReadVector3(byte[] buffer, ref int offset, ref Vector3 value)
    {
        value.x = BitConverter.ToSingle(buffer, offset);
        offset += 4;
        value.y = BitConverter.ToSingle(buffer, offset);
        offset += 4;
        value.z = BitConverter.ToSingle(buffer, offset);
        offset += 4;
    }

}
