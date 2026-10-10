using System.Collections;
using UnityEngine;

public sealed partial class Chapter2Controller
{
    IEnumerator OrderSurvivorsToTrees(Chapter2Rifle rifle)
    {
        var threat = new GameObject("Survivors rifle aim").transform;
        threat.SetParent(dayGroup.transform);
        rifle.aimTarget = threat;
        threat.position = SurvivorChest(workers[1]);
        CameraBeat = "threat-raising";
        ui.Line("", "");
        for (float t = 0; t < 1; t += Time.deltaTime / .75f)
        {
            rifle.aim = Mathf.SmoothStep(0, 1, t);
            officer.Face(threat.position); yield return null;
        }
        rifle.aim = 1;
        CameraBeat = "forced-order";
        ui.Line("日本警察", "給我去砍樹");
        float orderEnd = Time.unscaledTime + BeginChapterVoice(officer, "日本警察", "給我去砍樹", 3.2f);
        officer.speaking = true;
        // Keep the officer looking at the survivors, including while they are crouching.
        for (float t = 0; t < 3.2f; t += Time.deltaTime)
        {
            float sweep = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.9f, 2.3f, t));
            threat.position = Vector3.Lerp(SurvivorChest(workers[1]), SurvivorChest(workers[2]), sweep);
            officer.Face(threat.position); yield return null;
        }
        while (Time.unscaledTime < orderEnd) { officer.Face(threat.position); yield return null; }
        if (chapterAudio) chapterAudio.StopVoice();
        officer.speaking = false;
        ui.Line("", ""); CameraBeat = "survivors-standing";
        for (int i = 1; i < workers.Length; i++) workers[i].GetComponent<Chapter2GriefReaction>().StandUp((i - 1) * .22f);
        foreach (var worker in workers)
        {
            var reaction = worker.GetComponent<Chapter2GriefReaction>();
            if (!reaction) continue;
            while (!reaction.Standing)
            {
                threat.position = SurvivorChest(workers[2]); officer.Face(threat.position);
                yield return null;
            }
        }
        var walks = new Chapter2ReluctantWalk[workers.Length - 1];
        for (int i = 1; i < workers.Length; i++)
        {
            Vector3 treeSide = sacredTree.position + new Vector3(i == 1 ? 2.6f : -2.7f, 0, -1.65f);
            walks[i - 1] = workers[i].gameObject.AddComponent<Chapter2ReluctantWalk>();
            walks[i - 1].Begin(treeSide - workers[i].transform.position, (i - 1) * .16f);
        }
        CameraBeat = "survivors-short-walk";
        while (System.Array.Exists(walks, walk => walk.StepsCompleted < 2))
        {
            threat.position = (SurvivorChest(workers[1]) + SurvivorChest(workers[2])) * .5f;
            officer.Face(threat.position); yield return null;
        }
        // The next statement in Run starts fading; the third step continues under it.
        CameraBeat = "survivors-short-walk-fade";
    }
    static Vector3 SurvivorChest(Chapter2Actor actor)
    {
        return Vector3.Lerp(actor.Rig.Hips.position, actor.Rig.Head.position, .6f);
    }
}
