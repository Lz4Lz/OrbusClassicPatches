using HarmonyLib;
using MagicalActual;
using UnityEngine;

namespace OrbusAnchor
{
    [HarmonyPatch(typeof(Archer), "ClassEnabled")]
    static class Ranger
    {
        static void Postfix(Archer __instance)
        {
            bool local = Traverse.Create(__instance).Field("isLocallyControlled").GetValue<bool>();
            
            if (!local) 
                return;

            var editor = __instance.GetComponent<AnchorEditor>();
            
            if (editor == null)
                editor = __instance.gameObject.AddComponent<AnchorEditor>();

            var t = Traverse.Create(__instance);
            
            Register(editor, __instance.transform, "Ranger.Slot1Arrow", t.Field("arrowSlot1").GetValue<GameObject>());
            Register(editor, __instance.transform, "Ranger.Slot2Arrow", t.Field("arrowSlot2").GetValue<GameObject>());
            Register(editor, __instance.transform, "Ranger.Trap", t.Field("beltTrap").GetValue<GameObject>());
        }

        private static void Register(AnchorEditor editor, Transform space, string id, GameObject go)
        {
            if (go == null)
                return;
            
            editor.Register(id, go.transform, space, Plugin.BindPos(id));
        }
    }
}