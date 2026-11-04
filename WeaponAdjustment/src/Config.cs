using BepInEx.Configuration;
using UnityEngine;

namespace WeaponAdjustment
{
    // Weapon saved transform, stored in its own config section, e.g. [Runemage].
    
    public class PoseConfig
    {
        private readonly ConfigEntry<bool> _enabled;
        public ConfigEntry<float> PX, PY, PZ, RX, RY, RZ;

        public PoseConfig(ConfigFile cfg, string section, Vector3 defaultPos = default(Vector3),
            Vector3 defaultEuler = default(Vector3))
        {
            _enabled = cfg.Bind(section, "Enabled", false,
                "false = use the game's own pose for this weapon. " +
                "Set to true automatically when you save from the in-game Adjust Weapon tool. " +
                "Reset Weapon only sets this back to false and keeps the values below.");

            PX = cfg.Bind(section, "Position X", defaultPos.x, "Local X offset of the weapon relative to the hand");
            PY = cfg.Bind(section, "Position Y", defaultPos.y, "Local Y offset of the weapon relative to the hand");
            PZ = cfg.Bind(section, "Position Z", defaultPos.z, "Local Z offset of the weapon relative to the hand");

            RX = cfg.Bind(section, "Rotation X", defaultEuler.x, "Local X rotation (degrees)");
            RY = cfg.Bind(section, "Rotation Y", defaultEuler.y, "Local Y rotation (degrees)");
            RZ = cfg.Bind(section, "Rotation Z", defaultEuler.z, "Local Z rotation (degrees)");
        }

        public bool Enabled
        {
            set => _enabled.Value = value;
        }

        public bool TryGet(out Vector3 pos, out Quaternion rot)
        {
            pos = new Vector3(PX.Value, PY.Value, PZ.Value);
            rot = Quaternion.Euler(RX.Value, RY.Value, RZ.Value);
            return _enabled.Value;
        }

        public void Set(Vector3 pos, Quaternion rot)
        {
            Vector3 e = rot.eulerAngles;
            PX.Value = pos.x;
            PY.Value = pos.y;
            PZ.Value = pos.z;
            RX.Value = e.x;
            RY.Value = e.y;
            RZ.Value = e.z;
            _enabled.Value = true;
        }
    }
}