using BepInEx.Configuration;
using MagicalActual;
using UnityEngine;

namespace WeaponAdjustment.Weapons
{
    public class Warrior
    {
        private static WeaponSpec Spec { get; set; }

        public static void Init(ConfigFile cfg)
        {
            Spec = new WeaponSpec
            {
                Name = "sword",
                ClassType = typeof(Swordboard),
                Config = new PoseConfig(cfg, "Warrior"),
                GetWeapon = () => Weapons.Weapon<Swordboard>("sword"),
                LengthAxis = Vector3.up,
                BlockInput = new[] { "doSwordTriggerClick", "doDoubleTriggerClick" },
            };
            Weapons.Register(Spec);
        }
    }
}