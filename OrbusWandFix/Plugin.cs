using BepInEx;
using HarmonyLib;
using MagicalActual;
using UnityEngine;
using BepInEx.Configuration;

namespace OrbusWandFix
{
    [BepInPlugin(
        "orbusvrcommunity.wandfix",
        "Orbus Classic/Preborn Wand Hand Fix",
        "1.1.0"
    )]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableTeleport;

        private void Awake()
        {
            EnableTeleport = Config.Bind(
                "WandFix",
                "Teleport",
                false,
                "Enable Teleport With Wand Hand"
            );
            
            var harmony = new Harmony("orbusvrcommunity.wandfix");


            harmony.PatchAll();
            Logger.LogInfo("OrbusWandFix loaded.");
        }
    }

    [HarmonyPatch(typeof(Runemage), "EnablePrimaryWeapon")]
    internal static class RunemagePatch
    {
        private static void Postfix(Runemage __instance)
        {
            var player = PlayerCharacter.Instance;

            if (player == null)
                return;

            if (player.myHeadsetType != PlayerCharacter.headsetType.VIVE)
                return;

            var primaryController = Traverse.Create(__instance).Field("primaryController").GetValue<GameObject>();

            if (primaryController == null)
                return;

            var teleporterTransform = primaryController.transform.Find("Teleporter");

            if (teleporterTransform == null)
                return;

            var teleporter = teleporterTransform.GetComponent<Teleporter>();

            if (teleporter == null)
                return;

            // For rotation
            teleporter.gameObject.SetActive(true);

            // Move/Tp with wand hand (By default, false)
            teleporter.SetAllowTeleport(Plugin.EnableTeleport.Value);
        }
    }

    // Disables casting with joystick movement
    [HarmonyPatch(typeof(Runemage), "doPadClick")]
    internal static class RunemageDoPadClickPatch
    {
        private static bool Prefix(object sender, ClickedEventArgs e)
        {
            var player = PlayerCharacter.Instance;

            if (player == null || player.myHeadsetType != PlayerCharacter.headsetType.VIVE)
                return true;

            if (Mathf.Abs(e.padX) > 0.25f || Mathf.Abs(e.padY) > 0.25f)
                return false;

            return true;
        }
    }
}