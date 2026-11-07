using System.Collections.Generic;
using UnityEngine;

// The connector and school share one daylight state before the gate handoff.
// Only runtime light values change; serialized lighting is restored on reset.
public sealed class Chapter3SchoolLighting : MonoBehaviour
{
    const float SchoolFogDensity=.0065f;
    readonly List<Light> previousLights=new List<Light>();
    Light schoolSun,previousSun;
    SunState schoolState,approachState;
    float previousFogDensity;
    bool captured,approaching;

    struct SunState
    {
        public Quaternion rotation;
        public Color color;
        public float intensity,strength,bias,normalBias,nearPlane,temperature;
        public bool enabled,useTemperature;
        public LightShadows shadows;
        public SunState(Light light)
        {
            rotation=light.transform.rotation;color=light.color;intensity=light.intensity;
            strength=light.shadowStrength;bias=light.shadowBias;normalBias=light.shadowNormalBias;
            nearPlane=light.shadowNearPlane;temperature=light.colorTemperature;
            enabled=light.enabled;useTemperature=light.useColorTemperature;shadows=light.shadows;
        }
        public void Apply(Light light)
        {
            light.transform.rotation=rotation;light.color=color;light.intensity=intensity;
            light.shadowStrength=strength;light.shadowBias=bias;light.shadowNormalBias=normalBias;
            light.shadowNearPlane=nearPlane;light.colorTemperature=temperature;
            light.useColorTemperature=useTemperature;light.shadows=shadows;light.enabled=enabled;
        }
    }

    void Capture(Chapter3SchoolController owner)
    {
        if(captured)return;
        schoolSun=owner.sun;previousSun=RenderSettings.sun;
        previousFogDensity=RenderSettings.fogDensity;
        if(schoolSun)schoolState=new SunState(schoolSun);
        captured=true;
    }
    public void Preview(Chapter3SchoolController owner)
    {
        Capture(owner);
        if(schoolSun)schoolSun.enabled=false;
    }
    public void Approach(Chapter3SchoolController owner,float worldZ)
    {
        Capture(owner);
        if(!approaching)
        {
            if(owner.flow)
                foreach(var region in new[]{owner.flow.villageRegion,owner.flow.stationRegion,owner.flow.combatRegion})
                    if(region)foreach(var light in region.GetComponentsInChildren<Light>())
                        if(light!=schoolSun&&light.isActiveAndEnabled&&light.type==LightType.Directional)
                            previousLights.Add(light);
            Light source=null;
            foreach(var light in previousLights)
                if(!source||light==previousSun||light.intensity>source.intensity&&source!=previousSun)source=light;
            approachState=source?new SunState(source):schoolState;
            if(schoolSun)
            {
                // Swap identical values in one frame, then blend one sun.
                // Two simultaneously enabled suns would over-light the trail.
                foreach(var light in previousLights)light.enabled=false;
                approachState.Apply(schoolSun);schoolSun.enabled=true;RenderSettings.sun=schoolSun;
            }
            approaching=true;
        }
        // Reach the school tone before the field is revealed at z=-28,
        // rather than waiting for the gameplay boundary near z=-18.
        float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-47,-31,worldZ));
        RenderSettings.fogDensity=Mathf.Lerp(previousFogDensity,SchoolFogDensity,t);
        if(!schoolSun)return;
        schoolSun.transform.rotation=Quaternion.Slerp(approachState.rotation,schoolState.rotation,t);
        schoolSun.color=Color.Lerp(approachState.color,schoolState.color,t);
        schoolSun.intensity=Mathf.Lerp(approachState.intensity,schoolState.intensity,t);
        schoolSun.shadowStrength=Mathf.Lerp(approachState.strength,schoolState.strength,t);
        schoolSun.shadowBias=Mathf.Lerp(approachState.bias,schoolState.bias,t);
        schoolSun.shadowNormalBias=Mathf.Lerp(approachState.normalBias,schoolState.normalBias,t);
        schoolSun.shadowNearPlane=Mathf.Lerp(approachState.nearPlane,schoolState.nearPlane,t);
        schoolSun.colorTemperature=Mathf.Lerp(approachState.temperature,schoolState.temperature,t);
        if(t>=1){schoolState.Apply(schoolSun);schoolSun.enabled=true;}
    }
    public void Enter(Chapter3SchoolController owner)
    {
        Capture(owner);
        if(schoolSun){schoolState.Apply(schoolSun);schoolSun.enabled=true;RenderSettings.sun=schoolSun;}
        RenderSettings.fogDensity=SchoolFogDensity;
    }
    public void Restore()
    {
        if(!captured)return;
        if(schoolSun)schoolState.Apply(schoolSun);
        foreach(var light in previousLights)if(light)light.enabled=true;
        previousLights.Clear();RenderSettings.sun=previousSun;RenderSettings.fogDensity=previousFogDensity;
        captured=approaching=false;
    }
    void OnDisable(){Restore();}
}
