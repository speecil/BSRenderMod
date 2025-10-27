using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine;
using BeatSaberMarkupLanguage;
using Zenject;
using SiraUtil.Logging;

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
