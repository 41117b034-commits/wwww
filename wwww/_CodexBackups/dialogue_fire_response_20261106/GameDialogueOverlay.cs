using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// HUD dialogue must remain readable even when a character is closer to the
// camera than its canvas. Keep the authored layout and VR world-space position.
[DisallowMultipleComponent,RequireComponent(typeof(Canvas)),DefaultExecutionOrder(6000)]
public sealed class GameDialogueOverlay : MonoBehaviour
{
    readonly HashSet<int> prepared=new HashSet<int>();
    Canvas canvas;
    public static void Ensure(Canvas target)
    {
        if(!target)return;
        var layer=target.GetComponent<GameDialogueOverlay>();
        if(!layer)layer=target.gameObject.AddComponent<GameDialogueOverlay>();
        layer.canvas=target;layer.Prepare();
    }
    void Awake(){canvas=GetComponent<Canvas>();}
    void LateUpdate(){Prepare();}
    void Prepare()
    {
        if(!canvas)return;
        // GraphicRegistry includes newly shown panels and TMP fallback glyph
        // submeshes. No scene searches or per-frame material allocations.
        var graphics=GraphicRegistry.GetGraphicsForCanvas(canvas);
        for(int i=0;i<graphics.Count;i++)
        {
            var graphic=graphics[i];if(!graphic||!prepared.Add(graphic.GetInstanceID()))continue;
            if(!graphic.GetComponent<GameDialogueOverlayGraphic>())graphic.gameObject.AddComponent<GameDialogueOverlayGraphic>();
        }
    }
}
