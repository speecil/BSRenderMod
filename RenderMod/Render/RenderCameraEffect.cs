using CameraUtils.Core;
using RenderMod.Util;
using SiraUtil.Logging;
using System.Linq;
using UnityEngine;

namespace RenderMod.Render
{
    internal class RenderCameraEffect : ICameraEffect
    {
        private SiraLog _siraLog;
        public RenderCameraEffect(SiraLog siraLog)
        {
            FoundCamera = null;
            _siraLog = siraLog;
            _siraLog.Notice("RenderCameraEffect created");
        }

        public Camera FoundCamera { get; private set; }

        public void HandleAddedToCamera(RegisteredCamera registeredCamera)
        {
            FoundCamera = registeredCamera.Camera;
            _siraLog.Info($"Render camera found: {FoundCamera.transform.GetPath()}");
        }

        public void HandleRemovedFromCamera(RegisteredCamera registeredCamera)
        {
            FoundCamera = null;
            _siraLog.Info($"Render camera removed: {registeredCamera.Camera.transform.GetPath()}");
        }

        public bool IsSuitableForCamera(RegisteredCamera registeredCamera)
        {
            if (registeredCamera.CameraFlags == CameraFlags.Mirror) return false;

            if (ReplayRenderSettings.CameraType == "ReeCamera")
            {
                var components = registeredCamera.Camera.GetComponents<Component>();
                bool isSuitable = components.Any(c => c.GetType().FullName == "ReeCamera.MainCameraController");
                return isSuitable;
            }

            return registeredCamera.Camera.transform.GetObjectPath(int.MaxValue).Contains(ReplayRenderSettings.SpecifiedCameraName);
        }
    }
}
