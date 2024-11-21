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
    [SerializeField] int m_CombatUnitId = 1;
    CombatUnitTable m_CombatUnitRow;
    void Awake()
    {
        m_Rigidbody = GetComponent<Rigidbody>();
        GetComponent<Animation>().Play("Running");
        gameObject.tag = "Enemy";
    }
    void Start()
    {
        var combatUnitTb = GF.DataTable.GetDataTable<CombatUnitTable>();
        m_CombatUnitRow = combatUnitTb.GetDataRow(m_CombatUnitId);
    }
    private void FixedUpdate()
    {
        if (m_Target != null)
        {
            transform.LookAt(m_Target);
            var offsetPos = m_Target.position - this.transform.position;
            offsetPos.y = 0;
            var moveDir = Vector3.Normalize(offsetPos);
            var targetVelocity = m_CombatUnitRow.MoveSpeed * moveDir;
            m_Rigidbody.velocity = Vector3.Lerp(m_Rigidbody.velocity, targetVelocity, 1 / math.distancesq(targetVelocity, m_Rigidbody.velocity));
        }
    }
  //  protected  bool ApplyDamage(CombatUnitEntity attacker, int damgeValue)
//    {
       // bool bekilled = base.ApplyDamage(attacker, damgeValue);
        //if (Hp > 0)
       // {
        //    m_Rigidbody.velocity = Vector3.Normalize(CachedTransform.position - attacker.CachedTransform.position) * 10f;
       // }
       // return bekilled;
 //   }

    public void SetTarget(CombatUnitEntity target)
    {
        m_Target = target.CachedTransform;
    }
}