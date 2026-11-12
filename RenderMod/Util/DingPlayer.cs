using BeatSaberMarkupLanguage;
using RenderMod.Render;
using SiraUtil.Logging;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace RenderMod.Util
{
    internal class DingPlayer : MonoBehaviour
    {
        public static volatile bool shouldPlayDing;

        [Inject] private readonly SiraLog _log = null;

        private void Update()
        {
            RenderManager.RaiseFinished();
            if (shouldPlayDing)
            {
                shouldPlayDing = false;
                StartCoroutine(Ding());
            }
        }

        private IEnumerator Ding()
        {
            string path = Path.Combine(Application.temporaryCachePath, "ding.ogg");
            File.WriteAllBytes(path, Utilities.GetResource(Assembly.GetExecutingAssembly(), "RenderMod.UI.Ding.ogg"));

            using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

#if BS_1_29
                if (www.responseCode == 200)
#else
                if (www.result == UnityWebRequest.Result.Success)
#endif
                    AudioSource.PlayClipAtPoint(DownloadHandlerAudioClip.GetContent(www), Vector3.zero);
                else
                    _log.Error("Failed to load ding: " + www.error);
            }
        }
    }
}
