using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent,RequireComponent(typeof(Graphic))]
public sealed class GameDialogueOverlayGraphic : MonoBehaviour,IMaterialModifier
{
    Material overlay;
    static Shader uiShader,tmpShader;
    public Material GetModifiedMaterial(Material source)
    {
        if(!isActiveAndEnabled||!source)return source;
        bool tmp=source.shader&&source.shader.name.StartsWith("TextMeshPro/");
        if(!uiShader)uiShader=Resources.Load<Shader>("DialogueUI/ReadableUI");
        if(!tmpShader)tmpShader=Resources.Load<Shader>("DialogueUI/ReadableTMP");
        Shader shader=tmp?tmpShader:uiShader;if(!shader)return source;
        if(!overlay)overlay=new Material(source){name=source.name+" · readable dialogue",hideFlags=HideFlags.HideAndDontSave};
        overlay.shader=shader;overlay.CopyPropertiesFromMaterial(source);overlay.renderQueue=4000;
        return overlay;
    }
    void OnEnable(){GetComponent<Graphic>().SetMaterialDirty();}
    void OnDisable(){GetComponent<Graphic>().SetMaterialDirty();}
    void OnDestroy(){if(overlay)Destroy(overlay);}
}
