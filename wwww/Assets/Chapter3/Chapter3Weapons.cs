using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class Chapter3Weapons : MonoBehaviour
{
    public enum Weapon { Knife, Rifle }
    public Chapter3Combat combat;
    public Transform knife, rifle, rightHand, leftHand, bolt, muzzle;
    public Transform cartridge,spentCase;
    public GameObject flash;
    public AudioSource audioSource;
    public AudioClip shotClip;
    public Weapon Selected { get; private set; } = Weapon.Rifle;
    public bool Loaded { get; private set; } = true;
    public bool Reloading { get; private set; }
    public bool Aiming { get; private set; }
    public bool Ready => combat && combat.Fighting && !Reloading && Time.time>=busyUntil &&
        !(combat.SchoolEncounter&&combat.SchoolEncounter.Rescue&&combat.SchoolEncounter.Rescue.Supporting);
    public int Reserve { get; private set; } = 8;
    public int Shots { get; private set; }
    public int Slashes { get; private set; }
    public int Reloads { get; private set; }
    public string Feedback { get; private set; }
    public float ReloadProgress { get; private set; }
    float busyUntil, firedAt=-10, swingAt=-10, feedbackUntil, aimWeight;
    Vector3 boltHome;
    bool initialized;
    void Awake(){Initialize();}
    void Initialize(){if(initialized)return;initialized=true;if(bolt)boltHome=bolt.localPosition;}
    public void ResetEquipment()
    {
        Initialize();StopAllCoroutines();Loaded=true;Reserve=8;Reloading=false;Aiming=false;aimWeight=0;
        // Debug jumps choose a rifle only when no story choice has been made.
        if(combat.flow&&!combat.flow.CarriedWeapon.HasValue)combat.flow.Carry(Weapon.Rifle);
        Selected=combat.flow?combat.flow.CarriedWeapon.Value:Weapon.Rifle;busyUntil=0;firedAt=swingAt=-10;Feedback="";ReloadProgress=0;
        Shots=Slashes=Reloads=0;gameObject.SetActive(true);if(flash)flash.SetActive(false);
        if(bolt)bolt.localPosition=boltHome;
        if(cartridge)cartridge.gameObject.SetActive(false);if(spentCase)spentCase.gameObject.SetActive(false);
    }
    public void Suspend()
    {StopAllCoroutines();Reloading=Aiming=false;if(flash)flash.SetActive(false);if(cartridge)cartridge.gameObject.SetActive(false);if(spentCase)spentCase.gameObject.SetActive(false);gameObject.SetActive(false);}
    void Update()
    {
        if(!combat||!combat.Fighting){Aiming=false;return;}
        var k=Keyboard.current;var m=Mouse.current;
        if(k?.escapeKey.wasPressedThisFrame==true){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return;}
        bool captured=Cursor.lockState==CursorLockMode.Locked;
        if(k?.digit1Key.wasPressedThisFrame==true)Select(Weapon.Knife);
        if(k?.digit2Key.wasPressedThisFrame==true)Select(Weapon.Rifle);
        Aiming=Selected==Weapon.Rifle&&!Reloading&&m?.rightButton.isPressed==true;
        if(k?.rKey.wasPressedThisFrame==true)Reload();
        if(m?.leftButton.wasPressedThisFrame==true)
        {
            if(!captured){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;}
            Attack();
        }
    }
    public bool Select(Weapon weapon)
    {
        if(!Ready)return false;
        if(combat.flow&&combat.flow.CarriedWeapon.HasValue&&weapon!=combat.flow.CarriedWeapon.Value)
        {Notice("只能持有一種武器，你沒有攜帶這把武器。",3);return false;}
        Selected=weapon;Aiming=false;busyUntil=Time.time+.22f;return true;
    }
    public bool Attack()
    {
        if(!Ready){NoticeNotReady();return false;}
        if(Selected==Weapon.Knife){Slashes++;swingAt=Time.time;busyUntil=Time.time+.68f;StartCoroutine(Slash());return true;}
        var camera=combat.player.view;
        // Bone-following hit zones and moved cover must be in the same physics
        // pose as the most recently rendered frame, even between fixed steps.
        Physics.SyncTransforms();
        bool traced=combat.Trace(camera.transform.position,camera.transform.forward,65,out RaycastHit hit);
        if(traced&&hit.collider.GetComponentInParent<Chapter3CompanionReaction>())
        {
            hit.collider.GetComponentInParent<Chapter3CompanionReaction>().WarnAgainstAttack();
            Notice("前方是同伴，請移開準星。");busyUntil=Time.time+.4f;return false;
        }
        if(traced&&hit.collider.GetComponentInParent<Chapter3Bystander>())
        {Notice("不要向運動會的人群開槍，沿任務路線前進。",3);busyUntil=Time.time+.4f;return false;}
        if(!Loaded){Notice(Reserve>0?"空膛，按 R 裝入一發。":"彈藥用盡，到身後木箱按 E 補給。");busyUntil=Time.time+.25f;return false;}
        Loaded=false;Shots++;firedAt=Time.time;busyUntil=Time.time+.48f;
        Notice("已射出一發，按 R 裝填。");
        if(audioSource&&shotClip)audioSource.PlayOneShot(shotClip,.65f);
        if(traced)
        {
            var enemy=hit.collider.GetComponentInParent<Chapter3Enemy>();
            if(enemy)
            {
                var zone=hit.collider.GetComponent<Chapter3EnemyHitZone>();
                if(zone)enemy.HitByRifle(zone,camera.transform.position);
                else enemy.Hit(100,camera.transform.position);
                Notice(enemy.Defeated?"命中，警察倒下":"命中腿部，警察受傷蹲低");
            }
            else Notice("擊中掩體");
        }
        return true;
    }
    IEnumerator Slash()
    {
        yield return new WaitForSeconds(.22f);
        if(!combat.Fighting)yield break;
        var view=combat.player.view.transform;
        // A swept fan gives the visible cut some width; each swing damages one target once.
        Chapter3Enemy victim=null;float nearest=2.2f;
        foreach(var enemy in combat.Enemies)
        {
            if(!enemy||enemy.Defeated)continue;
            Vector3 to=enemy.AimPoint-view.position;float d=to.magnitude;
            if(d>nearest||Vector3.Angle(view.forward,to)>42)continue;
            if(combat.Trace(view.position,to.normalized,d+.1f,out var hit)&&hit.collider.GetComponentInParent<Chapter3Enemy>()==enemy)
            {victim=enemy;nearest=d;}
        }
        Chapter3CompanionReaction friend=null;
        foreach(var companion in combat.Companions)
        {
            if(!companion)continue;
            Vector3 to=companion.AimPoint-view.position;float d=to.magnitude;
            if(d>nearest||Vector3.Angle(view.forward,to)>42)continue;
            if(combat.Trace(view.position,to.normalized,d+.1f,out var hit)&&hit.collider.GetComponentInParent<Chapter3CompanionReaction>()==companion)
            {friend=companion;nearest=d;}
        }
        if(friend){friend.KnifeHit();}
        else if(victim){victim.Hit(60,view.position);Notice("獵刀命中");}else Notice("沒有擊中，靠近再揮刀。");
    }
    void NoticeNotReady()
    {
        if(!combat||!combat.Fighting)return;
        if(combat.SchoolEncounter&&combat.SchoolEncounter.Rescue&&combat.SchoolEncounter.Rescue.Supporting)
            Notice("正在扶行，放下比胡後才能使用武器。");
        else if(Reloading)Notice("正在裝填，完成後再按左鍵射擊。");
        else Notice(Selected==Weapon.Rifle?"槍械復位中，稍後再操作。":"正在揮刀，稍後再操作。",1);
    }
    public bool Reload()
    {
        if(!Ready){NoticeNotReady();return false;}
        if(Selected!=Weapon.Rifle||Loaded||Reserve<=0)return false;
        Reloading=true;Aiming=false;StartCoroutine(LoadOne());return true;
    }
    IEnumerator LoadOne()
    {
        // Open bolt, eject, insert exactly one cartridge, close. Ammo commits at completion.
        float duration=2.6f;
        for(float t=0;t<duration;t+=Time.deltaTime){ReloadProgress=t/duration;yield return null;}
        if(combat.Fighting){Reserve--;Loaded=true;Reloads++;Notice("已裝入一發");}
        Reloading=false;ReloadProgress=0;if(bolt)bolt.localPosition=boltHome;
    }
    public bool Refill()
    {if(!combat.Fighting||Selected!=Weapon.Rifle||Reloading||Reserve>=8)return false;Reserve=8;Notice("取得備用彈藥，按 R 裝填。");return true;}
    public string Controls => Selected==Weapon.Knife ? "左鍵揮刀 · Esc 放開滑鼠" : "左鍵射擊 · 右鍵瞄準 · R 單發裝填 · Esc 放開滑鼠";
    void Notice(string text,float seconds=1.6f){Feedback=text;feedbackUntil=Time.time+seconds;}
    void LateUpdate()
    {
        if(!combat||!combat.Fighting)return;
        if(Time.time>feedbackUntil)Feedback="";
        float aimTarget=Aiming?1:0;aimWeight=Mathf.MoveTowards(aimWeight,aimTarget,Time.deltaTime*6);
        if(!combat.player.IsVR)combat.player.view.fieldOfView=Mathf.Lerp(60,43,aimWeight);
        knife.gameObject.SetActive(Selected==Weapon.Knife);rifle.gameObject.SetActive(Selected==Weapon.Rifle);
        float swing=Mathf.Clamp01((Time.time-swingAt)/.58f);
        float arc=Mathf.Sin(swing*Mathf.PI);
        knife.localPosition=new Vector3(.25f-arc*.48f,-.17f+arc*.10f,.60f+arc*.12f);
        knife.localRotation=Quaternion.Euler(-12+arc*38,-15-arc*32,-20+arc*104);
        float recoil=Mathf.Max(0,1-(Time.time-firedAt)/.25f);
        float reloadTilt=Reloading?Mathf.Sin(ReloadProgress*Mathf.PI):0;
        rifle.localPosition=Vector3.Lerp(new Vector3(.22f,-.22f,.85f),new Vector3(0,-.115f,.95f),aimWeight)+new Vector3(0,-reloadTilt*.08f,-recoil*.065f);
        rifle.localRotation=Quaternion.Euler(-recoil*3,reloadTilt*-13,reloadTilt*18);
        if(flash)flash.SetActive(Time.time-firedAt<.055f);
        if(bolt)
        {
            float open=Reloading?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.26f,ReloadProgress))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.94f,ReloadProgress))):0;
            bolt.localPosition=boltHome+Vector3.back*(open*.10f);
        }
        if(cartridge)
        {
            cartridge.gameObject.SetActive(Reloading&&ReloadProgress>.36f&&ReloadProgress<.74f);
            cartridge.localPosition=Vector3.Lerp(new Vector3(.09f,.14f,-.30f),new Vector3(0,.065f,-.24f),Mathf.InverseLerp(.36f,.70f,ReloadProgress));
        }
        if(spentCase)
        {
            float t=Mathf.InverseLerp(.22f,.38f,ReloadProgress);
            spentCase.gameObject.SetActive(Reloading&&ReloadProgress>.22f&&ReloadProgress<.38f);
            spentCase.localPosition=new Vector3(.04f+t*.4f,.08f+Mathf.Sin(t*Mathf.PI)*.12f-t*.15f,-.28f);spentCase.localRotation=Quaternion.Euler(t*180,t*90,0);
        }
        if(rightHand)
        {
            rightHand.gameObject.SetActive(true);
            Vector3 grip=Selected==Weapon.Knife?knife.TransformPoint(new Vector3(0,-.04f,0)):rifle.TransformPoint(new Vector3(.015f,-.055f,-.30f));
            // Authored hand mesh is centered on the closed grip, palm on the handle's right.
            rightHand.SetPositionAndRotation(grip,Selected==Weapon.Knife?knife.rotation:rifle.rotation*Quaternion.Euler(-78,0,0));
            if(Reloading)
            {
                float reach=Mathf.Sin(ReloadProgress*Mathf.PI);
                rightHand.position=Vector3.Lerp(grip,rifle.TransformPoint(new Vector3(.085f,.06f,-.26f)),reach);
            }
        }
        if(leftHand)
        {
            leftHand.gameObject.SetActive(Selected==Weapon.Rifle);
            leftHand.SetPositionAndRotation(rifle.TransformPoint(new Vector3(0,-.045f,.14f)),rifle.rotation*Quaternion.Euler(-90,0,90));
        }
    }
}
