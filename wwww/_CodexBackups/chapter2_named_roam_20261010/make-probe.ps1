$ErrorActionPreference='Stop'
$taskText=Get-Content '_CodexBackups/chapter2_single_escort_20261010/verification-tools/Chapter2EscortProbe.cs' -Raw -Encoding UTF8
$taskText=$taskText.Replace('Chapter2EscortProbe','Chapter2RoamPlaybackProbe').Replace('chapter2_single_escort_20261010','chapter2_named_roam_20261010').Replace('SingleEscortProbe','NamedRoamProbe').Replace('command.txt','play-command.txt')
$taskText=$taskText.Replace('static Vector3[] followPath;static float repath;','static Vector3[] followPath;static float repath;static bool replyShot,exchangeShot;')
$taskText=$taskText.Replace('public string label,stage,answer,error;','public string label,stage,answer,error;public string officerName,guideName,subtitleName;public bool allNamed,childPersona,officerChat,officerReply,exchangeVisible,facesOfficer;public float guideRoamed,approachSeconds,approachLength,spawnDistance,stepForward;')
$taskText=$taskText.Replace('skipped=positioned=asked=frontShot=explored=bothShot=false;','skipped=positioned=asked=frontShot=explored=bothShot=replyShot=exchangeShot=false;')
$taskText=$taskText.Replace('c.explorationSeconds=15','c.explorationSeconds=25')
$taskText=$taskText.Replace('report.participants=ex.ParticipantCount;',@'
report.participants=ex.ParticipantCount;
                report.officerName=c.officer.DisplayName;report.guideName=c.workers[0].DisplayName;
                report.allNamed=c.dayGroup.GetComponentsInChildren<Chapter2Actor>().All(a=>!string.IsNullOrWhiteSpace(a.characterName)&&a.name==a.characterName);
                var child=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>().First(n=>n.GetComponent<Chapter2Actor>().isChild);
                report.childPersona=c.GetComponent<Chapter2LocalDialogue>().Persona(child).Contains("小孩")&&Chapter2Exploration.DisplayName(child)=="都比·阿威";
'@)
$taskText=$taskText.Replace('if(label=="protect-full")'+"`r`n"+'                {','if(label=="protect-full" || label.Contains("east"))'+"`r`n"+'                {')
$taskText=$taskText.Replace('var npc=c.dayGroup.GetComponentsInChildren<Chapter2AmbientNPC>().First(n=>n.name.Contains("小孩"));','var npc=c.officer.GetComponent<Chapter2AmbientNPC>();')
$taskText=$taskText.Replace('ex.TryOpen(npc);'+"`r`n"+'                }','report.officerChat=ex.TryOpen(npc);if(label.Contains("east"))ex.SubmitQuestion("你叫什麼名字？你在這裡做什麼？");'+"`r`n"+'                }')
$taskText=$taskText.Replace('report.hintHidden&=string.IsNullOrEmpty(c.ui.hint.text);',@'
report.hintHidden&=string.IsNullOrEmpty(c.ui.hint.text);
            report.guideRoamed=Mathf.Max(report.guideRoamed,c.workers[0].GetComponent<Chapter2AmbientNPC>().MaxDistanceFromHome);
            if(ex.LastReply!=null&&!replyShot)
            {replyShot=true;report.officerReply=ex.LastReply.Contains(c.officer.DisplayName);Shot("officer-reply");}
'@)
$taskText=$taskText.Replace('report.noArrowsWhileApproaching&=c.routeGuide.VisibleArrows==0;','report.noArrowsWhileApproaching&=c.routeGuide.VisibleArrows==0;report.spawnDistance=Vector3.ProjectOnPlane(escort.ApproachStart-c.player.transform.position,Vector3.up).magnitude;')
$taskText=$taskText.Replace('frontShot=true;var guide=c.workers[0];', 'frontShot=true;report.approachSeconds=escort.ApproachSeconds;report.approachLength=escort.ApproachPathLength;var guide=c.workers[0];')
$taskText=$taskText.Replace('if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice)',@'
if(c.CameraBeat=="villager-speaking"&&c.ui.subtitle.text.Contains("守護者")&&!exchangeShot)
        {
            exchangeShot=true;var g=c.workers[0];
            var gp=c.player.view.WorldToViewportPoint(g.Rig.Head.position);var op=c.player.view.WorldToViewportPoint(c.officer.Rig.Head.position);
            report.exchangeVisible=gp.z>0&&op.z>0&&gp.x>.05f&&gp.x<.95f&&op.x>.05f&&op.x<.95f;
            report.facesOfficer=Vector3.Angle(g.Rig.Forward,Vector3.ProjectOnPlane(c.officer.transform.position-g.transform.position,Vector3.up))<8;
            report.stepForward=Vector3.ProjectOnPlane(g.transform.position-c.WorkerDestination(0),Vector3.up).magnitude;
            report.subtitleName=c.ui.speaker.text;Shot("tree-exchange");
        }
        if(c.CurrentStage==Chapter2Controller.Stage.TreeChoice)
'@)
[IO.File]::WriteAllText((Join-Path (Get-Location) 'Assets/Editor/Chapter2RoamPlaybackProbe.cs'),$taskText,[Text.UTF8Encoding]::new($false))
