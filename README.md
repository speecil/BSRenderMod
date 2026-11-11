# BSRenderMod

**BSRenderMod** is a mod for Beat Saber that enables rendering replay gameplay to high quality `.mp4` video files with full control over output settings.

## Features

- Render replay gameplay to `.mp4`
- Preset quality levels: `Low`, `Medium`, and `High`  
  *(These indicate how lossless the render is — not visual effects settings)*
  
  *(Beat Saber visuals are not handled by RenderMod, what you see is what you get)*
- Manual configuration of:
  - Bitrate  
  - Resolution (supports output higher than your display resolution)  
  - Frame rate (FPS)  
  - Camera settings (Camera2, ReeCamera, None or Default)
  - Encoding (h.264, HEVC, AV1)
- Output files saved to:  
  ```
  Beat Saber/Renders/Finished
  ```

## Configuration

Settings are available from the gameplay setup menu (i.e: to the left of song selection):  
**Gameplay Setup** → **Mods** → **Render Mod** → *(Open Settings Button)*

From there, you can:
- Choose a quality preset
- Adjust advanced settings manually
- Customize camera selection

## Notes

- Needs `ffmpeg.exe` in `Libs` from a current **release** build (e.g. gyan.dev "release full"), not the
  bundled 4.3.x and not a git/nightly build. Old builds can't use NVENC on current NVIDIA drivers, and
  newer ffmpeg needs a matching driver (9.x needs 610+). If hardware encoding fails, the log says which
  of the two to update, and the mod falls back to software encoding.
- ****CameraUtils must be installed from your target mod manager and in `Beat Saber/Plugins`****
- This mod renders from replay data and at best, renders close to real time with lower settings.

  *(The mod will not run unless using `-fpfc` or the first person flying controller)*
- Ensure that the replay files being played are not broken or your replay playback mod is broken, this mod does not handle the replays themselves
- If you are on Linux with an NVIDIA card and planning to use AV1, you need to manually install NVIDIA Libs to your Wine prefix, instructions can be found here: https://github.com/SveSop/nvidia-libs

  *(If you are using BSManager, your Wine prefix should be here: `/home/username/BSManager/SharedContent/compatdata/pfx/drive_c/windows/system32/`)*

  *(If that still doesn't work, check `syswow64` for `nvcuvid.dll`, `nvencodeapi64.dll` and `nvoptix.dll`, and if you find those there, move those to `system32`)*

## Building

The project builds for several game versions from one codebase. Pick the configuration matching
your install, e.g. `Release-1.42.3`. Each configuration sets the game version, which selects the
`#if` defines (`BS_1_29`, `BS_1_40`, `BS_1_42`), the references and the manifest.

| Configuration | Use for game versions |
|---|---|
| `*-1.29.1` | 1.17.0 – 1.29.1 |
| `*-1.40.8` | 1.37.2 – 1.40.8 |
| `*-1.42.3` | 1.40.13 – 1.44.3 |

Set `BeatSaberDir` in `RenderMod.csproj.user` as usual. To build for several installs, give each
version its own path:

```xml
<BeatSaberDir Condition="'$(BSVersion)' == '1.29.1'">D:\Beat Saber 1.29.1</BeatSaberDir>
<BeatSaberDir Condition="'$(BSVersion)' == '1.42.3'">D:\Beat Saber 1.42.3</BeatSaberDir>
```

- `manifest.json` is generated on build from `Manifests/manifest.<version>.json`; edit those instead.
- Visual Studio's Reference Paths are ignored; references always come from `BeatSaberDir`.
- The mod only loads when the game is launched with `fpfc`, and needs `ffmpeg.exe` in `Libs`.

### Adding a game version
1. In `RenderMod.csproj`, add `Debug-x`/`Release-x` configuration blocks setting `BSVersion` and a
   `DefineConstants` line for it, then add both configurations to the solution.
2. Add `Manifests/manifest.x.json`.
3. Wrap API differences in `#if`.
4. If its `UnityPlayer.dll` differs from the supported ones, add a `MixTarget` (RVA, byte pattern,
   stolen bytes) in `NativePatches/UnityAudioSyncHook.cs`, or audio won't be synced.
