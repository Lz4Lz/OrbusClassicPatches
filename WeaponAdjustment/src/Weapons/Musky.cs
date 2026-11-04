using BepInEx.Configuration;
using MagicalActual;
using UnityEngine;

namespace WeaponAdjustment.Weapons
{
    public class Musky
    {
        private static WeaponSpec Spec { get; set; }

        public static void Init(ConfigFile cfg)
        {
            Spec = new WeaponSpec
            {
                Name = "gun",
                ClassType = typeof(Orbhealer),
                Config = new PoseConfig(cfg, "Musketeer"),
                GetWeapon = () => Weapons.Weapon<Orbhealer>("leftBow"),
                LengthAxis = Vector3.forward,
                BlockInput = new[] { "doPrimaryTriggerClick", "doPrimaryTriggerUnClick" },
            };
            Weapons.Register(Spec);
        }
    }
}