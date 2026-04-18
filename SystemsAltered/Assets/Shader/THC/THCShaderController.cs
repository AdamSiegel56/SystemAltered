using System;
using UnityEngine;

public class THCShaderController : MonoBehaviour
{
    public Material thcMaterial;
    public float value;
    public float ratePerSecond;
    public float fullvalue;
    public bool hasStarted;
    private void Start()
    {
        thcMaterial = GetComponent<Renderer>().material;
    }

    private void Update()
    {
        if (value < fullvalue) {return;}
        
        value += ratePerSecond * Time.deltaTime;
        thcMaterial.SetFloat("_Progress", value);
    }
}
