using CameraUtils.Core;
using RenderMod.Render;
using SiraUtil.Logging;
using System;
using Zenject;

namespace RenderMod.Util
{
    internal class EffectManager : IInitializable, IDisposable
    {
        [Inject] private readonly SiraLog _log = null;

        public RenderCameraEffect RenderCameraEffect { get; private set; }

        public void Initialize()
        {
            RenderCameraEffect = new RenderCameraEffect(_log);
            CamerasManager.RegisterCameraEffect(RenderCameraEffect);
        }

        public void Dispose()
        {
            CamerasManager.UnRegisterCameraEffect(RenderCameraEffect);
            RenderCameraEffect = null;
        }
    }
}
