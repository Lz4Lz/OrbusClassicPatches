using BepInEx;
using HarmonyLib;
using MagicalActual;


namespace OrbusLaserPointer
{
    [BepInPlugin("orbusvrcommunity.laserpointer", "Orbus Classic Laser Pointer", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            new Harmony("orbusvrcommunity.laserpointer").PatchAll();
            Logger.LogInfo("Orbus Laser Pointer loaded");
        }
    }

    [HarmonyPatch(typeof(Grabber), "Start")]
    static class AttachLaserPatch
    {
        static void Postfix(Grabber __instance)
        {
            if (__instance.myController != null && __instance.GetComponent<MenuLaser>() == null)
                __instance.gameObject.AddComponent<MenuLaser>();
        }
    }
}