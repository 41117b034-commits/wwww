using System.Collections;
using UnityEngine;

public partial class Chapter1PerformanceController
{
    [Header("Wedding Confrontation")]
    [Min(1f)] public float leaderApproachSeconds = 4.4f;
    [Min(1f)] public float leaderQuestionSeconds = 2.8f;
    [Min(0.1f)] public float weddingSpeakerCameraSeconds = 0.8f;
    private Vector3 weddingDramaCenter;
    private float weddingDramaHeight;
    private string weddingDramaBeat;

    private void SetWeddingDramaBeat(string beat)
    {
        weddingDramaBeat = beat;
        Debug.Log("[Wedding Confrontation] " + beat);
    }

    private Vector3 WeddingActorFloor(Transform actor)
    {
        Vector3 floor = actor.position;
        if (TryGetIncidentSurfaceY(floor, out float y)) floor.y = y;
        return floor;
    }

    private IEnumerator WeddingCameraTo(Vector3 position, Vector3 focus, float fov, float seconds)
    {
        if (incidentCameraView == null) yield break;
        RestoreIncidentShotOccluders();
        if (incidentCameraPoseLock == null)
            incidentCameraPoseLock = BeginCinematicCameraPoseLock(incidentCameraView);
        Vector3 start = incidentCameraView.position;
        Quaternion rotation = incidentCameraView.rotation;
        Quaternion destination = Quaternion.LookRotation(focus - position, Vector3.up);
        Camera camera = incidentCameraView.GetComponent<Camera>();
        float initialFov = camera != null ? camera.fieldOfView : fov;
        float duration = Mathf.Max(0.05f, seconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock,
                Vector3.Lerp(start, position, t), Quaternion.Slerp(rotation, destination, t));
            if (camera != null) camera.fieldOfView = Mathf.Lerp(initialFov, fov, t);
            yield return null;
        }
        SetCinematicCameraPose(incidentCameraView, incidentCameraPoseLock, position, destination);
        if (camera != null) camera.fieldOfView = fov;
    }

    private IEnumerator WeddingSpeakerShot(Transform speaker, bool policeSpeaker)
    {
        if (speaker == null) yield break;
        float height = Mathf.Max(1.7f, weddingDramaHeight);
        Vector3 floor = WeddingActorFloor(speaker);
        // Both angles remain on the near side of the conversation axis. The
        // reverse angle puts the leader in the centre and the villagers behind.
        Vector3 offset = policeSpeaker
            ? new Vector3(1.62f, 0.91f, -0.46f)
            : new Vector3(-0.68f, 0.87f, -1.64f);
        yield return WeddingCameraTo(floor + offset * height,
            floor + Vector3.up * height * 0.72f, 46f, weddingSpeakerCameraSeconds);
    }

    private IEnumerator WeddingActionShot()
    {
        // Match the approved reference: police left, leader centre, the same
        // fire and buildings behind, with all bystanders in their original slots.
        float height = Mathf.Max(1.7f, weddingDramaHeight);
        yield return WeddingCameraTo(
            weddingDramaCenter + new Vector3(-0.55f, 1.1f, -4.15f) * height,
            weddingDramaCenter + new Vector3(-0.28f, 0.50f, 0.05f) * height,
            50f, 0.9f);
    }

    private IEnumerator WeddingCupAndConfrontation()
    {
        if (ceremonyCup != null)
        {
            Vector3 cup = ceremonyCup.position;
            float height = Mathf.Max(1.7f, weddingDramaHeight);
            yield return WeddingCameraTo(cup + new Vector3(-0.19f, 0.20f, -0.45f) * height,
                cup, 42f, 0.65f);
            ShowLine("旁白", "日警的手掃向桌邊，將酒杯打落。", 2f);
            yield return new WaitForSeconds(0.55f);
            PlayPoliceEventClip(cupCrashClip);
            if (ceremonyCupRigidbody != null)
            {
                ceremonyCupRigidbody.isKinematic = false;
                ceremonyCupRigidbody.useGravity = true;
                ceremonyCupRigidbody.AddForce(Vector3.right * 1.6f + Vector3.up * 0.55f, ForceMode.Impulse);
            }
            else ceremonyCup.Rotate(Vector3.forward, 78f, Space.Self);
            yield return new WaitForSeconds(0.85f);
        }
        yield return WeddingLeaderConfrontation();
    }

    private IEnumerator WeddingLeaderConfrontation()
    {
        Transform leader = groomActor != null ? groomActor : shovedVillagerActor;
        Transform officer = primaryPoliceActor;
        if (leader == null || officer == null)
        {
            Debug.LogError("[Wedding Confrontation] Leader or police actor is missing.");
            yield break;
        }
        shovedVillagerActor = leader;
        Chapter1IncidentRig leaderRig = PrepareDoorwayRig(leader, false);
        Chapter1IncidentRig officerRig = PrepareDoorwayRig(officer, true);
        float height = Mathf.Max(leaderRig.Height, officerRig.Height);
        leaderRig.smoothLocomotion = true;
        leaderRig.strideScale = 0.82f;
        leaderRig.frozen = false;
        leaderRig.speakingWeight = 0f;
        officerRig.pointTarget = null;
        officerRig.batonInLeftHand = true; // Right hand is free for the shove.
        yield return WeddingActionShot();

        Vector3 start = leader.position;
        Vector3 destination = officer.position + new Vector3(0.64f, 0f, 0.045f) * height;
        destination.y = start.y;
        // Curve around the near edge of the roasting pit, never through it.
        Vector3 control1 = start + Vector3.back * height * 0.80f;
        Vector3 control2 = destination + Vector3.right * height * 0.60f;
        Quaternion startRotation = leader.rotation;
        leaderRig.Face(control1 - start);
        Quaternion walkingRotation = leader.rotation;
        leader.rotation = startRotation;
        ShowLine("旁白", "一名族人走上前，向警察質問。", leaderApproachSeconds + 0.7f);
        SetWeddingDramaBeat("leader-approach");
        for (float t = 0f; t < 0.4f; t += Time.deltaTime)
        {
            leader.rotation = Quaternion.Slerp(startRotation, walkingRotation, Mathf.SmoothStep(0, 1, t / 0.4f));
            yield return null;
        }
        leaderRig.walking = true;
        float duration = Mathf.Max(1f, leaderApproachSeconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / duration);
            // Gentle acceleration and deceleration without stopping at waypoints.
            t = t * t * (3f - 2f * t);
            float u = 1f - t;
            Vector3 position = u*u*u*start + 3f*u*u*t*control1 + 3f*u*t*t*control2 + t*t*t*destination;
            Vector3 tangent = 3f*u*u*(control1-start) + 6f*u*t*(control2-control1) + 3f*t*t*(destination-control2);
            Quaternion from = leader.rotation;
            leaderRig.Face(tangent);
            leader.rotation = Quaternion.Slerp(from, leader.rotation, 1f - Mathf.Exp(-8f * Time.deltaTime));
            PlaceDoorwayActor(leaderRig, position);
            Quaternion officerFrom = officer.rotation;
            officerRig.Face(leader.position - officer.position);
            officer.rotation = Quaternion.Slerp(officerFrom, officer.rotation, 1f - Mathf.Exp(-4f * Time.deltaTime));
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, destination);
        leaderRig.walking = false;
        leaderRig.Face(officer.position - leader.position);
        officerRig.Face(leader.position - officer.position);
        leaderRig.conversationTarget = officer;
        SetWeddingDramaBeat("leader-question");
        ShowLine("族人", "我們只是辦婚禮，為什麼要這樣對待我們？", leaderQuestionSeconds);
        for (float elapsed = 0f; elapsed < leaderQuestionSeconds; elapsed += Time.deltaTime)
        {
            leaderRig.speakingWeight = Mathf.SmoothStep(0f, 1f, elapsed / 0.35f);
            yield return null;
        }

        leaderRig.speakingWeight = 0f;
        leaderRig.conversationTarget = null;
        officerRig.pushTarget = leaderRig;
        Vector3 officerStart = officer.position;
        Vector3 pushDirection = Vector3.ProjectOnPlane(leader.position - officer.position, Vector3.up).normalized;
        Vector3 officerContact = officerStart + pushDirection * height * 0.22f;
        SetWeddingDramaBeat("police-shove");
        for (float elapsed = 0f; elapsed < 0.34f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0, 1, elapsed / 0.34f);
            officerRig.pushWeight = t;
            PlaceDoorwayActor(officerRig, Vector3.Lerp(officerStart, officerContact, t));
            yield return null;
        }
        officerRig.pushWeight = 1f;
        PlaceDoorwayActor(officerRig, officerContact);
        // Reaction begins after contact, so there is no invisible remote shove.
        ShowLine("旁白", "警察猛地推開他，他踉蹌後退，差點跌倒。", 3.8f);
        SetWeddingDramaBeat("shove-contact");
        Vector3 contactStart = leader.position;
        for (float elapsed = 0f; elapsed < 0.26f; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.26f);
            leaderRig.stumbleWeight = 0.62f * t;
            PlaceDoorwayActor(leaderRig, contactStart + pushDirection * height * 0.06f * t);
            PlaceDoorwayActor(officerRig, officerContact + pushDirection * height * 0.08f * t);
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, contactStart + pushDirection * height * 0.06f);
        PlaceDoorwayActor(officerRig, officerContact + pushDirection * height * 0.08f);
        SetWeddingDramaBeat("leader-stumble");
        Vector3 leaderStart = leader.position;
        const float stumbleSeconds = 1.65f;
        for (float elapsed = 0f; elapsed < stumbleSeconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / stumbleSeconds);
            float travel = 1f - Mathf.Pow(1f - t, 2f);
            leaderRig.stumbleProgress = t;
            leaderRig.stumbleWeight = t < 0.2f ? Mathf.Lerp(0.62f, 1f, Mathf.SmoothStep(0, 1, t / 0.2f))
                : Mathf.Lerp(1f, 0.36f, Mathf.SmoothStep(0, 1, (t-0.2f)/0.8f));
            PlaceDoorwayActor(leaderRig, leaderStart + pushDirection * height * 0.38f * travel);
            officerRig.pushWeight = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.04f, 0.48f, t));
            yield return null;
        }
        PlaceDoorwayActor(leaderRig, leaderStart + pushDirection * height * 0.38f);
        officerRig.pushWeight = 0f;
        officerRig.pushTarget = null;
        leaderRig.stumbleProgress = 1f;
        SetWeddingDramaBeat("leader-recover");
        for (float elapsed = 0f; elapsed < 0.9f; elapsed += Time.deltaTime)
        {
            leaderRig.stumbleWeight = Mathf.Lerp(0.36f, 0f, Mathf.SmoothStep(0, 1, elapsed / 0.9f));
            yield return null;
        }
        leaderRig.stumbleWeight = 0f;
        yield return new WaitForSeconds(0.75f);
        SetWeddingDramaBeat("confrontation-complete");
    }
}
