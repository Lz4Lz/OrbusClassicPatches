using BepInEx;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace OrbusWandFix
{
    [BepInPlugin(
        "orbusvrcommunity.wandfix",
        "Orbus Classic/Preborn Wand Hand Fix",
        "1.0.0"
    )]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            var harmony = new Harmony("orbusvrcommunity.wandfix");
            
            harmony.PatchAll();
            Logger.LogInfo("OrbusWandFix loaded.");
        }
    }

    [HarmonyPatch(typeof(MagicalActual.Runemage), "EnablePrimaryWeapon")]
    internal static class RunemagePatch
    {
        private static void Postfix(MagicalActual.Runemage __instance)
        {
            var player = MagicalActual.PlayerCharacter.Instance;

            if (player == null)
                return;

            if (player.myHeadsetType != MagicalActual.PlayerCharacter.headsetType.VIVE)
            {
                return;
            }

            var primaryController = Traverse.Create(__instance).Field("primaryController").GetValue<GameObject>();

            if (primaryController == null)
                return;

            var teleporterTransform = primaryController.transform.Find("Teleporter");

            if (teleporterTransform == null) 
                return;

            var teleporter = teleporterTransform.GetComponent<MagicalActual.Teleporter>();

            if (teleporter == null)
                return;
            
            // For rotation
            teleporter.gameObject.SetActive(true);

            
            // move/tp with wand hand
            teleporter.SetAllowTeleport(true);
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
