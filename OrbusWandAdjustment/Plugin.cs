using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace OrbusWandAdjustment
{
    [BepInPlugin("orbusvrcommunity.wandadjustment", "Orbus Classic/Preborn Wand Adjustment", "1.2.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> OffsetX, OffsetY, OffsetZ, RotX, RotY, RotZ;
        public static ConfigEntry<bool> AlignServerAim;
        public static ConfigEntry<bool> AlignServerPosition;

        private void Awake()
        {
            OffsetX = Config.Bind("Wand Offset", "Position X", 0.0f, "Local X offset of the wand relative to the hand");
            OffsetY = Config.Bind("Wand Offset", "Position Y", 0.0f, "Local Y offset of the wand relative to the hand");
            OffsetZ = Config.Bind("Wand Offset", "Position Z", -0.048f,
                "Local Z offset of the wand relative to the hand");

            RotX = Config.Bind("Wand Offset", "Rotation X", 100.89f, "Local X rotation (degrees)");
            RotY = Config.Bind("Wand Offset", "Rotation Y", 0.0f, "Local Y rotation (degrees)");
            RotZ = Config.Bind("Wand Offset", "Rotation Z", 0.0f, "Local Z rotation (degrees)");
            
            AlignServerAim = Config.Bind("Server Aim", "Align Server Aim", true,
                "Adjust the controller rotation sent to the server so Ice Lance / Super go where your adjusted wand points (recommended true, leave default)");

            AlignServerPosition = Config.Bind("Server Aim", "Align Server Position", true,
                "Adjust the sent hand position so the server wand tip is where the wand tip really is. Others might see your hand slightly off. (recommended true, leave default)");

            new Harmony("orbusvrcommunity.wandadjustment").PatchAll();
            Logger.LogInfo("OrbusWandAdjustment loaded.");
        }
    }

    [HarmonyPatch(typeof(Runemage), "SwitchWeaponHand")]
    static class WandOffsetPatch
    {
        public static Runemage LocalMage;

        static void Postfix(Runemage __instance)
        {
            var t = Traverse.Create(__instance);

            if (!t.Field("isLocallyControlled").GetValue<bool>())
                return;

            LocalMage = __instance;

            var wand = t.Field("wand").GetValue<GameObject>();

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

    [HarmonyPatch(typeof(OrbusNetManager), "WriteObjectPosRot")]
    static class ServerAimPatch
    {
        // Server-side wand settings, taken from PlayerRigv3.prefab
        private static readonly Vector3 PrefabWandPos = new Vector3(0f, 0f, -0.048f);
        private static readonly Quaternion PrefabWandRot = new Quaternion(0.77101916f, 0f, 0f, 0.636812f);
        private static readonly Vector3 TipPosInWand = new Vector3(0.0025f, 0.3352f, 0f);

        static bool Prefix(BinaryWriter writer, GameObject obj)
        {
            if (!Plugin.AlignServerAim.Value)
                return true;

            var mage = WandOffsetPatch.LocalMage;

            if (mage == null)
                return true;

            var wand = Traverse.Create(mage).Field("wand").GetValue<GameObject>();

            if (wand == null || !wand.activeInHierarchy)
                return true;

            Transform hand = wand.transform.parent;

            if (hand == null || hand.gameObject != obj)
                return true;

            Vector3 pos = obj.transform.localPosition;
            Quaternion rot = obj.transform.localRotation;

            Vector3 wandPos = wand.transform.localPosition;
            Quaternion wandRot = wand.transform.localRotation;

            Quaternion sentRot = rot * wandRot * Quaternion.Inverse(PrefabWandRot);

            Vector3 sentPos = pos;
            if (Plugin.AlignServerPosition.Value)
            {
                Vector3 wantedTip = pos + rot * (wandPos + wandRot * TipPosInWand);
                sentPos = wantedTip - sentRot * (PrefabWandPos + PrefabWandRot * TipPosInWand);
            }

            writer.Write(sentPos.x);
            writer.Write(sentPos.y);
            writer.Write(sentPos.z);
            writer.Write(sentRot.x);
            writer.Write(sentRot.y);
            writer.Write(sentRot.z);
            writer.Write(sentRot.w);

            return false;
        }
    }
}