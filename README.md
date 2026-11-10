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

- ****FFMPEG must be installed from your target mod manager and in `Beat Saber/Libs`****
- ****CameraUtils must be installed from your target mod manager and in `Beat Saber/Plugins`****
- This mod renders from replay data and at best, renders close to real time with lower settings.

  *(The mod will not run unless using `-fpfc` or the first person flying controller)*
- Ensure that the replay files being played are not broken or your replay playback mod is broken, this mod does not handle the replays themselves
- If you are on Linux with an NVIDIA card and planning to use AV1, you need to manually install NVIDIA Libs to your Wine prefix, instructions can be found here: https://github.com/SveSop/nvidia-libs

  *(If you are using BSManager, your Wine prefix should be here: `/home/username/BSManager/SharedContent/compatdata/pfx/drive_c/windows/system32/`)*

  *(If that still doesn't work, check `syswow64` for `nvcuvid.dll`, `nvencodeapi64.dll` and `nvoptix.dll`, and if you find those there, move those to `system32`)*