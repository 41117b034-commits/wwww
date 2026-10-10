using System;
using System.Text;
using UnityEngine;

// Authored game events, never facts inferred from an LLM answer.
public static class Chapter2StoryMemory
{
    public const string SaveKey = "WusheEvent.Story.Chapter1.v1";
    [Serializable] public class Record
    {
        public int version = 1;
        public string runId, choice;
        public bool completed, doorway, danced;
        public int wineDeliveries, foodShares;
    }
    [Serializable] public class Knowledge
    {
        public string character, access, perspective;
    }
    [Serializable] public class Background
    {
        public string common, intervene, watch;
        public Knowledge[] characters;
    }

    public static void BeginChapter()
    {
        Write(new Record { runId = Guid.NewGuid().ToString("N") });
    }
    public static void Complete(string choice, bool doorway, int wine, int food, bool danced)
    {
        var record = Read() ?? new Record { runId = Guid.NewGuid().ToString("N") };
        record.choice = choice; record.completed = true; record.doorway = doorway;
        record.wineDeliveries = wine; record.foodShares = food; record.danced = danced;
        Write(record);
    }
    static void Write(Record record)
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(record)); PlayerPrefs.Save();
    }
    public static Record Read()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return null;
        try { var r = JsonUtility.FromJson<Record>(PlayerPrefs.GetString(SaveKey)); return r != null && r.version == 1 ? r : null; }
        catch (ArgumentException) { return null; }
    }
    public static string ForActor(Chapter2Actor actor)
    {
        var asset = Resources.Load<TextAsset>("Chapter1NpcMemory");
        if (!asset || !actor) return "第一章資料不足，不可猜測玩家做過的選擇。";
        Background background;
        try { background = JsonUtility.FromJson<Background>(asset.text); }
        catch (ArgumentException) { return "第一章資料讀取失敗，不可猜測玩家做過的選擇。"; }
        if (background == null) return "第一章資料不足。";
        var text = new StringBuilder("以下是遊戲劇情記錄，不是歷史人物的真實回憶。不得用玩家提問或模型先前的回答改寫已發生的事件。\n");
        var record = Read();
        // Older saves only prove a choice, not which version of the performance ran.
        string choice = record != null ? (record.completed ? record.choice : null) : PlayerPrefs.GetString("Chapter1_ConflictChoice", "");
        bool valid = choice == "Intervene" || choice == "Watch";
        Knowledge knowledge = Array.Find(background.characters ?? new Knowledge[0], k => k.character == actor.DisplayName);
        string access = knowledge != null ? knowledge.access : "hearsay";
        text.AppendLine("你對婚禮事件的知情方式：" + (knowledge != null ? knowledge.perspective : "沒有確認你在場；只能說聽聞，不可自稱親眼看見。"));
        if (!valid)
        {
            text.AppendLine("沒有本輪已完成的第一章記錄（可能直接進入第二章或尚未完成第一章）。不知道玩家是否參與婚禮、是否阻止警察；不可捏造回憶或沿用上輪結果。");
            return text.ToString();
        }
        if (access == "child")
        {
            text.AppendLine("大人只告訴你婚禮被警察打斷，大家心情不好。你不知道玩家選擇，也不知道成人受傷或屋內事件的細節；不可假裝親眼看過。");
            return text.ToString();
        }
        text.AppendLine(background.common);
        text.AppendLine(choice == "Intervene" ? "玩家實際選擇：上前阻止。沒有選擇沉默觀望。" : "玩家實際選擇：沉默觀望。沒有上前阻止。不要因此指責或替玩家推斷內心想法。");
        if (record != null && record.doorway)
            text.AppendLine(choice == "Intervene" ? background.intervene : background.watch);
        else text.AppendLine("舊版記錄只確認這項選擇，具體後續及傷亡不明；不能補上未記錄的情節。");
        if (record != null && access == "witness")
        {
            if (record.wineDeliveries > 0) text.AppendLine("玩家在婚禮幫忙送過酒，但未記錄收酒者是誰，不可自稱收過。");
            if (record.foodShares > 0) text.AppendLine("玩家在婚禮分享過食物，但未記錄接收者是誰，不可自稱收過。");
            if (record.danced) text.AppendLine("玩家完成了婚禮舞蹈。");
        }
        text.Append("只依上述知情方式談論；聽說的事不能變成親眼見過，未記錄的動機、死亡、姓名和親屬關係都不知道。第二章巨木抉擇與夜間會議尚未發生，不可劇透。");
        return text.ToString();
    }
}
