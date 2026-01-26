# Proximity Grab for Virt-A-Mate

**Proximity Grab** is a plugin that allows you to easily grab and attach objects using a proximity sphere instead of a precise raycast. It uses physics-based joints for smooth, natural movement.

🔗 **[Virt-A-Mate Hub Link](https://hub.virtamate.com/resources/)** (Pending)

## Features
*   **Proximity Detection**: Uses a sphere overlap to find targets near your hand. No aiming required!
*   **Visual Feedback**: Draws a green wireframe sphere to show exactly what is in range.
*   **Smart Parenting**: Intelligently ignores the parent object if attached to a hand (so you don't grab your own chest).
*   **Physics Presets**: Choose between Soft, Firm, or Locked joints.
*   **Adjustable Offsets**: Fine-tune the grab position relative to the controller.

## Installation
1.  Download the `ProximityGrab.var` package (or scripts).
2.  Place in your `AddonPackages` (or `Custom/Scripts`) folder.
3.  Select a Controller (e.g., Right Hand).
4.  Add Plugin -> Select `ProximityGrab.cs`.

## Credits
*   **Kimowal**: Core physics logic derived from `PhysicsAttachmentEngine` (CC BY-SA).
*   **Skynet**: Visualization patterns derived from `Rigify` (CC BY).
*   **acidbubbles**: Project bootstrapped using `vam-plugin-template` (MIT).
*   **FivelSystems**: Proximity logic and UI implementation.

## License
This project is licensed under **CC BY-SA 4.0**.

---

<details>
<summary><b>VaM Hub BBCode (Click to Expand)</b></summary>

[size=5][b]Proximity Grab for Virt-A-Mate[/b][/size]

[b]Proximity Grab[/b] is a plugin that allows you to easily grab and attach objects using a proximity sphere instead of a precise raycast. It uses physics-based joints for smooth, natural movement.

[size=4][b]Features[/b][/size]
[list]
[*] [b]Proximity Detection[/b]: Uses a sphere overlap to find targets near your hand. No aiming required!
[*] [b]Visual Feedback[/b]: Draws a green wireframe sphere to show exactly what is in range.
[*] [b]Smart Parenting[/b]: Intelligently ignores the parent object if attached to a hand (so you don't grab your own chest).
[*] [b]Physics Presets[/b]: Choose between Soft, Firm, or Locked joints.
[*] [b]Adjustable Offsets[/b]: Fine-tune the grab position relative to the controller.
[/list]

[size=4][b]Credits[/b][/size]
[list]
[*] [b]Kimowal[/b]: Core physics logic derived from [i]PhysicsAttachmentEngine[/i] (CC BY-SA).
[*] [b]Skynet[/b]: Visualization patterns derived from [i]Rigify[/i] (CC BY).
[*] [b]acidbubbles[/b]: Project bootstrapped using [i]vam-plugin-template[/i] (MIT).
[*] [b]FivelSystems[/b]: Proximity logic and UI implementation.
[/list]

[size=4][b]License[/b][/size]
This project is licensed under [b]CC BY-SA 4.0[/b].

</details>
