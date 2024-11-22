using ArcadeVP;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace AAAGame.Scripts.Entity
{
    public class PlayerCarEntity : CombatUnitEntity
    {
        public const string P_OnBeKilled = "OnBeKilled";
        const string EnemyTag = "Enemy";
        private PlayerDataModel m_PlayerData;
        private ArcadeVehicleController m_ArcadeVehicleController;
        private bool mCtrlable;
        
        Action m_OnPlayerBeKilled = null;
        
        float m_DamageTimer;
        float m_DamageInterval = 0.25f;
        public bool Ctrlable
        {
            get => mCtrlable;
            set
            {
                mCtrlable = value;
                GF.StaticUI.JoystickEnable = mCtrlable;
            }
        }
        public override int Hp { get => m_PlayerData.Hp; protected set => m_PlayerData.Hp = value; }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (CombatUnitRow == null) return;
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(CachedTransform.position, CombatUnitRow.AttackRadius);
        }
#endif
        
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_ArcadeVehicleController = GetComponent<ArcadeVehicleController>();
            m_PlayerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            m_PlayerData.SetData(PlayerDataType.Hp, CombatUnitRow.Hp);
            m_OnPlayerBeKilled = Params.Get<VarAction>(P_OnBeKilled);
            GF.StaticUI.Joystick.OnPointerUpCallback += OnJoystickUp;
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            GF.StaticUI.Joystick.OnPointerUpCallback -= OnJoystickUp;
            base.OnHide(isShutdown, userData);
        }


        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            //Jump(elapseSeconds);//跳跃
        }
        
        private void OnJoystickUp()
        {
            
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.collider.CompareTag(EnemyTag)) return;
            var enemey = collision.gameObject.GetComponent<AIEnemyCarEntity>();
            enemey.Attack(this);
        }
        private void OnCollisionStay(Collision collision)
        {
            if (!collision.collider.CompareTag(EnemyTag) || (m_DamageTimer += Time.deltaTime) < m_DamageInterval) return;
            var enemey = collision.gameObject.GetComponent<AIEnemyCarEntity>();
            enemey.Attack(this);
            m_DamageTimer = 0;
        }
        
        /// <summary>
        /// 死亡之后
        /// </summary>
        protected override void OnBeKilled()
        {
            Ctrlable = false;
            //m_Animator.SetBool("BeKilled", true);
            GF.LogInfo("玩家死亡");
            m_OnPlayerBeKilled.Invoke();
        }
        /// <summary>
        /// 显示射击特效
        /// </summary>
        /// <param name="position"></param>
        /// <param name="hitPoint"></param>
        private void ShootFx(Vector3 position, Vector3 hitPoint)
        {
            var fxParams = EntityParams.Create(position, Quaternion.LookRotation(Vector3.Normalize(hitPoint - position)).eulerAngles);
            float duration = Vector3.Distance(position, hitPoint) * 0.01f;
            fxParams.OnShowCallback = SetShootParticleDuration;
            GF.Entity.ShowEffect("Effect/FireFx", fxParams, duration);
        }

        private void SetShootParticleDuration(EntityLogic obj)
        {
            var fx = obj.GetComponent<ParticleSystem>();
            var fxSettings = fx.main;
            fxSettings.duration = (obj as ParticleEntity).LifeTime;
            fx.Play(true);
        }
    }
}