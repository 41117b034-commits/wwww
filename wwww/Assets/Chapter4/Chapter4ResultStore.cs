using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

// A separate, versioned Chapter 4 result. Previous chapter keys are read-only.
public static class Chapter4ResultStore
{
    public const string SaveKey = "WusheEvent.Chapter4.Result";
    public const int CurrentVersion = 2;
    public enum Outcome { Together, Memory }
    [Serializable] public sealed class Record
    {
        public int version = CurrentVersion;
        public bool completed;
        public Chapter4Evacuation.BihuState inheritedBihu;
        public Outcome outcome;
        public bool firstContact, secondContact, obingReunited;
        public bool gumuArrived, lubiArrived, epilogueSeen;
        public int departed, sheltered, fallen;
        public string fallenNames = string.Empty;
    }

    public static bool IsValid(Record r, bool completed = false)
    {
        if (r == null || (r.version != CurrentVersion && r.version != 1) || completed && !r.completed ||
            !Enum.IsDefined(typeof(Chapter4Evacuation.BihuState), r.inheritedBihu) ||
            !r.gumuArrived || !r.lubiArrived || !r.epilogueSeen) return false;
        if (r.version >= 2 && (r.departed < 3 || r.sheltered < 3 || r.fallen < 0 ||
            r.sheltered + r.fallen + (r.obingReunited ? 0 : 1) != r.departed ||
            (string.IsNullOrEmpty(r.fallenNames) ? 0 : r.fallenNames.Split('、').Length) != r.fallen)) return false;
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
            if (!IsValid(record, true)) return null;
            var expected = JObject.Parse(JsonUtility.ToJson(record));
            if (record.version == 1)
            {
                expected.Remove(nameof(Record.departed)); expected.Remove(nameof(Record.sheltered));
                expected.Remove(nameof(Record.fallen)); expected.Remove(nameof(Record.fallenNames));
            }
            return JToken.DeepEquals(payload, expected) ? record : null;
        }
        catch (ArgumentException) { return null; }
        catch (JsonException) { return null; }
    }
}
