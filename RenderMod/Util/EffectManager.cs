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
            _renderCameraEffect = new RenderCameraEffect(_log);
            CameraUtils.Core.CamerasManager.RegisterCameraEffect(_renderCameraEffect);
        }

        public void Dispose()
        {
            _renderCameraEffect = null;
            CameraUtils.Core.CamerasManager.UnRegisterCameraEffect(_renderCameraEffect);
        }
    }
}
