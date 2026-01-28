# Proximity Grab for Virt-A-Mate
![License](https://img.shields.io/badge/License-CC_BY--SA_4.0-lightgrey.svg)
![Version](https://img.shields.io/badge/Version-v3-blue.svg)
[![Support](https://img.shields.io/badge/Support-Buy_Me_A_Coffee-orange.svg)](https://buymeacoffee.com/fivelsystems)

**Proximity Grab** allows you to grab onto other objects (Atoms) simply by being near them and activating a trigger. It uses physics joints to create stable, customizable attachments, perfect for "gluing" hands to hips, holding props, or creating dynamic interactions without complex parenting.

🔗 **[Virt-A-Mate Hub Link](https://hub.virtamate.com/resources/proximitygrab.64126/)**

## ✨ Features
*   🟢 **Easy Grabbing**: Select a body part (e.g., `lHand`) and click "Grab". The plugin scans for the nearest rigidbody.
*   👁️ **Visual Feedback**: Green sphere = Scanning, Blue sphere = Holding.
*   🧠 **Smart Filtering**: Safely ignores internal `PhysicsMeshJoint` artifacts and Auto-Targets Person physics.
*   ⚙️ **Real-Time Physics**: Adjust Stiffness (Soft/Firm/Lock) and Modes (Grab/Glue/Follow) on the fly without re-grabbing.
*   🌊 **Fluid Blending**: Smooth transitions when attaching and detaching objects.
*   🔧 **Power User Tools**: Toggle support for grabbing Triggers or internal MeshJoints.
*   🛑 **Kinematic Control**: "Grab Kinematic" toggle to avoid grabbing static objects.

## 🚀 Installation
1.  Download the `ProximityGrab.var` package (or `.cs` file).
2.  Place in your `AddonPackages` (or `Custom/Scripts`) folder.
3.  Select an Atom (e.g., a Person).
4.  Add Plugin -> Select `ProximityGrab.cs`.

## 🎮 Usage
1.  **Select Origin**: Top-left selector (e.g., `rHand`).
2.  **Adjust Radius**: Helper sphere shows your reach.
3.  **Grab**: Click **Grab** or use the **Toggle Grab** button.
    *   Finds nearest valid object in range.
    *   Visual changes to **Blue**.
4.  **Release**: Click **Release** to detach.

### Advanced
*   **Grab MeshJoints**: Enable to debug/grab internal simulation joints.
*   **Grab Triggers**: Enable to grab trigger colliders.

## 🤝 Credits
*   **Kimowal**: Core physics logic derived from `PhysicsAttachmentEngine` (CC BY-SA).
*   **Skynet**: Visualization patterns derived from `Rigify` (CC BY).
*   **acidbubbles**: Project bootstrapped using `vam-plugin-template` (MIT).
*   **FivelSystems**: Proximity logic and UI implementation.
*   **Antigravity + Gemini**: AI Assistance & Code Generation.

## ❤️ Support
If you like this plugin, consider buying me a coffee! ☕

<a href="https://buymeacoffee.com/fivelsystems" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174"></a>

## 📜 License
This project is licensed under **CC BY-SA 4.0**.

---

<details>
<summary><b>VaM Hub BBCode (Click to Expand)</b></summary>

[size=5][b]Proximity Grab for Virt-A-Mate[/b][/size]

[b]Proximity Grab[/b] allows you to grab onto other objects (Atoms) simply by being near them and activating a trigger. It uses physics joints to create stable, customizable attachments.

[size=4][b]Features[/b][/size]
[list]
[*] 🟢 [b]Easy Grabbing[/b]: Select a body part and click Grab.
[*] 👁️ [b]Visual Feedback[/b]: Green sphere = Scanning, Blue sphere = Holding.
[*] 🧠 [b]Smart Filtering[/b]: Safely ignores internal Physics artifacts.
[*] ⚙️ [b]Customizable Physics[/b]: Presets for Stiffness and Modes.
[*] 🛑 [b]Kinematic Control[/b]: Prevent grabbing static objects.
[/list]

[size=4][b]Credits[/b][/size]
[list]
[*] [b]Kimowal[/b]: Core physics logic (CC BY-SA).
[*] [b]Skynet[/b]: Visualization patterns (CC BY).
[*] [b]FivelSystems[/b]: UI & Logic.
[/list]

[size=4][b]Support[/b][/size]
If you like this plugin, consider buying me a coffee! ☕
[url=https://buymeacoffee.com/fivelsystems]buymeacoffee.com/fivelsystems[/url]

[size=4][b]License[/b][/size]
This project is licensed under [b]CC BY-SA 4.0[/b].

</details>
