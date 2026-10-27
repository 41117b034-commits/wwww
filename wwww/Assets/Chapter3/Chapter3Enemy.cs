using UnityEngine;

[DefaultExecutionOrder(4400)]
public sealed class Chapter3Enemy : MonoBehaviour
{
    public Chapter3Combat combat;
    public Chapter2Actor actor;
    public Chapter2Rifle rifle;
    public bool advances;
    // Mission staging may move a guard before allowing it to aim. Existing
    // mountain and rescue encounters keep their original enabled default.
    public bool AttackEnabled=true;
    public bool HoldingFire=>!Defeated&&Time.time<holdFireUntil;
    public float SafeWindowSeconds=>!Defeated&&AttackEnabled&&!Telegraphing?Mathf.Max(0,Mathf.Max(holdFireUntil,nextAttack)-Time.time):0;
    // School cover mission targets the moving escort. Null preserves the
    // original mountain encounter's player-only targeting.
    public Transform escortTarget;
    public int Health { get; private set; }=100;
    public bool Defeated=>Health<=0;
    public bool Telegraphing { get; private set; }
    public int Shots { get; private set; }
    public bool LegWounded { get; private set; }
    public Vector3 AimPoint=>actor&&actor.Rig&&actor.Rig.Hips&&actor.Rig.Head?
        Vector3.Lerp(actor.Rig.Hips.position,actor.Rig.Head.position,.4f):transform.position+Vector3.up*1.05f;
    public float attackInterval=4.5f;
    public float engagementRange=32;
    float nextAttack,raisedAt,hitAt=-20,holdFireUntil;
    bool attackGranted;
    bool started,restorePending;
    Vector3 restoredFallSource;
    AudioSource reportAudio;
    Transform escortAim;
    Vector3 fallGrip,fallenWeaponPosition;
    Quaternion fallRotation,fallenWeaponRotation;
    Renderer[] weaponRenderers;
    void Start()
    {
        started=true;nextAttack=Time.time+2;if(!restorePending)Health=100;
        Chapter3EnemyHitZone.Install(this);
        if(!GetComponent<Chapter3EnemyWoundPose>())gameObject.AddComponent<Chapter3EnemyWoundPose>();
        reportAudio=gameObject.AddComponent<AudioSource>();reportAudio.playOnAwake=false;reportAudio.spatialBlend=.85f;reportAudio.maxDistance=45;reportAudio.volume=.45f;
        if(rifle){rifle.target=null;rifle.aimTarget=combat.playerTarget;rifle.aim=0;rifle.lowered=.8f;}
        if(restorePending)
        {
            restorePending=false;
            if(Defeated)BeginDefeat(restoredFallSource);
            else if(LegWounded)HoldFire(1.6f);
        }
    }
    void Update()
    {
        if(!AttackEnabled){CancelAim();attackGranted=false;return;}
        if(HoldingFire)
        {
            CancelAim();
            // The staged pause already supplies the reaction window. Preserve
            // attack eligibility so release does not add a second entry delay.
            attackGranted=combat&&combat.Fighting&&combat.CanAttack(this);return;
        }
        if(!combat||!combat.Fighting||Defeated)return;
        if(!combat.CanAttack(this))
        {attackGranted=false;Telegraphing=false;if(rifle){rifle.aim=0;rifle.lowered=.8f;}return;}
        bool knife=combat.weapons.Selected==Chapter3Weapons.Weapon.Knife;
        if(!attackGranted){attackGranted=true;nextAttack=Time.time+(knife?3.5f:2);}
        Vector3 target=escortTarget?escortTarget.position+Vector3.up*1.05f:combat.playerTarget.position;
        actor.Face(target);
        if(rifle)
        {
            rifle.target=escortTarget?escortTarget.GetComponent<Chapter2Actor>():null;
            if(escortTarget&&!rifle.target)
            {
                if(!escortAim){escortAim=new GameObject("同伴胸前瞄準點").transform;escortAim.SetParent(transform,false);}
                escortAim.position=target;rifle.aimTarget=escortAim;
            }
            else rifle.aimTarget=rifle.target?null:combat.playerTarget;
        }
        float distance=Vector3.Distance(AimPoint,target);
        bool visible=distance<engagementRange&&combat.ClearLine(AimPoint,target,this);
        if(advances&&!LegWounded&&distance>2.6f&&distance<22&&!Telegraphing)
        {
            Vector3 direction=Vector3.ProjectOnPlane(combat.player.transform.position-transform.position,Vector3.up).normalized;
            // Short advance along the open lane, never walk through the player's cover.
            if(!AdvanceBlocked(direction))
                transform.position+=direction*Time.deltaTime*.95f;
        }
        if(!visible){Telegraphing=false;if(rifle)rifle.aim=0;return;}
        if(!Telegraphing&&Time.time>=nextAttack){Telegraphing=true;raisedAt=Time.time;}
        if(rifle)rifle.aim=Telegraphing?Mathf.Clamp01((Time.time-raisedAt)*2):.15f;
        if(Telegraphing&&Time.time-raisedAt>=(knife?2.2f:1.45f))
        {
            // Recheck actual cover when the shot lands, not only when aiming starts.
            if(combat.ClearLine(AimPoint,target,this))
            {
                Shots++;if(rifle)rifle.Fire();if(combat.weapons.shotClip)reportAudio.PlayOneShot(combat.weapons.shotClip);
                if(escortTarget&&combat.SchoolEncounter)
                {if(!combat.SchoolEncounter.CampusAllyUnderFire(escortTarget))combat.SchoolEncounter.EscortHit();}
                else combat.Damage(20,AimPoint);
            }
            Telegraphing=false;nextAttack=Time.time+(knife?Mathf.Max(6.5f,attackInterval):attackInterval);
        }
    }
    bool AdvanceBlocked(Vector3 direction)
    {
        // Split bone hit zones can lie ahead of the actor root during a step.
        // They are part of this officer, never an obstacle to its own advance.
        foreach(var hit in Physics.SphereCastAll(transform.position+Vector3.up*.7f,.3f,direction,.7f,~0,QueryTriggerInteraction.Ignore))
            if(!hit.collider.transform.IsChildOf(transform))return true;
        return false;
    }
    public void HoldFire(float seconds)
    {
        if(Defeated||seconds<=0||float.IsNaN(seconds))return;
        holdFireUntil=Mathf.Max(holdFireUntil,Time.time+seconds);
        nextAttack=Mathf.Max(nextAttack,holdFireUntil);
        CancelAim();
    }
    // Checkpoint restoration is only accepted on a fresh instance, before its
    // rig/weapon Start methods run. It never awards a kill or emits hit feedback.
    public bool RestoreEncounterState(int health,bool legWounded,Vector3 fallSource)
    {
        if(started||restorePending)return false;
        Health=Mathf.Clamp(health,0,100);LegWounded=legWounded;
        restoredFallSource=fallSource;restorePending=true;
        return true;
    }
    void CancelAim()
    {Telegraphing=false;raisedAt=0;if(rifle){rifle.aim=0;rifle.lowered=.8f;}}
    void OnDisable(){holdFireUntil=0;attackGranted=false;CancelAim();}
    public void HitByRifle(Chapter3EnemyHitZone zone,Vector3 from)
    {
        if(!zone||zone.Enemy!=this||!isActiveAndEnabled||!combat||!combat.Fighting||Defeated)return;
        bool leg=zone.Region==Chapter3EnemyHitZone.BodyRegion.Leg;
        Hit(leg?50:100,from);
        if(leg&&!Defeated)
        {
            LegWounded=true;
            HoldFire(1.6f);
        }
    }
    public void Hit(int damage,Vector3 from)
    {
        if(!isActiveAndEnabled||!combat||!combat.Fighting||Defeated)return;
        Health=Mathf.Max(0,Health-damage);hitAt=Time.time;Telegraphing=false;nextAttack=Time.time+2.2f;
        if(Defeated)
        {
            BeginDefeat(from);combat.EnemyDefeated();
        }
    }
    void BeginDefeat(Vector3 from)
    {
        if(rifle)
        {
            rifle.aim=0;rifle.lowered=1;rifle.enabled=false;
            if(rifle.muzzleFlash)rifle.muzzleFlash.SetActive(false);
            if(rifle.weapon&&actor.Rig&&actor.Rig.Hips)
            {
                var hips=actor.Rig.Hips;
                fallGrip=Quaternion.Inverse(hips.rotation)*(rifle.weapon.position-hips.position);
                fallRotation=Quaternion.Inverse(hips.rotation)*rifle.weapon.rotation;
                Vector3 direction=Vector3.ProjectOnPlane(transform.position-from,Vector3.up).normalized;
                if(direction.sqrMagnitude<.01f)direction=transform.forward;
                fallenWeaponPosition=transform.position+direction*.55f+Vector3.Cross(Vector3.up,direction)*.48f;
                fallenWeaponRotation=Quaternion.LookRotation(direction,Vector3.up)*Quaternion.Euler(0,0,88);
                weaponRenderers=rifle.weapon.GetComponentsInChildren<Renderer>();
            }
        }
        foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
        actor.BeginFall(from);
    }
    void LateUpdate()
    {
        if(Defeated&&rifle&&rifle.weapon&&actor.Rig&&weaponRenderers!=null)
        {
            // Follow the falling skeleton, then settle the released rifle next to
            // the body. Disabling the aiming script alone leaves it in mid-air.
            var hips=actor.Rig.Hips;float settle=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,1,actor.FallProgress));
            rifle.weapon.SetPositionAndRotation(Vector3.Lerp(hips.position+hips.rotation*fallGrip,fallenWeaponPosition,settle),
                Quaternion.Slerp(hips.rotation*fallRotation,fallenWeaponRotation,settle));
            float low=float.PositiveInfinity;
            foreach(var renderer in weaponRenderers)if(renderer.enabled&&renderer.gameObject.activeInHierarchy)low=Mathf.Min(low,renderer.bounds.min.y);
            if(!float.IsInfinity(low))rifle.weapon.position+=Vector3.up*(settle>=1?.035f-low:Mathf.Max(0,.035f-low));
        }
        if(!Defeated&&!LegWounded&&actor.Rig&&actor.Rig.Hips)
        {
            float t=(Time.time-hitAt)/.4f;
            if(t>=0&&t<1)actor.Rig.Hips.rotation=Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI)*15,transform.right)*actor.Rig.Hips.rotation;
        }
    }
}
