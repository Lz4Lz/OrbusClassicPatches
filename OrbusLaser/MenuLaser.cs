using System.Collections.Generic;
using ExtUI;
using MagicalActual;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OrbusLaserPointer
{
    public class MenuLaser : MonoBehaviour
    {
        private const int MenuMask = 1 << 19; // "524288" layer 19, the layer Orbus menus use (FindNearbyWorldUI)

        private const float MaxDistance = 8f; // laser reach
        private const float PokeDistance = 0.05f; // Orbus "poke" handles it closer than this.

        private static readonly Color HoverColor = new Color(0.1f, 1f, 0.4f, 0.95f);
        private static readonly Color PressedColor = new Color(1f, 0.85f, 0.1f, 1f);

        private static MenuLaser _overrideOwner;
        private static RaycastHit _overrideHit;
        private static System.Delegate _installedResolver;

        private OrbusTrackedController _controller;
        private Transform _aim;
        private LineRenderer _line;
        private bool _triggerWasDown, _powerUiDown;

        // World-space uGUI buttons (NPC dialogue). Cached because FindObjectsOfType is slow.
        private static readonly List<Selectable> GUIList = new List<Selectable>();
        private static float _nextGuiRefresh;

        private void Start()
        {
            Grabber grabber = GetComponent<Grabber>();
            _controller = grabber.myController;

            _aim = grabber.myUILaser.transform; // same transform orbus aims its menu poke with

            _line = new GameObject("MenuLaserLine").AddComponent<LineRenderer>();
            _line.transform.SetParent(transform, false);
            _line.useWorldSpace = true;
            _line.material = new Material(Shader.Find("Sprites/Default")); // might change this later
            _line.startWidth = 0.004f;
            _line.endWidth = 0.0025f;
            _line.positionCount = 2;
            _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.enabled = false;
        }

        private static void EnsureResolver()
        {
            var current = PowerUI.Input.OnResolve3D;
            if (ReferenceEquals(current, _installedResolver))
                return;

            PowerUI.Input.OnResolve3D = delegate(out RaycastHit hit, PowerUI.UIEvent ev)
            {
                if (_overrideOwner != null)
                {
                    hit = _overrideHit;
                    return true;
                }

                if (current != null)
                    return current(out hit, ev);

                hit = default(RaycastHit);
                return false;
            };
            _installedResolver = PowerUI.Input.OnResolve3D;
        }

        private void Update()
        {
            if (_controller == null)
                return;

            EnsureResolver();
            Ray ray = new Ray(_aim.position, _aim.forward);

            bool hitMenu = Physics.Raycast(ray, out var hit, MaxDistance, MenuMask);

            bool hitGui = RaycastGui(ray, out var gui, out var guiDist, out var guiPoint);

            bool useGui = hitGui && (!hitMenu || guiDist < hit.distance);
            bool useMenu = hitMenu && !useGui;
            bool powerUi = useMenu && hit.collider.name == "PowerUI-BatchBox";
            bool down = _controller.triggerPressed;

            if (powerUi)
            {
                _overrideOwner = this;
                _overrideHit = hit;
                TrackPowerUi(hit);
            }

            // Laser only while pointing at something clickable
            _line.enabled = useGui || useMenu;
            if (_line.enabled)
            {
                _line.SetPosition(0, _aim.position);
                _line.SetPosition(1, useGui ? guiPoint : hit.point);
                _line.startColor = _line.endColor = down ? PressedColor : HoverColor;
            }

            if (down && !_triggerWasDown)
            {
                if (useGui)
                    ClickGui(gui);
                else if (useMenu && hit.distance >= PokeDistance)
                    ClickMenu(hit, powerUi);
            }
            else if (!down && _triggerWasDown)
            {
                ReleaseMenu();
            }

            _triggerWasDown = down;
            

            if (!powerUi && _overrideOwner == this && !_powerUiDown)
                _overrideOwner = null;
        }

        // PowerUI menus

        private static void TrackPowerUi(RaycastHit hit)
        {
            Transform p = hit.collider.transform.parent;
            if (p == null || p.parent == null)
                return;
            InterfaceAPI.lastMenuTouched = p.parent.gameObject;
            InterfaceAPI.lastMenuTouchPosition = hit.point;
        }

        private void ClickMenu(RaycastHit hit, bool powerUi)
        {
            if (powerUi)
            {
                ReleaseMenu();
                _overrideOwner = this;
                _overrideHit = hit;
                PowerUI.Input.OnMouseDown(0, 0);
                _powerUiDown = true;
            }
            else
            {
                GameObject go = hit.collider.gameObject;
                IPointerClickHandler handler = go.GetComponent<IPointerClickHandler>();
                if (handler != null)
                    handler.OnPointerClick(new PointerEventData(EventSystem.current));
                else
                    go.SendMessage("ClickButton", SendMessageOptions.DontRequireReceiver);
            }

            _controller.HapticPulse(700, 0f);
        }

        private void ReleaseMenu()
        {
            if (!_powerUiDown)
                return;
            _powerUiDown = false;
            PowerUI.Input.OnMouseUp(0, 0);
        }
        
        private bool RaycastGui(Ray ray, out Selectable best, out float bestDist, out Vector3 bestPoint)
        {
            best = null;
            bestDist = MaxDistance;
            bestPoint = Vector3.zero;

            if (Time.unscaledTime >= _nextGuiRefresh)
            {
                _nextGuiRefresh = Time.unscaledTime + 0.3f;
                GUIList.Clear();
                foreach (Selectable s in FindObjectsOfType<Selectable>())
                {
                    Canvas c = s.GetComponentInParent<Canvas>();
                    if (s.transform is RectTransform && c != null && c.renderMode == RenderMode.WorldSpace)
                        GUIList.Add(s);
                }
            }

            foreach (Selectable s in GUIList)
            {
                if (s == null || !s.IsActive() || !s.IsInteractable())
                    continue;

                RectTransform rt = (RectTransform)s.transform;

                if (!new Plane(rt.forward, rt.position).Raycast(ray, out var enter) || enter >= bestDist)
                    continue;

                Vector3 p = ray.GetPoint(enter);
                Vector3 local = rt.InverseTransformPoint(p);

                if (!rt.rect.Contains(new Vector2(local.x, local.y)))
                    continue;

                best = s;
                bestDist = enter;
                bestPoint = p;
            }

            return best != null;
        }

        private void ClickGui(Selectable s)
        {
            Button b = s as Button;
            if (b != null)
                b.onClick.Invoke();
            else
                ExecuteEvents.Execute(s.gameObject, new PointerEventData(EventSystem.current),
                    ExecuteEvents.pointerClickHandler);
            _controller.HapticPulse(700, 0f);
        }

        private void OnDisable()
        {
            ReleaseMenu();
            if (_overrideOwner == this)
                _overrideOwner = null;
            _triggerWasDown = false;
            if (_line != null)
                _line.enabled = false;
        }
    }
}