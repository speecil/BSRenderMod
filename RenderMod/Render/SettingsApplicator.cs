using System;
using System.Linq;
using UnityEngine;
using Zenject;

namespace RenderMod.Render
{
    internal class SettingsApplicator : IInitializable, IDisposable
    {
        private readonly ReplayVideoRenderer _videoRenderer;
        private readonly ReplayProgressUI _progressUI;
        private SettingsApplicatorSO _settings;

        public SettingsApplicator(ReplayVideoRenderer videoRenderer, ReplayProgressUI progressUI)
        {
            _videoRenderer = videoRenderer;
            _progressUI = progressUI;
        }

        // CURRENTLY JUST FORCES MAX SETTINGS
        // TODO: actually have a settings menu for it
        public void Initialize()
        {
            _settings = Resources.FindObjectsOfTypeAll<SettingsApplicatorSO>().FirstOrDefault();
            BeatSaber.Settings.Settings settings = new BeatSaber.Settings.Settings();

            settings.quality.renderViewportScale = 1;
            settings.quality.maxQueuedFrames = -1;
            settings.quality.vSyncCount = 0;
            settings.quality.targetFramerate = UnityEngine.Application.targetFrameRate;

            // shockwaves and walls unused in ApplyPerformancePreset
            int antiAlias = 0;
            bool distortions = true;
            if (!distortions)
            {
                switch (3)
                {
                    case 0:
                        antiAlias = 0;
                        break;
                    case 1:
                        antiAlias = 2;
                        break;
                    case 2:
                        antiAlias = 4;
                        break;
                    case 3:
                        antiAlias = 8;
                        break;
                }
            }
            settings.quality.antiAliasingLevel = antiAlias;

            settings.quality.bloom = BeatSaber.Settings.QualitySettings.BloomQuality.Game;
            settings.quality.mainEffect = true ? BeatSaber.Settings.QualitySettings.MainEffectOption.Game
                                                                       : BeatSaber.Settings.QualitySettings.MainEffectOption.Off;
            settings.quality.mirror = BeatSaber.Settings.QualitySettings.MirrorQuality.High;

            _settings.ApplyGraphicSettings(settings, sceneType: SceneType.Game);
        }

        public void Dispose()
        {
            _settings = null;
        }
    }
}
