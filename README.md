# Proximity Grab for Virt-A-Mate
![License](https://img.shields.io/badge/License-CC_BY--SA_4.0-lightgrey.svg)
![Version](https://img.shields.io/badge/Version-1.0-blue.svg)
[![Support](https://img.shields.io/badge/Support-Buy_Me_A_Coffee-orange.svg)](https://buymeacoffee.com/fivelsystems)

**Proximity Grab** allows you to grab and attach objects using a configurable proximity sphere. It uses physics-based joints for smooth, natural movement.

🔗 **[Virt-A-Mate Hub Link](https://hub.virtamate.com/resources/proximitygrab.64126/)**

## ✨ Features
*   🟢 **Proximity Detection**: Uses a sphere overlap to find targets near your hand. No aiming required!
*   👁️ **Visual Feedback**: Draws a green wireframe sphere to show exactly what is in range.
*   🧠 **Smart Parenting**: Intelligently ignores the parent object if attached to a hand (so you don't grab your own chest).
*   ⚙️ **Physics Presets**: Choose between **Soft**, **Firm**, or **Locked** joints.
*   📏 **Adjustable Offsets**: Fine-tune the grab position relative to the controller.

## 🚀 Installation
1.  Download the `ProximityGrab.var` package (or scripts).
2.  Place in your `AddonPackages` (or `Custom/Scripts`) folder.
3.  Select a Controller (e.g., Right Hand).
4.  Add Plugin -> Select `ProximityGrab.cs`.

## 🎮 Usage
1.  Add the **ProximityGrab** plugin to an empty Atom.
2.  **Parent** this empty Atom to your hand (or any object you want to grab *with*).
3.  Use the plugin UI to adjust the **Grab Radius** (Green Sphere visualization).
4.  Move your hand near another object (e.g., a prop or person).
5.  Click **Attach** or trigger the **Grab** action!

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

[b]Proximity Grab[/b] allows you to grab and attach objects using a configurable proximity sphere. It uses physics-based joints for smooth, natural movement.

[size=4][b]Features[/b][/size]
[list]
[*] 🟢 [b]Proximity Detection[/b]: Uses a sphere overlap to find targets near your hand. No aiming required!
[*] 👁️ [b]Visual Feedback[/b]: Draws a green wireframe sphere to show exactly what is in range.
[*] 🧠 [b]Smart Parenting[/b]: Intelligently ignores the parent object if attached to a hand (so you don't grab your own chest).
[*] ⚙️ [b]Physics Presets[/b]: Choose between Soft, Firm, or Locked joints.
[*] 📏 [b]Adjustable Offsets[/b]: Fine-tune the grab position relative to the controller.
[/list]

[size=4][b]Credits[/b][/size]
[list]
[*] [b]Kimowal[/b]: Core physics logic derived from [i]PhysicsAttachmentEngine[/i] (CC BY-SA).
[*] [b]Skynet[/b]: Visualization patterns derived from [i]Rigify[/i] (CC BY).
[*] [b]acidbubbles[/b]: Project bootstrapped using [i]vam-plugin-template[/i] (MIT).
[*] [b]FivelSystems[/b]: Proximity logic and UI implementation.
[*] [b]Antigravity + Gemini[/b]: AI Assistance & Code Generation.
[/list]

[size=4][b]Support[/b][/size]
If you like this plugin, consider buying me a coffee! ☕
[url=https://buymeacoffee.com/fivelsystems]buymeacoffee.com/fivelsystems[/url]

[size=4][b]License[/b][/size]
This project is licensed under [b]CC BY-SA 4.0[/b].

</details>
