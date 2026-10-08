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
        _image.uvRect = new Rect(uv.position + new Vector2(_x, _y) * Time.unscaledDeltaTime, uv.size);
    }
}
