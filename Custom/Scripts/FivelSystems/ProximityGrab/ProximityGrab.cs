using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SimpleJSON;

namespace FivelSystems
{
    /// <summary>
    /// Proximity Grab (Physics Attachment Engine Edition)
    /// Merges the robustness of Kimowal's PhysicsAttachmentEngine with a Proximity/Trigger workflow.
    /// Uses Physics.OverlapSphere to find grab targets.
    /// 
    /// CREDITS:
    /// Core physics logic and attachment engine based on 'PhysicsAttachmentEngine' by Kimowal.
    /// Adapted for Proximity usage by FivelSystems.
    /// </summary>
    public class ProximityGrab : MVRScript
    {
        // Stiffness presets (Kimowal's values)
        private const int PRESET_SOFT = 0;
        private const int PRESET_FIRM = 1;
        private const int PRESET_LOCK = 2;

        // Attachment modes
        private const int MODE_GRAB = 0;
        private const int MODE_GLUE = 1;
        private const int MODE_LOOSE = 2;
        private const int MODE_PERFECT = 3;

        // Active attachments
        private List<PhysicsAttachment> attachments = new List<PhysicsAttachment>();

        // Offset memory
        private Dictionary<string, OffsetMemory> savedOffsets = new Dictionary<string, OffsetMemory>();

        // UI
        private JSONStorableStringChooser sourceControllerChooser; // "Grab Origin"
        private JSONStorableStringChooser stiffnessPresetChooser;
        private JSONStorableStringChooser attachmentModeChooser;     
        private JSONStorableFloat blendSpeed;
        private JSONStorableBool positionOnly;
        private JSONStorableBool attachInCurrentPose;                
        private JSONStorableString statusText;
        private UIDynamicTextField statusUI;

        // Proximity Settings
        private JSONStorableFloat grabRadius;
        private JSONStorableFloat offsetX;
        private JSONStorableFloat offsetY;
        private JSONStorableFloat offsetZ;
        private JSONStorableBool showDebugSphere;

        // Runtime Logic
        private Rigidbody cachedOriginRB;
        private GameObject debugVisualsParent;
        private LineRenderer circleX;
        private LineRenderer circleY;
        private LineRenderer circleZ;

        // UI for active attachments
        private List<UIDynamic> attachmentUIElements = new List<UIDynamic>();

        public override void Init()
        {
            try
            {
                // 1. Core Actions (Always on Top for easy access)
                // Exposed as "ToggleGrab", "Grab", "Ungrab" for other plugins/triggers
                JSONStorableAction toggleAction = new JSONStorableAction("ToggleGrab", ToggleGrab);
                RegisterAction(toggleAction);
                CreateButton("Toggle Attach").button.onClick.AddListener(ToggleGrab);

                JSONStorableAction attachAction = new JSONStorableAction("Grab", Grab);
                RegisterAction(attachAction);
                CreateButton("Attach").button.onClick.AddListener(Grab);

                JSONStorableAction detachAction = new JSONStorableAction("Ungrab", Ungrab);
                RegisterAction(detachAction);
                CreateButton("Detach All").button.onClick.AddListener(Ungrab);

                JSONStorableAction detachLastAction = new JSONStorableAction("Detach Last", OnDetachLast);
                RegisterAction(detachLastAction);
                CreateButton("Detach Last").button.onClick.AddListener(OnDetachLast);

                CreateSpacer(false).height = 15f;

                // 2. Status
                statusText = new JSONStorableString("Status", "Select Origin, then Attach.");
                RegisterString(statusText);
                statusUI = CreateTextField(statusText);
                statusUI.height = 80f;

                // 3. Proximity Settings
                grabRadius = new JSONStorableFloat("Grab Radius", 0.15f, 0.01f, 0.5f, true);
                RegisterFloat(grabRadius);
                CreateSlider(grabRadius);

                offsetX = new JSONStorableFloat("Offset X", 0f, -0.5f, 0.5f, true);
                RegisterFloat(offsetX);
                CreateSlider(offsetX);

                offsetY = new JSONStorableFloat("Offset Y", 0f, -0.5f, 0.5f, true);
                RegisterFloat(offsetY);
                CreateSlider(offsetY);

                offsetZ = new JSONStorableFloat("Offset Z", 0f, -0.5f, 0.5f, true);
                RegisterFloat(offsetZ);
                CreateSlider(offsetZ);

                showDebugSphere = new JSONStorableBool("Show Debug Sphere", true);
                RegisterBool(showDebugSphere);
                
                CreateToggle(showDebugSphere);

                // 4. Stiffness preset
                stiffnessPresetChooser = new JSONStorableStringChooser("StiffnessPreset",
                    new List<string> { "Soft", "Firm", "Lock" },
                    "Lock", "Stiffness Preset"); 
                RegisterStringChooser(stiffnessPresetChooser);
                CreateScrollablePopup(stiffnessPresetChooser);

                // 5. Attachment Mode
                attachmentModeChooser = new JSONStorableStringChooser("AttachmentMode",
                    new List<string> { "Grab/Hold", "Glue", "Loose Follow", "Perfect Follow" },
                    "Grab/Hold", "Attachment Mode");
                RegisterStringChooser(attachmentModeChooser);
                CreateScrollablePopup(attachmentModeChooser);

                // Blend speed
                blendSpeed = new JSONStorableFloat("BlendSpeed", 2f, 0.1f, 10f, true);
                RegisterFloat(blendSpeed);
                CreateSlider(blendSpeed);

                // Position only toggle
                positionOnly = new JSONStorableBool("PositionOnly", false);
                RegisterBool(positionOnly);
                CreateToggle(positionOnly).label = "Position Only (No Rotation)";

                // Attach in current pose
                attachInCurrentPose = new JSONStorableBool("AttachInCurrentPose", true);
                RegisterBool(attachInCurrentPose);
                CreateToggle(attachInCurrentPose).label = "Attach in Current Pose (Keep Offset)";

                // Initialize Visualization
                InitializeDebugVisuals();
                
                CreateSpacer(false).height = 15f;

                // Version label
                pluginLabelJSON.val = "Proximity Grab v1.2";

                BuildAttachmentUI();
                
                // Moved Origin Setup to Start() to allow VaM to finalize Links/Physics before we cache
                StartCoroutine(InitDelayed());
                
                SuperController.LogMessage("ProximityGrab: UI Initialized.");
            }
            catch (Exception e)
            {
                SuperController.LogError("ProximityGrab Init Error: " + e);
            }
        }

        private IEnumerator InitDelayed()
        {
            // Wait for 2 frames to ensure VaM has fully applied 'LinkTo' constraints
            yield return null;
            yield return null;

            try 
            {
                // 4. Source Controller (Grab Origin)
                var originChoices = matchObjects(containingAtom);
                sourceControllerChooser = new JSONStorableStringChooser("Grab Origin", originChoices, "", "Grab Origin Body");
                RegisterStringChooser(sourceControllerChooser);
                CreateFilterablePopup(sourceControllerChooser);
                sourceControllerChooser.setCallbackFunction += UpdateCachedOrigin;
                
                // Auto-select Hand if available (smart default)
                string defaultHand = originChoices.FirstOrDefault(c => c.ToLower().Contains("rhand") || c.ToLower().Contains("lhand"));
                if (!string.IsNullOrEmpty(defaultHand)) 
                    sourceControllerChooser.val = defaultHand;
                else if (originChoices.Count > 0)
                    sourceControllerChooser.val = originChoices[0];

                CreateButton("Refresh Origin List").button.onClick.AddListener(() => {
                    sourceControllerChooser.choices = matchObjects(containingAtom);
                    if (!sourceControllerChooser.choices.Contains(sourceControllerChooser.val) && sourceControllerChooser.choices.Count > 0)
                        sourceControllerChooser.val = sourceControllerChooser.choices[0];
                    UpdateCachedOrigin(sourceControllerChooser.val);
                });

                // Update cache immediately
                UpdateCachedOrigin(sourceControllerChooser.val);
                
                SuperController.LogMessage("ProximityGrab: Physics Ready. Origin set to: " + sourceControllerChooser.val);
            }
            catch (Exception e)
            {
                SuperController.LogError("ProximityGrab DelayedInit Error: " + e);
            }
        }

        private void InitializeDebugVisuals()
        {
            try 
            {
                debugVisualsParent = new GameObject("ProximityGrab_Visuals");
                debugVisualsParent.transform.SetParent(transform, false);
                debugVisualsParent.layer = LayerMask.NameToLayer("UI");

                // Create 3 circles for the sphere wireframe
                circleX = CreateCircleLiner(debugVisualsParent, "CircleX");
                circleY = CreateCircleLiner(debugVisualsParent, "CircleY");
                circleZ = CreateCircleLiner(debugVisualsParent, "CircleZ");
            }
            catch (Exception e)
            {
                SuperController.LogError("ProximityGrab: Visuals setup failed: " + e);
            }
        }

        private LineRenderer CreateCircleLiner(GameObject parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.layer = LayerMask.NameToLayer("UI");
            
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            
            // Robust Shader Finding
            Shader sh = Shader.Find("Hidden/Internal-Colored");
            if (sh == null) sh = Shader.Find("GUI/Text Shader");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Diffuse");

            if (sh != null)
            {
                Material mat = new Material(sh);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 4000;

                lr.material = mat;
                lr.startColor = new Color(0f, 1f, 0f, 0.5f);
                lr.endColor = new Color(0f, 1f, 0f, 0.5f);
                lr.startWidth = 0.005f;
                lr.endWidth = 0.005f;
                lr.positionCount = 33; // 32 segments + 1 to close loop
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.loop = true;
            }
            return lr;
        }

        private void UpdateCachedOrigin(string val)
        {
            cachedOriginRB = GetRigidbodyFromSelection(containingAtom, val);
            if (cachedOriginRB != null) statusText.val = "Ready. Origin: " + val;
            else statusText.val = "Origin not found. Select a valid part.";
        }

        private Vector3 GetGrabCenter()
        {
            if (cachedOriginRB == null) return transform.position;
            // Apply local offset relative to origin rotation
            Vector3 localOffset = new Vector3(offsetX.val, offsetY.val, offsetZ.val);
            return cachedOriginRB.position + cachedOriginRB.transform.rotation * localOffset;
        }

        public void Update()
        {
            try
            {
                // Update Visualization
                if (debugVisualsParent != null)
                {
                    if (showDebugSphere.val && cachedOriginRB != null)
                    {
                        debugVisualsParent.SetActive(true);
                        Vector3 center = GetGrabCenter();
                        float r = grabRadius.val;
                        
                        UpdateCircle(circleX, center, r, Vector3.right, Vector3.up); // XY plane
                        UpdateCircle(circleY, center, r, Vector3.up, Vector3.forward); // YZ plane
                        UpdateCircle(circleZ, center, r, Vector3.forward, Vector3.right); // ZX plane
                    }
                    else
                    {
                        debugVisualsParent.SetActive(false);
                    }
                }

                // Update active attachments
                for (int i = attachments.Count - 1; i >= 0; i--)
                {
                    var attachment = attachments[i];
                    if (attachment != null)
                    {
                        attachment.Update(Time.deltaTime);
                        if (attachment.ReadyToDestroy())
                        {
                            attachment.Destroy();
                            attachments.RemoveAt(i);
                            BuildAttachmentUI();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // Throttle log? For now just log once per frame if error
                 // SuperController.LogError("ProximityGrab Update: " + e); 
                 // Updating log spam prevention is tricky here, keeping simple for now
            }
        }

        private void UpdateCircle(LineRenderer lr, Vector3 center, float radius, Vector3 axis1, Vector3 axis2)
        {
            if (lr == null) return;
            int segments = 32;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                Vector3 pos = center + (Mathf.Cos(angle) * axis1 + Mathf.Sin(angle) * axis2) * radius;
                lr.SetPosition(i, pos);
            }
        }

        private void ToggleGrab()
        {
            if (attachments.Count > 0) Ungrab();
            else Grab();
        }

        public void Ungrab()
        {
            OnDetachAll();
        }

        public void Grab()
        {
            try
            {
                if (cachedOriginRB == null)
                {
                    statusText.val = "Error: Invalid Origin.";
                    return;
                }

                Vector3 center = GetGrabCenter();
                float radius = grabRadius.val;

                // OverlapSphere
                Collider[] hits = Physics.OverlapSphere(center, radius);
                
                // Sort by distance to center
                System.Array.Sort(hits, (x, y) => {
                    float dX = Vector3.SqrMagnitude(x.transform.position - center);
                    float dY = Vector3.SqrMagnitude(y.transform.position - center);
                    return dX.CompareTo(dY);
                });

                Rigidbody targetRB = null;

                foreach (var hit in hits)
                {
                    Rigidbody rb = hit.attachedRigidbody;
                    if (rb == null) continue;

                    // Logic:
                    // 1. If this plugin atom is "linked" to another object (Parented in VaM UI), exclude that parent.
                    
                    bool shouldExclude = false;
                    
                    Atom parentAtom = null;

                    // Method A: Check Physics Link (The "VaM Way" for parenting Atoms)
                    // We check if the main controller is physically linked to another Rigidbody.
                    if (containingAtom.mainController != null)
                    {
                         FreeControllerV3 mainCtrl = containingAtom.mainController;
                         if (mainCtrl != null && mainCtrl.linkToRB != null)
                         {
                             parentAtom = GetAtomForRigidbody(mainCtrl.linkToRB);
                             // Debug info
                             if (showDebugSphere.val && parentAtom != null)
                                 SuperController.LogMessage($"ProximityGrab: Linked to Parent Atom: {parentAtom.uid}");
                         }
                    }

                    // REMOVED: Transform.parent check (Method B) as it is incorrect for VaM Atoms.

                    if (parentAtom != null)
                    {
                         // We found the Parent Atom.
                         Atom hitAtom = GetAtomForRigidbody(rb);
                         // If the hit object belongs to the Parent Atom, ignore it.
                         if (hitAtom == parentAtom)
                         {
                             shouldExclude = true;
                             if (showDebugSphere.val) SuperController.LogMessage($"ProximityGrab: Ignored {rb.name} (Linked Parent: {parentAtom.uid})");
                         }
                    }

                    // Also exclude the specific RB we are using as the Origin (the hand itself)
                    if (rb == cachedOriginRB) 
                    {
                        shouldExclude = true;
                         if (showDebugSphere.val) SuperController.LogMessage($"ProximityGrab: Ignored {rb.name} (Origin RB)");
                    }

                    if (!shouldExclude)
                    {
                        targetRB = rb;
                        break;
                    }
                }

                if (targetRB == null)
                {
                    statusText.val = "Nothing in range.";
                    return;
                }

                // Create Attachment
                int preset = PRESET_LOCK;
                if (stiffnessPresetChooser.val == "Soft") preset = PRESET_SOFT;
                else if (stiffnessPresetChooser.val == "Firm") preset = PRESET_FIRM;

                int mode = MODE_GRAB;
                if (attachmentModeChooser.val == "Glue") mode = MODE_GLUE;
                else if (attachmentModeChooser.val == "Loose Follow") mode = MODE_LOOSE;
                else if (attachmentModeChooser.val == "Perfect Follow") mode = MODE_PERFECT;

                string sourceName = sourceControllerChooser.val;
                string targetName = targetRB.name;
                string targetAtomName = "Unknown";
                
                Atom targetAtom = GetAtomForRigidbody(targetRB);
                if (targetAtom != null) targetAtomName = targetAtom.uid;

                string offsetKey = string.Format("{0}:{1}→{2}:{3}", containingAtom.uid, sourceName, targetAtomName, targetName);

                Vector3? savedOffsetPos = null;
                Quaternion? savedOffsetRot = null;
                if (attachInCurrentPose.val && savedOffsets.ContainsKey(offsetKey))
                {
                    savedOffsetPos = savedOffsets[offsetKey].position;
                    savedOffsetRot = savedOffsets[offsetKey].rotation;
                }

                var attachment = new PhysicsAttachment(cachedOriginRB, targetRB, preset, blendSpeed.val,
                    containingAtom.uid, sourceName, targetAtomName, targetName, positionOnly.val,
                    mode, attachInCurrentPose.val, "", savedOffsetPos, savedOffsetRot);

                if (attachInCurrentPose.val && !savedOffsets.ContainsKey(offsetKey))
                {
                    savedOffsets[offsetKey] = new OffsetMemory(attachment.GetOffsetPosition(), attachment.GetOffsetRotation());
                }

                if (attachment.IsValid())
                {
                    attachments.Add(attachment);
                    statusText.val = $"Attached to {targetAtomName}:{targetName}";
                    BuildAttachmentUI();
                }
                else
                {
                    statusText.val = "Error creating attachment joint.";
                }

            }
            catch (Exception e)
            {
                SuperController.LogError("ProximityGrab Grab Error: " + e);
                statusText.val = "Error in Grab: " + e.Message;
            }
        }
        
        public void OnDestroy()
        {
            if (debugVisualsParent != null) Destroy(debugVisualsParent);
            
            foreach (var attachment in attachments)
            {
                if (attachment != null) attachment.Destroy();
            }
            attachments.Clear();
        }

        // --- Helpers ---

        private Atom GetAtomForRigidbody(Rigidbody rb)
        {
            Transform t = rb.transform;
            while (t != null)
            {
                Atom a = t.GetComponent<Atom>();
                if (a != null) return a;
                t = t.parent;
            }
            return null;
        }

        private List<string> matchObjects(Atom atom)
        {
            var choices = new List<string>();
            if (atom == null) return choices;

            foreach (var fc in atom.freeControllers)
            {
                if (fc != null) choices.Add(fc.name);
            }

            foreach (var rb in atom.GetComponentsInChildren<Rigidbody>())
            {
                string name = rb.name;
                if (!name.Contains("Collision") && !name.Contains("Trigger") && !choices.Contains(name))
                {
                     if (atom.type == "Person") 
                     {
                         if (name.Contains("hand") || name.Contains("head") || name.Contains("chest") || 
                             name.Contains("hip") || name.Contains("pelvis") || name.Contains("foot"))
                         {
                             choices.Add(name);
                         }
                     }
                     else
                     {
                        choices.Add(name);
                     }
                }
            }
            choices.Sort();
            return choices;
        }

        private Rigidbody GetRigidbodyFromSelection(Atom atom, string selection)
        {
            if (atom == null || string.IsNullOrEmpty(selection)) return null;
            
            var fc = atom.freeControllers.FirstOrDefault(c => c.name == selection);
            if (fc != null)
            {
                // FIX: Do NOT return linkToRB or use missing 'currentRigidbody'
                // Just get the standard Rigidbody component.
                
                var rb = fc.GetComponent<Rigidbody>();
                if (rb != null) return rb;
                return fc.GetComponentInChildren<Rigidbody>();
            }

            var allRBs = atom.GetComponentsInChildren<Rigidbody>();
            return allRBs.FirstOrDefault(r => r.name == selection);
        }

        private void OnDetachAll()
        {
            foreach (var attachment in attachments)
                if (attachment != null) attachment.Destroy();
            attachments.Clear();
            statusText.val = "All attachments removed";
            BuildAttachmentUI();
        }

        private void OnDetachLast()
        {
            if (attachments.Count == 0) return;
            int lastIndex = attachments.Count - 1;
            if (attachments[lastIndex] != null) attachments[lastIndex].Destroy();
            attachments.RemoveAt(lastIndex);
            BuildAttachmentUI();
        }

        private void DetachSingle(int index)
        {
            if (index < 0 || index >= attachments.Count) return;
            var attachment = attachments[index];
            if (attachment != null) attachment.BeginDetach();
        }

        private void BuildAttachmentUI()
        {
            foreach (var elem in attachmentUIElements)
            {
                if (elem is UIDynamicButton) RemoveButton((UIDynamicButton)elem);
                else if (elem is UIDynamicTextField) RemoveTextField((UIDynamicTextField)elem);
                else RemoveSpacer(elem);
            }
            attachmentUIElements.Clear();

            if (attachments.Count == 0) return;

            UIDynamic spacer = CreateSpacer(true);
            spacer.height = 20f;
            attachmentUIElements.Add(spacer);

            var header = CreateTextField(new JSONStorableString("Header", $"Active Attachments ({attachments.Count})"), true);
            attachmentUIElements.Add(header);

            for (int i = 0; i < attachments.Count; i++)
            {
                var att = attachments[i];
                int idx = i;
                string lbl = $"{i+1}. {att.GetTargetName()} [{att.GetPresetName()}]";
                attachmentUIElements.Add(CreateTextField(new JSONStorableString("info"+i, lbl), true));

                var btn = CreateButton("Detach", true);
                btn.buttonColor = new Color(1f, 0.5f, 0.5f);
                btn.button.onClick.AddListener(()=> DetachSingle(idx));
                attachmentUIElements.Add(btn);
            }
        }

        // --- PHYSICS ENGINE CLASSES (Kimowal - Unchanged) ---

        public class PhysicsAttachment
        {
            private Rigidbody sourceRB;
            private Rigidbody targetRB;
            private ConfigurableJoint joint;
            private int preset;
            private float blendSpeed;
            private float currentBlend = 0f;
            private bool isBlendingIn = true;
            private bool isDetaching = false;
            private bool positionOnly = false;
            
            private int attachmentMode = 0;
            private bool useOffset = false;
            private Vector3 offsetPosition = Vector3.zero;
            private Quaternion offsetRotation = Quaternion.identity;

            private string targetName;
            private string targetAtom;

            private static readonly float[] springValues = { 100f, 1000f, 10000f };
            private static readonly float[] damperValues = { 10f, 100f, 1000f };
            private static readonly float[] maxForceValues = { 1000f, 10000f, 1000000f };

            private bool isPerfectFollow = false;
            private bool wasKinematic = false;

            public PhysicsAttachment(Rigidbody source, Rigidbody target, int presetType, float speed,
                string srcAtom, string srcName, string tgtAtom, string tgtName, bool posOnly,
                int mode, bool withOffset, string role, Vector3? savedPos, Quaternion? savedRot)
            {
                sourceRB = source;
                targetRB = target;
                preset = presetType;
                blendSpeed = speed;
                positionOnly = posOnly;
                attachmentMode = mode;
                useOffset = withOffset;
                targetAtom = tgtAtom;
                targetName = tgtName;

                if (useOffset && source != null && target != null)
                {
                    if (savedPos.HasValue && savedRot.HasValue)
                    {
                        offsetPosition = savedPos.Value;
                        offsetRotation = savedRot.Value;
                    }
                    else
                    {
                        offsetPosition = source.transform.InverseTransformPoint(target.transform.position);
                        offsetRotation = Quaternion.Inverse(source.transform.rotation) * target.transform.rotation;
                    }
                }
                CreateJoint();
            }

            private void CreateJoint()
            {
                if (attachmentMode == 3 && targetRB != null) // Perfect Follow
                {
                    isPerfectFollow = true;
                    wasKinematic = targetRB.isKinematic;
                    targetRB.isKinematic = true;
                    currentBlend = 1f;
                    return;
                }

                if (sourceRB == null || targetRB == null) return;

                joint = sourceRB.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = targetRB;

                if (useOffset)
                {
                    joint.anchor = offsetPosition;
                    joint.targetRotation = offsetRotation;
                    joint.configuredInWorldSpace = false;
                }

                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;

                if (positionOnly)
                {
                    joint.angularXMotion = ConfigurableJointMotion.Free;
                    joint.angularYMotion = ConfigurableJointMotion.Free;
                    joint.angularZMotion = ConfigurableJointMotion.Free;
                }
                else
                {
                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                }

                currentBlend = 0f;
                UpdateJointParameters();
            }

            public void Update(float deltaTime)
            {
                if (isPerfectFollow && sourceRB != null && targetRB != null)
                {
                    Vector3 tPos = useOffset ? sourceRB.transform.TransformPoint(offsetPosition) : sourceRB.transform.position;
                    Quaternion tRot = useOffset ? sourceRB.transform.rotation * offsetRotation : sourceRB.transform.rotation;
                    targetRB.MovePosition(tPos);
                    if (!positionOnly) targetRB.MoveRotation(tRot);
                    return;
                }

                if (joint == null) return;

                if (isBlendingIn && currentBlend < 1f)
                {
                    currentBlend += deltaTime * blendSpeed;
                    if (currentBlend >= 1f) { currentBlend = 1f; isBlendingIn = false; }
                    UpdateJointParameters();
                }
                else if (isDetaching && currentBlend > 0f)
                {
                    currentBlend -= deltaTime * blendSpeed;
                    if (currentBlend <= 0f) currentBlend = 0f;
                    UpdateJointParameters();
                }
            }

            private void UpdateJointParameters()
            {
                if (joint == null) return;

                float baseSpring = springValues[preset];
                float baseDamper = damperValues[preset];
                float baseForce = maxForceValues[preset];

                float linS = baseSpring, linD = baseDamper, linF = baseForce;
                float angS = baseSpring, angD = baseDamper, angF = baseForce;

                if (attachmentMode == 0) { angS *= 0.3f; angD *= 0.5f; angF *= 0.5f; }
                else if (attachmentMode == 1) { linD *= 1.2f; angD *= 1.2f; }
                else if (attachmentMode == 2) { linS *= 0.4f; linD *= 0.6f; linF *= 0.7f; angS *= 0.4f; angD *= 0.6f; angF *= 0.7f; }

                float spr = Mathf.Lerp(0f, linS, currentBlend);
                float dmp = Mathf.Lerp(0f, linD, currentBlend);
                float frc = Mathf.Lerp(0f, linF, currentBlend);

                var drive = new JointDrive { positionSpring = spr, positionDamper = dmp, maximumForce = frc };
                joint.xDrive = joint.yDrive = joint.zDrive = drive;

                float aSpr = Mathf.Lerp(0f, angS, currentBlend);
                float aDmp = Mathf.Lerp(0f, angD, currentBlend);
                float aFrc = Mathf.Lerp(0f, angF, currentBlend);
                
                var aDrive = new JointDrive { positionSpring = aSpr, positionDamper = aDmp, maximumForce = aFrc };
                joint.angularXDrive = joint.angularYZDrive = aDrive;
            }

            public void BeginDetach() { isBlendingIn = false; isDetaching = true; }
            public bool ReadyToDestroy() { return isDetaching && currentBlend <= 0f; }
            public bool IsValid() { return joint != null || isPerfectFollow; }
            public void Destroy()
            {
                if (isPerfectFollow && targetRB != null) targetRB.isKinematic = wasKinematic;
                if (joint != null) UnityEngine.Object.Destroy(joint);
            }
            public string GetTargetName() { return $"{targetAtom}/{targetName}"; }
            public string GetPresetName() 
            {
                if (attachmentMode == 3) return "Perfect";
                switch(preset) { case 0: return "Soft"; case 1: return "Firm"; case 2: return "Lock"; default: return "Unknown"; }
            }
            public Vector3 GetOffsetPosition() { return offsetPosition; }
            public Quaternion GetOffsetRotation() { return offsetRotation; }

            public JSONClass GetJSON() { return new JSONClass(); }
        }

        public class OffsetMemory
        {
            public Vector3 position;
            public Quaternion rotation;
            public OffsetMemory(Vector3 p, Quaternion r) { position = p; rotation = r; }
        }
    }
}
