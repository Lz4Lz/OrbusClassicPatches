using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace TeleportAdjustment
{
    [BepInPlugin(
        "orbusvrcommunity.teleportadjustment",
        "Orbus Classic/Preborn Teleporter Adjustment",
        "1.0.0"
    )]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<float> TpX;
        public static ConfigEntry<float> TpY;

        private void Awake()
        {
            TpX = Config.Bind(
                "Teleport",
                "X",
                0f,
                "Tilts the teleporters aim axis up/down (positive = down)"
            );

            TpY = Config.Bind(
                "Teleport",
                "Y",
                0f,
                "Turns the teleporters aim axis left/right (positive = right)"
            );

            Harmony harmony = new Harmony(
                "orbusvrcommunity.teleportadjustment"
            );

            harmony.PatchAll();

            Logger.LogInfo("TeleportAdjustment loaded.");
        }
    }

    internal static class TeleportOffset
    {
        private static Quaternion _originalRotation;
        private static Transform _holder;
        private static bool _applied;

        public static void Apply(LaserPointer lp)
        {
            if (lp == null || lp.holder == null)
                return;

            Transform currentHolder = lp.holder.transform;

            if (_applied && _holder == currentHolder)
                return;

            Restore();

            _holder = currentHolder;
            _originalRotation = _holder.localRotation;

            _holder.localRotation = _originalRotation * Quaternion.Euler(Plugin.TpX.Value, Plugin.TpY.Value, 0f);

            _applied = true;
        }

        public static void Restore()
        {
            if (_applied && _holder != null)
                _holder.localRotation = _originalRotation;

            _holder = null;
            _applied = false;
        }
    }

    [HarmonyPatch(typeof(Teleporter), "doPadClick")]
    static class TeleportOffsetOn
    {
        static void Postfix(Teleporter __instance)
        {
            var lp = Traverse.Create(__instance)
                .Field("laserPointer")
                .GetValue<LaserPointer>();

            var state = Traverse.Create(__instance)
                .Field("currentState")
                .GetValue<object>();

            if (lp == null || state == null)
                return;

            if (state.ToString() != "TELEPORTING")
                return;

            TeleportOffset.Apply(lp);
        }
    }

    [HarmonyPatch(typeof(Teleporter), "doPadUnClick")]
    static class TeleportOffsetOff
    {
        static void Postfix()
        {
            TeleportOffset.Restore();
        }
    }
}