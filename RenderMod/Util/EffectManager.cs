using RenderMod.Render;
using SiraUtil.Logging;
using System;
using Zenject;

namespace RenderMod.Util
{
    internal class EffectManager : IInitializable, IDisposable
    {
        [Inject] private SiraLog _log;

        private RenderCameraEffect _renderCameraEffect = null;

        public RenderCameraEffect RenderCameraEffect
        {
            get
            {
                _log.Info("Getting RenderCameraEffect");
                return _renderCameraEffect;
            }
        }

        public void RefreshCameraEffect()
        {
            CameraUtils.Core.CamerasManager.UnRegisterCameraEffect(_renderCameraEffect);
            CameraUtils.Core.CamerasManager.RegisterCameraEffect(_renderCameraEffect);
        }

        public void Initialize()
        {
            _log.Info("Initializing EffectManager");
            _renderCameraEffect = new RenderCameraEffect(_log);
            CameraUtils.Core.CamerasManager.RegisterCameraEffect(_renderCameraEffect);
        }

        public void Dispose()
        {
            _log.Info("Disposing EffectManager");
            _renderCameraEffect = null;
            CameraUtils.Core.CamerasManager.UnRegisterCameraEffect(_renderCameraEffect);
        }
    }
}
