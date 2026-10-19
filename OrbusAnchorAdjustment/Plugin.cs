using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OrbusAnchor
{
    [BepInPlugin("orbusvrcommunity.orbusanchor", "Orbus Anchor Adjustment", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource Log;
        public static ConfigEntry<KeyCode> ToggleKey;
        public static bool EditMode;
        private static ConfigFile _config;

        private void Awake()
        {
            Log = Logger;
            _config = Config;
            ToggleKey = Config.Bind("Keys", "ToggleEdit", KeyCode.F8, "Toggle edit mode (show orbs, drag anchors)");

            new Harmony("orbusvrcommunity.orbusanchor").PatchAll();

            Logger.LogInfo("OrbusAnchor loaded");
        }

        public static ConfigEntry<string> BindPos(string id)
        {
            return _config.Bind("Positions", id, "", "local x,y,z - empty = default");
        }

        public static bool TryParse(string s, out Vector3 v)
        {
            v = Vector3.zero;

            if (string.IsNullOrEmpty(s))
                return false;

            var p = s.Split(',');

            if (p.Length != 3)
                return false;

            var c = CultureInfo.InvariantCulture;

            float x, y, z;

            if (!float.TryParse(p[0], NumberStyles.Float, c, out x)) return false;
            if (!float.TryParse(p[1], NumberStyles.Float, c, out y)) return false;
            if (!float.TryParse(p[2], NumberStyles.Float, c, out z)) return false;

            v = new Vector3(x, y, z);

            return true;
        }

        public static string Format(Vector3 v)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.####},{1:0.####},{2:0.####}", v.x, v.y, v.z);
        }
    }
}