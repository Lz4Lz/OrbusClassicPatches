using System.Collections.Generic;
using BepInEx.Configuration;
using MagicalActual;
using UnityEngine;

namespace OrbusAnchor
{
    public class AnchorTarget
    {
        public string Id;
        public Transform Transform;
        public Transform Space;
        public ConfigEntry<string> Config;
        public Transform Orb;
    }

    public class AnchorEditor : MonoBehaviour
    {
        private const float OrbScale = 0.05f;
        private const float GrabRadius = 0.20f;

        private readonly List<AnchorTarget> _list = new List<AnchorTarget>();

        private readonly int[] _held = { -1, -1 };
        private readonly Vector3[] _offset = new Vector3[2]; // hand to target, in Space-local
        private readonly bool[] _wasGripped = new bool[2];
        
        public void Register(string id, Transform target, Transform space, ConfigEntry<string> cfg)
        {
            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Id != id)
                    continue;

                _list[i].Transform = target;
                _list[i].Space = space;

                ApplySaved(_list[i]);

                return;
            }

            var a = new AnchorTarget { Id = id, Transform = target, Space = space, Config = cfg };

            ApplySaved(a);
            a.Orb = MakeOrb(target);
            _list.Add(a);
            Plugin.Log.LogInfo("Registered: " + id);
        }

        private static void ApplySaved(AnchorTarget a)
        {
            if (!Plugin.TryParse(a.Config.Value, out var v))
                return;

            a.Transform.position = a.Space.TransformPoint(v);
        }

        private static string SavePos(AnchorTarget a)
        {
            return Plugin.Format(a.Space.InverseTransformPoint(a.Transform.position));
        }

        private static Transform MakeOrb(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "AnchorOrb";

            Destroy(go.GetComponent<Collider>());

            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;

            float ps = parent.lossyScale.x;

            go.transform.localScale = Vector3.one * (OrbScale / ps);

            var r = go.GetComponent<Renderer>();

            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var sh = Shader.Find("Sprites/Default");

            r.material = new Material(sh)
            {
                color = new Color(0.2f, 0.9f, 1f, 1f)
            };

            go.SetActive(false);
            return go.transform;
        }

        private void Update()
        {
            if (Input.GetKeyDown(Plugin.ToggleKey.Value))
            {
                if (Plugin.EditMode)
                {
                    SaveAndRelease(0);
                    SaveAndRelease(1);
                }

                Plugin.EditMode = !Plugin.EditMode;
            }

            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Orb != null)
                    _list[i].Orb.gameObject.SetActive(Plugin.EditMode);
            }

            if (!Plugin.EditMode)
                return;

            var pc = PlayerCharacter.Instance;
            if (pc == null)
                return;

            var left = pc.leftController;
            var right = pc.rightController;

            if (left == null || right == null)
                return;

            UpdateHand(0, left);
            UpdateHand(1, right);
        }

        private void UpdateHand(int handIdx, OrbusTrackedController hand)
        {
            int heldIndex = _held[handIdx];

            if (heldIndex >= 0)
            {
                var a = _list[heldIndex];

                if (!hand.gripped)
                {
                    SaveAndRelease(handIdx);
                }
                else
                {
                    Vector3 handLocal = a.Space.InverseTransformPoint(hand.transform.position);

                    a.Transform.position = a.Space.TransformPoint(handLocal + _offset[handIdx]);
                }
            }
            else if (hand.gripped && !_wasGripped[handIdx])
            {
                TryGrab(handIdx, hand);
            }

            _wasGripped[handIdx] = hand.gripped;
        }

        private void SaveAndRelease(int handIdx)
        {
            int heldIndex = _held[handIdx];

            if (heldIndex < 0)
                return;

            var a = _list[heldIndex];
            a.Config.Value = SavePos(a);

            _held[handIdx] = -1;
        }

        private void TryGrab(int handIdx, OrbusTrackedController hand)
        {
            int best = -1;
            float bestD = GrabRadius;

            for (int i = 0; i < _list.Count; i++)
            {
                if (_list[i].Transform == null)
                    continue;

                bool alreadyHeld = false;

                for (int h = 0; h < 2; h++)
                {
                    if (_held[h] == i)
                    {
                        alreadyHeld = true;
                        break;
                    }
                }

                if (alreadyHeld)
                    continue;

                float d = Vector3.Distance(hand.transform.position, _list[i].Transform.position);

                if (d < bestD)
                {
                    bestD = d;
                    best = i;
                }
            }

            if (best < 0)
                return;

            var a = _list[best];

            Vector3 handLocal = a.Space.InverseTransformPoint(hand.transform.position);

            Vector3 targetLocal = a.Space.InverseTransformPoint(a.Transform.position);

            _offset[handIdx] = targetLocal - handLocal;
            _held[handIdx] = best;

            hand.HapticPulse(1500, 0f);
        }
    }
}