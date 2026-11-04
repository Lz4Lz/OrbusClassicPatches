using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace OrbusAnchor
{
    static class GlobalEditor
    {
        // one global editor, lives on the local PlayerCharacter
        public static AnchorEditor Get()
        {
            var pc = PlayerCharacter.Instance;

            if (pc == null)
                return null;

            var e = pc.GetComponent<AnchorEditor>();

            if (e != null) return e;
            e = pc.gameObject.AddComponent<AnchorEditor>();
            e.Global = true;

            return e;
        }
    }

    [HarmonyPatch(typeof(PlayerCompass), "Start")]
    static class Compass
    {
        static void Postfix(PlayerCompass __instance)
        {
            var pc = PlayerCharacter.Instance;

            if (pc == null || pc.myCompass != __instance)
                return;

            var t = __instance.transform;
            var space = t.parent != null ? t.parent : pc.transform;

            GlobalEditor.Get().Register("Body.Compass", t, space, Plugin.BindPos("Body.Compass"));
        }
    }

    [HarmonyPatch(typeof(PlayerCompass), "FinishUngrip")]
    static class CompassKeepSaved
    {
        static void Postfix(PlayerCompass __instance)
        {
            var pc = PlayerCharacter.Instance;
            var space = __instance.transform.parent;

            if (pc == null || pc.myCompass != __instance || space == null)
                return;

            if (Plugin.TryParse(Plugin.BindPos("Body.Compass").Value, out var v))
                __instance.transform.position = space.TransformPoint(v);
        }
    }

    [HarmonyPatch(typeof(PlayerCharacter), "Update")]
    static class CompassUngrabbable
    {
        private static bool _applied;
        private static Collider[] _cols;
        private static bool[] _orig;

        static void Postfix(PlayerCharacter __instance)
        {
            if (__instance.myCompass == null || Plugin.EditMode == _applied)
                return;

            if (Plugin.EditMode)
            {
                _cols = __instance.myCompass.GetComponentsInChildren<Collider>(true);
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
                    if (_cols[i] != null)
                        _cols[i].enabled = _orig[i];

                _cols = null;
            }

            _applied = Plugin.EditMode;
        }
    }
}