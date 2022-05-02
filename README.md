# appmana-unity-plugin

Stream your apps and games with AppMana for Unity Plugin

[Join our Discord](https://discord.gg/sTSzaHSJWV)

### Project Requirements:

 - Unity 2021.2 or higher.
 - HDRP 12.1 or higher.
 - Vulkan, DirectX 11, DirectX 12 or DirectX DXR. If you are developing on a macOS device, you **cannot** use any Metal-specific features.
 - New InputSystem (`"com.unity.inputsystem": "1.3.0"`). `Input.mousePosition` and other legacy input approaches are **not supported**. See [here for migration tips](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/Migration.html).
 - You **cannot** use overlay canvases. Use **Camera Space** in your canvases, or [use this package](https://github.com/alelievr/HDRP-UI-Camera-Stacking) to prevent UI elements from receiving motion blur.
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
    "com.appmana.unity.public": "https://github.com/AppMana/appmana-unity-plugin.git",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.neuecc.unirx": "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts",
    // end selection for copy and paste
  }
}
```

### Usage

 1. Add the `RemotePlayableConfiguration` component and configure the camera, audio source and `Canvas` components associated with the player.
 3. Multiplayer is supported in an experimental mode. Please contact us via `https://appmana.com` or [Discord](https://discord.gg/sTSzaHSJWV) to learn more. 
 4. Use the provided `InputSettings` and `InputActions` assets for maximum compatibility with the simulated inputs platform.
 5. Check **Project Settings | Player | Resolution and Presentation | Run in Background**
 6. Uncheck  **Project Settings | Player | Other Settings | Auto Graphics API for Windows** and add **DirectX 11** and **DirectX 12 (Experimental)** to your list of **Graphics APIs for Windows**.
 7. Configure **Project Settings | Input System Package | Background Behavior** to **Ignore Focus**.
