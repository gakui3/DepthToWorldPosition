using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using System.Runtime.InteropServices;
using UnityEngine;

public class DepthToWorldPosConverter : MonoBehaviour
{
    [SerializeField]
    AROcclusionManager arOcclusionManager;

    [SerializeField]
    ComputeShader worldPositionCalcurator;

    [SerializeField]
    GameObject footDebugObj;

    [SerializeField]
    LEDFloorController lEDFloorController;

    float timer = 0;

    RenderTexture debugRT;
    Texture2D humanDepthTex;

    const int depthTextureHeight = 256;
    const int depthTextureWidth = 192;
    const float depthOffset = 1.0f;

    Vector3 screenPos;
    Matrix4x4 viewportInv;
    Vector4 screenSize;

    public struct HumanData
    {
        public Vector2 screenPos;
        public float depth;
    };
    ComputeBuffer humanDatasBuffer;


    void Start()
    {
        var data = new List<HumanData>();
        for (int i = 0; i < depthTextureHeight * depthTextureWidth; i++)
        {
            var d = new HumanData();
            d.screenPos = Vector2.zero;
            d.depth = 0;
            data.Add(d);
        }

        humanDatasBuffer = new ComputeBuffer(depthTextureHeight * depthTextureWidth, Marshal.SizeOf(typeof(HumanData)));
        humanDatasBuffer.SetData(data.ToArray());

        debugRT = new RenderTexture(depthTextureWidth, depthTextureHeight, 0, RenderTextureFormat.ARGBFloat);
        debugRT.enableRandomWrite = true;
        debugRT.filterMode = FilterMode.Point;
        debugRT.Create();

        screenSize = new Vector4(Screen.width, Screen.height, 0, 0);
    }

    void Update()
    {
        timer += Time.deltaTime;
        humanDepthTex = arOcclusionManager.humanDepthTexture;
        if (!humanDepthTex)
            return;

        var cameraPos = Camera.main.transform.position;

        worldPositionCalcurator.SetTexture(0, "depthTexture", humanDepthTex);
        worldPositionCalcurator.SetTexture(0, "debugTexture", debugRT);
        worldPositionCalcurator.SetBuffer(0, "humanDatas", humanDatasBuffer);
        worldPositionCalcurator.SetVector("cameraPosition", new Vector4(cameraPos.x, cameraPos.y, cameraPos.z, 1f));
        worldPositionCalcurator.SetFloat("uVMultiplierPortrait", CalculateUVMultiplierPortrait(humanDepthTex));
        worldPositionCalcurator.SetVector("screenSize", screenSize);
        worldPositionCalcurator.SetFloat("depthOffset", depthOffset);
        worldPositionCalcurator.Dispatch(0, depthTextureWidth, depthTextureHeight, 1);

        var resultHumanPositionsArray = new HumanData[depthTextureHeight * depthTextureWidth];
        humanDatasBuffer.GetData(resultHumanPositionsArray);

        for (int i = 0; i < resultHumanPositionsArray.Length; i++)
        {
            if (resultHumanPositionsArray[i].depth > 0)
            {
                var spos = new Vector3(resultHumanPositionsArray[i].screenPos.x, resultHumanPositionsArray[i].screenPos.y, resultHumanPositionsArray[i].depth);
                var wpos = Camera.main.ScreenToWorldPoint(spos);
                detectCollision(wpos);
                footDebugObj.transform.position = wpos;
                break;
            }
        }
    }

    float CalculateUVMultiplierPortrait(Texture textureFromAROcclusionManager)
    {
        float screenAspect = (float)Screen.height / Screen.width;
        float cameraTextureAspect = (float)textureFromAROcclusionManager.width / textureFromAROcclusionManager.height;
        return screenAspect / cameraTextureAspect;
    }

    void SetViewPortInv()
    {
        viewportInv = Matrix4x4.identity;
        viewportInv.m00 = viewportInv.m03 = Screen.width / 2f;
        viewportInv.m11 = Screen.height / 2f;
        viewportInv.m13 = Screen.height / 2f;
        viewportInv.m22 = (Camera.main.farClipPlane - Camera.main.nearClipPlane) / 2f;
        viewportInv.m23 = (Camera.main.farClipPlane + Camera.main.nearClipPlane) / 2f;
        viewportInv = viewportInv.inverse;
    }

    void OnDestroy()
    {
        humanDatasBuffer.Release();
        debugRT.Release();
    }

    void OnGUI()
    {
        if (humanDepthTex != null)
        {
            Graphics.DrawTexture(new Rect(10, 10, depthTextureWidth * 3, depthTextureHeight * 3), debugRT);
        }
    }

    public void detectCollision(Vector3 collisionPos)
    {
        if (timer > 1f)
        {
            lEDFloorController.fire(collisionPos);
            timer = 0;
        }
    }
}
