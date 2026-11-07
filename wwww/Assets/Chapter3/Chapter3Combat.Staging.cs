using System.Collections;
using UnityEngine;

public sealed partial class Chapter3Combat
{
    readonly Transform[] stagedRocks=new Transform[4];
    readonly Transform[] stagedTrees=new Transform[9];
    Chapter3RouteGuide coverGuide;
    int guidedOfficer=-1;
    public bool OpeningPrepared {get;private set;}
    public Vector3 OpeningPosition=>WorldPoint(new Vector3(0,0,-6.9f));
    public Chapter3RouteGuide CoverGuide=>coverGuide;
    public Transform StagedRock(int index)=>index>=0&&index<stagedRocks.Length?stagedRocks[index]:null;
    public Transform StagedTree(int index)=>index>=0&&index<stagedTrees.Length?stagedTrees[index]:null;
    public int StagedTreeCount=>stagedTrees.Length;

    void PrepareEncounterCover()
    {
        if(stagedRocks[0])return;
        Transform source=null;
        int oldRock=0;
        foreach(var candidate in GetComponentsInChildren<Transform>(true))
            if(candidate.name=="山路岩石掩體")
            {
                if(!source)source=candidate;
                Bounds old=RockBounds(candidate);Vector3 basePoint=new Vector3(old.center.x,old.min.y,old.center.z);
                FitCoverStone(candidate,basePoint,new Vector3(Mathf.Min(old.size.x,2.8f),1.25f+(oldRock++%3)*.1f,Mathf.Min(old.size.z,2.5f)));
            }
        if(!source)return;
        Vector3[] centers={new Vector3(0,0,-3.4f),new Vector3(-3.5f,0,3.3f),
            new Vector3(-1.5f,0,10.9f),new Vector3(1.2f,0,17.8f)};
        Vector3[] sizes={new Vector3(2.6f,1.25f,2),new Vector3(2.8f,1.4f,2.6f),
            new Vector3(2.8f,1.3f,2),new Vector3(2.6f,1.45f,2)};
        for(int i=0;i<stagedRocks.Length;i++)
        {
            var rock=Instantiate(source.gameObject,transform).transform;
            rock.name="山路連續掩體 "+i;rock.gameObject.SetActive(true);
            rock.SetPositionAndRotation(WorldPoint(centers[i]),Quaternion.identity);
            rock.localScale=Vector3.one;
            FitCoverStone(rock,WorldPoint(centers[i]),sizes[i]);
            stagedRocks[i]=rock;
        }
        // Three separate trunks protect the stationary opening group. Further
        // trunks interrupt the officers' sightlines along the physical knife route.
        Vector3[] trunks={new Vector3(0,0,-5.35f),new Vector3(-1.85f,0,-5.2f),new Vector3(1.85f,0,-5.2f),
            new Vector3(-4.2f,0,1.5f),new Vector3(-3.5f,0,5.75f),new Vector3(-3,0,11.8f),
            new Vector3(3.8f,0,17.4f),new Vector3(3.3f,0,19.4f),new Vector3(-4.35f,0,-3)};
        for(int i=0;i<trunks.Length;i++)stagedTrees[i]=Chapter3NaturalCover.Tree(transform,WorldPoint(trunks[i]),i);
        if(!coverGuide)coverGuide=gameObject.AddComponent<Chapter3RouteGuide>();
    }
    static void FitCoverStone(Transform stone,Vector3 basePoint,Vector3 size)
    {
        var bounds=RockBounds(stone);
        stone.localScale=Vector3.Scale(stone.localScale,new Vector3(size.x/Mathf.Max(.01f,bounds.size.x),
            size.y/Mathf.Max(.01f,bounds.size.y),size.z/Mathf.Max(.01f,bounds.size.z)));
        bounds=RockBounds(stone);stone.position+=basePoint-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    }
    static Bounds RockBounds(Transform root)
    {
        var renderers=root.GetComponentsInChildren<Renderer>();
        Bounds bounds=renderers.Length>0?renderers[0].bounds:new Bounds(root.position,Vector3.one);
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
    IEnumerator CoveredIntroduction()
    {
        OpeningPrepared=false;guidedOfficer=-1;
        ui.fade.color=Color.clear;ui.Say("",null);
        ui.objective.text="先到前方樹幹後，與巴萬、比胡會合";
        ui.hint.text="沿黃色箭頭走到樹幹後方 · WASD 移動";
        player.canMove=player.canLook=true;weapons.gameObject.SetActive(true);
        if(coverGuide)coverGuide.ShowAhead(player.transform.position,new[]{OpeningPosition},player.transform);
        while(true)
        {
            UpdateCompanionCover();
            if(CompanionsInCover&&Flat(player.transform.position,OpeningPosition)<1.25f&&OpeningShielded())break;
            yield return null;
        }
        OpeningPrepared=true;if(coverGuide)coverGuide.Hide();
        player.canMove=false;ui.objective.text="躲在樹幹後，確認前往學校的路線";
        yield return Say(bawan,"去學校的路被前面的巡警擋住了。先躲在樹幹後。",lineSeconds);
        yield return Say(pihu,weapons.Selected==Chapter3Weapons.Weapon.Knife?
            "沿著左邊的樹幹和岩石靠近。後面還有掩體，別迎著槍口衝。":
            "槍打完一發，就要再裝一發。裝填時躲好。",lineSeconds);
        StartFighting();
    }
    bool OpeningShielded()
    {
        Vector3 target=player.view.transform.position-Vector3.up*.38f;
        foreach(var enemy in localEnemies)
            if(enemy&&!enemy.Defeated&&ClearLine(enemy.AimPoint,target,enemy))return false;
        return localEnemies.Count>0;
    }
    public Vector3[] CoverRoute(int officer)
    {
        Vector3[] points;
        if(officer<=0)points=new[]{new Vector3(-5.9f,0,-6.1f),new Vector3(-5.9f,0,6.9f),
            new Vector3(-3.4f,0,7.4f),new Vector3(0,0,7)};
        else if(officer==1)points=new[]{new Vector3(-5.2f,0,8.4f),new Vector3(-5.2f,0,13.3f),
            new Vector3(-3.4f,0,14.2f),new Vector3(-2,0,14)};
        else points=new[]{new Vector3(0,0,15.2f),new Vector3(5.6f,0,15.2f),
            new Vector3(5.6f,0,20.5f),new Vector3(3.4f,0,21.4f),new Vector3(2,0,21)};
        for(int i=0;i<points.Length;i++)points[i]=WorldPoint(points[i]);
        return points;
    }
    void UpdateCoverGuide()
    {
        if(!coverGuide)return;
        if(weapons.Selected!=Chapter3Weapons.Weapon.Knife)
        {if(coverGuide.VisibleMarks>0)coverGuide.Hide();return;}
        int next=-1;
        for(int i=0;i<localEnemies.Count;i++)if(localEnemies[i]&&!localEnemies[i].Defeated){next=i;break;}
        if(next==guidedOfficer)return;guidedOfficer=next;
        if(next<0){coverGuide.Hide();return;}
        coverGuide.ShowAhead(player.transform.position,CoverRoute(next),player.transform);
    }
    void HideCoverGuide()
    {if(coverGuide)coverGuide.Hide();guidedOfficer=-1;}
}
