using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class Chapter2Controller
{
    public string CouncilCue { get; private set; }
    public int CouncilLineIndex { get; private set; }
    public bool RefusalApproachComplete { get; private set; }
    readonly List<Chapter2Actor> councilCast = new List<Chapter2Actor>();

    void PrepareCouncilDrama()
    {
        // Named references are authored into the scene. Fallback also supports older saved scenes.
        if(!tado)tado=leaders[1];if(!bawan)bawan=leaders[3];if(!watan)watan=leaders[0];
        mona.name="莫那·魯道";tado.name="達多·莫那";bawan.name="巴萬·拿威";watan.name="瓦旦";
        councilCast.Clear();councilCast.Add(mona);
        foreach(var a in leaders)if(a&&!councilCast.Contains(a))councilCast.Add(a);
        foreach(var a in conservatives)if(a&&!councilCast.Contains(a))councilCast.Add(a);
        foreach(var a in councilCast)
        {
            var pose=a.GetComponent<Chapter2CouncilDrama>();
            if(!pose)pose=a.gameObject.AddComponent<Chapter2CouncilDrama>();
            pose.hasKnife=a!=mona&&a!=watan;
        }
        CouncilRally=false;CouncilStandingTogether=false;CouncilLineIndex=0;RefusalApproachComplete=false;
        // One supplied line remains one visual line, including the longest refusal sentence.
        ui.subtitle.resizeTextForBestFit=true;ui.subtitle.resizeTextMinSize=22;ui.subtitle.resizeTextMaxSize=30;
        ui.subtitle.horizontalOverflow=HorizontalWrapMode.Overflow;
        ui.subtitle.alignByGeometry=true;
    }

    Chapter2CouncilDrama Pose(Chapter2Actor actor,Chapter2CouncilDrama.Beat beat)
    {
        var pose=actor.GetComponent<Chapter2CouncilDrama>();pose.Play(beat);return pose;
    }

    IEnumerator CouncilLines(Chapter2Actor actor,string speaker,Chapter2CouncilDrama.Beat beat,string[] lines,bool frame=true)
    {
        ui.Line("","");
        if(frame&&actor)yield return FrameCouncilSpeaker(actor);
        CouncilCue=beat.ToString();
        if(actor){Pose(actor,beat);actor.speaking=true;actor.Rig.conversationTarget=null;}
        foreach(string line in lines)
        {
            CouncilLineIndex++;ui.Line(speaker,line);
            // Short exclamations still get a readable hold; long lines get more time.
            yield return new WaitForSeconds(Mathf.Clamp(1.45f+line.Length*.14f,2.15f,6.8f));
        }
        if(actor)actor.speaking=false;
        ui.Line("","");yield return new WaitForSeconds(.25f);
    }

    IEnumerator CouncilScript()
    {
        yield return CouncilLines(tado,tado.name,Chapter2CouncilDrama.Beat.Hilt,new[]{
            "父親！","日本人逼我們砍巨木、扣工資，","連敬酒都要被打！","我們還要忍到什麼時候？"});
        yield return CouncilLines(watan,watan.name,Chapter2CouncilDrama.Beat.Concern,new[]{
            "達多，冷靜！","日本人的槍砲比樹葉還多。","動了手，","部落的婦女和孩子怎麼辦？","莫那，這是一條死路啊！"});
        yield return CouncilLines(mona,mona.name,Chapter2CouncilDrama.Beat.Fire,new[]{
            "死路？","瓦旦，看看我們的年輕人，","手是用來握獵刀的，","現在卻在幫日人扛木材。","丟了 Gaya（祖靈的教誨），","我們活著和野獸有什麼兩樣？"});
        yield return CouncilLines(bawan,bawan.name,Chapter2CouncilDrama.Beat.Resolve,new[]{
            "頭目！","我寧可做自由的鬼，","也不做屈辱的人！"});
        yield return CouncilLines(watan,watan.name,Chapter2CouncilDrama.Beat.EyesClosed,new[]{"我們打不贏日本帝國的……"});
        // Frame before rising so the full movement is visible, then follow the standing head.
        yield return FrameCouncilSpeaker(mona);
        CouncilCue="MonaRise";Pose(mona,Chapter2CouncilDrama.Beat.Declare);mona.StandFromSeat(0,1.1f);
        yield return CouncilRiseShot(mona,1.3f);
        yield return CouncilLines(mona,mona.name,Chapter2CouncilDrama.Beat.Declare,new[]{
            "明天的運動會，","日本官吏都在公學校。","那是祖靈給的唯一機會！","我莫那·魯道已經決定起義，","用鮮血洗刷屈辱！","賽德克巴萊的子孫們，","你們，願不願意跟隨我？"},false);
    }

    IEnumerator CouncilRiseShot(Chapter2Actor actor,float seconds)
    {
        Vector3 inward=Vector3.ProjectOnPlane(campfire.position-actor.transform.position,Vector3.up).normalized;
        Vector3 end=actor.transform.position+inward*3.2f+Vector3.up*1.6f;
        yield return CouncilCameraShot(end,actor.transform.position+Vector3.up*.95f,54,seconds);
        CouncilSpeaker=actor;CameraBeat="council-standing-speaker";
    }

    IEnumerator CouncilAgreement()
    {
        CouncilRally=true;ui.objective.text="跟隨起義";
        yield return FrameCouncilSpeaker(tado);
        tado.StandFromSeat(0,1.05f);CouncilCue="TadoRise";
        yield return CouncilRiseShot(tado,1.3f);
        Pose(tado,Chapter2CouncilDrama.Beat.Stab);CouncilCue="Stab";
        yield return new WaitForSeconds(2.2f);
        yield return CouncilLines(tado,tado.name,Chapter2CouncilDrama.Beat.Planted,new[]{"我跟隨您！","為了祖靈，血祭彩虹！"},false);
        yield return FrameCouncilOverview(1.1f);
        CouncilStandingTogether=true;CouncilCue="WarriorsRise";
        int i=0;
        foreach(var actor in councilCast)
        {
            if(actor==mona||actor==tado)continue;
            actor.Face(campfire.position);actor.StandFromSeat(i*.12f,1.1f);
            Pose(actor,actor==watan?Chapter2CouncilDrama.Beat.Declare:Chapter2CouncilDrama.Beat.Draw);i++;
        }
        yield return new WaitForSeconds(2.3f);
        foreach(var actor in councilCast)if(actor!=mona&&actor!=tado){actor.speaking=true;if(actor!=watan)Pose(actor,Chapter2CouncilDrama.Beat.Pledge);}
        yield return CouncilLines(null,"巴萬與眾戰士",Chapter2CouncilDrama.Beat.Pledge,new[]{"我們跟隨您！","為了祖靈！","血祭彩虹！"},false);
        foreach(var actor in councilCast)actor.speaking=false;
        yield return CouncilLines(mona,mona.name,Chapter2CouncilDrama.Beat.Nod,new[]{
            "好！","今夜回去對妻兒好一點，","別露破綻。","天亮之後……我們彩虹橋上見！"});
    }

    IEnumerator CouncilRefusal()
    {
        ui.objective.text="走向莫那·魯道";CouncilCue="PlayerApproach";
        Pose(mona,Chapter2CouncilDrama.Beat.Rest);
        yield return WalkToMona();
        RefusalApproachComplete=true;ui.objective.text="面對莫那·魯道";
        yield return CouncilLines(null,"玩家",Chapter2CouncilDrama.Beat.Rest,new[]{"莫那頭目，這根本是自殺！我想活下去……"},false);
        // Keep the player's eye-level view throughout the confrontation.
        CouncilSpeaker=mona;mona.Face(player.view.transform.position);
        yield return CouncilLines(mona,mona.name,Chapter2CouncilDrama.Beat.Cold,new[]{
            "活下去？","那就去向日本人跪下！","去幫他們扛木頭、","聽他們叫你生番！"},false);
        mona.GetComponent<Chapter2CouncilDrama>().pointAt=campfire.position+new Vector3(-7,1.3f,-1);
        yield return CouncilLines(mona,mona.name,Chapter2CouncilDrama.Beat.Expel,new[]{
            "滾！","彩虹橋上沒有你的位置，","你不配稱為 Sediq Bale（真的人）！"},false);
    }

    IEnumerator WalkToMona()
    {
        Vector3 target=mona.transform.position+Vector3.up*1.45f;
        // Start at the player's place in the circle, then walk outside the fire ring.
        Vector3[] route={campfire.position+new Vector3(0,.05f,-5.8f),campfire.position+new Vector3(4.7f,.05f,-3.8f),
            campfire.position+new Vector3(4.5f,.05f,1.5f),mona.transform.position+new Vector3(1.35f,.05f,-1.85f)};
        if(player.IsVR)
        {
            // Follow the same route with the origin; leave tracked head motion untouched.
            player.Warp(route[0],target);
        }
        else yield return CouncilCameraShot(route[0]+Vector3.up*1.65f,target,55,.8f);
        player.Warp(route[0],target);
        CameraBeat="council-player-walk";
        for(int i=1;i<route.Length;i++)
        {
            Vector3 start=route[i-1];float seconds=Vector3.Distance(start,route[i])/1.65f;
            for(float t=0;t<1;t+=Time.deltaTime/seconds)
            {
                player.Warp(Vector3.Lerp(start,route[i],Mathf.SmoothStep(0,1,t)),target);
                mona.Face(player.view.transform.position);
                if(!player.IsVR)
                {
                    player.view.transform.position+=Vector3.up*(Mathf.Sin(Time.time*9)*.022f*Mathf.Sin(t*Mathf.PI));
                    player.FocusOn(target);
                }
                yield return null;
            }
        }
        player.Warp(route[route.Length-1],target);player.FocusOn(target);
        mona.Face(player.view.transform.position);
        CameraBeat="council-player-confrontation";
    }
}
