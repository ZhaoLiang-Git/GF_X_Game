using ArcadeVP;
using UnityEngine;

namespace AAAGame.Scripts.Entity
{
    public class PlayerCarEntity : CombatUnitEntity
    {
        public const string P_OnBeKilled = "OnBeKilled";
        private ArcadeVehicleController m_ArcadeVehicleController;
        private bool mCtrlable;
        public bool Ctrlable
        {
            get => mCtrlable;
            set
            {
                mCtrlable = value;
                GF.StaticUI.JoystickEnable = mCtrlable;
            }
        }
        
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_ArcadeVehicleController = GetComponent<ArcadeVehicleController>();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

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
    }
}