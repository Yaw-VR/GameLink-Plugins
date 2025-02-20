This is the Profile's Code (receiver).
The UEVR plugin (sender) can be found here:
https://github.com/McFredward/ace-combat-uevr-telemetry-sender

# Playing Ace Combat 7 in VR with a YawVR device.

## Installation Steps

1. Download [the latest UEVR Nightly version](https://github.com/praydog/UEVR-nightly/releases) and extract it to any location.
2. Download UE4SS from [this link](https://www.nexusmods.com/acecombat7skiesunknown/mods/2474?tab=files) and extract the files into the game folder located at `...\common\ACE COMBAT 7`.
3. Create the following directories:
   - `ACE COMBAT 7\Game\Content\Paks\LogicMods`
   - `ACE COMBAT 7\Game\Content\Paks\~LogicMods`
4. Download this mod from Nexus Mods: [UEVR Compatibility Mod](https://www.nexusmods.com/acecombat7skiesunknown/mods/2387).
5. Extract the file `UEVR_Compatibility_Mod_P.pak` into `ACE COMBAT 7\Game\Content\Paks\~LogicMods`.
6. Download the UEVR Profile from [this GitHub repository](https://github.com/McFredward/ace-combat-uevr-telemetry-sender/releases).
7. Launch UEVR and import the UEVR profile `Ace7Game.zip` using the "Import Config" option.
8. Download the GameLink profile [here](https://github.com/McFredward/GameLink-Plugins/releases/tag/ace-combat-profile) and put the .dll in `YawGameLink\Gameplugins`.

## Important Warnings

**⚠️ Warning: UEVR requires a very powerful gaming PC! The motion may lag even if the game itself runs smoothly.  
It is highly recommended to set the in-game graphics settings to medium to ensure optimal performance.**

> **Note:** Even with my RTX 4090 and AMD Ryzen 7 7800X3D, I had to set the graphics to medium for smooth operation.

## Starting the Game

1. Start UEVR.
2. Launch the YawVR Game Engine.
3. Select the Ace Combat 7 profile and start it.
4. Launch Ace Combat 7.
5. Choose "Ace Combat 7" in the dropdown menu in UEVR and press "Inject".

---

Made by McFredward based on the great work of keton, kosnag & praydog.  

Happy Yaw'ing!