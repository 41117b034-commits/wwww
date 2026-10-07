using UnityEngine;

// Reuses the authored trees. Only the night transition changes their transforms.
public sealed class Chapter2LoggedForest : MonoBehaviour
{
    public Chapter2Controller chapter;
    public Transform[] trees;
    public Collider ground;
    public GameObject timberPiles;
    public bool Applied { get; private set; }
    Vector3[] positions;
    Quaternion[] rotations;
    float[] trunkRadii;

    void Awake()
    {
        positions=new Vector3[trees.Length];rotations=new Quaternion[trees.Length];trunkRadii=new float[trees.Length];
        for(int i=0;i<trees.Length;i++)
        {
            var tree=trees[i];positions[i]=tree.position;rotations[i]=tree.rotation;
            var renderers=tree.GetComponentsInChildren<Renderer>(true);
            Bounds bounds=new Bounds(tree.position,Vector3.zero);
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            trunkRadii[i]=tree==chapter.sacredTree?1.3f:Mathf.Clamp(bounds.size.y*.018f,.18f,.48f);
        }
    }

    public void ApplyNight()
    {
        if(Applied)return;
        Applied=true;
        Vector3 center=chapter.campfire.position;
        for(int i=0;i<trees.Length;i++)
        {
            var tree=trees[i];
            Vector3 radial=Vector3.ProjectOnPlane(positions[i]-center,Vector3.up);
            float distance=radial.magnitude;
            radial=distance>.01f?radial/distance:Vector3.forward;
            // Mostly tangential falls show long horizontal trunks behind the council.
            // A small outward component keeps their crowns outside the seating area.
            float angle=(i%2==0?1:-1)*(72+Mathf.Sin(i*2.39996f)*12);
            Vector3 direction=Quaternion.AngleAxis(angle,Vector3.up)*radial;
            Vector3 basePoint=center+radial*Mathf.Max(8.5f,distance);
            if(tree==chapter.sacredTree)
            {
                basePoint=center+new Vector3(-7,0,12);
                direction=new Vector3(1,0,.25f).normalized;
                foreach(Transform part in tree)if(part.name=="Buttress root")part.gameObject.SetActive(false);
            }
            float floor=ground&&ground.Raycast(new Ray(basePoint+Vector3.up*80,Vector3.down),out var hit,160)?hit.point.y:center.y;
            basePoint.y=floor+trunkRadii[i];
            tree.SetPositionAndRotation(basePoint,Quaternion.FromToRotation(Vector3.up,direction)*rotations[i]);
            tree.gameObject.SetActive(true);
            foreach(var collider in tree.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        }
        chapter.fallenStump.SetActive(false);
        if(timberPiles)timberPiles.SetActive(true);
        Physics.SyncTransforms();
    }
}
