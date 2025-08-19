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
        private MainSettingsModelSO settings;

        public SettingsApplicator(ReplayVideoRenderer videoRenderer, ReplayProgressUI progressUI)
        {
            _videoRenderer = videoRenderer;
            _progressUI = progressUI;
        }

        // CURRENTLY JUST FORCES MAX SETTINGS
        // TODO: actually have a settings menu for it
        public void Initialize()
        {
            settings = Resources.FindObjectsOfTypeAll<MainSettingsModelSO>().FirstOrDefault();
            MainSettingsBestGraphicsValues.ApplyValues(settings);
            settings.smokeGraphicsSettings.value = false;
            settings.maxShockwaveParticles.value = 0;

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
            settings.antiAliasingLevel.value = antiAlias;
        }

        public void Dispose()
        {
            settings = null;
        }
    }
}
