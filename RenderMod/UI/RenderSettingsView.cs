using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using RenderMod.Render;
using RenderMod.Util;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Zenject;

namespace RenderMod.UI
{
    [HotReload(RelativePathToLayout = @"RenderSettingsView.bsml")]
    [ViewDefinition("RenderMod.UI.RenderSettingsView.bsml")]
    internal class RenderSettingsView : BSMLAutomaticViewController
    {
        [Inject] private SiraLog _log;
        private Button _actionButton;
        
        [UIComponent("resolutionPreview")] private TMPro.TextMeshProUGUI resolutionPreview;

        [UIComponent("tabSelector")] private TabSelector tabSelector;
        [UIComponent("qualitywarningText")] private TMPro.TextMeshProUGUI qualityWarningText;
        [UIComponent("resolutionwarningText")] private TMPro.TextMeshProUGUI resolutionWarningText;
        [UIComponent("bitratewarningText")] private TMPro.TextMeshProUGUI bitrateWarningText;
        [UIComponent("camerawarningText")] private TMPro.TextMeshProUGUI cameraWarningText;
        [UIComponent("otherwarningText")] private TMPro.TextMeshProUGUI otherWarningText;

        [UIValue("aspectRatio")]
        private string aspectRatio
        {
            get
            {
                switch (ReplayRenderSettings.AspectRatio)
                {
                    case "16:9" when !ReplayRenderSettings.PortraitToggle: return "16:9";
                    case "20:9" when !ReplayRenderSettings.PortraitToggle: return "20:9";
                    case "21:9" when !ReplayRenderSettings.PortraitToggle: return "21:9";
                    case "32:9" when !ReplayRenderSettings.PortraitToggle: return "32:9";
                    case "3:2" when !ReplayRenderSettings.PortraitToggle: return "3:2";
                    case "4:3" when !ReplayRenderSettings.PortraitToggle: return "4:3";
                    case "16:9" when ReplayRenderSettings.PortraitToggle: return "9:16";
                    case "20:9" when ReplayRenderSettings.PortraitToggle: return "9:20";
                    case "21:9" when ReplayRenderSettings.PortraitToggle: return "9:21";
                    case "32:9" when ReplayRenderSettings.PortraitToggle: return "9:32";
                    case "3:2" when ReplayRenderSettings.PortraitToggle: return "2:3";
                    case "4:3" when ReplayRenderSettings.PortraitToggle: return "3:4";
                    default:
                        _log.Warn($"Unknown aspect ratio setting: {ReplayRenderSettings.AspectRatio}, defaulting to 16:9");
                        return "16:9";
                }
            }
            set
            {
                switch (value)
                {
                    case "16:9": ReplayRenderSettings.AspectRatio = "16:9"; ReplayRenderSettings.Width = 1920; ReplayRenderSettings.Height = 1080; break;
                    case "20:9": ReplayRenderSettings.AspectRatio = "20:9"; ReplayRenderSettings.Width = 2400; ReplayRenderSettings.Height = 1080; break;
                    case "21:9": ReplayRenderSettings.AspectRatio = "21:9"; ReplayRenderSettings.Width = 2560; ReplayRenderSettings.Height = 1080; break;
                    case "32:9": ReplayRenderSettings.AspectRatio = "32:9"; ReplayRenderSettings.Width = 3840; ReplayRenderSettings.Height = 1080; break;
                    case "3:2": ReplayRenderSettings.AspectRatio = "3:2"; ReplayRenderSettings.Width = 1620; ReplayRenderSettings.Height = 1080; break;
                    case "4:3": ReplayRenderSettings.AspectRatio = "4:3"; ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 1080; break;
                    case "9:16": ReplayRenderSettings.AspectRatio = "9:16"; ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 1920; break;
                    case "9:20": ReplayRenderSettings.AspectRatio = "9:20"; ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 2400; break;
                    case "9:21": ReplayRenderSettings.AspectRatio = "9:21"; ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 2560; break;
                    case "9:32": ReplayRenderSettings.AspectRatio = "9:32"; ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 3840; break;
                    case "2:3": ReplayRenderSettings.AspectRatio = "2:3"; ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 1620; break;
                    case "3:4": ReplayRenderSettings.AspectRatio = "3:4"; ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 1440; break;
                    default:
                        _log.Warn($"Unknown aspect ratio option: {value}, defaulting to 16:9");
                        ReplayRenderSettings.AspectRatio = "16:9";
                        break;
                }
                ReplayRenderSettings.SaveSettings();
                UpdateWarnings();
            }
        }
        [UIValue("resolution")]
        private string resolution
        {
            get
            {
                if (aspectRatio == "16:9")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 640 when ReplayRenderSettings.Height == 360: return "360p";
                        case 854 when ReplayRenderSettings.Height == 480: return "480p";
                        case 1280 when ReplayRenderSettings.Height == 720: return "720p";
                        case 1920 when ReplayRenderSettings.Height == 1080: return "1080p";
                        case 2560 when ReplayRenderSettings.Height == 1440: return "1440p";
                        case 3840 when ReplayRenderSettings.Height == 2160: return "4K";
                        case 7680 when ReplayRenderSettings.Height == 4320: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "20:9")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 800 when ReplayRenderSettings.Height == 360: return "360p";
                        case 1066 when ReplayRenderSettings.Height == 480: return "480p";
                        case 1600 when ReplayRenderSettings.Height == 720: return "720p";
                        case 2400 when ReplayRenderSettings.Height == 1080: return "1080p";
                        case 3200 when ReplayRenderSettings.Height == 1440: return "1440p";
                        case 4800 when ReplayRenderSettings.Height == 2160: return "4K";
                        case 9600 when ReplayRenderSettings.Height == 4320: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "21:9")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 840 when ReplayRenderSettings.Height == 360: return "360p";
                        case 1120 when ReplayRenderSettings.Height == 480: return "480p";
                        case 1680 when ReplayRenderSettings.Height == 720: return "720p";
                        case 2560 when ReplayRenderSettings.Height == 1080: return "1080p";
                        case 3440 when ReplayRenderSettings.Height == 1440: return "1440p";
                        case 5120 when ReplayRenderSettings.Height == 2160: return "4K";
                        case 10080 when ReplayRenderSettings.Height == 4320: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "32:9")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 1280 when ReplayRenderSettings.Height == 360: return "360p";
                        case 1706 when ReplayRenderSettings.Height == 480: return "480p";
                        case 2560 when ReplayRenderSettings.Height == 720: return "720p";
                        case 3840 when ReplayRenderSettings.Height == 1080: return "1080p";
                        case 5120 when ReplayRenderSettings.Height == 1440: return "1440p";
                        case 7680 when ReplayRenderSettings.Height == 2160: return "4K";
                        case 15360 when ReplayRenderSettings.Height == 4320: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "3:2")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 540 when ReplayRenderSettings.Height == 360: return "360p";
                        case 720 when ReplayRenderSettings.Height == 480: return "480p";
                        case 1080 when ReplayRenderSettings.Height == 720: return "720p";
                        case 1620 when ReplayRenderSettings.Height == 1080: return "1080p";
                        case 2160 when ReplayRenderSettings.Height == 1440: return "1440p";
                        case 3240 when ReplayRenderSettings.Height == 2160: return "4K";
                        case 6480 when ReplayRenderSettings.Height == 4320: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "4:3")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 480 when ReplayRenderSettings.Height == 360: return "360p";
                        case 640 when ReplayRenderSettings.Height == 480: return "480p";
                        case 960 when ReplayRenderSettings.Height == 720: return "720p";
                        case 1440 when ReplayRenderSettings.Height == 1080: return "1080p";
                        case 1920 when ReplayRenderSettings.Height == 1440: return "1440p";
                        case 2880 when ReplayRenderSettings.Height == 2160: return "4K";
                        case 5760 when ReplayRenderSettings.Height == 4320: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "9:16")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 360 when ReplayRenderSettings.Height == 640: return "360p";
                        case 480 when ReplayRenderSettings.Height == 854: return "480p";
                        case 720 when ReplayRenderSettings.Height == 1280: return "720p";
                        case 1080 when ReplayRenderSettings.Height == 1920: return "1080p";
                        case 1440 when ReplayRenderSettings.Height == 2560: return "1440p";
                        case 2160 when ReplayRenderSettings.Height == 3840: return "4K";
                        case 4320 when ReplayRenderSettings.Height == 7680: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "9:20")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 360 when ReplayRenderSettings.Height == 800: return "360p";
                        case 480 when ReplayRenderSettings.Height == 1066: return "480p";
                        case 720 when ReplayRenderSettings.Height == 1600: return "720p";
                        case 1080 when ReplayRenderSettings.Height == 2400: return "1080p";
                        case 1440 when ReplayRenderSettings.Height == 3200: return "1440p";
                        case 2160 when ReplayRenderSettings.Height == 4800: return "4K";
                        case 4320 when ReplayRenderSettings.Height == 9600: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "9:21")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 360 when ReplayRenderSettings.Height == 840: return "360p";
                        case 480 when ReplayRenderSettings.Height == 1120: return "480p";
                        case 720 when ReplayRenderSettings.Height == 1680: return "720p";
                        case 1080 when ReplayRenderSettings.Height == 2560: return "1080p";
                        case 1440 when ReplayRenderSettings.Height == 3440: return "1440p";
                        case 2160 when ReplayRenderSettings.Height == 5120: return "4K";
                        case 4320 when ReplayRenderSettings.Height == 10080: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "9:32")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 360 when ReplayRenderSettings.Height == 1280: return "360p";
                        case 480 when ReplayRenderSettings.Height == 1706: return "480p";
                        case 720 when ReplayRenderSettings.Height == 2560: return "720p";
                        case 1080 when ReplayRenderSettings.Height == 3840: return "1080p";
                        case 1440 when ReplayRenderSettings.Height == 5120: return "1440p";
                        case 2160 when ReplayRenderSettings.Height == 7680: return "4K";
                        case 4320 when ReplayRenderSettings.Height == 15360: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "2:3")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 360 when ReplayRenderSettings.Height == 540: return "360p";
                        case 480 when ReplayRenderSettings.Height == 720: return "480p";
                        case 720 when ReplayRenderSettings.Height == 1080: return "720p";
                        case 1080 when ReplayRenderSettings.Height == 1620: return "1080p";
                        case 1440 when ReplayRenderSettings.Height == 2160: return "1440p";
                        case 2160 when ReplayRenderSettings.Height == 3240: return "4K";
                        case 4320 when ReplayRenderSettings.Height == 6480: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                if (aspectRatio == "3:4")
                {
                    switch (ReplayRenderSettings.Width)
                    {
                        case 360 when ReplayRenderSettings.Height == 480: return "360p";
                        case 480 when ReplayRenderSettings.Height == 640: return "480p";
                        case 720 when ReplayRenderSettings.Height == 960: return "720p";
                        case 1080 when ReplayRenderSettings.Height == 1440: return "1080p";
                        case 1440 when ReplayRenderSettings.Height == 1920: return "1440p";
                        case 2160 when ReplayRenderSettings.Height == 2880: return "4K";
                        case 4320 when ReplayRenderSettings.Height == 5760: return "8K";
                        default:
                            _log.Warn($"Unknown resolution setting: {ReplayRenderSettings.Width}x{ReplayRenderSettings.Height}, defaulting to 1080p");
                            return "1080p";
                    }
                }
                _log.Error("if this gets called ima kms");
                return "uh oh";
            }
            set
            {
                if (aspectRatio == "16:9")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 640; ReplayRenderSettings.Height = 360; break;
                        case "480p": ReplayRenderSettings.Width = 854; ReplayRenderSettings.Height = 480; break;
                        case "720p": ReplayRenderSettings.Width = 1280; ReplayRenderSettings.Height = 720; break;
                        case "1080p": ReplayRenderSettings.Width = 1920; ReplayRenderSettings.Height = 1080; break;
                        case "1440p": ReplayRenderSettings.Width = 2560; ReplayRenderSettings.Height = 1440; break;
                        case "4K": ReplayRenderSettings.Width = 3840; ReplayRenderSettings.Height = 2160; break;
                        case "8K": ReplayRenderSettings.Width = 7680; ReplayRenderSettings.Height = 4320; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1920;
                            ReplayRenderSettings.Height = 1080;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "20:9")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 800; ReplayRenderSettings.Height = 360; break;
                        case "480p": ReplayRenderSettings.Width = 1066; ReplayRenderSettings.Height = 480; break;
                        case "720p": ReplayRenderSettings.Width = 1600; ReplayRenderSettings.Height = 720; break;
                        case "1080p": ReplayRenderSettings.Width = 2400; ReplayRenderSettings.Height = 1080; break;
                        case "1440p": ReplayRenderSettings.Width = 3200; ReplayRenderSettings.Height = 1440; break;
                        case "4K": ReplayRenderSettings.Width = 4800; ReplayRenderSettings.Height = 2160; break;
                        case "8K": ReplayRenderSettings.Width = 9600; ReplayRenderSettings.Height = 4320; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 2400;
                            ReplayRenderSettings.Height = 1080;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "21:9")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 840; ReplayRenderSettings.Height = 360; break;
                        case "480p": ReplayRenderSettings.Width = 1120; ReplayRenderSettings.Height = 480; break;
                        case "720p": ReplayRenderSettings.Width = 1680; ReplayRenderSettings.Height = 720; break;
                        case "1080p": ReplayRenderSettings.Width = 2560; ReplayRenderSettings.Height = 1080; break;
                        case "1440p": ReplayRenderSettings.Width = 3440; ReplayRenderSettings.Height = 1440; break;
                        case "4K": ReplayRenderSettings.Width = 5120; ReplayRenderSettings.Height = 2160; break;
                        case "8K": ReplayRenderSettings.Width = 10080; ReplayRenderSettings.Height = 4320; break;;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 2560;
                            ReplayRenderSettings.Height = 1080;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "32:9")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 1280; ReplayRenderSettings.Height = 360; break;
                        case "480p": ReplayRenderSettings.Width = 1706; ReplayRenderSettings.Height = 480; break;
                        case "720p": ReplayRenderSettings.Width = 2560; ReplayRenderSettings.Height = 720; break;
                        case "1080p": ReplayRenderSettings.Width = 3840; ReplayRenderSettings.Height = 1080; break;
                        case "1440p": ReplayRenderSettings.Width = 5120; ReplayRenderSettings.Height = 1440; break;
                        case "4K": ReplayRenderSettings.Width = 7680; ReplayRenderSettings.Height = 2160; break;
                        case "8K": ReplayRenderSettings.Width = 15360; ReplayRenderSettings.Height = 4320; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 3840;
                            ReplayRenderSettings.Height = 1080;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "3:2")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 540; ReplayRenderSettings.Height = 360; break;
                        case "480p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 480; break;
                        case "720p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 720; break;
                        case "1080p": ReplayRenderSettings.Width = 1620; ReplayRenderSettings.Height = 1080; break;
                        case "1440p": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 1440; break;
                        case "4K": ReplayRenderSettings.Width = 3240; ReplayRenderSettings.Height = 2160; break;
                        case "8K": ReplayRenderSettings.Width = 6480; ReplayRenderSettings.Height = 4320; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1620;
                            ReplayRenderSettings.Height = 1080;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "4:3")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 360; break;
                        case "480p": ReplayRenderSettings.Width = 640; ReplayRenderSettings.Height = 480; break;
                        case "720p": ReplayRenderSettings.Width = 960; ReplayRenderSettings.Height = 720; break;
                        case "1080p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 1080; break;
                        case "1440p": ReplayRenderSettings.Width = 1920; ReplayRenderSettings.Height = 1440; break;
                        case "4K": ReplayRenderSettings.Width = 2880; ReplayRenderSettings.Height = 2160; break;
                        case "8K": ReplayRenderSettings.Width = 5760; ReplayRenderSettings.Height = 4320; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1440;
                            ReplayRenderSettings.Height = 1080;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "9:16")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 360; ReplayRenderSettings.Height = 640; break;
                        case "480p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 854; break;
                        case "720p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 1280; break;
                        case "1080p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 1920; break;
                        case "1440p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 2560; break;
                        case "4K": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 3840; break;
                        case "8K": ReplayRenderSettings.Width = 4320; ReplayRenderSettings.Height = 7680; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1080;
                            ReplayRenderSettings.Height = 1920;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "9:20")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 360; ReplayRenderSettings.Height = 800; break;
                        case "480p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 1066; break;
                        case "720p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 1600; break;
                        case "1080p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 2400; break;
                        case "1440p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 3200; break;
                        case "4K": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 4800; break;
                        case "8K": ReplayRenderSettings.Width = 4320; ReplayRenderSettings.Height = 9600; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1080;
                            ReplayRenderSettings.Height = 2400;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "9:21")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 360; ReplayRenderSettings.Height = 840; break;
                        case "480p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 1120; break;
                        case "720p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 1680; break;
                        case "1080p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 2560; break;
                        case "1440p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 3440; break;
                        case "4K": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 5120; break;
                        case "8K": ReplayRenderSettings.Width = 4320; ReplayRenderSettings.Height = 10080; break;;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1080;
                            ReplayRenderSettings.Height = 2560;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "9:32")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 360; ReplayRenderSettings.Height = 1280; break;
                        case "480p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 1706; break;
                        case "720p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 2560; break;
                        case "1080p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 3840; break;
                        case "1440p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 5120; break;
                        case "4K": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 7680; break;
                        case "8K": ReplayRenderSettings.Width = 4320; ReplayRenderSettings.Height = 15360; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1080;
                            ReplayRenderSettings.Height = 3840;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "2:3")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 360; ReplayRenderSettings.Height = 540; break;
                        case "480p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 720; break;
                        case "720p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 1080; break;
                        case "1080p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 1620; break;
                        case "1440p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 2160; break;
                        case "4K": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 3240; break;
                        case "8K": ReplayRenderSettings.Width = 4320; ReplayRenderSettings.Height = 6480; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1080;
                            ReplayRenderSettings.Height = 1620;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
                if (aspectRatio == "3:4")
                {
                    switch (value)
                    {
                        case "360p": ReplayRenderSettings.Width = 360; ReplayRenderSettings.Height = 480; break;
                        case "480p": ReplayRenderSettings.Width = 480; ReplayRenderSettings.Height = 640; break;
                        case "720p": ReplayRenderSettings.Width = 720; ReplayRenderSettings.Height = 960; break;
                        case "1080p": ReplayRenderSettings.Width = 1080; ReplayRenderSettings.Height = 1440; break;
                        case "1440p": ReplayRenderSettings.Width = 1440; ReplayRenderSettings.Height = 1920; break;
                        case "4K": ReplayRenderSettings.Width = 2160; ReplayRenderSettings.Height = 2880; break;
                        case "8K": ReplayRenderSettings.Width = 4320; ReplayRenderSettings.Height = 5760; break;
                        default:
                            _log.Warn($"Unknown resolution option: {value}, defaulting to 1080p");
                            ReplayRenderSettings.Width = 1080;
                            ReplayRenderSettings.Height = 1440;
                            break;
                    }
                    ReplayRenderSettings.SaveSettings();
                    UpdateWarnings();
                }
            }
        }

        [UIComponent("specifier-vert")]
        private UnityEngine.UI.VerticalLayoutGroup specifierVert;

        [UIValue("fps")] private int fps = ReplayRenderSettings.FPS;
        [UIValue("camera-option")] private string cameraSpecifier = ReplayRenderSettings.SpecifiedCameraName;
        [UIValue("cameraType-option")] private string cameraTypeSpecifier = ReplayRenderSettings.CameraType;
        [UIValue("bitrate")] private int bitrate = ReplayRenderSettings.BitrateKbps;
        [UIValue("audioBitrate")] private int audioBitrate = ReplayRenderSettings.AudioBitrateKbps;
        [UIValue("extraFFmpegArgs")] private string extraFFmpegArgs = ReplayRenderSettings.ExtraFFmpegArgs;

        [UIValue("video-codec-options")] private List<string> videoCodecs = new List<string>() { "h264", "hevc", "av1" };
        [UIValue("video-codec")] private string videoCodec = ReplayRenderSettings.VideoCodec;
        [UIValue("preset-options")] private List<string> presetOptions = new List<string>() { "Low", "Medium", "High" };
        [UIValue("preset-option")] private string currentPreset = ReplayRenderSettings.Preset.ToString();
        [UIValue("resolution-options")]
        private List<string> resolutionOptions = new List<string>()
        {
            "360p", "480p", "720p", "1080p", "1440p", "4K", "8K"
        };
        [UIValue("aspectRatio-options")]
        private List<string> aspectRatioOptions = new List<string>()
        {
            "16:9", "20:9", "21:9", "32:9", "3:2", "4:3"
        };
        [UIValue("portraitToggle")] private bool portraitToggle = ReplayRenderSettings.PortraitToggle;

        private List<object> _cameraOptions = new List<object>();

        [UIValue("camera-options")]
        private List<object> cameraOptions
        {
            get => _cameraOptions.ToList();
            set { _cameraOptions = value; NotifyPropertyChanged(); }
        }

        private List<object> _cameraTypeOptions = new List<object>()
        {
            "Camera2", "ReeCamera", "None"
        };

        [UIValue("cameraType-options")]
        private List<object> cameraTypeOptions
        {
            get => _cameraTypeOptions.ToList();
            set { _cameraTypeOptions = value; NotifyPropertyChanged(); }
        }

        [UIComponent("camera-specifier")] private DropDownListSetting cameraSpecifierDropDown;

        [UIComponent("cameraType-specifier")] private DropDownListSetting cameraTypeDropDown;

        private readonly string FourKWarning = "4K/8K renders require significant processing power and disk space.";
        private readonly string EightKWarning = "8K renders require the use of a HEVC encoder to render properly.";
        private readonly string Not169Warning = "You are currently running a non-16:9 aspect ratio with ReeCamera! Make sure to enable Spout\nand set the correct resolution in your ReeCamera preset!\n You can view your current resolution in UserData > RenderModSettings.json";
        private readonly string FPSWarning = "High FPS values increase file size and CPU usage.";
        private readonly string BitrateWarning = "Bitrates over 10,000 kbps may cause instability or lag.";
        private readonly string ExtraArgsWarning = "Extra FFmpeg arguments can cause instability or crashes.";
        private readonly string PresetWarning = "High Quality preset uses substantial storage and processing.";
        private readonly string CodecWarning = "AV1 is chosen as the current codec! You need either an RTX 40 series GPU or an AMD RX 7000 series GPU.";
        private readonly string NonMainCameraWarning = "Camera is not called \"Main\". Ensure this is the correct camera.";
        private readonly string NoneCameraTypeWarning = "No camera mod installed, main camera will be used.";

        [UIAction("OnResolutionChanged")]
        private void OnResolutionChanged(string value)
        {
            resolution = value;
        }
        
        [UIAction("OnAspectRatioChanged")]
        private void OnAspectRatioChanged(string value)
        {
            aspectRatio = value;
        }
        
        [UIAction("OnPortraitToggleChanged")]
        private void OnPortraitToggleChanged(bool value)
        {
            ReplayRenderSettings.PortraitToggle = value;
            if (_actionButton != null)
            {
                _actionButton.interactable = !value;
            }
            UpdateWarnings();
        }

        [UIAction("OnFPSChanged")]
        private void OnFPSChanged(int value)
        {
            ReplayRenderSettings.FPS = value;
            fps = value;
            UpdateWarnings();
        }

        [UIAction("OnCameraSpecifierChanged")]
        private void OnCameraSpecifierChanged(string value)
        {
            ReplayRenderSettings.SpecifiedCameraName = value;
            UpdateWarnings();
        }

        [UIAction("OnCameraTypeSpecifierChanged")]
        private void OnCameraTypeSpecifierChanged(string value)
        {
            ReplayRenderSettings.CameraType = value;
            cameraSpecifierDropDown.Interactable = true;
            switch (value)
            {
                case "Camera2":
                    break;
                case "None":
                case "ReeCamera":
                    cameraSpecifierDropDown.Interactable = false;
                    break;
            }
            UpdateWarnings();
        }

        [UIAction("OnBitrateChanged")]
        private void OnBitrateChanged(int value)
        {
            ReplayRenderSettings.BitrateKbps = value;
            bitrate = value;
            UpdateWarnings();
        }

        [UIAction("OnAudioBitrateChanged")]
        private void OnAudioBitrateChanged(int value)
        {
            ReplayRenderSettings.AudioBitrateKbps = value;
            UpdateWarnings();
        }

        [UIAction("OnExtraArgsChanged")]
        private void OnExtraArgsChanged(string value)
        {
            ReplayRenderSettings.ExtraFFmpegArgs = value;
            extraFFmpegArgs = value;
            UpdateWarnings();
        }

        [UIAction("OnPresetChanged")]
        private void OnPresetChanged(string value)
        {
            if (Enum.TryParse(value, out QualityPreset preset))
            {
                ReplayRenderSettings.Preset = preset;
                currentPreset = value;
            }
            UpdateWarnings();
        }

        [UIAction("OnVideoCodecChanged")]
        private void OnVideoCodecChanged(string value)
        {
            ReplayRenderSettings.VideoCodec = value;
            UpdateWarnings();
        }

        [UIComponent("encoder-test-text")] private TMPro.TextMeshProUGUI encoderTestText;

        [UIAction("OnEncoderTestClicked")]
        private void OnTestEncoder()
        {
            try
            {
                var (encoder, args) = EncoderHelpers.BuildEncoderArgs();
                encoderTestText.text = $"RESULT: {encoder} encoder";
            }
            catch (Exception ex)
            {
                _log.Error($"Failed to build encoder args: {ex}");
            }
        }

        private readonly List<string> QualityWarnings = new List<string>();
        private readonly List<string> ResolutionWarnings = new List<string>();
        private readonly List<string> BitrateWarnings = new List<string>();
        private readonly List<string> CameraWarnings = new List<string>();
        private readonly List<string> OtherWarnings = new List<string>();

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            cameraOptions.Clear();
            var cameras = CameraUtils.Core.CamerasManager.GetRegisteredCameras().Where(x => x.CameraFlags != CameraUtils.Core.CameraFlags.Mirror
                                                                                            && (!x.Camera.transform.GetObjectPath(2).Contains("/ReeLayout") && !x.Camera.transform.GetObjectPath(2).Contains("/Origin/")))
                .Select(x => (object)x.Camera.transform.GetObjectPath(2))
                .ToList();

            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

            OnCameraTypeSpecifierChanged(ReplayRenderSettings.CameraType);

            cameraOptions = cameras;
            cameraSpecifierDropDown.Values = cameraOptions;
            cameraSpecifierDropDown.Value = ReplayRenderSettings.SpecifiedCameraName;
            cameraSpecifierDropDown.UpdateChoices();

            if (firstActivation)
            {
                tabSelector.TextSegmentedControl.didSelectCellEvent += (_, cell) =>
                {
                    RefreshTab(cell);
                };

                tabSelector.TextSegmentedControl.SelectCellWithNumber(0);
                UpdateWarnings();
            }
        }

        [UIAction("#post-parse")]
        public void PostParse()
        {
            UpdateWarnings();
        }

        private void RefreshTab(int tabIndex)
        {
            switch (tabIndex)
            {
                case 0: qualityWarningText.text = BuildWarningText(QualityWarnings, "quality"); break;
                case 1: resolutionWarningText.text = BuildWarningText(ResolutionWarnings, "resolution"); break;
                case 2: bitrateWarningText.text = BuildWarningText(BitrateWarnings, "bitrate"); break;
                case 3: cameraWarningText.text = BuildWarningText(CameraWarnings, "camera"); break;
                case 4: otherWarningText.text = BuildWarningText(OtherWarnings, "other"); break;
            }
        }

        public void UpdateWarnings()
        {
            if (qualityWarningText == null || resolutionWarningText == null || bitrateWarningText == null || cameraWarningText == null || otherWarningText == null)
                return;
            
            QualityWarnings.Clear();
            ResolutionWarnings.Clear();
            BitrateWarnings.Clear();
            CameraWarnings.Clear();
            OtherWarnings.Clear();

            string currentResolution = resolution;
            string currentAspectRatio = aspectRatio;
            bool currentPortraitState = ReplayRenderSettings.PortraitToggle;
            int currentFps = ReplayRenderSettings.FPS;
            int currentBitrate = ReplayRenderSettings.BitrateKbps;
            string currentExtraArgs = ReplayRenderSettings.ExtraFFmpegArgs;
            string currentPresetString = ReplayRenderSettings.Preset.ToString();
            string currentCodec = ReplayRenderSettings.VideoCodec;
            string cameraName = ReplayRenderSettings.SpecifiedCameraName;
            string cameraType = ReplayRenderSettings.CameraType;

            if (currentResolution == "4K" || currentResolution == "8K")
                ResolutionWarnings.Add(FourKWarning);
            if (currentResolution == "8K")
                ResolutionWarnings.Add(EightKWarning);
            if (currentAspectRatio != "16:9" || currentPortraitState && cameraType == "ReeCamera")
                ResolutionWarnings.Add(Not169Warning);
            if (currentPresetString == QualityPreset.High.ToString())
                QualityWarnings.Add(PresetWarning);
            if (currentCodec == "av1")
                QualityWarnings.Add(CodecWarning);

            if (currentFps > 60)
                BitrateWarnings.Add(FPSWarning);
            if (currentBitrate > 10000)
                BitrateWarnings.Add(BitrateWarning);

            if (!string.IsNullOrEmpty(currentExtraArgs))
                OtherWarnings.Add(ExtraArgsWarning);

            if (!cameraName.ToLower().Contains("main"))
                CameraWarnings.Add(NonMainCameraWarning);

            if (cameraType == "None")
                CameraWarnings.Add(NoneCameraTypeWarning);

            qualityWarningText.text = BuildWarningText(QualityWarnings, "quality");
            resolutionWarningText.text = BuildWarningText(ResolutionWarnings, "resolution");
            bitrateWarningText.text = BuildWarningText(BitrateWarnings, "bitrate");
            cameraWarningText.text = BuildWarningText(CameraWarnings, "camera");
            otherWarningText.text = BuildWarningText(OtherWarnings, "other");

            RefreshTab(tabSelector.TextSegmentedControl.selectedCellNumber);
        }

        private string BuildWarningText(List<string> warnings, string category)
        {
            if (warnings.Count == 0)
                return $"<color=#888888>No {category} warnings.</color>";

            return string.Join("\n", warnings.Select(w => $"<color=#ffcc00>• {w}</color>"));
        }
    }
}
