# BSRenderMod

**BSRenderMod** is a mod for Beat Saber that enables rendering replay gameplay to high-quality `.mp4` video files with full control over output settings.

## Features

- Render replay gameplay to `.mp4`
- Preset quality levels: `Low`, `Medium`, and `High`  
  *(These indicate how lossless the render is — not visual effects settings)*
  
  *(Beat Saber graphics settings currently forced to max while rendering)*
- Manual configuration of:
  - Bitrate  
  - Resolution (supports output higher than your display resolution)  
  - Frame rate (FPS)  
  - Camera settings
- Output files saved to:  
  ```
  Beat Saber/Renders/Finished
  ```

## Configuration

Settings are available from the in-game menu:  
**Gameplay Setup** → **Mods** → **Render Mod**

From there, you can:
- Choose a quality preset
- Adjust advanced settings manually
- Customize camera behavior for rendered footage

## Notes

- This mod renders from replay data and is not limited by real-time performance.
  *(The mod will not run unless in fpfc)*
- Ensure that the replay files being played are not broken or your replay playback mod is broken
- Audio is **NOT** recorded from the game, and is instead muxed in from the beatmaps ogg/egg file
