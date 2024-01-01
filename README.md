# Vege Drop for Dyson Sphere Program

**DSP Vege Drop** is a mod for the Unity game Dyson Sphere Program developed by Youthcat Studio and published by Gamera Game.  The game is available on [here](https://store.steampowered.com/app/1366540/Dyson_Sphere_Program/).

This mod will cause vegetation, rocks, etc to dropped loot as if mined whenever they are destroyed (by placing buildings, foundations, etc..). Items are dropped as litter if inventory is full. Hand mining drops extra items vs when the vege are destroyed.

Ever get annoyed that you have to hand mine all the planets vegetation to get the resources?  This mod is for you.

Enable and disable loot drop functionality in-game with this toggle button on your HUD.
![Enable Disable Button image](https://raw.githubusercontent.com/w-gates/dsp-vege-drop/master/EnableDisableButton.jpg)

If you like this mod, please click the thumbs up at the [top of the page](https://dsp.thunderstore.io/package/wgates/DSP_Vege_Drop/) (next to the Total rating).  That would be a nice thank you for me, and help other people to find a mod you enjoy.

If you have issues with this mod, please report them on [GitHub](https://github.com/w-gates/dsp-vege-drop/issues).  You can also contact me at milamber#8441 on the [DSP Modding](https://discord.gg/XxhyTNte) Discord #tech-support channel.

## Config Settings
Configuration settings are loaded when you game is loaded.  So if you want to change the settings file, quit the game, but don't exit to desktop, then continue your game.

This mod is also compatible with [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager) which provides an in-game GUI for changing the settings in real-time.

Settings include
 - Mod enable flag.
 - Control over clearing of rocks, trees, small rocks and ice.
 - Control over planet types clearing is performed on.

![Config Settings Window image](https://raw.githubusercontent.com/w-gates/dsp-vege-drop/master/ConfigSettingsWindow.jpg)

Want fine-grained control over which items are not dropped?  If you tell the mod exactly what IDs not to clear using the DisableItemIds config setting, it will apply those rules.  DisableItemIds is a string containing a comma-separated list of vege proto ID shorts.  To determine what the ID of the item is you want to prevent clearing for, follow this process.
 - Update your `BepInEx.cfg` file, under `[Logging.Console]`, to set `Enabled = true`
 - In your `BepInEx.cfg` file, under `[Logging.Console]`, also set `LogLevels = All`
 - Load a save where you can access the item or items you wish to have the mod not clear.
 - If vege drop is enabled, temporarily disable vege drop using the toggle button on the HUD.
 - Use Icarus to remove the items you want this mod not to clear.
 - Each time Icarus removes an item this mod will print to the debug console the vege proto IDs which are removed.
 - Add that list of IDs as a comma-separated list, to the DisableItemIds setting in `w-gates.dysonsphereprogram.droneclearing.cfg`.
 - Once done, you can revert the settings in your `BepInEx.cfg` file if you want.

The configuration file is called `w-gates.dysonsphereprogram.droneclearing.cfg`.  It is generated the first time you run the game with this mod installed.  On Windows 10 it is located at
 - If you installed manually:  `%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\config\w-gates.dysonsphereprogram.droneclearing.cfg`
 - If you installed with r2modman:  `C:\Users\<username>\AppData\Roaming\r2modmanPlus-local\DysonSphereProgram\profiles\Default\BepInEx\config\w-gates.dysonsphereprogram.droneclearing.cfg`

## Installation
This mod uses the BepInEx mod plugin framework.  So BepInEx must be installed to use this mod.  Find details for installing BepInEx [in their user guide](https://bepinex.github.io/bepinex_docs/master/articles/user_guide/installation/index.html#installing-bepinex-1).  This mod was tested with BepInEx x64 5.4.17.0 and Dyson Sphere Program 0.10.28.21172 on Windows 11.

To manually install this mod, add the `DysonSphereVegeDrop.dll` to your `%PROGRAMFILES(X86)%\Steam\steamapps\common\Dyson Sphere Program\BepInEx\plugins\` folder.

This mod can also be installed using ebkr's [r2modman](https://dsp.thunderstore.io/package/ebkr/r2modman/) mod manager by clicking "Install with Mod Manager" on the [DSP Modding](https://dsp.thunderstore.io/package/wgates/DSP_Vege_Drop/) site.

## Open Source
The source code for this mod is available for download, review and forking on GitHub [here](https://github.com/w-gates/dsp-vege-drop) under the BSD 3 clause license.

## Change Log
### Unreleased
 - Optimized check for disabled planets.
### v1.0.0
 - Initial release.

# Credit
This mod was created using the [DSP Drone Clearing](https://dsp.thunderstore.io/package/GreyHak/DSP_Drone_Clearing/) mod as a base. When I was unable to figure out how to update that mod after the Dark Fog update, I decided to use this to get the same end result (even if it is more of a cheat mod than that one).

# Conflicts
If [DSP Drone Clearing](https://dsp.thunderstore.io/package/GreyHak/DSP_Drone_Clearing/) is ever updated to work again, this mod will conflict with that one.
