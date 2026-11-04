using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace WeaponAdjustment.Weapons
{
    public class WeaponSpec
    {
        public string Name;
        public Type ClassType; // the class e.g., typeof(Runemage)
        public PoseConfig Config;

        public Func<GameObject> GetWeapon; // weapon root. null if the class isnt active

        public Vector3 LengthAxis = Vector3.up;
        public string[] BlockInput = new string[0]; // private methods to skip while adjusting (e.g., casting)
        public bool PositionOnly; // the game drives the weapons rotation itself (e.g. Ranger bow)

        private GameObject _nativeOwner;
        private Vector3 _nativePos;
        private Quaternion _nativeRot;

        public bool IsCurrent()
        {
            var pc = PlayerCharacter.Instance;

            if (pc == null || pc.myPlayerAvatar == null)
                return false;
            
            var cls = pc.myPlayerAvatar.currentActiveClass;
            return cls != null && cls.GetComponent(ClassType) != null;
        }

        private void CaptureNative(GameObject weapon)
        {
            if (_nativeOwner == weapon)
                return;

            _nativePos = weapon.transform.localPosition;
            _nativeRot = weapon.transform.localRotation;
            _nativeOwner = weapon;
        }

        public void Apply()
        {
            var weapon = GetWeapon?.Invoke();
            if (weapon == null)
                return;

            CaptureNative(weapon);

            if (!Config.TryGet(out Vector3 pos, out Quaternion rot))
            {
                pos = _nativePos;
                rot = _nativeRot;
            }

            weapon.transform.localPosition = pos;

            if (!PositionOnly)
                weapon.transform.localRotation = rot;
        }
    }

    public static class Weapons
    {
        private static readonly List<WeaponSpec> All = new List<WeaponSpec>();

        public static void Register(WeaponSpec spec)
        {
            if (spec != null && !All.Contains(spec))
                All.Add(spec);
        }

        public static WeaponSpec ForAdjustment()
        {
            foreach (var spec in All)
                if (spec.IsCurrent())
                    return spec;

            return null;
        }

        public static void ApplyCurrent()
        {
            var spec = ForAdjustment();

            spec?.Apply();
        }

        private static T ActiveClass<T>() where T : Component
        {
            var pc = PlayerCharacter.Instance;

            if (pc == null || pc.myPlayerAvatar == null)
                return null;

            var cls = pc.myPlayerAvatar.currentActiveClass;

            return cls == null ? null : cls.GetComponent<T>();
        }

        public static GameObject Weapon<T>(string fieldName) where T : Component
        {
            var c = ActiveClass<T>();

            if (c == null)
                return null;

            var field = typeof(T).GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );

            if (field == null)
            {
                Plugin.Log.LogWarning($"Could not find field {typeof(T).Name}.{fieldName}");
                return null;
            }

            if (!typeof(GameObject).IsAssignableFrom(field.FieldType))
            {
                Plugin.Log.LogWarning($"Field {typeof(T).Name}.{fieldName} is not a GameObject");
                return null;
            }

            return field.GetValue(c) as GameObject;
        }

        // EnsureDrawn Equips your weapon if you havent already
        public static bool EnsureDrawn(GameObject weapon)
        {
            if (weapon == null)
                return false;

            if (weapon.activeInHierarchy)
                return true;

            var pc = PlayerCharacter.Instance;

            if (pc == null || pc.myPlayerAvatar == null)
                return false;

            var av = pc.myPlayerAvatar;

            int idx = weapon.transform.IsChildOf(av.leftController.transform) ? 0 : 1;

            pc.setHandActive(idx, true);

            return weapon.activeInHierarchy;
        }

        public static void Reset(WeaponSpec spec)
        {
            spec.Config.Enabled = false;
            spec.Apply();
            Plugin.Log.LogInfo("Reset " + spec.Name + " to default.");
        }

        // skips each class listed methods while adjusting.
        // For example (casting, shooting with musky, etc)
        public static void PatchInputBlocks(Harmony h)
        {
            var prefix = new HarmonyMethod(typeof(Weapons), nameof(BlockPrefix));
            foreach (var spec in All)
            foreach (var name in spec.BlockInput)
            {
                var m = AccessTools.Method(spec.ClassType, name);
                if (m == null)
                {
                    Plugin.Log.LogWarning("Cannot block " + spec.ClassType.Name + "." + name + " (not found).");
                    continue;
                }

                h.Patch(m, prefix: prefix);
            }
        }

        private static bool BlockPrefix() => !AdjustSession.Active;
    }

    // One patch for every class
    [HarmonyPatch]
    internal static class ReapplyPoseOnHandUpdate
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PlayerAvatar), "UpdateLeftHandWeaponActive");
            yield return AccessTools.Method(typeof(PlayerAvatar), "UpdateRightHandWeaponActive");
        }

        static void Postfix(PlayerAvatar __instance)
        {
            if (__instance.localControl) Weapons.ApplyCurrent();
        }
    }
}