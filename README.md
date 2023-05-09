# appmana-unity-plugin

Stream your apps and games with AppMana for Unity Plugin

[Join our Discord](https://discord.gg/sTSzaHSJWV)

### Get Started with a Sample and Templates

Visit and fork our templates to start a new project ready to deploy on AppMana:

 - For URP: https://github.com/appmana/appmana-unity-starter-urp
 - For HDRP: https://github.com/appmana/appmana-unity-starter-hdrp

Plastic is supported. For the best experience, use Git.

### Usage and Installation in an Existing Project
 
 1. Add the following scoped registries and dependencies to your `Packages/manifest.json` file:

```json
{
  // start selection for copy and paste
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp",
        "com.neuecc"
      ]
    }
  ],
  // end selection for copy and paste
  "dependencies": {
    // start selection for copy and paste
    "com.unity.inputsystem": "1.5.1",
    "com.appmana.unity.public": "https://github.com/AppMana/appmana-unity-plugin.git",
    // end selection for copy and paste
  }
}
```

 2. Add the `RemotePlayableConfiguration` component and configure the camera, audio source and `CanvasScaler` components associated with the player.
 3. Use the provided `InputSettings` and `InputActions` assets for maximum compatibility with the simulated inputs platform. You can find these in our plugin's entry in your Packages directory in the Asset Browser window.
 4. Once you have committed these changes, sign up at [appmana.com](https://appmana.com) and supply the repository URL when prompted. We will build and deploy your game.

### Enabling Multiplayer

This example will help you make a 2 player multiplayer game where each player has her own camera and interaction is done with 3D objects and the canvas.

 1. Create two layers: `Player1` and `Player2`. These will be used to limit objects to be interactable only by the layer's corresponding players. 
 2. Create a **Player** prefab, and create 2 instances of it in your scene. Both should be active.
    1. In this prefab, add a `RemotePlayableConfiguration`. to its root.
    2. Add a camera and a `PhysicsRaycaster` to a child game object.
       1. Set the **Camera** on the `RemotePlayableConfiguration` to the camera you created.
       2. To limit the player to interacting with only certain objects in the scene, set the **Event Mask** property to `Player1` (created earlier).
       3. You can add `Default` or other layers to allow multiple players to interact with the same object.
    3. Add a `Canvas` to another child game object to set up the player's UI. Set it to **Screen Space - Camera**, and add a `GraphicRaycaster`.
    4. Setup an Event System in this hierarchy.
       1. Add an `InputSystemUIInputModule` and a `MultiplayerEventSystem` component (from the Input System package) to another child game object. Observe you you add these in the wrong order, a `StandaloneEventSystem` is added, and you must click the "Replace" button in the inspector to resolve this error.
       2. On the `InputSystemUIInputModule`, set the **Pointer Behaviour** to **All Pointers As Is**, and set the **Actions Asset** to the **Input Actions** asset located in `Packages/AppMana Unity Plugin` in your Project tab. Due to a Unity Editor quirk, you cannot locate this asset by clocking the Find button in the inspector.
4. Set up your second player prefab instance.
   1. Set the `PhysicsRaycaster`'s **Event Mask** property to `Player2` to allow this player instance to interact with only "Player2" layer objects. See the Event System documentation for the significance of these options.
   2. Set the **Target Display** in the camera's **Output** rollout to **Display 2**.
   3. Add a **Game** tab to the editor, and set its **Display** to **Display 2** in the dropdown.

Hit play. Observe your two screens now represent the two distinct player devices.

### Tips and Tricks

 - To visualize mobile viewports, switch to the Simulator view and choose an AppMana device profile by searching the word "AppMana" in the dropdown.
 - Use the `CinemachinePressableInputProvider` component to get Input System-based input for your Cinemachine cameras. Reference the `UI/Delta` input action for the **XY Axis**, and `UI/Click` for **Enable When Pressed** if you want to limit camemra looking while a pointer (touch or mouse) are pressed. 

### Requirements and Limitations

 - Unity 2021 or higher.
 - Built-In Render Pipeline, HDRP or URP.
 - Use only one camera and one scene. Use Cinemachine wherever you would ordinarily use multiple cameras.
 - You can develop on a Windows or macOS device. Vulkan, DirectX 11, DirectX 12 or DirectX DXR will be used to render your game.
 - You must use InputSystem (`"com.unity.inputsystem": "1.5.1"`). `Input.mousePosition` and other legacy input approaches are **not supported**. See [here for migration tips](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.3/manual/Migration.html). To ease migration, use `AppMana.Compatibility.Input`.
 - You **cannot** use overlay canvases. Use **Camera Space** in your canvases. In HDRP, use a custom pass to bypass postprocessing in your screen space canvas. See the HDRP template for a complete example.
 - Use a **Constant Physical Size** setting for your Canvas Scaler.
 - Use the `AppMana.Compatibility.Screen` class instead of `UnityEngine.Screen`.
 - `PlayerPrefs` are not supported. Use `AppMana.Compatibility.PlayerPrefs` or the `RemotePlayableConfiguration.playerPrefs` property.
