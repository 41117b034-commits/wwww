using UnityEngine;

// Two places to call the rear companion. An acknowledgement never becomes an
// unreturned ending because of a timer; reunion requires her real displacement.
[DefaultExecutionOrder(60)]
public sealed class Chapter4Contact : MonoBehaviour
{
    public enum State { Uncontacted, Rising, Following, Reunited }
    public Chapter4SceneController chapter;
    public State CurrentState { get; private set; }
    public bool FirstContact { get; private set; }
    public bool SecondContact { get; private set; }
    public int OpportunityMask { get; private set; }
    public bool Responded => FirstContact || SecondContact;
    public bool Reunited => CurrentState == State.Reunited;
    public int AvailableOpportunity
    {
        get
        {
            if (!initialized || Responded || chapter.WorldFrozen || flow.Regrouping || chapter.gas.IsCollapsed || !chapter.gas.ObingSeated) return 0;
            int leg = flow.LegIndex;
            if (leg != 1 && leg != 2) return 0;
            if (flow.ScoutedDistance < flow.LegEnd - 2.2f || !chapter.player.IsGrounded ||
                Chapter4Evacuation.FlatDistance(chapter.player.transform.position, flow.ScoutTarget) > 5 ||
                Mathf.Abs(chapter.player.transform.position.y - flow.ScoutTarget.y) > 1.3f) return 0;
            return leg;
        }
    }

    Chapter4Evacuation flow;
    Chapter4Companion obing, tail;
    float riseClock;
    bool initialized;

    public void Begin()
    {
        flow = chapter.evacuation; obing = chapter.gas.Obing;
        foreach (var actor in flow.Party) if (actor != obing) tail = actor;
        chapter.presentation.EnsureEndingUI();
        chapter.presentation.contactButton.onClick.AddListener(TryContact);
        initialized = true;
    }

    void Update()
    {
        if (!initialized || chapter.WorldFrozen) return;
        int opportunity = AvailableOpportunity;
        if (opportunity > 0) OpportunityMask |= 1 << (opportunity - 1);
        if (chapter.player.ContactPressed) TryContact();
        if (CurrentState == State.Rising)
        {
            riseClock += Time.deltaTime;
            obing.Pose.Set(Chapter4CompanionPose.State.Seated, 1 - Mathf.SmoothStep(0, 1, riseClock / 3.2f));
            if (riseClock >= 3.2f)
            {
                obing.Pose.Set(Chapter4CompanionPose.State.Normal);
                obing.MotionHeld = false;
                obing.IndependentFollow = true;
                CurrentState = State.Following;
            }
        }
        if (CurrentState == State.Following)
        {
            // Follow the real last companion even while the player is scouting.
            // The existing predecessor spacing still prevents overtaking.
            obing.FollowTo(Mathf.Max(obing.Progress, tail.Progress - 1.45f), 0);
            bool near = Chapter4Evacuation.FlatDistance(obing.transform.position, tail.transform.position) <= 2.25f &&
                Mathf.Abs(obing.transform.position.y - tail.transform.position.y) <= 1.2f;
            if (near && obing.Progress >= tail.Progress - 2.1f)
            {
                CurrentState = State.Reunited;
                obing.IndependentFollow = false;
                obing.FollowTo(flow.LegEnd - (flow.ActivePartyCount - 1) * 1.45f, 0);
                chapter.gas.MarkObingRejoined();
                flow.Announce("obing", "我看見你們了……謝謝你告訴我方向。我會跟緊。", 6.2f);
            }
        }
        string call = chapter.player.IsVR ? "按左扳機呼喊歐冰" : "按 F 呼喊歐冰";
        chapter.presentation.SetContactStatus(Responded
            ? Reunited ? "歐冰已沿路會合・繼續一起撤離" : "歐冰已回應，正在沿路跟上・等她會合"
            : opportunity == 1 ? "中段岔路：" + call + "\n告訴她上坡通路的方向"
            : opportunity == 2 ? "最後岩肩：" + call + "\n告訴她通往集合地的方向" : "", opportunity > 0);
    }

    public void NotifySignal(int leg)
    {
        if (leg == 1 || leg == 2) OpportunityMask |= 1 << (leg - 1);
    }

    public void TryContact()
    {
        int opportunity = AvailableOpportunity;
        if (opportunity == 0) return;
        FirstContact = opportunity == 1; SecondContact = opportunity == 2;
        OpportunityMask |= 1 << (opportunity - 1);
        CurrentState = State.Rising; riseClock = 0;
        obing.MotionHeld = true;
        flow.Announce("obing", opportunity == 1
            ? "聽見了！你們沿上坡的通路走，我站起來就跟上。"
            : "聽見了！穿過岩肩就是集合地……我沿著這條路過去！", 6.2f);
        chapter.presentation.SetContactStatus("歐冰已回應，正在起身・等她沿路會合", false);
    }

    public sealed class Snapshot
    {
        public State state;
        public bool first, second;
        public int opportunities;
        public float rise;
    }
    public Snapshot Capture() => new Snapshot { state = CurrentState, first = FirstContact, second = SecondContact, opportunities = OpportunityMask, rise = riseClock };
    public void Restore(Snapshot snapshot)
    {
        if (snapshot == null) return;
        CurrentState = snapshot.state; FirstContact = snapshot.first; SecondContact = snapshot.second;
        OpportunityMask = snapshot.opportunities; riseClock = snapshot.rise;
        chapter.presentation.SetContactStatus("", false);
    }
    void OnDestroy()
    {
        if (chapter && chapter.presentation && chapter.presentation.contactButton)
            chapter.presentation.contactButton.onClick.RemoveListener(TryContact);
    }
}
