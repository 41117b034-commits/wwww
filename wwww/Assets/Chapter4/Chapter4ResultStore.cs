using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// A separate, versioned Chapter 4 result. Previous chapter keys are read-only.
public static class Chapter4ResultStore
{
    public const string SaveKey = "WusheEvent.Chapter4.Result";
    public const int CurrentVersion = 1;
    public enum Outcome { Together, Memory }
    [Serializable] public sealed class Record
    {
        public int version = CurrentVersion;
        public bool completed;
        public Chapter4Evacuation.BihuState inheritedBihu;
        public Outcome outcome;
        public bool firstContact, secondContact, obingReunited;
        public bool gumuArrived, lubiArrived, epilogueSeen;
    }

    public static bool IsValid(Record r, bool completed = false)
    {
        if (r == null || r.version != CurrentVersion || completed && !r.completed ||
            !Enum.IsDefined(typeof(Chapter4Evacuation.BihuState), r.inheritedBihu) ||
            !r.gumuArrived || !r.lubiArrived || !r.epilogueSeen) return false;
        bool responded = r.firstContact || r.secondContact;
        if (r.firstContact && r.secondContact) return false; // one successful call is enough
        return r.outcome == Outcome.Together ? responded && r.obingReunited :
            r.outcome == Outcome.Memory && !responded && !r.obingReunited;
    }

    public static bool SaveCompleted(Record record) => SaveCompleted(record, PlayerPrefs.Save);
    internal static bool SaveCompleted(Record record, Action persist)
    {
        if (!IsValid(record) || persist == null) return false;
        var copy = JsonUtility.FromJson<Record>(JsonUtility.ToJson(record));
        copy.completed = true;
        bool existed = PlayerPrefs.HasKey(SaveKey);
        string previous = PlayerPrefs.GetString(SaveKey, "");
        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(copy));
            persist();
            return true;
        }
        catch (PlayerPrefsException) { }
        catch (ArgumentException) { }
        if (existed) PlayerPrefs.SetString(SaveKey, previous); else PlayerPrefs.DeleteKey(SaveKey);
        return false;
    }

    public static Record Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return null;
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrWhiteSpace(json) || json.Length > 4096) return null;
        try
        {
            var payload = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
            var record = JsonUtility.FromJson<Record>(json);
            return IsValid(record, true) && JToken.DeepEquals(payload, JObject.Parse(JsonUtility.ToJson(record))) ? record : null;
        }
        catch (ArgumentException) { return null; }
        catch (JsonException) { return null; }
    }
}
