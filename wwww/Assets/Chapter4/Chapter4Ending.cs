using UnityEngine;

// Personal fiction and shared historical aftermath are separate cards. Saving
// happens only after all cards have run; a failed flush leaves the prior save.
[DefaultExecutionOrder(70)]
public sealed class Chapter4Ending : MonoBehaviour
{
    public enum Stage { Waiting, Personal, History1930, History1931, SaveFailed, Complete, LeavingTrail, FadingOut }
    public Chapter4SceneController chapter;
    public Stage CurrentStage { get; private set; }
    public bool HasStarted => CurrentStage != Stage.Waiting;
    public float CardElapsed { get; private set; }
    public bool Saved { get; private set; }
    public Chapter4ResultStore.Outcome Outcome { get; private set; }
    public const string History1930 = "十月二十七日，霧社一帶的六個賽德克部落起事，反抗日本統治。族人退進山裡，日本軍警封住山路。飛機在山谷上空投下炸彈，日軍也用山砲和毒氣攻擊躲進岩窟的族人。\n\n那一年，帶領族人起事的莫那・魯道在山中自盡。活下來的人陸續被送進收容所，在那裡過了冬。";
    public const string History1931 = "隔年四月二十五日清晨，在日本警察的默許下，道澤群的人襲擊了收容所。許多熬過山中戰事的人，沒能活過那一天。這就是後來所說的「第二次霧社事件」。\n\n五月六日，日本當局強迫倖存的族人離開霧社，遷往北港溪畔的川中島，不准他們再回故鄉。至今，那裡叫作清流部落，當年倖存者的子孫仍生活在那裡。";
    const string BothAlive = "露比扶著古慕走進馬赫坡岩窟。比胡已經坐下來了，聽見腳步，又朝洞口望了一眼。\n\n是歐冰。她在岔路上聽見你的呼喊，一路跟到了這裡。";
    const string RememberBihu = "露比扶著古慕走進馬赫坡岩窟。過了一會兒，洞口又響起腳步聲。歐冰循著你的呼喊，終於追上了大家。\n\n比胡雖然在戰爭中犧牲了，但我們仍會記得他。";
    const string RememberObing = "到了馬赫坡岩窟，露比才扶著古慕坐下。比胡也活著走到了這裡。大家望向來時的山路，始終沒能等到歐冰。\n\n歐冰雖然在戰爭中犧牲了，但我們仍會記得她。";
    const string RememberBoth = "露比攙著古慕，終於走進馬赫坡岩窟。等兩人坐穩，身旁還空著兩個位置。\n\n歐冰和比胡雖然都在戰爭中犧牲了，但我們仍會記得他們。";
    const string ObingAliveBihuUnknown = "露比扶著古慕走進馬赫坡岩窟。歐冰聽見你的呼喊，也沿著山路趕了上來。\n\n至於比胡，那一天的下落，我們還不知道。";
    const string ObingDeadBihuUnknown = "到了馬赫坡岩窟，露比扶著古慕坐下。比胡的下落，我們還不知道。歐冰也沒能從那條山路上走回來。\n\n歐冰雖然在戰爭中犧牲了，但我們仍會記得她。";

    public static string PersonalBody(bool obingAlive, Chapter4Evacuation.BihuState bihu)
    {
        switch (bihu)
        {
            case Chapter4Evacuation.BihuState.Rescued: return obingAlive ? BothAlive : RememberObing;
            case Chapter4Evacuation.BihuState.Dead: return obingAlive ? RememberBihu : RememberBoth;
            default: return obingAlive ? ObingAliveBihuUnknown : ObingDeadBihuUnknown;
        }
    }
    Chapter4ResultStore.Record pending;

    public bool TryBegin()
    {
        var flow = chapter.evacuation; var contact = chapter.contact; var gas = chapter.gas;
        if (HasStarted || !flow.ArrivalReady || !gas.Gumu.AtDestination || !gas.Lubi.AtDestination ||
            contact.Responded && !contact.Reunited || !contact.Responded && contact.OpportunityMask != 3) return false;
        Outcome = contact.Reunited ? Chapter4ResultStore.Outcome.Together : Chapter4ResultStore.Outcome.Memory;
        pending = new Chapter4ResultStore.Record { inheritedBihu = flow.InheritedBihu, outcome = Outcome,
            firstContact = contact.FirstContact, secondContact = contact.SecondContact, obingReunited = contact.Reunited,
            gumuArrived = true, lubiArrived = true, epilogueSeen = false,
            departed = flow.DepartedCount, sheltered = flow.InsideCount, fallen = flow.FallenCount, fallenNames = flow.FallenNames };
        chapter.player.SetInputEnabled(false);
        chapter.presentation.HideSpeech();
        chapter.presentation.SetContactStatus("", false);
        chapter.presentation.SetGasVeil(0,0);
        CurrentStage = Stage.LeavingTrail;
        CardElapsed = 0;
        return true;
    }

    void Update()
    {
        if (!HasStarted || chapter.IsPaused || CurrentStage == Stage.SaveFailed || CurrentStage == Stage.Complete) return;
        float step = Time.unscaledDeltaTime;
        if (CurrentStage == Stage.LeavingTrail || CurrentStage == Stage.FadingOut)
        {
            CardElapsed += step;
            bool lastPage = CurrentStage == Stage.FadingOut;
            float duration = lastPage ? 1.65f : 1.1f;
            chapter.presentation.SetFade(Mathf.SmoothStep(0,1,CardElapsed/duration));
            // Give the fully dark frame time to reach the display before changing scenes.
            if (CardElapsed < duration + .20f) return;
            if (lastPage) { pending.epilogueSeen = true; RetrySave(); }
            else Show(Stage.Personal);
            return;
        }
        CardElapsed += Time.deltaTime;
        if (CurrentStage == Stage.Personal && chapter.presentation.FadeAmount > 0)
            chapter.presentation.SetFade(1-Mathf.SmoothStep(0,1,CardElapsed/.70f));
        if (chapter.Voice.Busy) return;
        if (CurrentStage == Stage.Personal) Show(Stage.History1930);
        else if (CurrentStage == Stage.History1930) Show(Stage.History1931);
        else
        {
            CurrentStage = Stage.FadingOut;
            CardElapsed = 0;
        }
    }

    void Show(Stage stage)
    {
        chapter.Voice.Stop();
        CurrentStage = stage; CardElapsed = 0;
        if (stage == Stage.Personal)
            chapter.presentation.ShowEnding(chapter,
                pending.obingReunited && pending.inheritedBihu != Chapter4Evacuation.BihuState.Dead ? "一起走到了岩窟" : "我們仍會記得",
                PersonalBody(pending.obingReunited, pending.inheritedBihu),
                $"同行 {pending.departed} 人，走到洞後林徑 {pending.sheltered} 人。" +
                (pending.fallen > 0 ? $"　途中受傷倒下的同伴：{pending.fallenNames}。" : "") +
                "\n人物命運依本作劇情而定。接下來兩頁記述霧社事件的史實。");
        else chapter.presentation.ShowEnding(chapter,
            stage == Stage.History1930 ? "1930 年 · 戰火追進了山裡" : "1931 年 · 從霧社到川中島",
            stage == Stage.History1930 ? History1930 : History1931);
        chapter.Voice.Enqueue("narrator", chapter.presentation.endingBody.text, 0, true);
    }

    public void RetrySave()
    {
        if (pending == null || !pending.epilogueSeen || Saved) return;
        if (!Chapter4ResultStore.SaveCompleted(pending))
        {
            chapter.presentation.SetFade(0);
            CurrentStage = Stage.SaveFailed;
            chapter.ShowEndingSaveFailure();
            return;
        }
        Saved = true; CurrentStage = Stage.Complete;
        chapter.ReturnToChapterMenu();
    }
}
