using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualController : MonoBehaviour
{
    public PCUdpClient pcUpdClient;

    public CubismParametersInspector cubismParametersInspector;
    public CubismParameter[] cubismParameters;

    void Start()
    {
        cubismParameters = GetComponentsInChildren<CubismParameter>();

        foreach (var parameter in cubismParameters)
        {
            Debug.Log(parameter.Id);
        }
    }

    void Update()
    {
        SetParameter("ParamEyeBallX", pcUpdClient.eyeX);
        SetParameter("ParamEyeBallY", pcUpdClient.eyeY);
    }

    public void SetParameter(string Name, float value)
    {
        foreach (var parameter in cubismParameters)
        {
            if (parameter.Id == Name)
            {
                parameter.Value = value;
            }
        }
    }
}
