using UnityEngine;

public sealed partial class Chapter3SchoolAmbient
{
    // Japanese survivors now shelter inside school. Keep the old read-only
    // diagnostics compatible; there is no pursuit motor or retreat follower.
    public int CivilianFollowerCount=>0;
    public int CivilianArrivedCount=>CiviliansAtRefuge;
    public int JapanesePursuerCount=>0;
    public int PursuingSpectators=>0;
    public int PursuitCompletedCount=>0;
    public Transform JapanesePursuerActor(int index)=>null;
    public Transform JapanesePursuitTarget(int index)=>null;
    public float JapanesePursuitDistance(int index)=>0;
    public static bool InsideSchool(Vector3 point)=>point.x>=-21.5f&&point.x<=21.5f&&point.z>=0&&point.z<=48.1f;
}
