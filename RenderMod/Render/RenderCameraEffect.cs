using CameraUtils.Core;
using RenderMod.Util;
using SiraUtil.Logging;
using System.Linq;
using UnityEngine;

namespace RenderMod.Render
{
    internal class RenderCameraEffect : ICameraEffect
    {
        private readonly SiraLog _log;

        public RenderCameraEffect(SiraLog log)
        {
            _log = log;
        }

        public Camera FoundCamera { get; private set; }

        public void HandleAddedToCamera(RegisteredCamera registeredCamera)
        {
            FoundCamera = registeredCamera.Camera;
            _log.Info($"Render camera found: {FoundCamera.transform.GetPath()}");
        }

        public void HandleRemovedFromCamera(RegisteredCamera registeredCamera)
        {
            FoundCamera = null;
            _log.Info($"Render camera removed: {registeredCamera.Camera.transform.GetPath()}");
        }

        public bool IsSuitableForCamera(RegisteredCamera registeredCamera)
        {
            // "None" renders from Camera.main
            if (registeredCamera.CameraFlags == CameraFlags.Mirror || ReplayRenderSettings.CameraType == "None")
                return false;

            if (ReplayRenderSettings.CameraType == "ReeCamera")
                return registeredCamera.Camera.GetComponents<Component>().Any(c => c.GetType().FullName == "ReeCamera.MainCameraController");

            return registeredCamera.Camera.transform.GetObjectPath(int.MaxValue).Contains(ReplayRenderSettings.SpecifiedCameraName);
        }
    }
}
