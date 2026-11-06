using UnityEngine;

public sealed partial class Chapter3Ending
{
    GameObject shelterBack;
    public bool EntrancePrepared=>monaBorrowed&&Mona&&shelterBack;
    public void PrepareEntrance(Chapter3SchoolController owner)
    {
        PrepareShelter(owner);
        if(!monaBorrowed)BorrowMona();
    }
    public void PrepareShelter(Chapter3SchoolController owner)
    {
        school=owner;
        if(!shelterBack)
        {
            // Extend the existing shelter's cloth down its back. Mona is
            // staged here before the party enters the forest, never spawned
            // into a player's current view during the ending.
            Material cloth=null;
            foreach(var renderer in owner.GetComponentsInChildren<MeshRenderer>(true))
                if(renderer.name=="接應棚斜布"){cloth=renderer.sharedMaterial;break;}
            shelterBack=GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelterBack.name="接應棚後方遮風布";shelterBack.transform.SetParent(transform,false);
            shelterBack.transform.position=new Vector3(13,1.35f,111.1f);
            shelterBack.transform.localScale=new Vector3(8,2.7f,.08f);
            if(cloth)shelterBack.GetComponent<MeshRenderer>().sharedMaterial=cloth;
        }
    }
}
