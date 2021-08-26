using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class LEDFloorController : MonoBehaviour
{

    public struct Ripple
    {
        public Vector3 centerPos;
        public Color color;
        public float value;
    }

    [SerializeField]
    Material mat;

    int effectIdx = 4;

    float bloomIntencity = 2f;

    float lEDFloorOpacity = 1f;

    float LEDpanelDensity = 15f;

    float hue = 1f;

    float saturation = 1f;

    float value = 1f;

    float rippleVelocity = 10f;

    float rippleThickness = 1.5f;

    float effectRange = 20f;

    [SerializeField]
    Gradient grad01;

    [SerializeField]
    Vector4 effectCenter = new Vector4(0, 0, 10, 0);

    [SerializeField]
    List<Texture> textures;

    int currentTextureIdx = 0;
    const int ripplesMaxCount = 50;
    ComputeBuffer ripplesBuffer;
    int ripplesBufferIdx = 0;
    Ripple[] ripplesArray = new Ripple[ripplesMaxCount];


    void Start()
    {
        mat.SetTexture("gradTex01", CreateTexture(grad01));

        initRipplesBuffer();
    }

    void Update()
    {
        mat.SetInt("effectIdx", effectIdx);
        mat.SetFloat("bloomIntencity", bloomIntencity);
        mat.SetFloat("lEDFloorOpacity", lEDFloorOpacity);
        mat.SetFloat("hue", hue);
        mat.SetFloat("saturation", saturation);
        mat.SetFloat("value", value);
        mat.SetFloat("rippleVelocity", rippleVelocity);
        mat.SetFloat("rippleThickness", rippleThickness);
        mat.SetFloat("panelWidth", LEDpanelDensity);
        mat.SetFloat("panelHeight", LEDpanelDensity);
        mat.SetVector("effectCenter", effectCenter);
        mat.SetFloat("effectRange", effectRange);

        if (effectIdx == 4)
        {
            collisionDetect();
            updateRipplesBuffer();
        }
    }

    void initRipplesBuffer()
    {
        ripplesBuffer = new ComputeBuffer(ripplesMaxCount, Marshal.SizeOf(typeof(Ripple)));

        for (int i = 0; i < ripplesMaxCount; i++)
        {
            var data = new Ripple();
            data.centerPos = Vector3.zero;
            data.value = 10f;
            data.color = new Color(0, 0, 0, 0);

            ripplesArray[i] = data;
        }

        ripplesBuffer.SetData(ripplesArray);
        mat.SetBuffer("ripplesBuffer", ripplesBuffer);
    }

    void updateRipplesBuffer()
    {
        for (int i = 0; i < ripplesMaxCount; i++)
        {
            if (ripplesArray[i].value > 10f)
            {
                continue;
            }
            ripplesArray[i].value += Time.deltaTime;
        }

        ripplesBuffer.SetData(ripplesArray);
        mat.SetBuffer("ripplesBuffer", ripplesBuffer);
    }

    void setRipple(Vector3 collisionPos)
    {
        ripplesArray[ripplesBufferIdx].centerPos = collisionPos;
        ripplesArray[ripplesBufferIdx].value = 0;
        ripplesArray[ripplesBufferIdx].color = Color.HSVToRGB(Random.value, 1f, 1f);

        ripplesBufferIdx += 1;
        if (ripplesBufferIdx >= ripplesMaxCount)
        {
            ripplesBufferIdx = 0;
        }
    }

    void collisionDetect()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {
                setRipple(hit.point);
            }
            return;
        }

        if (Input.touchCount <= 0) return;
        if (Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).deltaPosition);
            RaycastHit hit = new RaycastHit();

            if (Physics.Raycast(ray, out hit))
            {
                setRipple(hit.point);
            }
        }
    }

    Texture2D CreateTexture(Gradient gradient)
    {
        Texture2D texture = new Texture2D(128, 1);
        for (int h = 0; h < texture.height; h++)
        {
            for (int w = 0; w < texture.width; w++)
            {
                texture.SetPixel(w, h, gradient.Evaluate((float)w / texture.width));
            }
        }

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }

    void changeTexture()
    {
        currentTextureIdx += 1;
        if (currentTextureIdx >= textures.Count)
        {
            currentTextureIdx = 0;
        }
        mat.SetTexture("_MainTex", textures[currentTextureIdx]);
    }

    public void fire(Vector3 wpos)
    {
        setRipple(wpos);
    }
}
