using System.IO;
using BepInEx.Configuration;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace WeaponAdjustment.Weapons
{
    public static class Mage
    {
        public static WeaponSpec Spec { get; private set; }

        public static void Init(ConfigFile cfg)
        {
            Spec = new WeaponSpec
            {
                Name = "wand",
                ClassType = typeof(Runemage),
                Config = new PoseConfig(cfg, "Runemage", new Vector3(0f, 0f, -0.048f), new Vector3(100.89f, 0f, 0f)),
                GetWeapon = () => Weapons.Weapon<Runemage>("wand"),
                LengthAxis = Vector3.up,
                BlockInput = new[] { "doPrimaryTriggerClick", "doPrimaryPadClick" },
            };
            Weapons.Register(Spec);
        }
    }

    // For Lend A Hand adjustment
    [HarmonyPatch(typeof(OrbusNetManager), "WriteObjectPosRot")]
    static class LendAHandPatch
    {
        static bool Prefix(BinaryWriter writer, GameObject obj)
        {
            float x = Plugin.LendRotX.Value, y = Plugin.LendRotY.Value, z = Plugin.LendRotZ.Value;

            if (x == 0f && y == 0f && z == 0f)
                return true;


            var wand = Mage.Spec.GetWeapon();

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
    static class RuneMageServerAimPatch
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
            if (o == "Steam") return SteamWandRot;
            if (o == "Rift") return RiftWandRot;

            var pc = PlayerCharacter.Instance;
            bool rift = pc != null && pc.myHeadsetType == PlayerCharacter.headsetType.OCULUS;
            return rift ? RiftWandRot : SteamWandRot;
        }

        static bool Prefix(BinaryWriter writer, GameObject obj)
        {
            if (!Plugin.AlignServerAim.Value) return true;

            var wand = Mage.Spec.GetWeapon();
            if (wand == null || !wand.activeInHierarchy)
                return true;

            Transform hand = wand.transform.parent;
            if (hand == null || hand.gameObject != obj)
                return true;

            Vector3 pos = obj.transform.localPosition;
            Quaternion rot = obj.transform.localRotation;

            Vector3 wandPos = wand.transform.localPosition;
            Quaternion wandRot = wand.transform.localRotation;

            Quaternion serverRot = ServerWandRot();
            Quaternion sentRot = rot * wandRot * Quaternion.Inverse(serverRot);

            Vector3 sentPos = pos;
            if (Plugin.AlignServerPosition.Value)
            {
                Vector3 wantedTip = pos + rot * (wandPos + wandRot * TipPosInWand);
                sentPos = wantedTip - sentRot * (PrefabWandPos + serverRot * TipPosInWand);
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