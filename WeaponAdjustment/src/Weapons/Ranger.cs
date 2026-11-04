using BepInEx.Configuration;
using MagicalActual;
using UnityEngine;

namespace WeaponAdjustment.Weapons
{
    public class Ranger
    {
        private static WeaponSpec Spec { get; set; }

        public static void Init(ConfigFile cfg)
        {
            Spec = new WeaponSpec
            {
                Name = "bow",
                ClassType = typeof(Archer),
                Config = new PoseConfig(cfg, "Ranger"),
                GetWeapon = () => Weapons.Weapon<Archer>("bow"),
                LengthAxis = Vector3.up,
                BlockInput = new[] { "doTriggerClick", "doTriggerUnClick", "doGrip", "doUnGrip"},
                PositionOnly = true,
            };
            Weapons.Register(Spec);
        }
    }
}