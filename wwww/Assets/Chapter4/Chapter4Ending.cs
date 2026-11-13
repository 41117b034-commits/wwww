using UnityEngine;

// Personal fiction and shared historical aftermath are separate cards. Saving
// happens only after all cards have run; a failed flush leaves the prior save.
[DefaultExecutionOrder(70)]
public sealed class Chapter4Ending : MonoBehaviour
{
    public enum Stage { Waiting, Personal, History1930, History1931, SaveFailed, Complete }
    public Chapter4SceneController chapter;
    public Stage CurrentStage { get; private set; }
    public bool HasStarted => CurrentStage != Stage.Waiting;
    public float CardElapsed { get; private set; }
    public bool Saved { get; private set; }
    public Chapter4ResultStore.Outcome Outcome { get; private set; }
    public const string History1930 = "霧社事件爆發後，日本當局調集軍警，\n使用飛機、山砲與毒氣等武器鎮壓。\n\n戰局轉趨不利，莫那・魯道於同年自盡。\n\n本作這段撤離的結束，並不代表戰爭已經結束。";
    public const string History1931 = "1931 年 4 月，第二次霧社事件發生，收容所中的倖存者再遭攻擊。\n\n其後，倖存族人被日本當局強制遷往川中島（今清流部落），離開原居地。\n\n川中島是後來的強制遷居地，並非 1930 年撤離途中可抵達的自由避難終點。";
    Chapter4ResultStore.Record pending;

    public bool TryBegin()
    {
        var flow = chapter.evacuation; var contact = chapter.contact; var gas = chapter.gas;
        if (HasStarted || !flow.ArrivalReady || !gas.Gumu.AtDestination || !gas.Lubi.AtDestination ||
            contact.Responded && !contact.Reunited || !contact.Responded && contact.OpportunityMask != 3) return false;
        Outcome = contact.Reunited ? Chapter4ResultStore.Outcome.Together : Chapter4ResultStore.Outcome.Memory;
        pending = new Chapter4ResultStore.Record { inheritedBihu = flow.InheritedBihu, outcome = Outcome,
            firstContact = contact.FirstContact, secondContact = contact.SecondContact, obingReunited = contact.Reunited,
            gumuArrived = true, lubiArrived = true, epilogueSeen = false };
        chapter.player.SetInputEnabled(false);
        chapter.presentation.HideSpeech();
        chapter.presentation.SetInteraction("", false);
        chapter.presentation.SetContactStatus("", false);
        chapter.presentation.SetGasStatus("", 0, 0);
        Show(Stage.Personal);
        return true;
    }

    void Update()
    {
        if (!HasStarted || chapter.IsPaused || CurrentStage == Stage.SaveFailed || CurrentStage == Stage.Complete) return;
        CardElapsed += Time.deltaTime;
        float duration = CurrentStage == Stage.Personal ? 12 : CurrentStage == Stage.History1930 ? 16 : 19;
        if (CardElapsed < duration) return;
        if (CurrentStage == Stage.Personal) Show(Stage.History1930);
        else if (CurrentStage == Stage.History1930) Show(Stage.History1931);
        else { pending.epilogueSeen = true; RetrySave(); }
    }

    void Show(Stage stage)
    {
        CurrentStage = stage; CardElapsed = 0;
        string bihu = pending.inheritedBihu == Chapter4Evacuation.BihuState.Rescued ? "比胡獲救的結果，承接自第三章。" :
            pending.inheritedBihu == Chapter4Evacuation.BihuState.Dead ? "比胡已於第三章死亡；這份記憶也隨你留下。" : "缺少有效的第三章紀錄，比胡的狀態仍未確認。";
        if (stage == Stage.Personal)
            chapter.presentation.ShowEnding("本作個人結局 · 1／3", Outcome == Chapter4ResultStore.Outcome.Together ? "與同伴一起活下來" : "帶著同伴的記憶活下來",
                (Outcome == Chapter4ResultStore.Outcome.Together ? "歐冰循著你告訴她的通路，實際走回隊伍。你們在集合地再次會合。" :
                    "兩處聯絡點都沒有傳出你的呼喊。主隊完成撤離，歐冰未歸。她的去向仍不明。") +
                "\n\n露比扶著古慕，兩人都已抵達。\n\n" + bihu,
                "人物互動與個人分支為本作劇情編排。接下來為共同史實後記。");
        else chapter.presentation.ShowEnding("共同史實後記 · " + (stage == Stage.History1930 ? "2／3" : "3／3"),
            stage == Stage.History1930 ? "1930 年 · 戰局與莫那之死" : "1931 年 · 再次受難與強制遷居",
            stage == Stage.History1930 ? History1930 : History1931,
            "史實參考：國立公共資訊圖書館「民國近代史」之第一次、第二次霧社事件。");
    }

    public void RetrySave()
    {
        if (pending == null || !pending.epilogueSeen || Saved) return;
        if (!Chapter4ResultStore.SaveCompleted(pending))
        {
            CurrentStage = Stage.SaveFailed;
            chapter.ShowEndingSaveFailure();
            return;
        }
        Saved = true; CurrentStage = Stage.Complete;
        chapter.ReturnToChapterMenu();
    }
}
