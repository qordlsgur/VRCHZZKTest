using Live2D.Cubism.Core;
using Live2D.Cubism.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualController : MonoBehaviour
{
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
        //if (Keyboard.current.spaceKey.isPressed)
        //{
        //    SetParameter("ParamHappy");
        //}
    }

    public void SetParameter(string Name)
    {
        foreach (var parameter in cubismParameters)
        {
            if (parameter.Id == Name)
            {
                parameter.Value = 1f;
            }
        }
    }
}
