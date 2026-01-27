using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

namespace FivelSystems
{
    /// <summary>
    /// Proximity Grab (Refactored)
    /// Architecture: Facade Pattern.
    /// ProximityGrab coordinates specialized modules (UI, Scanner, Visualizer, AttachmentManager).
    /// </summary>
    public class ProximityGrab : MVRScript
    {
        // --- Enums (Type Safety) ---
        public enum Stiffness { Soft = 0, Firm = 1, Lock = 2 }
        public enum GrabMode { Hold = 0, Glue = 1, Loose = 2, Perfect = 3 }

        // --- Configuration Struct ---
        public struct GrabbingConfig
        {
            public Stiffness Stiffness;
            public GrabMode Mode;
            public float BlendSpeed;
            public bool PositionOnly;
            public bool KeepOffset;
        }

        // --- Modules ---
        private ProximityUI _ui;
        private ProximityScanner _scanner;
        private ProximityVisualizer _visualizer;
        private AttachmentManager _attachmentManager;

        // --- State ---
        private Rigidbody _originRB;
        private Dictionary<string, OffsetMemory> _savedOffsets = new Dictionary<string, OffsetMemory>();

        // --- Unity Lifecycle ---

        public override void Init()
        {
            try
            {
                // 1. Initialize Modules
                _scanner = new ProximityScanner((msg) => SuperController.LogMessage(msg));
                _visualizer = new ProximityVisualizer(this);
                _attachmentManager = new AttachmentManager(this);
                
                // 2. Initialize UI (Builder Pattern)
                _ui = new ProximityUI(this, _attachmentManager);
                _ui.BuildCoreControls();
                
                // 3. Connect Events
                _ui.OnGrabRequest += PerformGrab;
                _ui.OnUngrabRequest += PerformUngrab;
                _ui.OnOriginChanged += UpdateOrigin;

                // 4. Delayed Physics Setup
                StartCoroutine(InitDelayed());
                
                SuperController.LogMessage("ProximityGrab: Initialized (Refactored).");
            }
            catch (Exception e)
            {
                SuperController.LogError($"ProximityGrab Init Failed: {e}");
            }
        }

        private IEnumerator InitDelayed()
        {
            // Wait for VaM physics/links to settle
            yield return null;
            yield return null;
            _ui.RefreshOriginList();
        }

        public void Update()
        {
            try
            {
                // Visualization
                if (_ui.ShowDebugSphere.val && _originRB != null)
                {
                    _visualizer.SetVisibility(true);
                    _visualizer.DrawSphere(GetGrabCenter(), _ui.GrabRadius.val);
                }
                else
                {
                    _visualizer.SetVisibility(false);
                }

                // Physics Updates
                _attachmentManager.UpdateAttachments(Time.deltaTime);
            }
            catch (Exception e)
            {
                // Throttle logging in production if needed
            }
        }

        public void OnDestroy()
        {
            _visualizer?.Destroy();
            _attachmentManager?.DestroyAll();
        }

        // --- Core Logic ---

        private void PerformGrab()
        {
            if (_originRB == null)
            {
                _ui.SetStatus("Error: Invalid Origin.");
                return;
            }

            // 1. Scan
            Vector3 center = GetGrabCenter();
            var targetRB = _scanner.ScanForTarget(center, _ui.GrabRadius.val, containingAtom, _originRB, _ui.ShowDebugSphere.val);
            
            if (targetRB == null)
            {
                _ui.SetStatus("Nothing in range.");
                return;
            }

            // 2. Prepare Config
            var config = _ui.GetConfig();
            
            // 3. Resolve Names & Offsets
            string sourceName = _ui.GetSelectedOriginName();
            string targetName = targetRB.name;
            Atom targetAtom = _scanner.GetAtomForRigidbody(targetRB);
            string targetAtomName = targetAtom != null ? targetAtom.uid : "Unknown";

            string offsetKey = $"{containingAtom.uid}:{sourceName}→{targetAtomName}:{targetName}";
            
            Vector3? savedPos = null;
            Quaternion? savedRot = null;

            if (config.KeepOffset && _savedOffsets.ContainsKey(offsetKey))
            {
                savedPos = _savedOffsets[offsetKey].Position;
                savedRot = _savedOffsets[offsetKey].Rotation;
            }

            // 4. Create Attachment
            var attachment = new PhysicsAttachment(_originRB, targetRB, config, containingAtom.uid, sourceName, targetAtomName, targetName, savedPos, savedRot);

            if (attachment.IsValid())
            {
                // Save Offset if new
                if (config.KeepOffset && !_savedOffsets.ContainsKey(offsetKey))
                {
                    _savedOffsets[offsetKey] = new OffsetMemory(attachment.GetOffsetPosition(), attachment.GetOffsetRotation());
                }

                _attachmentManager.Add(attachment);
                _ui.SetStatus($"Attached to {targetAtomName}:{targetName}");
            }
            else
            {
                _ui.SetStatus("Error creating joint.");
            }
        }

        private void PerformUngrab()
        {
            _attachmentManager.DestroyAll();
            _ui.SetStatus("Detached all.");
        }

        private void UpdateOrigin(string selection)
        {
            _originRB = _scanner.GetRigidbodyFromSelection(containingAtom, selection);
            _ui.SetStatus(_originRB != null ? $"Ready. Origin: {selection}" : "Origin not found.");
        }

        public Vector3 GetGrabCenter()
        {
            if (_originRB == null) return transform.position;
            return _originRB.transform.TransformPoint(_ui.GetOffsetVector());
        }

        // --- Modules (Nested Classes) ---

        /// <summary>
        /// Handles all UI generation and interaction (Builder Pattern).
        /// </summary>
        public class ProximityUI
        {
            private ProximityGrab _facade;
            private AttachmentManager _mgr;

            // Inputs
            public JSONStorableFloat GrabRadius;
            public JSONStorableBool ShowDebugSphere;
            public JSONStorableStringChooser StiffnessChooser;
            public JSONStorableStringChooser ModeChooser;
            // Configs
            private JSONStorableFloat _offsetX, _offsetY, _offsetZ;
            private JSONStorableFloat _blendSpeed;
            private JSONStorableBool _positionOnly;
            private JSONStorableBool _keepOffset;
            private JSONStorableString _statusText;
            private JSONStorableStringChooser _originChooser;
            private UIDynamicTextField _statusUI;

            // Events
            public event Action OnGrabRequest;
            public event Action OnUngrabRequest;
            public event Action<string> OnOriginChanged;

            public ProximityUI(ProximityGrab facade, AttachmentManager mgr)
            {
                _facade = facade;
                _mgr = mgr;
                _mgr.OnListChanged += RebuildAttachmentList; // Observer
            }

            public void BuildCoreControls()
            {
                // 1. Actions
                _facade.CreateButton("Toggle Attach").button.onClick.AddListener(() => {
                    if (_mgr.Count > 0) OnUngrabRequest?.Invoke(); else OnGrabRequest?.Invoke();
                });
                _facade.CreateButton("Attach").button.onClick.AddListener(()=> OnGrabRequest?.Invoke());
                _facade.CreateButton("Detach All").button.onClick.AddListener(()=> OnUngrabRequest?.Invoke());
                _facade.CreateButton("Detach Last").button.onClick.AddListener(() => _mgr.DestroyLast());

                _facade.RegisterAction(new JSONStorableAction("Grab", () => OnGrabRequest?.Invoke()));
                _facade.RegisterAction(new JSONStorableAction("Ungrab", () => OnUngrabRequest?.Invoke()));
                _facade.RegisterAction(new JSONStorableAction("ToggleGrab", () => {
                    if (_mgr.Count > 0) OnUngrabRequest?.Invoke(); else OnGrabRequest?.Invoke();
                }));

                _facade.CreateSpacer(false).height = 15f;

                // 2. Status
                _statusText = new JSONStorableString("Status", "Select Origin, then Attach.");
                _facade.RegisterString(_statusText);
                _statusUI = _facade.CreateTextField(_statusText);
                _statusUI.height = 80f;

                // 3. Settings
                GrabRadius = new JSONStorableFloat("Grab Radius", 0.15f, 0.01f, 0.5f, true);
                _facade.RegisterFloat(GrabRadius);
                _facade.CreateSlider(GrabRadius);

                _offsetX = new JSONStorableFloat("Offset X", 0f, -0.5f, 0.5f, true);
                _offsetY = new JSONStorableFloat("Offset Y", 0f, -0.5f, 0.5f, true);
                _offsetZ = new JSONStorableFloat("Offset Z", 0f, -0.5f, 0.5f, true);
                _facade.RegisterFloat(_offsetX); _facade.RegisterFloat(_offsetY); _facade.RegisterFloat(_offsetZ);
                _facade.CreateSlider(_offsetX); _facade.CreateSlider(_offsetY); _facade.CreateSlider(_offsetZ);

                ShowDebugSphere = new JSONStorableBool("Show Debug Sphere", true);
                _facade.RegisterBool(ShowDebugSphere);
                _facade.CreateToggle(ShowDebugSphere);

                // 4. Configs
                StiffnessChooser = new JSONStorableStringChooser("Stiffness", new List<string> { "Soft", "Firm", "Lock" }, "Lock", "Stiffness");
                _facade.RegisterStringChooser(StiffnessChooser);
                _facade.CreateScrollablePopup(StiffnessChooser);

                ModeChooser = new JSONStorableStringChooser("Mode", new List<string> { "Grab/Hold", "Glue", "Loose Follow", "Perfect Follow" }, "Grab/Hold", "Mode");
                _facade.RegisterStringChooser(ModeChooser);
                _facade.CreateScrollablePopup(ModeChooser);

                _blendSpeed = new JSONStorableFloat("BlendSpeed", 2f, 0.1f, 10f, true);
                _facade.RegisterFloat(_blendSpeed);
                _facade.CreateSlider(_blendSpeed);

                _positionOnly = new JSONStorableBool("PositionOnly", false);
                _facade.RegisterBool(_positionOnly);
                _facade.CreateToggle(_positionOnly).label = "Position Only (No Rotation)";

                _keepOffset = new JSONStorableBool("KeepOffset", true);
                _facade.RegisterBool(_keepOffset);
                _facade.CreateToggle(_keepOffset).label = "Attach in Current Pose";

                _facade.CreateSpacer(false).height = 15f;

                // 5. Origin Chooser (Placeholder, populated later)
                _originChooser = new JSONStorableStringChooser("Grab Origin", new List<string>(), "", "Grab Origin");
                _facade.RegisterStringChooser(_originChooser);
                _facade.CreateFilterablePopup(_originChooser);
                _originChooser.setCallbackFunction += (val) => OnOriginChanged?.Invoke(val);

                _facade.CreateButton("Refresh Origin List").button.onClick.AddListener(RefreshOriginList);
            }

            public void RefreshOriginList()
            {
                var choices = MatchObjects(_facade.containingAtom);
                _originChooser.choices = choices;
                
                // Smart Default
                if (string.IsNullOrEmpty(_originChooser.val) || !_originChooser.choices.Contains(_originChooser.val))
                {
                    string smartDefault = choices.FirstOrDefault(c => c.ToLower().Contains("rhand")) 
                                        ?? choices.FirstOrDefault(c => c.ToLower().Contains("lhand"))
                                        ?? choices.FirstOrDefault();
                    _originChooser.val = smartDefault;
                }
                
                // Force Update
                OnOriginChanged?.Invoke(_originChooser.val);
            }

            private List<string> MatchObjects(Atom atom)
            {
                var list = new List<string>();
                if (atom == null) return list;

                foreach (var fc in atom.freeControllers)
                    if (fc != null) list.Add(fc.name);

                foreach (var rb in atom.GetComponentsInChildren<Rigidbody>())
                {
                    string n = rb.name;
                    if (n.Contains("Collision") || n.Contains("Trigger") || list.Contains(n)) continue;
                    
                    if (atom.type == "Person") {
                        if (CanPickPersonPart(n)) list.Add(n);
                    } else {
                        list.Add(n);
                    }
                }
                list.Sort();
                return list;
            }

            private bool CanPickPersonPart(string name)
            {
                return name.Contains("hand") || name.Contains("head") || name.Contains("chest") || 
                       name.Contains("hip") || name.Contains("pelvis") || name.Contains("foot");
            }

            // --- Accessors ---
            public void SetStatus(string msg) => _statusText.val = msg;
            public Vector3 GetOffsetVector() => new Vector3(_offsetX.val, _offsetY.val, _offsetZ.val);
            public string GetSelectedOriginName() => _originChooser.val;

            public GrabbingConfig GetConfig()
            {
                return new GrabbingConfig
                {
                    Stiffness = ParseStiffness(StiffnessChooser.val),
                    Mode = ParseMode(ModeChooser.val),
                    BlendSpeed = _blendSpeed.val,
                    PositionOnly = _positionOnly.val,
                    KeepOffset = _keepOffset.val
                };
            }

            private Stiffness ParseStiffness(string val)
            {
                if (val == "Soft") return Stiffness.Soft;
                if (val == "Firm") return Stiffness.Firm;
                return Stiffness.Lock;
            }

            private GrabMode ParseMode(string val)
            {
                if (val == "Glue") return GrabMode.Glue;
                if (val == "Loose Follow") return GrabMode.Loose;
                if (val == "Perfect Follow") return GrabMode.Perfect;
                return GrabMode.Hold;
            }

            // --- Dynamic UI for Attachments ---
            private List<UIDynamic> _dynamicUI = new List<UIDynamic>();

            private void RebuildAttachmentList()
            {
                // Clear old
                foreach (var item in _dynamicUI)
                {
                    if (item is UIDynamicButton b) _facade.RemoveButton(b);
                    else if (item is UIDynamicTextField t) _facade.RemoveTextField(t);
                    else _facade.RemoveSpacer(item);
                }
                _dynamicUI.Clear();

                // Build new
                 var attachments = _mgr.GetAttachments();
                 if (attachments.Count == 0) return;

                 var sp = _facade.CreateSpacer(true); sp.height = 20f;
                 _dynamicUI.Add(sp);

                 var head = _facade.CreateTextField(new JSONStorableString("H", $"Active ({attachments.Count})"), true);
                 _dynamicUI.Add(head);

                 for(int i=0; i<attachments.Count; i++)
                 {
                     var att = attachments[i];
                     int idx = i;
                     string info = $"{i+1}. {att.TargetName} [{att.Config.Mode}]";
                     _dynamicUI.Add(_facade.CreateTextField(new JSONStorableString("i"+i, info), true));
                     
                     var btn = _facade.CreateButton("Detach", true);
                     btn.buttonColor = new Color(1f, 0.5f, 0.5f);
                     btn.button.onClick.AddListener(()=> _mgr.DestroyAt(idx));
                     _dynamicUI.Add(btn);
                 }
            }
        }

        /// <summary>
        /// Manages the collection of active physics attachments.
        /// </summary>
        public class AttachmentManager
        {
            private List<PhysicsAttachment> _items = new List<PhysicsAttachment>();
            private MonoBehaviour _host;
            public event Action OnListChanged;

            public AttachmentManager(MonoBehaviour host) { _host = host; }
            public int Count => _items.Count;

            public void Add(PhysicsAttachment att)
            {
                if (att == null) return;
                _items.Add(att);
                OnListChanged?.Invoke();
            }

            public void UpdateAttachments(float dt)
            {
                for (int i = _items.Count - 1; i >= 0; i--)
                {
                    var att = _items[i];
                    att.Update(dt);
                    if (att.ReadyToDestroy)
                    {
                        att.Destroy();
                        _items.RemoveAt(i);
                        OnListChanged?.Invoke();
                    }
                }
            }

            public void DestroyAt(int index)
            {
                if (index >= 0 && index < _items.Count)
                {
                    _items[index].BeginDetach(); // Smooth detach
                }
            }

            public void DestroyLast() => DestroyAt(_items.Count - 1);

            public void DestroyAll()
            {
                foreach (var att in _items) att.Destroy();
                _items.Clear();
                OnListChanged?.Invoke();
            }

            public List<PhysicsAttachment> GetAttachments() => _items;
        }

        /// <summary>
        /// Scans for rigidbodies in range (Service).
        /// </summary>
        public class ProximityScanner
        {
            private Action<string> _logger;
            public ProximityScanner(Action<string> logger) => _logger = logger;

            public Rigidbody ScanForTarget(Vector3 center, float radius, Atom sourceAtom, Rigidbody sourceRB, bool debug)
            {
                var hits = Physics.OverlapSphere(center, radius);
                if (hits.Length == 0) return null;

                // LINQ is okay here, not every frame usually (only on Grab click)
                var sorted = hits.OrderBy(h => Vector3.SqrMagnitude(h.transform.position - center));

                foreach (var hit in sorted)
                {
                    var rb = hit.attachedRigidbody;
                    if (rb != null && IsValidTarget(rb, sourceAtom, sourceRB, debug))
                        return rb;
                }
                return null;
            }

            private bool IsValidTarget(Rigidbody rb, Atom sourceAtom, Rigidbody sourceRB, bool debug)
            {
                if (rb == sourceRB) return false;
                
                // Check Parent Link
                Atom parent = GetLinkedParent(sourceAtom);
                if (parent != null)
                {
                    if (GetAtomForRigidbody(rb) == parent)
                    {
                        if (debug) _logger?.Invoke($"Ignored {rb.name} (Parent: {parent.uid})");
                        return false;
                    }
                }
                return true;
            }

            private Atom GetLinkedParent(Atom atom)
            {
                if (atom?.mainController?.linkToRB != null)
                    return GetAtomForRigidbody(atom.mainController.linkToRB);
                return null;
            }

            public Atom GetAtomForRigidbody(Rigidbody rb)
            {
                Transform t = rb.transform;
                while(t != null) {
                    var a = t.GetComponent<Atom>();
                    if (a != null) return a;
                    t = t.parent;
                }
                return null;
            }

            public Rigidbody GetRigidbodyFromSelection(Atom atom, string name)
            {
                if (atom == null || string.IsNullOrEmpty(name)) return null;
                var fc = atom.freeControllers.FirstOrDefault(c => c.name == name);
                if (fc != null) {
                    var rb = fc.GetComponent<Rigidbody>();
                    return rb ? rb : fc.GetComponentInChildren<Rigidbody>();
                }
                return atom.GetComponentsInChildren<Rigidbody>().FirstOrDefault(r => r.name == name);
            }
        }

        /// <summary>
        /// Visualizes the grab radius (View).
        /// </summary>
        public class ProximityVisualizer
        {
            private GameObject _root;
            private LineRenderer _x, _y, _z;

            public ProximityVisualizer(MonoBehaviour parent)
            {
                _root = new GameObject("ProximityVisuals");
                _root.transform.SetParent(parent.transform, false);
                _root.layer = LayerMask.NameToLayer("UI");
                _x = CreateCircle(_root); _y = CreateCircle(_root); _z = CreateCircle(_root);
            }

            public void SetVisibility(bool v) => _root.SetActive(v);
            public void Destroy() => UnityEngine.Object.Destroy(_root);

            public void DrawSphere(Vector3 center, float r)
            {
                if (!_root.activeSelf) return;
                DrawCircle(_x, center, r, Vector3.right, Vector3.up);
                DrawCircle(_y, center, r, Vector3.up, Vector3.forward);
                DrawCircle(_z, center, r, Vector3.forward, Vector3.right);
            }

            private void DrawCircle(LineRenderer lr, Vector3 c, float r, Vector3 a1, Vector3 a2)
            {
                for(int i=0; i<=32; i++) {
                    float angle = (float)i/32f * Mathf.PI * 2f;
                    lr.SetPosition(i, c + (Mathf.Cos(angle)*a1 + Mathf.Sin(angle)*a2) * r);
                }
            }

            private LineRenderer CreateCircle(GameObject p)
            {
                var go = new GameObject("Circle"); go.transform.SetParent(p.transform, false); go.layer = p.layer;
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.widthMultiplier = 0.005f;
                lr.positionCount = 33;
                lr.material = new Material(Shader.Find("Sprites/Default")); // Simplified shader
                lr.startColor = lr.endColor = new Color(0,1,0,0.5f);
                return lr;
            }
        }

        /// <summary>
        /// A single physics joint connection (Domain Object).
        /// </summary>
        public class PhysicsAttachment
        {
            public GrabbingConfig Config;
            public string SourceName, TargetName, TargetAtom;
            public bool ReadyToDestroy => _isDetaching && _currentBlend <= 0f;

            private Rigidbody _source, _target;
            private ConfigurableJoint _joint;
            private float _currentBlend = 0f;
            private bool _isBlendingIn = true;
            private bool _isDetaching = false;
            
            private bool _wasKinematic;
            private Vector3 _offsetPos;
            private Quaternion _offsetRot;

            public PhysicsAttachment(Rigidbody src, Rigidbody tgt, GrabbingConfig cfg, 
                string srcAtomName, string srcPart, string tgtAtomName, string tgtPart, 
                Vector3? savedPos, Quaternion? savedRot)
            {
                _source = src;
                _target = tgt;
                Config = cfg;
                SourceName = srcPart;
                TargetName = tgtPart;
                TargetAtom = tgtAtomName;

                if (Config.KeepOffset)
                {
                    if (savedPos.HasValue) { _offsetPos = savedPos.Value; _offsetRot = savedRot.Value; }
                    else {
                        _offsetPos = src.transform.InverseTransformPoint(tgt.transform.position);
                        _offsetRot = Quaternion.Inverse(src.transform.rotation) * tgt.transform.rotation;
                    }
                }
                
                CreateJoint();
            }

            private void CreateJoint()
            {
                if (Config.Mode == GrabMode.Perfect)
                {
                    _wasKinematic = _target.isKinematic;
                    _target.isKinematic = true;
                    _currentBlend = 1f;
                    return;
                }

                _joint = _source.gameObject.AddComponent<ConfigurableJoint>();
                _joint.connectedBody = _target;
                
                if (Config.KeepOffset) {
                    _joint.anchor = _offsetPos;
                    _joint.targetRotation = _offsetRot;
                    _joint.configuredInWorldSpace = false;
                }

                _joint.xMotion = _joint.yMotion = _joint.zMotion = ConfigurableJointMotion.Locked;
                var angMotion = Config.PositionOnly ? ConfigurableJointMotion.Free : ConfigurableJointMotion.Locked;
                _joint.angularXMotion = _joint.angularYMotion = _joint.angularZMotion = angMotion;

                _currentBlend = 0f;
                UpdateDrive();
            }

            public void Update(float dt)
            {
                if (Config.Mode == GrabMode.Perfect) {
                    if (_source == null || _target == null) return;
                    // Manual Transform copy for "Perfect" mode
                    Vector3 tsPos = Config.KeepOffset ? _source.transform.TransformPoint(_offsetPos) : _source.transform.position;
                    Quaternion tsRot = Config.KeepOffset ? _source.transform.rotation * _offsetRot : _source.transform.rotation;
                    _target.MovePosition(tsPos);
                    if (!Config.PositionOnly) _target.MoveRotation(tsRot);
                    return;
                }

                if (_joint == null) return;

                if (_isBlendingIn) {
                    _currentBlend += dt * Config.BlendSpeed;
                    if (_currentBlend >= 1f) { _currentBlend = 1f; _isBlendingIn = false; }
                    UpdateDrive();
                }
                else if (_isDetaching) {
                    _currentBlend -= dt * Config.BlendSpeed;
                    if (_currentBlend <= 0f) _currentBlend = 0f;
                    UpdateDrive();
                }
            }

            private void UpdateDrive()
            {
                if (_joint == null) return;
                
                // Kimowal's magic numbers
                float spr = 1000f, dmp = 100f, frc = 10000f;
                if (Config.Stiffness == Stiffness.Soft) { spr = 100f; dmp = 10f; frc = 1000f; }
                else if (Config.Stiffness == Stiffness.Lock) { spr = 10000f; dmp = 1000f; frc = 1000000f; }

                if (Config.Mode == GrabMode.Loose) { spr *= 0.4f; dmp *= 0.6f; }

                float curSpr = Mathf.Lerp(0, spr, _currentBlend);
                float curDmp = Mathf.Lerp(0, dmp, _currentBlend);
                float curFrc = Mathf.Lerp(0, frc, _currentBlend);

                var drv = new JointDrive { positionSpring=curSpr, positionDamper=curDmp, maximumForce=curFrc };
                _joint.xDrive = _joint.yDrive = _joint.zDrive = drv;
                _joint.angularXDrive = _joint.angularYZDrive = drv;
            }

            public void BeginDetach() { _isBlendingIn = false; _isDetaching = true; }
            public void Destroy() {
                if (Config.Mode == GrabMode.Perfect && _target != null) _target.isKinematic = _wasKinematic;
                if (_joint != null) UnityEngine.Object.Destroy(_joint);
            }
            public bool IsValid() => _joint != null || Config.Mode == GrabMode.Perfect;
            
            public Vector3 GetOffsetPosition() => _offsetPos;
            public Quaternion GetOffsetRotation() => _offsetRot;
        }

        public class OffsetMemory
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public OffsetMemory(Vector3 p, Quaternion r) { Position = p; Rotation = r; }
        }
    }
}
