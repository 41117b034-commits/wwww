using System.Collections;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    [Header("Doorway Incident")]
    public Vector3 incidentDoorPosition = new Vector3(3686f, -5826.4f, -63f);
    public Vector3 incidentDoorOutward = Vector3.back;
    [Min(0.1f)] public float incidentDoorDragSeconds = 5.5f;
    [Min(0.1f)] public float incidentKnockoutHoldSeconds = 3f;
    bool doorwayStaged;
    Chapter1IncidentRig doorwayOfficer, doorwayVictim;
    Vector3 doorwayPoliceRest, doorwayVictimRest, doorwayOut, doorwayRight;
    float doorwayHeight;
    Texture2D doorwayVignette;
    float doorwayVignetteAmount;
    Mesh doorwayHandMesh;
    GameObject doorwayFallenHand;

    void OnDestroy()
    {
        if(doorwayHandMesh!=null)Destroy(doorwayHandMesh);
        if(doorwayFallenHand!=null)Destroy(doorwayFallenHand);
        if(doorwayVignette!=null)Destroy(doorwayVignette);
    }

    void CreateDoorwayFallenHand(Vector3 cameraPosition)
    {
        if(doorwayFallenHand!=null || playerHandsRoot==null || incidentCameraView==null)return;
        SkinnedMeshRenderer source=null;
        foreach(var candidate in playerHandsRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if(candidate.name.ToLowerInvariant().Contains("hand.l")){source=candidate;break;}
        if(source==null)return;
        doorwayHandMesh=new Mesh{name="Incident fallen left hand"};source.BakeMesh(doorwayHandMesh);
        Bounds bounds=doorwayHandMesh.bounds;Vector3 size=bounds.size;
        Vector3 longAxis=size.x>size.y?(size.x>size.z?Vector3.right:Vector3.forward):(size.y>size.z?Vector3.up:Vector3.forward);
        Vector3 thinAxis=size.x<size.y?(size.x<size.z?Vector3.right:Vector3.forward):(size.y<size.z?Vector3.up:Vector3.forward);
        if(longAxis==thinAxis)return;
        float length=Mathf.Max(size.x,Mathf.Max(size.y,size.z));
        foreach(var bone in source.bones)
        {
            if(bone==null)continue;string name=bone.name.ToLowerInvariant();
            if(name.Contains("wrist") || name=="hand.l")
            {if(Vector3.Dot(bounds.center-source.transform.InverseTransformPoint(bone.position),longAxis)<0)longAxis=-longAxis;break;}
        }
        Vector3 right=Vector3.ProjectOnPlane(incidentCameraView.right,Vector3.up).normalized;
        Vector3 forward=Vector3.ProjectOnPlane(incidentCameraView.forward,Vector3.up).normalized;
        Quaternion rotation=Quaternion.LookRotation((right*0.75f+forward).normalized,Vector3.up)*Quaternion.Inverse(Quaternion.LookRotation(-longAxis,thinAxis));
        float scale=doorwayHeight*0.36f/Mathf.Max(0.001f,length);
        // Keep the cut end of the first-person forearm below the frame.
        Vector3 center=cameraPosition+forward*doorwayHeight*0.22f-right*doorwayHeight*0.18f;
        if(TryGetIncidentSurfaceY(center,out float floor))center.y=floor+doorwayHeight*0.025f;
        doorwayFallenHand=new GameObject("Player fallen left hand");
        doorwayFallenHand.AddComponent<MeshFilter>().sharedMesh=doorwayHandMesh;
        doorwayFallenHand.AddComponent<MeshRenderer>().sharedMaterials=source.sharedMaterials;
        doorwayFallenHand.transform.localScale=Vector3.one*scale;
        doorwayFallenHand.transform.SetPositionAndRotation(center-rotation*(bounds.center*scale),rotation);
    }

    Chapter1IncidentRig PrepareDoorwayRig(Transform actor,bool isPolice)
    {
        var rig=actor.GetComponent<Chapter1IncidentRig>();
        if(rig==null)rig=actor.gameObject.AddComponent<Chapter1IncidentRig>();
        rig.Initialize(actor.GetComponentInChildren<Animator>(true),isPolice);
        return rig;
    }
    void PreparePoliceProportions()
    {
        float height=Mathf.Max(GetVisibleRigHeight(femaleVillagerActor),GetVisibleRigHeight(groomActor))*1.04f;
        foreach(Transform actor in new[]{primaryPoliceActor,secondaryPoliceActor})
        {
            if(actor==null)continue;
            actor.rotation=Quaternion.Euler(0,actor.eulerAngles.y,0);
            Vector3 scale=actor.lossyScale;
            actor.localScale=Vector3.Scale(actor.localScale,new Vector3(scale.y/scale.x,1f,scale.y/scale.z));
            float current=GetVisibleRigHeight(actor);
            if(current>0.1f)actor.localScale*=height/current;
            incidentPoliceScaleAdjusted.Add(actor);
            PrepareDoorwayRig(actor,true);
        }
    }
    void PlaceDoorwayActor(Chapter1IncidentRig rig,Vector3 position)
    {
        rig.transform.position=position;
        if(TryGetIncidentSurfaceY(position,out float ground))rig.Ground(ground);
    }
    void SetDoorwayCamera(Vector3 position,Vector3 focus,float fov,float roll=0)
    {
        if(incidentCameraView==null)return;
        Camera camera=incidentCameraView.GetComponent<Camera>();
        if(camera!=null){camera.fieldOfView=fov;camera.rect=new Rect(0,0,1,1);}
        SetCinematicCameraPose(incidentCameraView,incidentCameraPoseLock,position,
            Quaternion.LookRotation(focus-position,Vector3.up)*Quaternion.Euler(0,0,roll));
    }
    IEnumerator DoorwayIncidentRoutine()
    {
        if (incidentHutDoor != null) incidentHutDoor.SetOpen(0f);
        Transform officer=secondaryPoliceActor!=null?secondaryPoliceActor:primaryPoliceActor;
        if(officer==null||femaleVillagerActor==null){Debug.LogError("[Doorway] Missing incident actors.");yield break;}
        StopIncidentActorMotions(true);
        RestoreIncidentShotOccluders();
        doorwayOfficer=PrepareDoorwayRig(officer,true);
        doorwayVictim=PrepareDoorwayRig(femaleVillagerActor,false);
        doorwayHeight=Mathf.Max(doorwayOfficer.Height,doorwayVictim.Height);
        doorwayOut=Vector3.ProjectOnPlane(incidentDoorOutward,Vector3.up).normalized;
        doorwayRight=Vector3.Cross(Vector3.up,-doorwayOut).normalized;
        Vector3 door=incidentDoorPosition;
        if(TryGetIncidentSurfaceY(door,out float floor))door.y=floor;
        doorwayPoliceRest=door+doorwayOut*doorwayHeight*0.16f;
        doorwayVictimRest=doorwayPoliceRest-doorwayRight*doorwayHeight*0.43f+doorwayOut*doorwayHeight*0.11f;
        Vector3 pull=-doorwayOut;
        doorwayOfficer.Face((pull+doorwayRight*0.4f).normalized);
        doorwayVictim.Face((pull+doorwayRight*0.65f).normalized);
        Vector3 startOffset=doorwayOut*doorwayHeight*0.86f-doorwayRight*doorwayHeight*0.58f;
        PlaceDoorwayActor(doorwayOfficer,doorwayPoliceRest+startOffset);
        PlaceDoorwayActor(doorwayVictim,doorwayVictimRest+startOffset);
        doorwayOfficer.batonVisible=true;
        doorwayOfficer.gripWithLeft=false;
        doorwayOfficer.batonInLeftHand=true;
        doorwayOfficer.gripPartner=doorwayVictim.transform;
        doorwayVictim.gripPartner=doorwayOfficer.transform;
        doorwayVictim.gripWithLeft=false;
        doorwayVictim.resisting=true;
        doorwayStaged=true;
        incidentCameraView=GetPlayerViewTransform();
        if(incidentCameraPoseLock==null)incidentCameraPoseLock=BeginCinematicCameraPoseLock(incidentCameraView);
        Vector3 cameraPosition=door+doorwayOut*doorwayHeight*2.1f-doorwayRight*doorwayHeight*1.25f+Vector3.up*doorwayHeight*0.87f;
        Vector3 focus=door-doorwayRight*doorwayHeight*0.66f+Vector3.up*doorwayHeight*0.50f;
        SetDoorwayCamera(cameraPosition,focus,49f);
        ShowLine("旁白","警察抓住女性族人的手腕，強行把她拉向木屋門口。",incidentDoorDragSeconds);
        yield return new WaitForSeconds(0.35f);
        doorwayOfficer.walking=doorwayVictim.walking=true;
        doorwayVictim.strideScale=0.65f;
        float elapsed=0;
        while(elapsed<incidentDoorDragSeconds)
        {
            elapsed+=Time.deltaTime;
            float t=Mathf.Clamp01(elapsed/incidentDoorDragSeconds);
            PlaceDoorwayActor(doorwayOfficer,doorwayPoliceRest+startOffset*(1f-t));
            PlaceDoorwayActor(doorwayVictim,doorwayVictimRest+startOffset*(1f-t));
            yield return null;
        }
        doorwayOfficer.walking=doorwayVictim.walking=false;
        Quaternion policeTurnFrom=officer.rotation,victimTurnFrom=femaleVillagerActor.rotation;
        doorwayOfficer.Face(doorwayOut+doorwayRight*0.15f);
        doorwayVictim.Face(doorwayRight*0.90f+doorwayOut*0.25f);
        Quaternion policeTurnTo=officer.rotation,victimTurnTo=femaleVillagerActor.rotation;
        for(float t=0;t<0.65f;t+=Time.deltaTime)
        {
            float blend=Mathf.SmoothStep(0,1,t/0.65f);
            officer.rotation=Quaternion.Slerp(policeTurnFrom,policeTurnTo,blend);
            femaleVillagerActor.rotation=Quaternion.Slerp(victimTurnFrom,victimTurnTo,blend);
            yield return null;
        }
        officer.rotation=policeTurnTo;femaleVillagerActor.rotation=victimTurnTo;
        doorwayOfficer.frozen=doorwayVictim.frozen=true;
        yield return PlayPoliceVoicedLine("女性族人","放開我！",femaleResistVoice,2.2f);
        if(dialogueUI!=null)dialogueUI.HideInstant();
        ShowConflictChoice();
        if(dialogueUI!=null)dialogueUI.HideInstant();
        if(choiceUI!=null)choiceUI.Hide(); // This shot uses a compact panel in its empty left area.
        SetWeddingDramaBeat("doorway-choice");
        Debug.Log("[Doorway] Choice ready; both actors held at the entrance.");
    }
    IEnumerator ResolveDoorwayChoice(ConflictChoice choice)
    {
        if(choiceUI!=null)choiceUI.Hide();
        if(dialogueUI!=null)dialogueUI.HideInstant();
        if(choice==ConflictChoice.Watch)
        {
            yield return DoorwayWatchRoutine();
            yield break;
        }
        doorwayOfficer.gripPartner=null;
        doorwayVictim.gripPartner=null;
        doorwayOfficer.frozen=false;
        doorwayVictim.resisting=false;
        doorwayVictim.frozen=true;
        yield return PlayPoliceVoicedLine("你","住手！放開她！",playerInterveneVoice,1.8f);
        if(dialogueUI!=null)dialogueUI.HideInstant();
        Vector3 center=(doorwayPoliceRest+doorwayVictimRest)*0.5f;
        Vector3 viewPosition=center+doorwayOut*doorwayHeight*1.45f-doorwayRight*doorwayHeight*0.28f+Vector3.up*doorwayHeight*0.88f;
        Vector3 startView=incidentCameraView.position;
        Quaternion startRotation=incidentCameraView.rotation;
        Vector3 viewFocus=doorwayOfficer.Head.position;
        Quaternion viewRotation=Quaternion.LookRotation(viewFocus-viewPosition,Vector3.up);
        for(float t=0;t<0.6f;t+=Time.deltaTime)
        {
            float blend=Mathf.SmoothStep(0,1,t/0.6f);
            SetCinematicCameraPose(incidentCameraView,incidentCameraPoseLock,Vector3.Lerp(startView,viewPosition,blend),Quaternion.Slerp(startRotation,viewRotation,blend));
            yield return null;
        }
        Vector3 approachEnd=viewPosition-doorwayOut*doorwayHeight*0.55f;approachEnd.y=doorwayPoliceRest.y;
        Vector3 approachStart=doorwayOfficer.transform.position;
        doorwayOfficer.Face(approachEnd-approachStart);
        doorwayOfficer.walking=true;
        float approachSeconds=Mathf.Max(2.4f,policeApproachPlayerSeconds);
        for(float t=0;t<approachSeconds;t+=Time.deltaTime)
        {
            PlaceDoorwayActor(doorwayOfficer,Vector3.Lerp(approachStart,approachEnd,t/approachSeconds));
            SetDoorwayCamera(viewPosition,doorwayOfficer.Head.position-Vector3.up*doorwayHeight*0.05f,56f);
            yield return null;
        }
        PlaceDoorwayActor(doorwayOfficer,approachEnd);doorwayOfficer.walking=false;
        doorwayOfficer.Face(viewPosition-approachEnd);
        doorwayOfficer.strike=true;
        bool impact=false;
        for(float t=0;t<1.15f;t+=Time.deltaTime)
        {
            float p=t/1.15f;doorwayOfficer.strikeProgress=p;
            if(!impact&&p>=0.70f){impact=true;PlayPoliceEventClip(struggleClip);Debug.Log("[Doorway] Baton impact.");}
            doorwayVignetteAmount=p>0.65f?Mathf.Clamp01((p-0.65f)*4f):0f;
            Vector3 shake=p>0.68f?doorwayRight*Mathf.Sin(t*65f)*doorwayHeight*0.005f:Vector3.zero;
            SetDoorwayCamera(viewPosition+shake,doorwayOfficer.Head.position,56f);
            yield return null;
        }
        doorwayOfficer.strike=false;
        // As the player falls the officer backs toward the woman and takes her arm again.
        Vector3 downPosition=center+doorwayOut*doorwayHeight*1.66f-doorwayRight*doorwayHeight*0.06f;
        if(TryGetIncidentSurfaceY(downPosition,out float ground))downPosition.y=ground+doorwayHeight*0.045f;
        Vector3 downFocus=center+Vector3.up*doorwayHeight*0.52f;
        Quaternion downRotation=Quaternion.LookRotation(downFocus-downPosition,Vector3.up)*Quaternion.Euler(0,0,16f);
        Vector3 fallStart=incidentCameraView.position;Quaternion fallRotation=incidentCameraView.rotation;
        Vector3 officerStart=doorwayOfficer.transform.position;
        Vector3 victimStart=doorwayVictim.transform.position;
        doorwayPoliceRest=center-doorwayRight*doorwayHeight*0.23f;
        doorwayVictimRest=center+doorwayRight*doorwayHeight*0.23f;
        doorwayOfficer.walking=true;doorwayOfficer.Face(doorwayPoliceRest-officerStart);
        float fallSeconds=1.15f;
        for(float t=0;t<fallSeconds;t+=Time.deltaTime)
        {
            float p=Mathf.SmoothStep(0,1,t/fallSeconds);
            PlaceDoorwayActor(doorwayOfficer,Vector3.Lerp(officerStart,doorwayPoliceRest,p));
            PlaceDoorwayActor(doorwayVictim,Vector3.Lerp(victimStart,doorwayVictimRest,p));
            SetCinematicCameraPose(incidentCameraView,incidentCameraPoseLock,Vector3.Lerp(fallStart,downPosition,p),Quaternion.Slerp(fallRotation,downRotation,p));
            yield return null;
        }
        PlaceDoorwayActor(doorwayOfficer,doorwayPoliceRest);
        PlaceDoorwayActor(doorwayVictim,doorwayVictimRest);
        doorwayOfficer.walking=false;doorwayOfficer.frozen=true;
        doorwayOfficer.Face(doorwayOut);
        doorwayVictim.Face((doorwayOut-doorwayRight*0.28f).normalized);
        doorwayOfficer.gripPartner=doorwayVictim.transform;
        doorwayVictim.gripPartner=doorwayOfficer.transform;
        doorwayOfficer.gripWithLeft=true;
        doorwayVictim.gripWithLeft=false;
        doorwayOfficer.batonInLeftHand=false;
        SetDoorwayCamera(downPosition,downFocus,58f,16f);
        CreateDoorwayFallenHand(downPosition);
        knockoutCameraPosition=downPosition;knockoutCameraRotation=downRotation;
        playerKnockedOut=true;knockoutDizzyVisible=false;
        doorwayVignetteAmount=0.9f;
        if(dialogueUI!=null)dialogueUI.HideInstant();
        if(fadeCanvas!=null)fadeCanvas.alpha=0;
        Debug.Log("[Doorway] Knockout tableau ready; stopping Play Mode in 3 seconds.");
        yield return new WaitForSecondsRealtime(incidentKnockoutHoldSeconds);
        chapterCompleted=true;cinematicStoryPlaying=false;
        StopChapterPlayMode();
    }
    void DrawDoorwayHud()
    {
        if(doorwayVignetteAmount>0)
        {
            if(doorwayVignette==null)
            {
                doorwayVignette=new Texture2D(128,128,TextureFormat.RGBA32,false);doorwayVignette.wrapMode=TextureWrapMode.Clamp;
                for(int y=0;y<128;y++)for(int x=0;x<128;x++)
                {float r=new Vector2((x-63.5f)/63.5f,(y-63.5f)/63.5f).magnitude;doorwayVignette.SetPixel(x,y,new Color(0,0,0,Mathf.SmoothStep(0,0.94f,Mathf.InverseLerp(0.42f,1.32f,r))));}
                doorwayVignette.Apply();
            }
            Color previous=GUI.color;GUI.color=new Color(1,1,1,doorwayVignetteAmount);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),doorwayVignette);GUI.color=previous;
        }
        if(!waitingForChoice)return;
        EnsureHudStyles();
        float scale=Mathf.Clamp(Screen.height/900f,0.6f,1.5f);
        var titleStyle=new GUIStyle(hudTitleStyle){fontSize=Mathf.RoundToInt(22*scale),alignment=TextAnchor.MiddleLeft,wordWrap=false};
        var buttonStyle=new GUIStyle(hudButtonStyle){fontSize=Mathf.RoundToInt(18*scale),alignment=TextAnchor.MiddleCenter,wordWrap=false};
        Rect box=new Rect(Screen.width*0.045f,Screen.height-202f*scale,365f*scale,166f*scale);
        GUI.Box(box,GUIContent.none,hudBoxStyle);
        GUI.Label(new Rect(box.x+18*scale,box.y+12*scale,box.width-36*scale,30*scale),"你要怎麼做？",titleStyle);
        if(GUI.Button(new Rect(box.x+18*scale,box.y+52*scale,box.width-36*scale,42*scale),"1 / A　上前阻止",buttonStyle)){PlayChapterUiClick();ChooseIntervene();}
        if(GUI.Button(new Rect(box.x+18*scale,box.y+108*scale,box.width-36*scale,42*scale),"2 / B　沉默觀望",buttonStyle)){PlayChapterUiClick();ChooseWatch();}
    }
}
