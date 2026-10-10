using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Morning-only quality override. The shared project pipeline and night scene stay authored as before.
public sealed class Chapter2ForestDetail : MonoBehaviour
{
    public Camera view;
    public Renderer ground;
    public Material detailedFloor;
    Material previousFloor;
    RenderPipelineAsset previousQuality;
    UniversalRenderPipelineAsset morningPipeline;
    UniversalAdditionalCameraData cameraData;
    AntialiasingMode previousAA;
    AntialiasingQuality previousAAQuality;
    bool previousPostProcessing;

    void OnEnable()
    {
        if(!Application.isPlaying)return;
        if(ground&&detailedFloor){previousFloor=ground.sharedMaterial;ground.sharedMaterial=detailedFloor;}
        previousQuality=QualitySettings.renderPipeline;
        var source=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(source)
        {
            morningPipeline=Instantiate(source);morningPipeline.name="Chapter2 morning detail (runtime)";
            morningPipeline.hideFlags=HideFlags.DontSave;
            morningPipeline.renderScale=Mathf.Max(1,source.renderScale);
            morningPipeline.mainLightShadowmapResolution=view&&view.stereoEnabled?2048:4096;
            morningPipeline.shadowDistance=65;
            morningPipeline.shadowCascadeCount=4;
            QualitySettings.renderPipeline=morningPipeline;
        }
        if(view)
        {
            cameraData=view.GetUniversalAdditionalCameraData();
            previousAA=cameraData.antialiasing;previousAAQuality=cameraData.antialiasingQuality;
            previousPostProcessing=cameraData.renderPostProcessing;
            // SMAA is compatible with this project's deferred renderer and avoids temporal blur.
            if(!view.stereoEnabled){cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;cameraData.antialiasingQuality=AntialiasingQuality.High;}
        }
    }

    void OnDisable()
    {
        if(ground&&previousFloor)ground.sharedMaterial=previousFloor;
        if(cameraData){cameraData.antialiasing=previousAA;cameraData.antialiasingQuality=previousAAQuality;cameraData.renderPostProcessing=previousPostProcessing;}
        if(morningPipeline)
        {
            if(QualitySettings.renderPipeline==morningPipeline)QualitySettings.renderPipeline=previousQuality;
            Destroy(morningPipeline);morningPipeline=null;
        }
    }
}
