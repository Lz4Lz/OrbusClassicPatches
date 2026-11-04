using System;
using System.Collections.Generic;
using MagicalActual;
using UnityEngine;
using WeaponAdjustment.Weapons;

namespace WeaponAdjustment
{
    public class AdjustSession : MonoBehaviour
    {
        private static AdjustSession _current;
        public static bool Active => _current != null;

        private const float GrabRadius = 0.35f;

        private WeaponSpec _spec;
        private GameObject _weapon;
        private Transform _hand;
        private OrbusTrackedController _ctrl;

        private readonly Dictionary<Renderer, bool> _hidden = new Dictionary<Renderer, bool>();

        private GameObject _handClone; // visual copy of the hand, only while adjusting
        private Animator _handAnim;

        private bool _holding, _wasGripped, _hasPose, _saveRequested, _finished;
        private Vector3 _relPos; // weapon model pose relative to the hand, as last held
        private Quaternion _relRot;
        
        public static void Begin(WeaponSpec spec)
        {
            if (Active)
            {
                _current.Finish(false);
                return;
            }

            if (spec == null) return;

            var pc = PlayerCharacter.Instance;
            if (pc == null) return;

            var weapon = spec.GetWeapon();
            if (weapon == null)
            {
                Plugin.Notify("Could not find the weapon.");
                return;
            }

            // Equip if needed.
            if (!Weapons.Weapons.EnsureDrawn(weapon))
            {
                Plugin.Notify("Could not equip the weapon for adjustment.");
                return;
            }

            if (weapon.transform.parent == null)
            {
                Plugin.Notify("Could not find the weapon's controller.");
                return;
            }

            pc.ClosePlayerMenu();

            GameObject weaponModel = MakeWeapon(weapon);

            PlaceInFront(
                weaponModel.transform,
                pc.trackingTarget.transform,
                spec.LengthAxis
            );

            var session = weaponModel.AddComponent<AdjustSession>();
            _current = session;
            session.Init(spec, weapon);
        }

        private void Init(WeaponSpec sp, GameObject w)
        {
            _spec = sp;
            _weapon = w;
            _hand = w.transform.parent;
            _ctrl = _hand.GetComponent<OrbusTrackedController>();
            if (_ctrl == null)
            {
                Plugin.Notify("Could not find the weapon hand's controller.");
                Finish(false);
                return;
            }

            foreach (var r in _weapon.GetComponentsInChildren<Renderer>(true))
            {
                _hidden[r] = r.enabled;
                r.enabled = false;
            }

            ShowHand();

            _ctrl.TriggerClicked += OnTrigger;

            Plugin.Log.LogInfo("Adjust mode started for " + _spec.Name + ".");
        }

        private void OnTrigger(object sender, ClickedEventArgs e)
        {
            if (!_hasPose)
            {
                Plugin.Notify("Hold the " + _spec.Name + " with the grip button first, then press trigger.");
                _current.Finish(false);
                return;
            }

            _saveRequested = true;
        }

        private void LateUpdate()
        {
            if (_finished) return;

            if (_weapon == null || !_weapon.activeInHierarchy || _hand == null)
            {
                Finish(false);
                return;
            }

            if (_saveRequested)
            {
                Finish(true);
                return;
            }

            bool gripped = _ctrl.gripped;

            if (gripped != _wasGripped && _handAnim != null)
                _handAnim.SetBool("grabbing", gripped);

            if (gripped && !_wasGripped) TryGrab();
            if (!gripped && _wasGripped) _holding = false;
            _wasGripped = gripped;

            if (_holding)
                transform.SetPositionAndRotation(_hand.TransformPoint(_relPos), _hand.rotation * _relRot);
        }

        private void TryGrab()
        {
            if (Vector3.Distance(_hand.position, transform.position) > GrabRadius)
                return;

            _relPos = _hand.InverseTransformPoint(transform.position);
            _relRot = Quaternion.Inverse(_hand.rotation) * transform.rotation;
            _holding = true;
            _hasPose = true;
        }

        private void Finish(bool save)
        {
            if (_finished) return;
            _finished = true;

            if (save)
            {
                _spec.Config.Set(_relPos, _relRot);
                _spec.Apply();
                Plugin.Log.LogInfo("Saved " + _spec.Name + " pose: pos " + _relPos + " euler " + _relRot.eulerAngles);
            }
            else
            {
                Plugin.Log.LogInfo("Adjust mode cancelled.");
            }

            Restore();

            if (_ctrl != null)
                _ctrl.TriggerClicked -= OnTrigger;

            if (_current == this) _current = null;
            Destroy(gameObject);
        }

        private void Restore()
        {
            foreach (var kv in _hidden)
                if (kv.Key != null)
                    kv.Key.enabled = kv.Value;

            _hidden.Clear();

            if (_handClone != null)
                Destroy(_handClone);

            _handClone = null;
            _handAnim = null;
        }

        private void OnDestroy()
        {
            if (_finished) return;
            _finished = true;
            Restore();
            if (_ctrl != null) _ctrl.TriggerClicked -= OnTrigger;
            if (_current == this) _current = null;
        }

        private void ShowHand()
        {
            Transform src = _hand.Find("Grabber/Hand");
            if (src == null)
            {
                Plugin.Log.LogWarning("No hand model found under " + _hand.name + "/Grabber/Hand.");
                return;
            }

            Transform grabber = src.parent;
            if (grabber.gameObject.activeInHierarchy) return;

            var anchor = new GameObject("WeaponAdjustHand")
            {
                layer = grabber.gameObject.layer
            };

            anchor.transform.SetParent(_hand, false);
            anchor.transform.localPosition = grabber.localPosition;
            anchor.transform.localRotation = grabber.localRotation;
            anchor.transform.localScale = grabber.localScale;

            var clone = Instantiate(src.gameObject, anchor.transform);
            clone.SetActive(true);

            _handAnim = clone.GetComponent<Animator>();
            if (_handAnim != null) _handAnim.SetBool("grabbing", _ctrl.gripped);

            _handClone = anchor;
        }
        
        private static GameObject MakeWeapon(GameObject weapon)
        {
            var holder = new GameObject("WeaponAdjustHolder");
            holder.SetActive(false);

            GameObject model = Instantiate(weapon, holder.transform);
            model.name = "WeaponAdjustModel";

            void Strip(Func<Component, bool> remove)
            {
                foreach (var c in model.GetComponentsInChildren<Component>(true))
                    if (c != null && remove(c))
                        DestroyImmediate(c);
            }

            Strip(c => c is MonoBehaviour || c is ParticleSystem);
            Strip(c => !(c is Transform || c is MeshFilter || c is MeshRenderer || c is SkinnedMeshRenderer));

            model.transform.SetParent(null, true);
            model.transform.localScale = weapon.transform.lossyScale;
            model.SetActive(true);
            Destroy(holder);

            return model;
        }
        
        private static void PlaceInFront(Transform weaponTransform, Transform head, Vector3 lengthAxis)
        {
            Vector3 fwd = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.forward;
            fwd.Normalize();

            weaponTransform.position = head.position + fwd * 0.5f + Vector3.down * 0.35f;
            weaponTransform.rotation = Quaternion.LookRotation(fwd, Vector3.up) *
                                       Quaternion.FromToRotation(lengthAxis, Vector3.forward);
        }
    }
}