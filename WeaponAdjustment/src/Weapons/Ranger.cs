using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace WeaponAdjustment.Weapons
{
    public class Ranger
    {
        internal static WeaponSpec Spec { get; private set; }

        public static void Init(ConfigFile cfg)
        {
            Spec = new WeaponSpec
            {
                Name = "bow",
                ClassType = typeof(Archer),
                Config = new PoseConfig(cfg, "Ranger"),
                GetWeapon = () => Weapons.Weapon<Archer>("bow"),
                LengthAxis = Vector3.up,
                BlockInput = new[] { "doTriggerClick", "doTriggerUnClick", "doGrip", "doUnGrip" },
            };
            Weapons.Register(Spec);
        }
    }

    // resets the bow to its default angles after every shot
    [HarmonyPatch(typeof(Archer), "deNotchArrow")]
    internal static class BowReapplyAfterShot
    {
        static void Postfix(Archer __instance)
        {
            var local = AccessTools.Field(typeof(Archer), "isLocallyControlled");
            if ((bool)local.GetValue(__instance))
                Ranger.Spec?.Apply();
        }
    }
}