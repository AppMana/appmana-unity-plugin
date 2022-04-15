
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