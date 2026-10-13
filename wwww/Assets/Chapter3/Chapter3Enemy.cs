using UnityEngine;

[DefaultExecutionOrder(4400)]
public sealed class Chapter3Enemy : MonoBehaviour
{
    public Chapter3Combat combat;
    public Chapter2Actor actor;
    public Chapter2Rifle rifle;
    public bool advances;
    // School cover mission targets the moving escort. Null preserves the
    // original mountain encounter's player-only targeting.
    public Transform escortTarget;
    public int Health { get; private set; }=100;
    public bool Defeated=>Health<=0;
    public bool Telegraphing { get; private set; }
    public int Shots { get; private set; }
    public Vector3 AimPoint=>transform.position+Vector3.up*1.05f;
    public float attackInterval=4.5f;
    float nextAttack,raisedAt,hitAt=-20;
    AudioSource reportAudio;
    Vector3 fallGrip,fallenWeaponPosition;
    Quaternion fallRotation,fallenWeaponRotation;
    Renderer[] weaponRenderers;
    void Start()
    {
        nextAttack=Time.time+2;Health=100;
        reportAudio=gameObject.AddComponent<AudioSource>();reportAudio.playOnAwake=false;reportAudio.spatialBlend=.85f;reportAudio.maxDistance=45;reportAudio.volume=.45f;
        if(rifle){rifle.target=null;rifle.aimTarget=combat.playerTarget;rifle.aim=0;rifle.lowered=.8f;}
    }
    void Update()
    {
        if(!combat||!combat.Fighting||Defeated)return;
        Vector3 target=escortTarget?escortTarget.position+Vector3.up*1.05f:combat.playerTarget.position;
        actor.Face(target);
        if(rifle){rifle.aimTarget=escortTarget?null:combat.playerTarget;rifle.target=escortTarget?escortTarget.GetComponent<Chapter2Actor>():null;}
        float distance=Vector3.Distance(AimPoint,target);
        bool visible=distance<32&&combat.ClearLine(AimPoint,target,this);
        if(advances&&distance>2.6f&&distance<22&&!Telegraphing)
        {
            Vector3 direction=Vector3.ProjectOnPlane(combat.player.transform.position-transform.position,Vector3.up).normalized;
            // Short advance along the open lane, never walk through the player's cover.
            if(!Physics.SphereCast(transform.position+Vector3.up*.7f,.3f,direction,out _,.7f,~0,QueryTriggerInteraction.Ignore))
                transform.position+=direction*Time.deltaTime*.95f;
        }
        if(!visible){Telegraphing=false;if(rifle)rifle.aim=0;return;}
        if(!Telegraphing&&Time.time>=nextAttack){Telegraphing=true;raisedAt=Time.time;}
        if(rifle)rifle.aim=Telegraphing?Mathf.Clamp01((Time.time-raisedAt)*2):.15f;
        if(Telegraphing&&Time.time-raisedAt>=1.45f)
        {
            // Recheck actual cover when the shot lands, not only when aiming starts.
            if(combat.ClearLine(AimPoint,target,this))
            {
                Shots++;if(rifle)rifle.Fire();if(combat.weapons.shotClip)reportAudio.PlayOneShot(combat.weapons.shotClip);
                if(escortTarget&&combat.SchoolEncounter)combat.SchoolEncounter.EscortHit();else combat.Damage(25,AimPoint);
            }
            Telegraphing=false;nextAttack=Time.time+attackInterval;
        }
    }
    public void Hit(int damage,Vector3 from)
    {
        if(!combat||!combat.Fighting||Defeated)return;
        Health=Mathf.Max(0,Health-damage);hitAt=Time.time;Telegraphing=false;nextAttack=Time.time+2.2f;
        if(Defeated)
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
            actor.BeginFall(from);combat.EnemyDefeated();
        }
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
        if(!Defeated&&actor.Rig&&actor.Rig.Hips)
        {
            float t=(Time.time-hitAt)/.4f;
            if(t>=0&&t<1)actor.Rig.Hips.rotation=Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI)*15,transform.right)*actor.Rig.Hips.rotation;
        }
    }
}
