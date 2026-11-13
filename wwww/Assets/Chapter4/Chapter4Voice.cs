using System;
using System.Collections.Generic;
using UnityEngine;

// Fixed, offline recordings. The selected voice language never changes Chinese
// subtitles. Draft Tgdaya provenance and review status live in the catalog.
[DefaultExecutionOrder(90)]
public sealed class Chapter4Voice : MonoBehaviour
{
    [Serializable] public sealed class Entry { public string role, chinese, mandarin, seediq, translation; public bool reviewed; }
    [Serializable] public sealed class Bank { public Entry[] entries; }
    sealed class Line { public string role, text; public float minimum; public bool card; }
    readonly Dictionary<string, Entry> entries = new Dictionary<string, Entry>();
    readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    readonly List<AudioClip> preparedClips = new List<AudioClip>();
    readonly Queue<Line> queue = new Queue<Line>();
    Chapter4SceneController chapter;
    Chapter4Companion speaker;
    Line current;
    float elapsed, duration;
    bool paused;
    public AudioSource Output { get; private set; }
    public bool Busy => current != null || queue.Count > 0;
    public bool Speaking => Output && Output.isPlaying;
    public string CurrentText => current == null ? "" : current.text;
    public int CompletedLines { get; private set; }
    public int MissingClips { get; private set; }
    public string CurrentResource { get; private set; }
    public float Elapsed => elapsed;
    public float Duration => duration;
    public string SpeakerRole => current == null ? "" : current.role;
    public float MouthWeight { get; private set; }
    readonly float[] waveform = new float[256];
    public IReadOnlyDictionary<string, Entry> Catalog => entries;

    public void Initialize(Chapter4SceneController owner)
    {
        chapter = owner;
        Output = gameObject.AddComponent<AudioSource>();
        Output.playOnAwake = false; Output.spatialBlend = 0; Output.volume = .85f; Output.priority = 32;
        var asset = Resources.Load<TextAsset>("Chapter4Voice/catalog");
        if (asset) foreach (var entry in JsonUtility.FromJson<Bank>(asset.text).entries)
            entries[entry.role + "|" + entry.chinese] = entry;
        GameLanguageSettings.Changed += LanguageChanged;
    }

    public void Enqueue(string role, string text, float minimum = 0, bool card = false)
    {
        if (current != null && current.text == text && current.role == role) return;
        foreach (var line in queue) if (line.text == text && line.role == role) return;
        queue.Enqueue(new Line { role = role, text = text, minimum = minimum, card = card });
        if (current == null) Next();
    }

    void Next()
    {
        ClearSpeaker();
        if (queue.Count == 0) { current = null; chapter.presentation.HideSpeech(); return; }
        current = queue.Dequeue(); elapsed = 0;
        speaker = Array.Find(chapter.evacuation.cast, a => a.actorId == current.role && a.gameObject.activeInHierarchy);
        if (!current.card) chapter.presentation.ShowSpeech(speaker ? speaker.displayName : "旁白", current.text);
        PlaySelected();
    }

    void PlaySelected()
    {
        Output.Stop(); Output.clip = null; CurrentResource = null;
        if (entries.TryGetValue(current.role + "|" + current.text, out var entry))
            CurrentResource = GameLanguageSettings.ChineseVoice ? entry.mandarin : entry.seediq;
        if (!string.IsNullOrEmpty(CurrentResource))
        {
            if (!clips.TryGetValue(CurrentResource, out var clip))
            {
                var original = Resources.Load<AudioClip>(CurrentResource);
                clip = GameLanguageSettings.ChineseVoice ? ChineseSpeechPacing.Prepare(original) : Chapter3VoiceTone.Prepare(original);
                clips[CurrentResource] = clip;
                if (clip && clip != original) preparedClips.Add(clip);
            }
            Output.clip = clip;
        }
        duration = Mathf.Max(current.minimum, current.text.Length / 4.2f + 1.5f);
        if (Output.clip)
        {
            duration = Mathf.Max(duration, Output.clip.length + .45f);
            Output.Play(); if (paused) Output.Pause();
        }
        else MissingClips++; // Keep the readable subtitle; never speak the wrong language.
    }

    void LanguageChanged()
    {
        if (current == null) return;
        elapsed = 0; PlaySelected(); // Replay this line in the newly selected language.
    }

    void Update()
    {
        if (!chapter) return;
        bool freeze = chapter.IsPaused || chapter.gas && chapter.gas.IsCollapsed;
        if (paused != freeze)
        {
            paused = freeze;
            if (paused) Output.Pause(); else Output.UnPause();
        }
        if (speaker) speaker.Speaking = !paused && Speaking;
        float mouth = 0;
        if (!paused && Speaking)
        {
            Output.GetOutputData(waveform, 0);
            float sum = 0; foreach (float value in waveform) sum += value * value;
            mouth = Mathf.Clamp01(Mathf.Sqrt(sum / waveform.Length) * 12);
        }
        MouthWeight = Mathf.MoveTowards(MouthWeight, mouth, Time.unscaledDeltaTime * 10);
        if (paused || current == null) return;
        elapsed += Time.deltaTime;
        if (elapsed >= duration && !Output.isPlaying) { CompletedLines++; Next(); }
    }

    void ClearSpeaker() { if (speaker) speaker.Speaking = false; speaker = null; }
    public void Stop()
    {
        queue.Clear(); current = null; ClearSpeaker();
        if (Output) { Output.Stop(); Output.clip = null; }
        if (chapter && chapter.presentation) chapter.presentation.HideSpeech();
    }
    void OnDisable() { Stop(); }
    void OnDestroy()
    {
        GameLanguageSettings.Changed -= LanguageChanged;
        foreach (var clip in preparedClips) if (clip) Destroy(clip);
    }
}
