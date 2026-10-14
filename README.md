# OrbusClassicPatches

Patches for OrbusVR Classic/Preborn.

These are client-side BepInEx plugins that patch or work around issues in the game client.

If you want to read more about BepInEx you can check out their [documentation](https://docs.bepinex.dev).

## Installing a patch

* Install the latest version of [BepInEx](https://github.com/BepInEx/BepInEx/releases) into your OrbusVR game directory.
* Download the `.dll` for the patch you want from the [Releases](https://github.com/Lz4Lz/OrbusClassicPatches/releases) page.
* Place the `.dll` in: `BepInEx/plugins/` directory (Note: You might need to launch the game once for this folder to generate)

For example:
`OrbusVR/BepInEx/plugins/OrbusWandFix.dll`

## Linux / Proton

If you are running through proton, add this to the games launch options:
`WINEDLLOVERRIDES="winhttp.dll=n,b" %command%`

## Building from source

Place these files in the `lib/` directory inside the patches folder:

Files from `OrbusVR/vrclient_Data/Managed`:
`Assembly-CSharp.dll`
`UnityEngine.dll`

Files from the BepInEx folder inside the games directory:
`0Harmony.dll`
`BepInEx.dll`

And then build the project.

Alternatively, you could edit `OrbusWandFix.csproj` to point to the correct directories.

## Issues and Contributing

If you encounter a bug or have a suggestion for a new patch, please open an [issue](https://github.com/Lz4Lz/OrbusClassicPatches/issues).

Contributions are welcome! If you would like to fix a bug, improve an existing patch, or add a new one, feel free to open a pull request.
