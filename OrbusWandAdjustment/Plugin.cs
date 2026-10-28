using System.IO;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace OrbusWandAdjustment
{
    [BepInPlugin("orbusvrcommunity.wandadjustment", "Orbus Classic/Preborn Wand Adjustment", "1.4.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> OffsetX, OffsetY, OffsetZ, RotX, RotY, RotZ;

        public static ConfigEntry<float> LendRotX, LendRotY, LendRotZ;

        public static ConfigEntry<bool> AlignServerAim;
        public static ConfigEntry<bool> AlignServerPosition;
        public static ConfigEntry<string> ServerRigOverride;

        private void Awake()
        {
            OffsetX = Config.Bind("Wand Offset", "Position X", 0.0f, "Local X offset of the wand relative to the hand");
            OffsetY = Config.Bind("Wand Offset", "Position Y", 0.0f, "Local Y offset of the wand relative to the hand");
            OffsetZ = Config.Bind("Wand Offset", "Position Z", -0.048f,
                "Local Z offset of the wand relative to the hand");

            RotX = Config.Bind("Wand Offset", "Rotation X", 100.89f, "Local X rotation (degrees)");
            RotY = Config.Bind("Wand Offset", "Rotation Y", 0.0f, "Local Y rotation (degrees)");
            RotZ = Config.Bind("Wand Offset", "Rotation Z", 0.0f, "Local Z rotation (degrees)");

            LendRotX = Config.Bind("Lend A Hand", "Rotation X", 0f,
                "Tilts the off hands aim axis up/down (positive = down)");

            LendRotY = Config.Bind("Lend A Hand", "Rotation Y", 0f,
                "Turns the off hands aim axis left/right (positive = right)");

            LendRotZ = Config.Bind("Lend A Hand", "Rotation Z", 0f,
                "Roll around the aim axis. Leave at zero, recommended. (nose still points the same way)");

            AlignServerAim = Config.Bind("Server Aim", "Align Server Aim", true,
                "Adjust the controller rotation sent to the server so Ice Lance / Super go where your adjusted wand points (recommended true, leave default)");

            AlignServerPosition = Config.Bind("Server Aim", "Align Server Position", true,
                "Adjust the sent hand position so the server wand tip is where the wand tip really is. (recommended true, leave default)");

            ServerRigOverride = Config.Bind("Server Aim", "Server Rig Type", "Auto",
                "Auto = detect from headset (recommended). Steam = force Vive/Index values. Rift = force Oculus/Rift values.");

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
    static class LendAHandPatch
    {
        static bool Prefix(BinaryWriter writer, GameObject obj)
        {
            float x = Plugin.LendRotX.Value, y = Plugin.LendRotY.Value, z = Plugin.LendRotZ.Value;

            if (x == 0f && y == 0f && z == 0f)
                return true;

            var mage = WandOffsetPatch.LocalMage;

            if (mage == null)
                return true;

            var wand = mage.wand;

            if (wand == null || !wand.activeInHierarchy || wand.transform.parent == null)
                return true;

            var pc = PlayerCharacter.Instance;

            if (pc == null || pc.myPlayerAvatar == null)
                return true;

            var av = pc.myPlayerAvatar;

            GameObject offHand = wand.transform.parent.gameObject == av.leftController
                ? av.rightController
                : av.leftController;

            if (offHand != obj)
                return true;

            Vector3 p = obj.transform.localPosition;
            Quaternion q = obj.transform.localRotation * Quaternion.Euler(x, y, z);

            writer.Write(p.x);
            writer.Write(p.y);
            writer.Write(p.z);
            writer.Write(q.x);
            writer.Write(q.y);
            writer.Write(q.z);
            writer.Write(q.w);

            return false;
        }
    }
    
    // Fixes the Ice Lance / Super issue.
    [HarmonyPatch(typeof(OrbusNetManager), "WriteObjectPosRot")]
    static class ServerAimPatch
    {
        // Server-side wand settings, taken from PlayerRigv3.prefab

        private static readonly Vector3 PrefabWandPos = new Vector3(0f, 0f, -0.048f);
        private static readonly Vector3 TipPosInWand = new Vector3(0.0025f, 0.3352f, 0f);

        // Steam
        private static readonly Quaternion SteamWandRot = new Quaternion(0.77101916f, 0f, 0f, 0.636812f);

        // Rift / Oculus
        private static readonly Quaternion RiftWandRot = new Quaternion(0.46863136f, 0f, 0f, 0.8833938f);

        private static Quaternion ServerWandRot()
        {
            string o = Plugin.ServerRigOverride.Value;

            if (o == "Steam")
                return SteamWandRot;

            if (o == "Rift")
                return RiftWandRot;

            var pc = PlayerCharacter.Instance;
            bool rift = pc != null && pc.myHeadsetType == PlayerCharacter.headsetType.OCULUS;
            return rift ? RiftWandRot : SteamWandRot;
        }

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

            Quaternion prefabRot = ServerWandRot();
            Quaternion sentRot = rot * wandRot * Quaternion.Inverse(prefabRot);

            Vector3 sentPos = pos;
            if (Plugin.AlignServerPosition.Value)
            {
                Vector3 wantedTip = pos + rot * (wandPos + wandRot * TipPosInWand);
                sentPos = wantedTip - sentRot * (PrefabWandPos + prefabRot * TipPosInWand);
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