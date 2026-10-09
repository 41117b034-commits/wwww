using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Attach beside Chapter2Controller. Voice is intentionally 2D for clear VR dialogue.
[DisallowMultipleComponent]
public sealed class Chapter2AudioManager : MonoBehaviour
{
    public enum Role { Narrator, Officer, Villager, Mona, Tado, Bawan, Watan, Warriors, Player }
    [Serializable]
    public class VoiceLine
    {
        public string id;
        public Role role;
        [TextArea(1, 4)] public string chinese;
        public AudioClip clip;
    }
    [Header("逐句配音：Officer 日語；族人與玩家賽德克族語；Narrator 中文")]
    public VoiceLine[] lines = new VoiceLine[] {
        new VoiceLine { id = "C2_001", role = Role.Narrator, chinese = "警察要求族人集合，宣讀新的伐木與搬運規定。" },
        new VoiceLine { id = "C2_002", role = Role.Narrator, chinese = "木材不准拖曳，必須肩扛下山。陡坡與長途搬運，使族人的生活更加艱困。" },
        new VoiceLine { id = "C2_003", role = Role.Narrator, chinese = "警察進一步命令族人前往聖地「西仔希克」，砍伐被視為守護者的巨木。" },
        new VoiceLine { id = "C2_004", role = Role.Narrator, chinese = "在槍口與鞭子的威逼下，族人走入森林。壓抑的憤怒，逐漸化為反抗的決心。" },
        new VoiceLine { id = "C2_005", role = Role.Officer, chinese = "把這些樹都砍了。" },
        new VoiceLine { id = "C2_006", role = Role.Villager, chinese = "這棵巨木是我們的守護者……真的要砍下去嗎？" },
        new VoiceLine { id = "C2_007", role = Role.Officer, chinese = "退開！誰敢違抗命令？" },
        new VoiceLine { id = "C2_008", role = Role.Officer, chinese = "木材不能再受損！放慢動作，重新找準落點。" },
        new VoiceLine { id = "C2_009", role = Role.Villager, chinese = "命令完成了。可是，我們該怎麼面對祖靈？" },
        new VoiceLine { id = "C2_010", role = Role.Villager, chinese = "前面就是西仔希克。這片森林，守護著我們的生活。" },
        new VoiceLine { id = "C2_011", role = Role.Villager, chinese = "沿著這條小徑，跟我來。不要離隊太遠。" },
        new VoiceLine { id = "C2_012", role = Role.Tado, chinese = "父親！" },
        new VoiceLine { id = "C2_013", role = Role.Tado, chinese = "日本人逼我們砍巨木、扣工資，" },
        new VoiceLine { id = "C2_014", role = Role.Tado, chinese = "連敬酒都要被打！" },
        new VoiceLine { id = "C2_015", role = Role.Tado, chinese = "我們還要忍到什麼時候？" },
        new VoiceLine { id = "C2_016", role = Role.Watan, chinese = "達多，冷靜！" },
        new VoiceLine { id = "C2_017", role = Role.Watan, chinese = "日本人的槍砲比樹葉還多。" },
        new VoiceLine { id = "C2_018", role = Role.Watan, chinese = "動了手，" },
        new VoiceLine { id = "C2_019", role = Role.Watan, chinese = "部落的婦女和孩子怎麼辦？" },
        new VoiceLine { id = "C2_020", role = Role.Watan, chinese = "莫那，這是一條死路啊！" },
        new VoiceLine { id = "C2_021", role = Role.Mona, chinese = "死路？" },
        new VoiceLine { id = "C2_022", role = Role.Mona, chinese = "瓦旦，看看我們的年輕人，" },
        new VoiceLine { id = "C2_023", role = Role.Mona, chinese = "手是用來握獵刀的，" },
        new VoiceLine { id = "C2_024", role = Role.Mona, chinese = "現在卻在幫日人扛木材。" },
        new VoiceLine { id = "C2_025", role = Role.Mona, chinese = "丟了 Gaya（祖靈的教誨），" },
        new VoiceLine { id = "C2_026", role = Role.Mona, chinese = "我們活著和野獸有什麼兩樣？" },
        new VoiceLine { id = "C2_027", role = Role.Bawan, chinese = "頭目！" },
        new VoiceLine { id = "C2_028", role = Role.Bawan, chinese = "我寧可做自由的鬼，" },
        new VoiceLine { id = "C2_029", role = Role.Bawan, chinese = "也不做屈辱的人！" },
        new VoiceLine { id = "C2_030", role = Role.Watan, chinese = "我們打不贏日本帝國的……" },
        new VoiceLine { id = "C2_031", role = Role.Mona, chinese = "明天的運動會，" },
        new VoiceLine { id = "C2_032", role = Role.Mona, chinese = "日本官吏都在公學校。" },
        new VoiceLine { id = "C2_033", role = Role.Mona, chinese = "那是祖靈給的唯一機會！" },
        new VoiceLine { id = "C2_034", role = Role.Mona, chinese = "我莫那·魯道已經決定起義，" },
        new VoiceLine { id = "C2_035", role = Role.Mona, chinese = "用鮮血洗刷屈辱！" },
        new VoiceLine { id = "C2_036", role = Role.Mona, chinese = "賽德克巴萊的子孫們，" },
        new VoiceLine { id = "C2_037", role = Role.Mona, chinese = "你們，願不願意跟隨我？" },
        new VoiceLine { id = "C2_038", role = Role.Tado, chinese = "我跟隨您！" },
        new VoiceLine { id = "C2_039", role = Role.Tado, chinese = "為了祖靈，血祭彩虹！" },
        new VoiceLine { id = "C2_040", role = Role.Warriors, chinese = "我們跟隨您！" },
        new VoiceLine { id = "C2_041", role = Role.Warriors, chinese = "為了祖靈！" },
        new VoiceLine { id = "C2_042", role = Role.Warriors, chinese = "血祭彩虹！" },
        new VoiceLine { id = "C2_043", role = Role.Mona, chinese = "好！" },
        new VoiceLine { id = "C2_044", role = Role.Mona, chinese = "今夜回去對妻兒好一點，" },
        new VoiceLine { id = "C2_045", role = Role.Mona, chinese = "別露破綻。" },
        new VoiceLine { id = "C2_046", role = Role.Mona, chinese = "天亮之後……我們彩虹橋上見！" },
        new VoiceLine { id = "C2_047", role = Role.Player, chinese = "莫那頭目，這根本是自殺！我想活下去……" },
        new VoiceLine { id = "C2_048", role = Role.Mona, chinese = "活下去？" },
        new VoiceLine { id = "C2_049", role = Role.Mona, chinese = "那就去向日本人跪下！" },
        new VoiceLine { id = "C2_050", role = Role.Mona, chinese = "去幫他們扛木頭、" },
        new VoiceLine { id = "C2_051", role = Role.Mona, chinese = "聽他們叫你生番！" },
        new VoiceLine { id = "C2_052", role = Role.Mona, chinese = "滾！" },
        new VoiceLine { id = "C2_053", role = Role.Mona, chinese = "彩虹橋上沒有你的位置，" },
        new VoiceLine { id = "C2_054", role = Role.Mona, chinese = "你不配稱為 Sediq Bale（真的人）！" },
        new VoiceLine { id = "C2_055", role = Role.Villager, chinese = "別碰它！這是我們的聖地。" },
        new VoiceLine { id = "C2_056", role = Role.Villager, chinese = "這是祖靈守護的地方……我們不能退。" },
        new VoiceLine { id = "C2_057", role = Role.Narrator, chinese = "槍聲過後，一名阻擋警察的族人倒下。同伴急忙上前查看。" },
        new VoiceLine { id = "C2_058", role = Role.Villager, chinese = "退後……它要倒下了。" },
        new VoiceLine { id = "C2_059", role = Role.Officer, chinese = "給我去砍樹" },
    };
    [Header("背景音樂：未指定時淡出，環境音仍由原控制器播放")]
    public AudioClip introMusic, forestMusic, treeChoiceMusic, protectMusic, choppingMusic;
    public AudioClip treeFallMusic, meetingMusic, voteMusic, supportMusic, refusalMusic;
    [Header("音量與轉場")]
    [Range(0, 1)] public float musicVolume = .35f;
    [Range(0, 1)] public float narrationVolume = 1f, characterVolume = 1f;
    [Range(0, 1)] public float duckFactor = .3f;
    [Min(0)] public float musicFadeSeconds = 1f;
    [Min(0)] public float voiceTailSeconds = .2f;
    public bool warnMissingClips = false;
    [Header("選用 Mixer Group；AudioSource 由本元件建立")]
    public AudioMixerGroup musicOutput, narrationOutput, characterOutput;
    AudioSource music, narration, character;
    Coroutine musicFade;
    float envelope, duck = 1;
    AudioClip requestedMusic;
    public bool VoicePlaying => (narration && narration.isPlaying) || (character && character.isPlaying);
    void Awake() { EnsureSources(); }
    void EnsureSources()
    {
        if (!music) music = MakeSource("Chapter2 BGM", true);
        if (!narration) narration = MakeSource("Chapter2 Narration", false);
        if (!character) character = MakeSource("Chapter2 Character Voice", false);
    }
    AudioSource MakeSource(string label, bool loop)
    {
        var child = new GameObject(label); child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = loop; source.spatialBlend = 0; source.pitch = 1;
        return source;
    }
    void Update()
    {
        EnsureSources();
        duck = Mathf.MoveTowards(duck, VoicePlaying ? duckFactor : 1f, Time.unscaledDeltaTime * 4);
        music.volume = musicVolume * envelope * duck;
        narration.volume = narrationVolume; character.volume = characterVolume;
        music.outputAudioMixerGroup = musicOutput;
        narration.outputAudioMixerGroup = narrationOutput; character.outputAudioMixerGroup = characterOutput;
    }
    public void MusicForStage(Chapter2Controller.Stage stage, int treeChoice, int meetingChoice)
    {
        AudioClip clip = null;
        switch (stage)
        {
            case Chapter2Controller.Stage.Intro: clip = introMusic; break;
            case Chapter2Controller.Stage.Follow: clip = forestMusic; break;
            case Chapter2Controller.Stage.TreeChoice: clip = treeChoiceMusic; break;
            case Chapter2Controller.Stage.Chopping: clip = choppingMusic; break;
            case Chapter2Controller.Stage.Consequence: clip = treeChoice == 0 ? protectMusic : treeFallMusic; break;
            case Chapter2Controller.Stage.Meeting: clip = meetingMusic; break;
            case Chapter2Controller.Stage.Vote: clip = voteMusic; break;
            case Chapter2Controller.Stage.Ending: clip = meetingChoice == 0 ? supportMusic : refusalMusic; break;
        }
        PlayMusic(clip);
    }
    public void PlayMusic(AudioClip clip)
    {
        if (!isActiveAndEnabled) return;
        EnsureSources();
        if (requestedMusic == clip && (musicFade != null || music.clip == clip)) return;
        requestedMusic = clip;
        if (musicFade != null) StopCoroutine(musicFade);
        musicFade = StartCoroutine(ChangeMusic(clip));
    }
    IEnumerator ChangeMusic(AudioClip clip)
    {
        float start = envelope, seconds = Mathf.Max(.01f, musicFadeSeconds * .5f);
        for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
        { envelope = Mathf.Lerp(start, 0, t / seconds); yield return null; }
        envelope = 0; music.Stop(); music.clip = clip;
        if (clip)
        {
            music.Play();
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            { envelope = t / seconds; yield return null; }
            envelope = 1;
        }
        musicFade = null;
    }
    // Returns a minimum subtitle duration, including the complete voice recording.
    public float BeginLine(Role role, string chinese, float originalSeconds)
    {
        StopVoice();
        if (!isActiveAndEnabled) return originalSeconds;
        EnsureSources();
        VoiceLine found = null;
        if (lines != null) foreach (var line in lines)
                if (line != null && line.role == role && (line.chinese ?? "").Trim() == chinese.Trim()) { found = line; break; }
        if (found == null || !found.clip)
        {
            if (warnMissingClips) Debug.LogWarning("[Chapter2Audio] Missing voice: " + role + " / " + chinese, this);
            return originalSeconds;
        }
        var source = role == Role.Narrator ? narration : character;
        source.clip = found.clip; source.volume = role == Role.Narrator ? narrationVolume : characterVolume;
        source.outputAudioMixerGroup = role == Role.Narrator ? narrationOutput : characterOutput;
        source.Play();
        return Mathf.Max(originalSeconds, found.clip.length + voiceTailSeconds);
    }
    public void StopVoice()
    {
        if (narration) narration.Stop(); if (character) character.Stop();
    }
    public void StopAllAudio()
    {
        StopAllCoroutines(); musicFade = null; requestedMusic = null; envelope = 0;
        StopVoice(); if (music) music.Stop();
    }
    public void SetMusicVolume(float value) { musicVolume = Mathf.Clamp01(value); }
    public void SetNarrationVolume(float value) { narrationVolume = Mathf.Clamp01(value); }
    public void SetCharacterVolume(float value) { characterVolume = Mathf.Clamp01(value); }
    void OnDisable() { StopAllAudio(); }
}
