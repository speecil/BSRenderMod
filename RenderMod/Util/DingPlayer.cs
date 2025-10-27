using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace RenderMod.Util
{
    // USAGE: DingPlayer.shouldPlayDing = true; it one shots the audio type shit
    internal class DingPlayer : MonoBehaviour
    {
        public static bool shouldPlayDing = false;

        [Inject] private readonly SiraLog _log;

        private IEnumerator Ding()
        {
            shouldPlayDing = false;
            float previousVolume = AudioListener.volume;
            AudioListener.volume = 0.75f;

            byte[] dingData = Utilities.GetResource(Assembly.GetExecutingAssembly(), "RenderMod.UI.Ding.ogg");

            string tempPath = Path.Combine(Application.temporaryCachePath, "ding.ogg");
            File.WriteAllBytes(tempPath, dingData);

            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.OGGVORBIS))
            {
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    AudioSource.PlayClipAtPoint(clip, Vector3.zero);
                }
                else
                {
                    _log.Error("Failed to load ding: " + www.error);
                }
            }
            AudioListener.volume = previousVolume;
        }

        void Update()
        {
            if (shouldPlayDing)
            {
                StartCoroutine(Ding());
            }
        }
    }
}
