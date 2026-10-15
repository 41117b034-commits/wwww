using System.Collections;
using UnityEngine;

// Each crossing has its own visible threat, command and safe arrival.
public sealed partial class Chapter3SchoolBattle
{
    void RevealCurrentThreat()
    {
        if(Step>=Enemies.Count||!Enemies[Step]||threatIntroduced[Step])return;
        int step=Step;var enemy=Enemies[step];threatIntroduced[step]=true;
        enemy.transform.position=threatPositions[step]+Vector3.right*2;
        enemy.AttackEnabled=false;enemy.gameObject.SetActive(true);ThreatRevealing=true;
        threatMotor.Reset();observedEnemyShots=0;dangerWasVisible=false;dangerPoseUntil=0;
        StartCoroutine(RevealThreat(enemy,step));
    }
    IEnumerator RevealThreat(Chapter3Enemy enemy,int step)
    {
        if(step>0)
        {
            Notice(school.pihu.DisplayName,"這一段到了，先在掩體後等！前面還有警察。",3);
            yield return new WaitForSeconds(2.5f);
        }
        if(!Fighting||Step!=step)yield break;
        Notice("警察","那邊有人！",2.4f);
        while(Fighting&&Step==step&&enemy&&!enemy.Defeated&&Flat(enemy.transform.position,threatPositions[step])>.12f)
        {threatMotor.Move(enemy.actor,threatPositions[step],1.7f);yield return null;}
        if(!Fighting||Step!=step)yield break;
        ThreatRevealing=false;
        if(enemy&&!enemy.Defeated)
        {
            enemy.AttackEnabled=true;
            enemy.HoldFire(school.flow.CarriedWeapon==Chapter3Weapons.Weapon.Knife?5:3.5f);
            Notice(school.pihu.DisplayName,"警察出來了！先看清他的動作，等你的信號。",3.5f);
        }
    }
    void UpdateCover()
    {
        if(!AtCrossing)
        {
            MoveRoute(school.pihu,guideMotor,activeRoute,ref guideIndex,true);
            MoveRoute(school.tado,escortMotor,activeRoute,ref escortIndex,true,new Vector3(-.85f,0,-.9f));
            if(guideIndex>=activeRoute.Length&&escortIndex>=activeRoute.Length)
            {
                AtCrossing=true;safePihu=school.pihu.transform.position;safeTado=school.tado.transform.position;
                guideIndex=escortIndex=0;activeRoute=east[Step];RevealCurrentThreat();
            }
        }
        else
        {
            if(player.ActionPressed&&Flat(player.transform.position,Ammo.position)>=2)CommandEscort();
            if(Returning)
            {
                guideMotor.Move(school.pihu,safePihu,3.8f);escortMotor.Move(school.tado,safeTado,3.8f);
                if(Flat(school.pihu.transform.position,safePihu)<.2f&&Flat(school.tado.transform.position,safeTado)<.2f)
                {Returning=false;Notice(school.pihu.DisplayName,"退回來了，等你重新下令。",2.5f);}
            }
            else if(Advancing)
            {
                MoveRoute(school.pihu,guideMotor,activeRoute,ref guideIndex,false);
                MoveRoute(school.tado,escortMotor,activeRoute,ref escortIndex,false,new Vector3(-.85f,0,-.9f));
                if(guideIndex>=activeRoute.Length&&escortIndex>=activeRoute.Length)
                {
                    Advancing=false;CrossingArrived=true;pihuPose.ReportSafe();tadoPose.ReportSafe();
                    Notice(school.pihu.DisplayName,CurrentThreatCleared?"我們都到了，這一段安全了！":"我們到掩體了！警察還在，先處理他再走！",3);
                }
            }
            if(CrossingArrived&&CurrentThreatCleared){CompleteMissionStep();if(!Fighting)return;}
        }
        foreach(var e in Enemies)if(e)
        {
            e.escortTarget=null;
            if(!AtCrossing||(!Advancing&&!Returning))continue;
            // Aim at the exposed member of the pair. The leading companion can
            // physically shield Tado; do not shoot through him or become idle.
            e.escortTarget=school.tado.transform;
            if(!combat.ClearLine(e.AimPoint,school.tado.transform.position+Vector3.up*1.05f,e))
                e.escortTarget=school.pihu.transform;
        }
        UpdateCoverReactions();
        ui.objective.text="跟比胡掩護 · 第 "+(Step+1)+" / 2 段";
        string order=!AtCrossing?"先跟比胡到東側掩體":ThreatRevealing?"警察正在出現 · 留意前方":Returning?"隊伍正在撤回":Advancing?"隊伍正在穿越 · E 撤回":
            CrossingArrived?"兩人已抵達 · 清除這一段警察":"隊伍在掩體後等待信號 · E 前進";
        var guard=Step<Enemies.Count?Enemies[Step]:null;
        string window=guard&&guard.gameObject.activeInHierarchy&&!guard.Defeated&&!ThreatRevealing?
            guard.Telegraphing?"警察正在抬槍！":guard.SafeWindowSeconds>.4f?"警察暫時停火 · 留意再次抬槍":"警察準備瞄準":"";
        combat.combatText.text=order+"\n隊伍受壓 "+EscortPressure+" / 3  ·  威脅 "+(CurrentThreatCleared?"已解除":"尚未解除")+(window.Length>0?"\n"+window:"");
        ui.hint.text=CompletionBlockedByThreat?"同伴已到，先處理警察 · "+combat.weapons.Controls:"E 指揮前進／撤回 · "+combat.weapons.Controls;
    }
    void UpdateCoverReactions()
    {
        var guard=Step<Enemies.Count?Enemies[Step]:null;
        bool danger=guard&&guard.gameObject.activeInHierarchy&&!guard.Defeated&&guard.Telegraphing;
        if(guard&&guard.Shots>observedEnemyShots)
        {
            observedEnemyShots=guard.Shots;dangerPoseUntil=Time.time+1.2f;
            if(Time.time>=nextCoverCall)
            {nextCoverCall=Time.time+3;Notice(school.pihu.DisplayName,"他開過槍了！看住前方，等你的信號！",2.5f);}
        }
        if(danger&&!dangerWasVisible&&Time.time>=nextCoverCall)
        {nextCoverCall=Time.time+3;Notice(school.pihu.DisplayName,Advancing?"警察抬槍了！掩護我們，必要時叫我們撤回！":"先壓低身體，他正在瞄準！",2.6f);}
        dangerWasVisible=danger;
        pihuPose.SetUnderFire(danger||Time.time<dangerPoseUntil);tadoPose.SetUnderFire(danger||Time.time<dangerPoseUntil);
    }
    public bool CommandEscort()
    {
        if(!Fighting||Selected!=Mission.Pihu||!AtCrossing||Returning||ThreatRevealing||CrossingArrived)return false;
        if(Flat(player.transform.position,school.pihu.transform.position)>18)
        {Notice(school.pihu.DisplayName,"離得太遠了，回到能照應我們的位置！",2.5f);return false;}
        if(Advancing)
        {
            Advancing=false;Returning=true;guideIndex=escortIndex=0;guideMotor.Reset();escortMotor.Reset();
            pihuPose.SignalForward(1.3f);Notice(school.pihu.DisplayName,"達多，退回剛才的掩體！",2.5f);
        }
        else
        {
            Advancing=true;pihuPose.SignalForward(1.6f);tadoPose.ReportSafe(1.1f);
            Notice(school.pihu.DisplayName,"收到！達多，跟著我走！",2.5f);
        }
        return true;
    }
    public void EscortHit()
    {
        if(!Fighting||Selected!=Mission.Pihu||(!Advancing&&!Returning))return;
        EscortPressure++;dangerPoseUntil=Time.time+1.6f;Notice(school.pihu.DisplayName,"火力太強！掩護我們，先撤回去！",2.5f);
        if(EscortPressure>=3)Fail("隊伍被火力壓制",CheckpointStep>0?"從第二段掩體重整。\n第一段已完成，任務與武器選擇保留。":"從第一段出發點重整。\n先觀察警察，再下令前進。任務與武器選擇保留。");
    }
}
