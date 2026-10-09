using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OrbusWandAdjustment
{
    [BepInPlugin(
        "orbusvrcommunity.wandadjustment",
        "Orbus Classic/Preborn Wand Adjustment",
        "1.0.0"
    )]
    public class Plugin : BaseUnityPlugin
    {
        
        public static ConfigEntry<float> OffsetX;
        public static ConfigEntry<float> OffsetY;
        public static ConfigEntry<float> OffsetZ;

        public static ConfigEntry<float> RotX;
        public static ConfigEntry<float> RotY;
        public static ConfigEntry<float> RotZ;
        private void Awake()
        {
            OffsetX = Config.Bind("Wand Offset", "Position X", 0.0f,  "Local X offset of the wand relative to the hand");
            OffsetY = Config.Bind("Wand Offset", "Position Y", 0.0f,  "Local Y offset of the wand relative to the hand");
            OffsetZ = Config.Bind("Wand Offset", "Position Z", 0.0f,  "Local Z offset of the wand relative to the hand");

            RotX = Config.Bind("Wand Offset", "Rotation X", 79.1f, "Local X rotation (degrees)");
            RotY = Config.Bind("Wand Offset", "Rotation Y", 180.0f, "Local Y rotation (degrees)");
            RotZ = Config.Bind("Wand Offset", "Rotation Z", 180.0f, "Local Z rotation (degrees)");
            
            var harmony = new Harmony("orbusvrcommunity.wandadjustment");
            
            harmony.PatchAll();
            Logger.LogInfo("OrbusWandAdjustment loaded.");
        }
    }

    [HarmonyPatch(typeof(MagicalActual.Runemage), "SwitchWeaponHand")]
    static class WandOffsetPatch
    {
        static void Postfix(MagicalActual.Runemage __instance)
        {
            var wand = Traverse.Create(__instance).Field("wand").GetValue<UnityEngine.GameObject>();
            
            if (wand == null) 
                return;
            
            wand.transform.localPosition = new Vector3(
                Plugin.OffsetX.Value,
                Plugin.OffsetY.Value,
                Plugin.OffsetZ.Value
            );

            wand.transform.localRotation = Quaternion.Euler(
                Plugin.RotX.Value,
                Plugin.RotY.Value,
                Plugin.RotZ.Value
            );
         
        }
    }
}
