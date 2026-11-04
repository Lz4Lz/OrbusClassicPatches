using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MagicalActual;
using OrbusModMenu;
using WeaponAdjustment.Weapons;

namespace WeaponAdjustment
{
    [BepInPlugin("orbusvrcommunity.weaponadjustment", "Orbus Classic Weapon Adjustment", "1.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        
        
        // RuneMage Extra / Advanced Configs
        
        public static ConfigEntry<bool> AlignServerAim, AlignServerPosition;
        public static ConfigEntry<string> ServerRigOverride;
        
        public static ConfigEntry<float> LendRotX, LendRotY, LendRotZ;

        private const string Section = "Weapons";
        
        private void Awake()
        {
            Log = Logger;

            AlignServerAim = Config.Bind("Server Aim", "Align Server Aim", true,
                "Adjust the controller rotation sent to the server so Ice Lance / Super / missiles go where your adjusted wand points (recommended true, leave default)");
            AlignServerPosition = Config.Bind("Server Aim", "Align Server Position", true,
                "Adjust the sent hand position so the server wand tip is where the wand tip really is. Others might see your hand slightly off. (recommended true, leave default)");
            ServerRigOverride = Config.Bind("Server Aim", "Server Rig Type", "Auto",
                "Auto = detect from headset (recommended). Steam = force Vive/Index values. Rift = force Oculus/Rift values.");
            
            LendRotX = Config.Bind("Lend A Hand", "Rotation X", 0f,
                "Tilts the off hands aim axis up/down (positive = down)");

            LendRotY = Config.Bind("Lend A Hand", "Rotation Y", 0f,
                "Turns the off hands aim axis left/right (positive = right)");

            LendRotZ = Config.Bind("Lend A Hand", "Rotation Z", 0f,
                "Roll around the aim axis. Leave at zero, recommended. (nose still points the same way)");

            
            Mage.Init(Config);
            Ranger.Init(Config);
            Musky.Init(Config);
            Warrior.Init(Config);
            
            var harmony = new Harmony("orbusvrcommunity.weaponadjustment");


            ModMenu.AddButton(Section, "Adjust Weapon",
                () =>
                {
                    var spec = Weapons.Weapons.ForAdjustment();
                    if (spec == null)
                    {
                        Notify("No supported weapon found for your class.");
                        return;
                    }

                    AdjustSession.Begin(spec);
                });
            ModMenu.AddButton(Section,"Reset Weapon", () =>
            {
                var spec = Weapons.Weapons.ForAdjustment();
                if (spec == null)
                {
                    Notify("No supported weapon found for your class.");
                    return;
                }

                Weapons.Weapons.Reset(spec);
            });
            

            harmony.PatchAll();
            Weapons.Weapons.PatchInputBlocks(harmony);

            Logger.LogInfo("WeaponAdjustment loaded.");
        }

        // in-game notification

        internal static void Notify(string msg)
        {
            try
            {
                if (PlayerCharacter.Instance != null) PlayerCharacter.Instance.ShowNotification("Adjust", msg);
            }
            catch
            {
                Log.LogInfo("Failed to notify player");
            }
        }
    }
}