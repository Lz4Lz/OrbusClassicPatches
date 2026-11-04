using BepInEx.Configuration;
using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace OrbusAnchor
{
    [HarmonyPatch(typeof(Swordboard), "ClassEnabled")]
    static class Warrior
    {
        static void Postfix(Swordboard __instance)
        {
            bool local = Traverse.Create(__instance).Field("isLocallyControlled").GetValue<bool>();

            if (!local)
                return;

            var editor = __instance.GetComponent<AnchorEditor>();

            if (editor == null)
                editor = __instance.gameObject.AddComponent<AnchorEditor>();

            var t = Traverse.Create(__instance);

            Register(editor, __instance.transform, "Warrior.tauntHorn", t.Field("tauntHorn").GetValue<GameObject>());
        }

        private static void Register(AnchorEditor editor, Transform space, string id, GameObject go)
        {
            if (go == null)
                return;

            editor.Register(id, go.transform, space, Plugin.BindPos(id));
        }
    }

    // no taunt while editing
    [HarmonyPatch(typeof(Swordboard), nameof(Swordboard.TriggerTaunt))]
    internal static class HornNoTaunt
    {
        static bool Prefix(ref bool __result)
        {
            if (!Plugin.EditMode)
                return true;

            __result = false;
            return false;
        }
    }

    // horn cant be grabbed while editing (only the orb works)
    [HarmonyPatch(typeof(Swordboard), "Update")]
    internal static class HornUngrabbable
    {
        private static bool _applied;
        private static Collider[] _cols;
        private static bool[] _orig;

        static void Postfix(Swordboard __instance)
        {
            if (__instance.GetComponent<AnchorEditor>() == null)
                return;

            var horn = __instance.tauntHorn;

            if (horn == null || Plugin.EditMode == _applied)
                return;

            if (Plugin.EditMode)
            {
                _cols = horn.GetComponentsInChildren<Collider>(true);
                _orig = new bool[_cols.Length];

                for (int i = 0; i < _cols.Length; i++)
                {
                    _orig[i] = _cols[i].enabled;
                    _cols[i].enabled = false;
                }
            }
            else if (_cols != null)
            {
                for (int i = 0; i < _cols.Length; i++)
                {
                    if (_cols[i] != null)
                        _cols[i].enabled = _orig[i];
                }

                _cols = null;
            }

            _applied = Plugin.EditMode;
        }
    }

    [HarmonyPatch(typeof(TauntHorn), "SnapBack")]
    static class HornKeepSavedPosition
    {
        private static ConfigEntry<string> _cfg;

        static void Postfix(TauntHorn __instance)
        {
            var sb = __instance.mySwordboard;

            if (sb == null)
                return;

            if (_cfg == null)
                _cfg = Plugin.BindPos("Warrior.tauntHorn");

            if (!Plugin.TryParse(_cfg.Value, out var v))
                return;

            __instance.transform.position = sb.transform.TransformPoint(v);
        }
    }
}