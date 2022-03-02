# appmana-unity-plugin

Stream your apps and games with AppMana for Unity Plugin

[Join our Discord](https://discord.gg/sTSzaHSJWV)

### Project Requirements:

 - Unity 2021.2 or higher.
 - HDRP 12.1 or higher.
 - Vulkan, DirectX 11, DirectX 12 or DirectX DXR. If you are developing on a macOS device, you **cannot** use any Metal-specific features.
 - New InputSystem (`"com.unity.inputsystem": "1.3.0"`). `Input.mousePosition` and other legacy input approaches are **not supported**. See [here for migration tips](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/Migration.html).
 - You **cannot** load or change scenes. Use `Prefabs` instead.
 - You **cannot** use overlay canvases. [Use this package](https://github.com/alelievr/HDRP-UI-Camera-Stacking) to prevent UI elements from receiving motion blur.
 - Disable **Validate Assembly Versions** in **Player Settings**.
 - Use a **Constant Pixel Size** setting for your Canvas Scaler. Set the **Base Scale** inside your **Remote Playable Configuration** component to the scale you use for the purposes of the art. The runtime will correctly then multiply the device's real scale on top of it.
 - Do not use **Screen** properties.

### Installation

Add the following dependencies to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    // start selection for copy and paste
    "com.unity.inputsystem": "1.3.0",
    "com.appmana.unity.public": "git@github.com:AppMana/appmana-unity-plugin.git",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.neuecc.unirx": "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts",
    // end selection for copy and paste
  }
}
```

### Usage

 1. Add the `RemotePlayableConfiguration` component and configure the camera, audio source and `Canvas` components associated with the player.
 2. Make sure only one scene is checked in the editor. This will be the one that will be built into your streaming application. 
 3. Multiplayer is supported in an experimental mode. Please contact us via `https://appmana.com` or [Discord](https://discord.gg/sTSzaHSJWV) to learn more. 
 4. Use the provided `InputSettings` and `InputActions` assets for maximum compatibility with the simulated inputs platform.
 5. Make sure Run in Background is checked in Player Settings.

### Tips and Tricks

#### Using Assets from Maya

 1. Create a new project in Maya and add to your git directory. Ensure the directories are kept by adding `.gitkeep` to each folder:

```shell
find . -type d -exec sh -c 'for d; do touch "$d/.gitkeep"; done' _ {} +
```

 2. Add Maya filetypes to `git-lfs`:

```shell
TODO
```

 3. Add the HDRP texture packer utility to your `manifest.json`

```json
{
  "dependencies": {
    // start selection for copy and paste
    "ca.andydbc.unity-texture-packer":"https://github.com/andydbc/unity-texture-packer.git#master"
    // end selection
  }
}
```