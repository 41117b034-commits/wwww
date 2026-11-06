using UnityEngine;

// Legacy TextMesh's default GUI material draws through people and buildings.
// Keep the dynamic font atlas, but depth-test signs as ordinary world objects.
public sealed class Chapter3SchoolSigns : MonoBehaviour
{
    public Font font;
    public Shader worldTextShader;
    Material worldMaterial;
    void OnEnable()
    {
        if(!font||!worldTextShader)return;
        if(!worldMaterial)worldMaterial=new Material(worldTextShader){name="School world lettering"};
        Font.textureRebuilt+=RefreshAtlas;
        foreach(var text in GetComponentsInChildren<TextMesh>(true))
        {
            font.RequestCharactersInTexture(text.text,text.fontSize,text.fontStyle);
            text.GetComponent<Renderer>().sharedMaterial=worldMaterial;
        }
        RefreshAtlas(font);
    }
    void RefreshAtlas(Font changed){if(changed==font&&worldMaterial)worldMaterial.mainTexture=font.material.mainTexture;}
    void OnDisable(){Font.textureRebuilt-=RefreshAtlas;}
    void OnDestroy(){if(worldMaterial)Destroy(worldMaterial);}
}
