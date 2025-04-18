using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;

public class AIEnemyCarEntity : MonoBehaviour
{
    public const string P_Target = "Target";
    Transform m_Target;
    Rigidbody m_Rigidbody; 
    [SerializeField]public GameObject DestroyPt;
    [SerializeField] int m_CombatUnitId = 5;
    public enum CombatFlag
    {
        Player,
        Enemy
    }
    /// <summary>
    /// 阵营
    /// </summary>
    protected CombatFlag CampFlag { get; private set; }
    public CombatUnitTable CombatUnitRow { get; private set; }
    public virtual int Hp { get; protected set; }
    public virtual int ID { get; set; }
    public virtual Vector3 HitPoint { get=>transform.position + Vector3.up; }
    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        GetComponent<Animation>().Play("Running");
        gameObject.tag = "Enemy";
        CampFlag = CombatFlag.Enemy;
        var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        CombatUnitRow = combatUnitTb.GetDataRow(m_CombatUnitId);
    }
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer(CampFlag == CombatFlag.Player ? "Player" : "Enemy");
        Hp = CombatUnitRow.Hp;
    }
    private void FixedUpdate()
    {
        if (m_Target != null)
        {
            transform.LookAt(m_Target);
            var offsetPos = m_Target.position - this.transform.position;
            offsetPos.y = 0;
            var moveDir = Vector3.Normalize(offsetPos);
            var targetVelocity = CombatUnitRow.MoveSpeed * moveDir;
            transform.position += CombatUnitRow.MoveSpeed * Time.deltaTime * transform.TransformDirection(Vector3.forward);
            //m_Rigidbody.velocity = Vector3.Lerp(m_Rigidbody.velocity, targetVelocity, 1 / math.distancesq(targetVelocity, m_Rigidbody.velocity));
        }
    }
    public  bool ApplyDamage2(CombatUnitEntity attacker, int damgeValue)
    {
        bool bekilled = ApplyDamage(attacker, damgeValue);
        if (Hp > 0)
        {
            m_Rigidbody.velocity = Vector3.Normalize(transform.position - attacker.CachedTransform.position) * 10f;
        }
        return bekilled;
    }

    public void SetTarget(CombatUnitEntity target)
    {
        m_Target = target.CachedTransform;
        ApplyDamage2(target, CombatUnitRow.Damage);
    }
    
    public virtual bool Attack(CombatUnitEntity unit)
    {
        return Attack(unit, CombatUnitRow.Damage);
    }
    
    internal bool Attack(CombatUnitEntity entity, int v)
    {
        return ApplyDamage(entity, v);
    }

    protected virtual bool ApplyDamage(CombatUnitEntity attacker, int damgeValue)
    {
        if (Hp <= 0) return false;
        Hp -= damgeValue;
        var hitPoint = HitPoint;
        var bloodFxParms = EntityParams.Create(hitPoint);
        GF.Entity.ShowEffect("Effect/BloodExplosion", bloodFxParms, 1.5f);
        var damageFxParms = EntityParams.Create(hitPoint);
        GF.Entity.ShowPopText(damageFxParms, damgeValue.ToString(), hitPoint + Vector3.up, 0.5f, 7);
        if (Hp <= 0)
        {
            OnBeKilled();
            return true;
        }
        return false;
    }
    
    protected virtual void OnBeKilled()
    {
        //GF.Entity.HideEntity(this.Entity);
        if(CampFlag == CombatFlag.Enemy)
        {
            GF.DataModel.GetDataModel<PlayerDataModel>().Coins++;
        }
        //敌人死后发送事件通知LevelEntity根据敌人ID从m_AIEnemyCarEntityList列表里移除
        GF.Event.Fire(this, NotificationDeletionEnemyEventArgs.Create(GFNotificationDeletionEnemy.killEnemy,ID));
        //通过框架方法添加特效和删除特效
        //Destroy(Instantiate(DestroyPt, transform.position + Vector3.up, Quaternion.identity), 5);
        ShootFx(transform.position, transform.position + Vector3.up);
        Destroy(gameObject);
    }
    
    private void ShootFx(Vector3 position, Vector3 hitPoint)
    {
        var fxParams = EntityParams.Create(position, Quaternion.LookRotation(Vector3.Normalize(hitPoint - position)).eulerAngles);
        float duration = 5;
        fxParams.OnShowCallback = SetShootParticleDuration;
        GF.Entity.ShowEffect("Effect/DeathSplat", fxParams, duration);
    }
    
    private void SetShootParticleDuration(EntityLogic obj)
    {
        var fx = obj.GetComponent<ParticleSystem>();
        var fxSettings = fx.main;
        //fxSettings.duration = (obj as ParticleEntity).LifeTime;
        fx.Play(true);
    }
}