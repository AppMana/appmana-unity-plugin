# appmana-unity-plugin

Stream your apps and games with AppMana for Unity Plugin

[Join our Discord](https://discord.gg/sTSzaHSJWV)

### Get Started with a Sample and Templates

Visit and fork our templates to start a new project ready to deploy on AppMana:

 - For URP: https://github.com/appmana/appmana-unity-starter-urp
 - For HDRP: https://github.com/appmana/appmana-unity-starter-hdrp

Plastic is supported. For the best experience, use Git.

### Usage and Installation in an Existing Project
 
 1. Add the following dependencies to the top of the `dependencies` section of your `Packages/manifest.json` file:

```json
{
  "dependencies": {
    // start selection for copy and paste
    "com.unity.inputsystem": "1.4.2",
    "com.appmana.unity.public": "https://github.com/AppMana/appmana-unity-plugin.git",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
    "com.neuecc.unirx": "https://github.com/neuecc/UniRx.git?path=Assets/Plugins/UniRx/Scripts",
    // end selection for copy and paste
  }
}
```

 2. Add the `RemotePlayableConfiguration` component and configure the camera, audio source and `CanvasScaler` components associated with the player.
 3. Use the provided `InputSettings` and `InputActions` assets for maximum compatibility with the simulated inputs platform. You can find these in our plugin's entry in your Packages directory in the Asset Browser window.
 4. Once you have committed these changes, sign up at [appmana.com](https://appmana.com) and supply the repository URL when prompted. We will build and deploy your game.
 5. Multiplayer is supported in an experimental mode. Please contact us via [Discord](https://discord.gg/sTSzaHSJWV) to learn more. 
 
### Tips and Tricks

 - To visualize an iPhone 12 Pro Mobile Safari viewport, add the screen resolution 780 x 1326.
 - Use the `CinemachinePressableInputProvider` component to get Input System-based input for your Cinemachine cameras. Reference the `UI/Delta` input action for the **XY Axis**, and `UI/Click` for **Enable When Pressed** if you want to limit camemra looking while a pointer (touch or mouse) are pressed. 

### Requirements and Limitations

 - Unity 2021 or higher.
 - Built-In Render Pipeline, HDRP or URP.
 - Use only one camera and one scene. Use Cinemachine wherever you would ordinarily use multiple cameras.
 - You can develop on a Windows or macOS device. Vulkan, DirectX 11, DirectX 12 or DirectX DXR will be used to render your game.
 - New InputSystem (`"com.unity.inputsystem": "1.4.2"`). `Input.mousePosition` and other legacy input approaches are **not supported**. See [here for migration tips](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/Migration.html).
 - You **cannot** use overlay canvases. Use **Camera Space** in your canvases. In HDRP, use a custom pass to bypass postprocessing in your screen space canvas. See the HDRP template for a complete example.
 - Use a **Constant Pixel Size** setting for your Canvas Scaler. Set the **Base Scale** inside your **Remote Playable Configuration** component to the scale you use for the purposes of the art. The runtime will correctly then multiply the device's real scale on top of it.
 - Do not use **Screen** properties. There is no screen when streaming remotely.
