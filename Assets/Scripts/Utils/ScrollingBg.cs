using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollingBg : MonoBehaviour
{
    public RawImage _image;
    public float _x, _y;

    // Update is called once per frame
    void Update()
    {
        if (_image == null) return;

        // Unscaled: the menu background must keep moving even if a paused timeScale leaked in.
        var uv = _image.uvRect;
        var p = uv.position + new Vector2(_x, _y) * Time.unscaledDeltaTime;
        p = new Vector2(Mathf.Repeat(p.x, 1f), Mathf.Repeat(p.y, 1f));   // the picture repeats every 1 in uv: stay small, stay precise
        _image.uvRect = new Rect(p, uv.size);
    }
}
