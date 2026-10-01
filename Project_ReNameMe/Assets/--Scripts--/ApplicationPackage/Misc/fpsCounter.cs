using System;
using UnityEngine;

public class fpsCounter : MonoBehaviour
{
    public bool show = true;
    
    private float deltaTime;

    private void Update()
    {
        if (!show) return;
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }
    
    // ai / youtube help:
    // https://www.youtube.com/watch?v=Vg_BvDx0zSw&time_continue=22&source_ve_path=MjM4NTE&embeds_
    // referring_euri=https%3A%2F%2Fwww.bing.com%2F&embeds_referring_origin=https%3A%2F%2Fwww.bing.com
    private void OnGUI()
    {
        if (!show) return;
        int width = Screen.width, height = Screen.height;
        GUIStyle style = new GUIStyle();
        Rect rect = new Rect(0, 0, width, height * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = height * 2 / 100;
        style.normal.textColor = Color.white;
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}
