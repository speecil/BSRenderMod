using CameraUtils.Core;
using RenderMod.Util;
using SiraUtil.Logging;
using UnityEngine;

namespace RenderMod.Render
{
    internal class RenderCameraEffect : ICameraEffect
    {
        SiraLog _siraLog;
        public RenderCameraEffect(SiraLog siraLog)
        {
            FoundCamera = null;
            _siraLog = siraLog;
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
            //if(registeredCamera.CameraFlags == CameraFlags.Mirror)
            //{
            //    return false;
            //}
            //_siraLog.Info($"Checking camera: {registeredCamera.Camera.transform.GetPath()}");
            //_siraLog.Info($"Camera Flags: {registeredCamera.CameraFlags}");
            //_siraLog.Info($"Render Enabled: {ReplayRenderSettings.RenderEnabled}");
            //_siraLog.Info($"Specified Camera Name: {ReplayRenderSettings.SpecifiedCameraName}");
            //_siraLog.Info($"Camera Path Contains Specified Name: {registeredCamera.Camera.transform.GetObjectPath(int.MaxValue).Contains(ReplayRenderSettings.SpecifiedCameraName)}");
            return registeredCamera.CameraFlags != CameraFlags.Mirror && registeredCamera.Camera.transform.GetObjectPath(int.MaxValue).Contains(ReplayRenderSettings.SpecifiedCameraName);
        }
    }
}
