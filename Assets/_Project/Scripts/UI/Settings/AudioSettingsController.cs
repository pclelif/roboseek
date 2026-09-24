using UnityEngine;
using UnityEngine.Audio;
using Robot.UI.Settings;
namespace Robot.UI.Production
{
    public sealed class AudioSettingsController : MonoBehaviour
    {
        [Tooltip("Optional existing mixer with exposed MusicVolume and SFXVolume parameters (dB).")]
        public AudioMixer mixer;
        public AudioSource[] musicSources = new AudioSource[0];
        public AudioSource[] sfxSources = new AudioSource[0];
        private SettingsManager settings;
        private float[] musicGains, sfxGains;
        public void Initialize(SettingsManager manager)
        {
            settings = manager;
            musicGains = Gains(musicSources); sfxGains = Gains(sfxSources);
            settings.SettingsChanged += Apply; Apply();
        }
        private static float[] Gains(AudioSource[] sources)
        {
            var values = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++) values[i] = sources[i] != null ? sources[i].volume : 1;
            return values;
        }
        private void Apply()
        {
            AudioListener.volume = settings.MasterVolume;
            if (mixer != null)
            {
                mixer.SetFloat("MusicVolume", Decibels(settings.MusicVolume));
                mixer.SetFloat("SFXVolume", Decibels(settings.SFXVolume));
            }
            else
            {
                for (int i = 0; i < musicSources.Length; i++) if (musicSources[i] != null) musicSources[i].volume = musicGains[i] * settings.MusicVolume;
                for (int i = 0; i < sfxSources.Length; i++) if (sfxSources[i] != null) sfxSources[i].volume = sfxGains[i] * settings.SFXVolume;
            }
        }
        private static float Decibels(float volume) => volume <= .0001f ? -80 : 20 * Mathf.Log10(volume);
        private void OnDestroy() { if (settings != null) settings.SettingsChanged -= Apply; }
    }
}
