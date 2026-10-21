using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// The chapter-ending controller owns the report and Mona's closing dialogue.
// Only that controller should call SaveCompleted, after the closing dialogue.
public static class Chapter3ResultStore
{
    public const string SaveKey="WusheEvent.Chapter3.Result";
    public const int CurrentVersion=2;
    public enum KnowledgeSource { None, Direct, Reported }

    [Serializable]
    public sealed class ActorKnowledge
    {
        public string actorId;
        public string schoolSide;
        public bool originalSquad;
        public bool alive;
        public KnowledgeSource westSchool;
        public KnowledgeSource eastSchool;
        public KnowledgeSource centralSchool;
        public KnowledgeSource injury;
        public KnowledgeSource outcome;
    }

    [Serializable]
    public sealed class Record
    {
        public int version=CurrentVersion;
        public bool completed;
        public Chapter3SchoolBattle.Mission mission;
        public Chapter3Rescue.Decision decision;
        public Chapter3Weapons.Weapon weapon;
        public bool pihuSurvived;
        // Includes the player, Bawan, Tado and (when rescued) Pihu. Excludes Mona.
        public int squadSurvivors;
        public ActorKnowledge[] actors;
    }

    // A proposed ending snapshot, not a save or a source of pre-report knowledge.
    // Mona's Reported facts may be used only after the reception report occurs.
    public static Record Build(Chapter3SchoolBattle.Mission mission,
        Chapter3Rescue.Decision decision,Chapter3Weapons.Weapon weapon)
    {
        return BuildVersion(CurrentVersion,mission,decision,weapon);
    }

    // Version 1 belongs to the earlier two-route story. Reading or copying it
    // must not move Tado into a mission that did not exist in that completed run.
    static Record BuildVersion(int version,Chapter3SchoolBattle.Mission mission,
        Chapter3Rescue.Decision decision,Chapter3Weapons.Weapon weapon)
    {
        if((version!=1&&version!=CurrentVersion)||!ValidChoices(mission,decision,weapon,version))return null;
        bool rescued=decision==Chapter3Rescue.Decision.Rescue;
        var outcome=rescued?KnowledgeSource.Direct:KnowledgeSource.Reported;
        return new Record
        {
            version=version,completed=false,mission=mission,decision=decision,weapon=weapon,
            pihuSurvived=rescued,squadSurvivors=rescued?4:3,
            actors=new[]
            {
                Companion("bawan","west",true,outcome,version),
                Companion("pihu","east",rescued,rescued?KnowledgeSource.Direct:KnowledgeSource.None,version),
                // On Leave, Tado returns to the trail and confirms the death.
                Companion("tado",version>=2?"central":mission==Chapter3SchoolBattle.Mission.Bawan?"east":"west",true,KnowledgeSource.Direct,version),
                new ActorKnowledge
                {
                    actorId="mona",schoolSide="none",originalSquad=false,alive=true,
                    westSchool=KnowledgeSource.Reported,eastSchool=KnowledgeSource.Reported,
                    centralSchool=version>=2?KnowledgeSource.Reported:KnowledgeSource.None,
                    injury=KnowledgeSource.Reported,outcome=KnowledgeSource.Reported
                }
            }
        };
    }

    static ActorKnowledge Companion(string id,string side,bool alive,KnowledgeSource outcome,int version)
    {
        return new ActorKnowledge
        {
            actorId=id,schoolSide=side,originalSquad=true,alive=alive,
            westSchool=side=="west"?KnowledgeSource.Direct:KnowledgeSource.Reported,
            eastSchool=side=="east"?KnowledgeSource.Direct:KnowledgeSource.Reported,
            centralSchool=version<2?KnowledgeSource.None:side=="central"?KnowledgeSource.Direct:KnowledgeSource.Reported,
            injury=KnowledgeSource.Direct,outcome=outcome
        };
    }

    static bool ValidChoices(Chapter3SchoolBattle.Mission mission,
        Chapter3Rescue.Decision decision,Chapter3Weapons.Weapon weapon,int version)
    {
        return (mission==Chapter3SchoolBattle.Mission.Bawan||mission==Chapter3SchoolBattle.Mission.Pihu||
            (version>=2&&mission==Chapter3SchoolBattle.Mission.Tado))&&
            (decision==Chapter3Rescue.Decision.Rescue||decision==Chapter3Rescue.Decision.Leave)&&
            (weapon==Chapter3Weapons.Weapon.Knife||weapon==Chapter3Weapons.Weapon.Rifle);
    }

    public static bool IsValid(Record record,bool requireCompleted=false)
    {
        if(record==null||(record.version!=1&&record.version!=CurrentVersion)||(requireCompleted&&!record.completed)||
            !ValidChoices(record.mission,record.decision,record.weapon,record.version)||
            record.actors==null||record.actors.Length!=4)return false;
        var expected=BuildVersion(record.version,record.mission,record.decision,record.weapon);
        if(record.pihuSurvived!=expected.pihuSurvived||record.squadSurvivors!=expected.squadSurvivors)return false;
        // Both versions preserve actor order and reject duplicate or altered facts.
        for(int i=0;i<expected.actors.Length;i++)
        {
            var actual=record.actors[i];var canonical=expected.actors[i];
            if(actual==null||actual.actorId!=canonical.actorId||actual.schoolSide!=canonical.schoolSide||
                actual.originalSquad!=canonical.originalSquad||actual.alive!=canonical.alive||
                actual.westSchool!=canonical.westSchool||actual.eastSchool!=canonical.eastSchool||
                actual.centralSchool!=canonical.centralSchool||
                actual.injury!=canonical.injury||actual.outcome!=canonical.outcome)return false;
        }
        return true;
    }

    public static Record Copy(Record record)
    {
        if(!IsValid(record))return null;
        var copy=BuildVersion(record.version,record.mission,record.decision,record.weapon);
        copy.completed=record.completed;return copy;
    }

    static JObject CanonicalPayload(Record record)
    {
        var payload=JObject.Parse(JsonUtility.ToJson(record));
        // Do not require a newly added field in real version-1 saves, or add it
        // when deliberately re-saving one. Unknown fields still fail validation.
        if(record.version==1)
            foreach(var actor in (JArray)payload["actors"])((JObject)actor).Remove("centralSchool");
        return payload;
    }

    public static bool SaveCompleted(Record record)
    {
        return SaveCompleted(record,PlayerPrefs.Save);
    }

    // Keep the commit boundary separate so a failed flush cannot become a later
    // accidental save when another chapter flushes the shared preferences.
    internal static bool SaveCompleted(Record record,Action persist)
    {
        // Invalid attempts do not touch the previous result. Build also deep-copies
        // every actor, so saving cannot mutate the pending record held by the caller.
        if(!IsValid(record)||persist==null)return false;
        var saved=Copy(record);
        saved.completed=true;
        bool hadPrevious=PlayerPrefs.HasKey(SaveKey);
        string previous=PlayerPrefs.GetString(SaveKey,"");
        try
        {
            PlayerPrefs.SetString(SaveKey,CanonicalPayload(saved).ToString(Formatting.None));
            persist();
            return true;
        }
        catch(PlayerPrefsException){ }
        catch(ArgumentException){ }
        if(hadPrevious)PlayerPrefs.SetString(SaveKey,previous);else PlayerPrefs.DeleteKey(SaveKey);
        return false;
    }

    public static Record Load()
    {
        if(!PlayerPrefs.HasKey(SaveKey))return null;
        string json=PlayerPrefs.GetString(SaveKey,"");
        if(string.IsNullOrWhiteSpace(json)||json.Length>8192)return null;
        try
        {
            var payload=JObject.Parse(json,new JsonLoadSettings
            {
                DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,
                CommentHandling=CommentHandling.Load
            });
            var record=JsonUtility.FromJson<Record>(json);
            if(!IsValid(record,true))return null;
            // JsonUtility silently defaults missing fields (notably Knife=0 and
            // false). Require the complete format emitted by our single writer.
            // Structural comparison accepts whitespace and property order changes,
            // while rejecting absent, additional or incorrectly typed fields.
            return JToken.DeepEquals(payload,CanonicalPayload(record))?record:null;
        }
        catch(ArgumentException){return null;}
        catch(JsonException){return null;}
    }

    // Deterministic authored facts only; never saves prompts or player free input.
    public static string KnowledgeSummary(Record record,string actorId)
    {
        if(!IsValid(record))return "";
        ActorKnowledge actor=null;
        foreach(var candidate in record.actors)if(candidate.actorId==actorId){actor=candidate;break;}
        if(actor==null)return "";
        string name=actorId=="bawan"?"巴萬":actorId=="pihu"?"比胡":actorId=="tado"?"達多":"莫那魯道";
        string origin=actor.originalSquad?"原小隊成員":"不屬原小隊，沒有在學校或受傷現場；在接應處聽取回報後才得知經過";
        string survival=actor.alive?"目前生還":"已死亡，不參與接應後的問答";
        string result=record.pihuSurvived?"比胡獲救生還":"比胡被留下並死亡";
        string boundary=record.decision==Chapter3Rescue.Decision.Leave&&actorId=="bawan"?
            "；親眼見過受傷與留下，由達多回報死亡，沒有目擊死亡瞬間":
            record.decision==Chapter3Rescue.Decision.Leave&&actorId=="tado"?
            "；回林路親眼確認死亡，再向接應處眾人回報":"";
        string central=record.version>=2?"），中央進入會場（"+Source(actor.centralSchool):"";
        return name+"："+origin+"；"+survival+"。學校左線（"+Source(actor.westSchool)+
            "），右線（"+Source(actor.eastSchool)+central+"）；比胡受傷（"+Source(actor.injury)+
            "）；"+result+"（"+Source(actor.outcome)+"）"+boundary+"。";
    }

    static string Source(KnowledgeSource source)
    {
        return source==KnowledgeSource.Direct?"親歷":source==KnowledgeSource.Reported?"轉述得知":"不知後續";
    }
}
