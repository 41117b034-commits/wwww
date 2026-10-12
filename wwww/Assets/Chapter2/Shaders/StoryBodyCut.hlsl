// A head-space mask removes the static face beneath the small animated patch.
// This avoids duplicating million-vertex body/hand-pose meshes at runtime.
float _StoryCutEnabled;
float4 _StoryCutOrigin, _StoryCutRight, _StoryCutUp, _StoryCutForward;
void StoryBodyFragment(Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    float3 d=input.positionWS-_StoryCutOrigin.xyz;
    float x=dot(d,_StoryCutRight.xyz),y=dot(d,_StoryCutUp.xyz),z=dot(d,_StoryCutForward.xyz);
    if(_StoryCutEnabled>.5 && abs(x)<.055 && y>-.043 && y<.064 && z>-.055)clip(-1);
    LitPassFragment(input,outColor
#ifdef _WRITE_RENDERING_LAYERS
        ,outRenderingLayers
#endif
    );
}
