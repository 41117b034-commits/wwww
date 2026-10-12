using UnityEngine;

[DefaultExecutionOrder(4400)]
public sealed class Chapter3Enemy : MonoBehaviour
{
    public Chapter3Combat combat;
    public Chapter2Actor actor;
    public Chapter2Rifle rifle;
    public bool advances;
    public int Health { get; private set; }=100;
    public bool Defeated=>Health<=0;
    public bool Telegraphing { get; private set; }
    public int Shots { get; private set; }
    public Vector3 AimPoint=>transform.position+Vector3.up*1.05f;
    public float attackInterval=4.5f;
    float nextAttack,raisedAt,hitAt=-20;
    AudioSource reportAudio;
    void Start()
    {
        nextAttack=Time.time+2;Health=100;
        reportAudio=gameObject.AddComponent<AudioSource>();reportAudio.playOnAwake=false;reportAudio.spatialBlend=.85f;reportAudio.maxDistance=45;reportAudio.volume=.45f;
        if(rifle){rifle.target=null;rifle.aimTarget=combat.playerTarget;rifle.aim=0;rifle.lowered=.8f;}
    }
    void Update()
    {
        if(!combat||!combat.Fighting||Defeated)return;
        actor.Face(combat.player.transform.position);
        float distance=Vector3.Distance(AimPoint,combat.playerTarget.position);
        bool visible=distance<32&&combat.ClearLine(AimPoint,combat.playerTarget.position,this);
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
            if(combat.ClearLine(AimPoint,combat.playerTarget.position,this))
            {Shots++;if(rifle)rifle.Fire();if(combat.weapons.shotClip)reportAudio.PlayOneShot(combat.weapons.shotClip);combat.Damage(25,AimPoint);}
            Telegraphing=false;nextAttack=Time.time+attackInterval;
        }
    }
    public void Hit(int damage,Vector3 from)
    {
        if(!combat||!combat.Fighting||Defeated)return;
        Health=Mathf.Max(0,Health-damage);hitAt=Time.time;Telegraphing=false;nextAttack=Time.time+2.2f;
        if(Defeated)
        {
            if(rifle){rifle.aim=0;rifle.lowered=1;rifle.enabled=false;}
            foreach(var c in GetComponentsInChildren<Collider>())c.enabled=false;
            actor.BeginFall(from);combat.EnemyDefeated();
        }
    }
    void LateUpdate()
    {
        if(!Defeated&&actor.Rig&&actor.Rig.Hips)
        {
            float t=(Time.time-hitAt)/.4f;
            if(t>=0&&t<1)actor.Rig.Hips.rotation=Quaternion.AngleAxis(Mathf.Sin(t*Mathf.PI)*15,transform.right)*actor.Rig.Hips.rotation;
        }
    }
}
