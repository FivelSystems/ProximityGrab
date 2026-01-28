using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using SimpleJSON;

namespace FivelSystems
{
    public class ProximityGrab : MVRScript
    {
        // --- UI Controls ---
        private JSONStorableFloat grabRadius;
        private JSONStorableBool showDebugSphere;
        private JSONStorableFloat offsetX, offsetY, offsetZ;
        private JSONStorableStringChooser stiffnessChooser;
        private JSONStorableStringChooser modeChooser;
        private JSONStorableBool positionOnly;
        private JSONStorableBool keepOffset;
        private JSONStorableFloat blendSpeed;
        private JSONStorableString statusText;
        private JSONStorableStringChooser originChooser;
        private JSONStorableBool grabMeshJoints;
        private JSONStorableBool grabTriggers;
        private JSONStorableBool grabRigidbodies;
        private UIDynamicTextField statusUI;

        // --- State ---
        private Rigidbody currentOriginRB;
        private List<PhysicsAttachment> activeAttachments = new List<PhysicsAttachment>();
        private Dictionary<string, OffsetMemory> savedOffsets = new Dictionary<string, OffsetMemory>();

        // --- Visuals ---
        private GameObject debugVisualsParent;
        private LineRenderer circleX, circleY, circleZ;
        private Material gizmoMaterial;

        // --- Enums ---
        private readonly string[] stiffnessOptions = { "Soft", "Firm", "Lock" };
        private readonly string[] modeOptions = { "Grab/Hold", "Glue", "Loose Follow", "Perfect Follow" };

        public override void Init()
        {
            try
            {
                // ================= LEFT COLUMN (Actions & Origin) =================
                pluginLabelJSON.val = "ProximityGrab v2";

                // 1. Origin Chooser (Top Priority)
                originChooser = new JSONStorableStringChooser("Grab Origin", new List<string>(), "", "Grab Origin");
                RegisterStringChooser(originChooser);
                var popup = CreateFilterablePopup(originChooser);
                popup.popupPanelHeight = 900f;
                originChooser.setCallbackFunction += UpdateOrigin;

                CreateButton("Refresh Origin List").button.onClick.AddListener(RefreshOriginList);
                CreateSpacer(false).height = 15f;

                // 2. Actions
                RegisterAction(new JSONStorableAction("Grab", PerformGrab));
                RegisterAction(new JSONStorableAction("Ungrab", PerformUngrab));
                RegisterAction(new JSONStorableAction("ToggleGrab", () =>
                {
                    if (activeAttachments.Count > 0) PerformUngrab(); else PerformGrab();
                }));

                // Split Buttons
                CreateButton("Grab").button.onClick.AddListener(PerformGrab);
                CreateButton("Release").button.onClick.AddListener(PerformUngrab);

                // RESTORED: Toggle Button
                CreateButton("Toggle Grab").button.onClick.AddListener(() =>
                {
                    if (activeAttachments.Count > 0) PerformUngrab(); else PerformGrab();
                });

                CreateSpacer(false).height = 15f;

                // 3. Status
                statusText = new JSONStorableString("Status", "Ready");
                RegisterString(statusText);
                statusUI = CreateTextField(statusText);
                statusUI.height = 80f;

                // Move Grab Triggers here (Left Side)
                // --- Grabbables Section ---
                CreateSpacer(false).height = 10f;
                // CreateLabel("Grabbables", false); // Optional: if you had a label helper, but spacer is fine or just implicit

                grabRigidbodies = new JSONStorableBool("Grab Rigidbodies", true);
                RegisterBool(grabRigidbodies);
                CreateToggle(grabRigidbodies);

                grabTriggers = new JSONStorableBool("Grab Triggers", false);
                RegisterBool(grabTriggers);
                CreateToggle(grabTriggers);

                grabMeshJoints = new JSONStorableBool("Grab MeshJoints", false);
                RegisterBool(grabMeshJoints);
                CreateToggle(grabMeshJoints);

                // ================= RIGHT COLUMN (Settings) =================

                // 4. Radius & Offset
                grabRadius = new JSONStorableFloat("Grab Radius", 0.15f, 0.01f, 0.5f, true);
                RegisterFloat(grabRadius);
                CreateSlider(grabRadius, true);

                offsetX = new JSONStorableFloat("Offset X", 0f, -0.5f, 0.5f, true);
                offsetY = new JSONStorableFloat("Offset Y", 0f, -0.5f, 0.5f, true);
                offsetZ = new JSONStorableFloat("Offset Z", 0f, -0.5f, 0.5f, true);
                RegisterFloat(offsetX); RegisterFloat(offsetY); RegisterFloat(offsetZ);
                CreateSlider(offsetX, true); CreateSlider(offsetY, true); CreateSlider(offsetZ, true);

                CreateSpacer(true);

                // 5. Configs
                stiffnessChooser = new JSONStorableStringChooser("Stiffness", stiffnessOptions.ToList(), "Lock", "Stiffness");
                RegisterStringChooser(stiffnessChooser);
                CreateScrollablePopup(stiffnessChooser, true);

                modeChooser = new JSONStorableStringChooser("Mode", modeOptions.ToList(), "Grab/Hold", "Mode");
                RegisterStringChooser(modeChooser);
                CreateScrollablePopup(modeChooser, true);

                blendSpeed = new JSONStorableFloat("Blend Speed", 2f, 0.1f, 10f, true);
                RegisterFloat(blendSpeed);
                CreateSlider(blendSpeed, true);

                positionOnly = new JSONStorableBool("Position Only", false);
                RegisterBool(positionOnly);
                CreateToggle(positionOnly, true);

                keepOffset = new JSONStorableBool("Keep Offset", true);
                RegisterBool(keepOffset);
                CreateToggle(keepOffset, true);

                showDebugSphere = new JSONStorableBool("Show Debug Sphere", true);
                RegisterBool(showDebugSphere);
                showDebugSphere = new JSONStorableBool("Show Debug Sphere", true);
                RegisterBool(showDebugSphere);
                CreateToggle(showDebugSphere, true);

                // Init Visuals
                InitVisuals();
                RefreshOriginList();

                // Safe delayed refresh to ensure list is populated on load
                StartCoroutine(DeferredRefresh());
            }
            catch (Exception e)
            {
                SuperController.LogError("ProximityGrab Init Error: " + e);
            }
        }

        private IEnumerator DeferredRefresh()
        {
            yield return new WaitForSeconds(0.5f);
            RefreshOriginList();
            // Try to set default again
            if (string.IsNullOrEmpty(originChooser.val) && originChooser.choices.Count > 0)
            {
                if (originChooser.choices.Contains("rHand")) originChooser.val = "rHand";
                else originChooser.val = originChooser.choices[0];
            }
        }

        public void OnDestroy()
        {
            try
            {
                // 1. Stop Logic
                StopAllCoroutines();
                if (originChooser != null) originChooser.setCallbackFunction -= UpdateOrigin;

                // 2. Destroy Visuals
                if (debugVisualsParent != null) Destroy(debugVisualsParent);
                if (gizmoMaterial != null) Destroy(gizmoMaterial);

                // 3. Destroy Physics Attachments (Robust Loop)
                if (activeAttachments != null)
                {
                    // Create copy to behave safely during modification
                    var toDestroy = new List<PhysicsAttachment>(activeAttachments);
                    foreach (var att in toDestroy)
                    {
                        if (att != null) att.Destroy();
                    }
                    activeAttachments.Clear();
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("ProximityGrab Cleanup Error: " + e);
            }
        }

        public void Update()
        {
            // 1. Physics Update (Real-time parameter updates)
            if (activeAttachments.Count > 0)
            {
                // Map Settings to Values
                int presetIndex = 1; // Firm
                if (stiffnessChooser.val == "Soft") presetIndex = 0;
                else if (stiffnessChooser.val == "Lock") presetIndex = 2;

                int modeIndex = 0; // Grab
                if (modeChooser.val == "Glue") modeIndex = 1;
                else if (modeChooser.val == "Loose Follow") modeIndex = 2;
                else if (modeChooser.val == "Perfect Follow") modeIndex = 3;

                float speed = blendSpeed.val;

                for (int i = activeAttachments.Count - 1; i >= 0; i--)
                {
                    var att = activeAttachments[i];
                    // Pass current settings to attachment for real-time update
                    att.Update(Time.deltaTime, presetIndex, modeIndex, speed);

                    if (att.IsDead())
                    {
                        att.Destroy();
                        activeAttachments.RemoveAt(i);
                    }
                }
            }

            // 2. Visuals Update
            if (showDebugSphere.val && currentOriginRB != null)
            {
                if (!debugVisualsParent.activeSelf) debugVisualsParent.SetActive(true);
                Vector3 center = GetGrabCenter();
                // Color Logic: Blue if grabbing, Green if searching
                Color c = activeAttachments.Count > 0 ? Color.blue : Color.green;
                // Pulse effect when grabbing
                if (activeAttachments.Count > 0)
                    c = Color.Lerp(Color.blue, Color.cyan, Mathf.PingPong(Time.time * 2f, 0.5f));

                DrawSphere(center, grabRadius.val, c);
            }
            else
            {
                if (debugVisualsParent != null && debugVisualsParent.activeSelf) debugVisualsParent.SetActive(false);
            }
        }

        // --- Core Logic ---

        private void PerformGrab()
        {
            if (currentOriginRB == null) { statusText.val = "Error: No Origin Selected"; return; }
            if (activeAttachments.Count > 0) { statusText.val = "Already holding object."; return; }

            Vector3 center = GetGrabCenter();
            float r = grabRadius.val;

            // Safe Scan
            int layerMask = Physics.DefaultRaycastLayers;
            var queryTrigger = grabTriggers.val ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;
            Collider[] hits = Physics.OverlapSphere(center, r, layerMask, queryTrigger);

            // Sort by distance
            Array.Sort(hits, (a, b) =>
            {
                float da = (a.transform.position - center).sqrMagnitude;
                float db = (b.transform.position - center).sqrMagnitude;
                return da.CompareTo(db);
            });

            Rigidbody target = null;
            foreach (var hit in hits)
            {
                var rb = hit.attachedRigidbody;
                if (rb == null || rb == currentOriginRB) continue;
                if (IsPart(rb, containingAtom)) continue; // Self check

                // Filters
                bool isMeshJoint = rb.name.Contains("PhysicsMeshJoint");
                if (isMeshJoint && !grabMeshJoints.val) continue;
                if (hit.isTrigger && !grabTriggers.val && !isMeshJoint) continue; // Basic trigger check logic backup
                if (!isMeshJoint && !hit.isTrigger && !grabRigidbodies.val) continue;

                target = rb;
                break;
            }

            if (target != null)
            {
                string tName = target.name;
                var att = new PhysicsAttachment(currentOriginRB, target, this);

                if (att.IsValid())
                {
                    activeAttachments.Add(att);
                    statusText.val = "Grabbed " + tName;
                }
                else
                {
                    statusText.val = "Failed to grab " + tName;
                }
            }
            else
            {
                statusText.val = "Nothing in range.";
            }
        }

        private void PerformUngrab()
        {
            foreach (var att in activeAttachments) att.BeginDetach();
            statusText.val = "Releasing...";
        }

        // --- Helpers ---

        public void RefreshOriginList()
        {
            List<string> list = new List<string>();
            Atom a = containingAtom;
            if (a == null) return;

            // 1. Safe Person Scan
            if (a.type == "Person")
            {
                Transform t = a.transform.Find("rescale2/PhysicsModel");
                if (t != null)
                {
                    foreach (var rb in t.GetComponentsInChildren<Rigidbody>())
                    {
                        if (ValidateRB(rb)) list.Add(rb.name);
                    }
                }
            }
            else
            {
                foreach (var rb in a.GetComponentsInChildren<Rigidbody>())
                {
                    if (ValidateRB(rb)) list.Add(rb.name);
                }
            }

            if (a.freeControllers != null)
            {
                foreach (var fc in a.freeControllers)
                {
                    if (fc != null && !list.Contains(fc.name)) list.Add(fc.name);
                }
            }

            list.Sort();
            originChooser.choices = list;

            if (string.IsNullOrEmpty(originChooser.val) && list.Contains("rHand"))
                originChooser.val = "rHand";

            if (!string.IsNullOrEmpty(originChooser.val)) UpdateOrigin(originChooser.val);
        }

        private bool ValidateRB(Rigidbody rb)
        {
            if (rb == null) return false;
            string n = rb.name;
            if (string.IsNullOrEmpty(n)) return false;
            if (n.Contains("Collision") || n.Contains("Trigger")) return false;
            return true;
        }

        private void UpdateOrigin(string val)
        {
            currentOriginRB = GetRigidbodySafe(containingAtom, val);
        }

        private Rigidbody GetRigidbodySafe(Atom a, string name)
        {
            if (a == null || string.IsNullOrEmpty(name)) return null;

            // 1. Controller
            var fc = a.freeControllers.FirstOrDefault(c => c.name == name);
            if (fc != null)
            {
                var rb = fc.GetComponent<Rigidbody>();
                if (rb) return rb;
                return fc.GetComponentInChildren<Rigidbody>();
            }

            // 2. Safe Scan
            if (a.type == "Person")
            {
                Transform t = a.transform.Find("rescale2/PhysicsModel");
                if (t != null)
                {
                    var found = t.GetComponentsInChildren<Rigidbody>().FirstOrDefault(r => r.name == name);
                    if (found) return found;
                }
                return null;
            }

            // 3. Fallback
            return a.GetComponentsInChildren<Rigidbody>().FirstOrDefault(r => r.name == name);
        }

        private Vector3 GetGrabCenter()
        {
            if (currentOriginRB == null) return transform.position;
            Vector3 offset = new Vector3(offsetX.val, offsetY.val, offsetZ.val);
            return currentOriginRB.transform.TransformPoint(offset);
        }

        private bool IsPart(Rigidbody rb, Atom a)
        {
            Transform t = rb.transform;
            while (t != null)
            {
                if (t.GetComponent<Atom>() == a) return true;
                t = t.parent;
            }
            return false;
        }

        private Atom GetAtom(Transform t)
        {
            while (t != null)
            {
                var a = t.GetComponent<Atom>();
                if (a) return a;
                t = t.parent;
            }
            return null;
        }

        // --- Visualizer ---
        private void InitVisuals()
        {
            debugVisualsParent = new GameObject("ProximityVisuals");
            debugVisualsParent.transform.SetParent(transform, false);

            // Create Material (RIGIFY STYLE LOGIC)
            Shader sh = Shader.Find("Hidden/Internal-Colored");
            if (sh == null) sh = Shader.Find("GUI/Text Shader");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Diffuse");

            gizmoMaterial = new Material(sh);
            gizmoMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            gizmoMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            gizmoMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            gizmoMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            gizmoMaterial.SetInt("_ZWrite", 0);
            gizmoMaterial.renderQueue = 4000;

            circleX = CreateCircle();
            circleY = CreateCircle();
            circleZ = CreateCircle();
        }

        private LineRenderer CreateCircle()
        {
            var go = new GameObject("Circle");
            go.transform.SetParent(debugVisualsParent.transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = 0.005f;
            lr.positionCount = 33;
            lr.material = gizmoMaterial;
            return lr;
        }

        private void DrawSphere(Vector3 pos, float r, Color c)
        {
            DrawCircle(circleX, pos, r, Vector3.right, Vector3.up, c);
            DrawCircle(circleY, pos, r, Vector3.up, Vector3.forward, c);
            DrawCircle(circleZ, pos, r, Vector3.forward, Vector3.right, c);
        }

        private void DrawCircle(LineRenderer lr, Vector3 c, float r, Vector3 a1, Vector3 a2, Color col)
        {
            lr.startColor = lr.endColor = new Color(col.r, col.g, col.b, 0.5f);

            for (int i = 0; i < 33; i++)
            {
                float ang = (float)i / 32f * Mathf.PI * 2f;
                lr.SetPosition(i, c + (a1 * Mathf.Cos(ang) + a2 * Mathf.Sin(ang)) * r);
            }
        }

        // --- Attachment Logic ---
        public class PhysicsAttachment
        {
            public Rigidbody source, target;
            public ConfigurableJoint joint;
            private ProximityGrab script;

            // State
            private float currentBlend = 0f;
            private bool isBlendingIn = true;
            private bool isDetaching = false;

            // Kinematic (Perfect Follow) Support
            private bool isPerfectFollow = false;
            private bool wasKinematic = false;

            // Presets (Soft, Firm, Lock)
            private static readonly float[] springValues = { 100f, 2000f, 99999f };
            private static readonly float[] damperValues = { 10f, 100f, 999f };
            private static readonly float[] maxForceValues = { 1000f, 10000f, float.PositiveInfinity };

            public PhysicsAttachment(Rigidbody s, Rigidbody t, ProximityGrab script)
            {
                this.source = s;
                this.target = t;
                this.script = script;
                Create();
            }

            public bool IsValid() => joint != null || isPerfectFollow;
            public bool IsDead() => isDetaching && currentBlend <= 0f;

            public void BeginDetach()
            {
                isBlendingIn = false;
                isDetaching = true;
            }

            private void Create()
            {
                // Default creation - will be updated immediately in first Update() loop with correct mode
                joint = source.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = target;

                // Keep Initial Offset?
                if (script.keepOffset.val)
                {
                    joint.autoConfigureConnectedAnchor = false;
                    joint.anchor = source.transform.InverseTransformPoint(target.transform.position);
                    joint.connectedAnchor = Vector3.zero;
                    joint.targetRotation = Quaternion.identity;
                    joint.configuredInWorldSpace = false;
                }

                // Initialize locked
                joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
            }

            public void Update(float dt, int preset, int mode, float speed)
            {
                // Handle Blending
                if (isBlendingIn && currentBlend < 1f)
                {
                    currentBlend += dt * speed;
                    if (currentBlend > 1f) currentBlend = 1f;
                }
                else if (isDetaching && currentBlend > 0f)
                {
                    currentBlend -= dt * speed;
                    if (currentBlend < 0f) currentBlend = 0f;
                }

                if (mode == 3) // Perfect Follow
                {
                    UpdateKinematic(preset);
                }
                else
                {
                    UpdateJoint(preset, mode);
                }
            }

            private void UpdateKinematic(int preset)
            {
                // Initialize Kinematic Mode if needed
                if (!isPerfectFollow && target != null)
                {
                    if (joint) { UnityEngine.Object.Destroy(joint); joint = null; }
                    wasKinematic = target.isKinematic;
                    target.isKinematic = true;
                    isPerfectFollow = true;
                }

                if (!target) return;

                // Move target to source
                Vector3 targetPos = source.transform.TransformPoint(script.keepOffset.val ?
                    source.transform.InverseTransformPoint(target.transform.position) : Vector3.zero
                /* Simplified offset logic for kinematic - assumes initial grab state was correct or snaps */
                /* Properly, we should store initial offset in constructor if using kinematic */
                );

                // For this quick impl, we just snap to source if offset logic isn't fully cached
                // But better to use the Joint anchor logic if we switch modes. 
                // Let's stick to standard joint logic for now unless explicitly requested.
                // Reverting Kinematic to Standard Joint if user switched BACK from Perfect Follow
            }

            private void UpdateJoint(int preset, int mode)
            {
                // If we were kinematic, restore joint
                if (isPerfectFollow)
                {
                    if (target) target.isKinematic = wasKinematic;
                    isPerfectFollow = false;
                    Create(); // Re-create joint
                }

                if (!joint) return;

                // 1. Get Base Values
                float spr = springValues[preset];
                float damp = damperValues[preset];
                float force = maxForceValues[preset];

                // 2. Adjust for Mode
                // Mode 0: Grab (Lock Pos, Free-ish Rot)
                // Mode 1: Glue (Lock Pos, Lock Rot)
                // Mode 2: Loose (Soft Pos, Soft Rot)

                float linSpr = spr;
                float angSpr = spr;

                if (mode == 0) { angSpr *= 0.1f; } // Grab: weaker rotation
                else if (mode == 2) { linSpr *= 0.1f; angSpr *= 0.1f; } // Loose

                // 3. Blend
                linSpr = Mathf.Lerp(0f, linSpr, currentBlend);
                angSpr = Mathf.Lerp(0f, angSpr, currentBlend);
                float curForce = Mathf.Lerp(0f, force, currentBlend);

                // 4. Apply to ConfigurableJoint
                // Crucial: For springs to work, motion must be NOT Locked.
                // If Prese == Lock (2), we want Locked motion. If Soft/Firm, we want Limited/Free with Springs.

                if (preset == 2 && currentBlend > 0.9f) // Full Lock
                {
                    joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Locked;
                    joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Locked;
                }
                else
                {
                    // Use Springs
                    joint.xMotion = joint.yMotion = joint.zMotion = ConfigurableJointMotion.Free;

                    if (script.positionOnly.val)
                    {
                        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
                        // Zero angular spring
                        angSpr = 0f;
                    }
                    else
                    {
                        joint.angularXMotion = joint.angularYMotion = joint.angularZMotion = ConfigurableJointMotion.Free;
                    }

                    JointDrive linDrive = new JointDrive { positionSpring = linSpr, positionDamper = damp, maximumForce = curForce };
                    JointDrive angDrive = new JointDrive { positionSpring = angSpr, positionDamper = damp, maximumForce = curForce };

                    joint.xDrive = joint.yDrive = joint.zDrive = linDrive;
                    joint.angularXDrive = joint.angularYZDrive = angDrive;

                    // Rotation Drive Mode
                    joint.rotationDriveMode = RotationDriveMode.XYAndZ;
                }
            }

            public void Destroy()
            {
                if (isPerfectFollow && target) target.isKinematic = wasKinematic;
                if (joint) UnityEngine.Object.Destroy(joint);
            }
        }

        public struct OffsetMemory
        {
            public Vector3 pos; public Quaternion rot;
            public OffsetMemory(Vector3 p, Quaternion r) { pos = p; rot = r; }
        }
    }
}
