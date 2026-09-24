using System;
using UnityEngine;
using Robot.UI.Settings;
using Robot.UI.Production;
using Robot.ObjectHunt;

namespace Robot.Audio
{
    [DisallowMultipleComponent]
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Music Tracks (Optional Overrides)")]
        [SerializeField] private AudioClip lobbyMusicClip;
        [SerializeField] private AudioClip gameplayMusicClip;
        [SerializeField] private AudioClip victoryMusicClip;
        [SerializeField] private AudioClip defeatMusicClip;

        [Header("Sound Effects (Optional Overrides)")]
        [SerializeField] private AudioClip walkClip;
        [SerializeField] private AudioClip runClip;
        [SerializeField] private AudioClip attackClip;
        [SerializeField] private AudioClip fallWaterClip;
        [SerializeField] private AudioClip correctTargetClip;
        [SerializeField] private AudioClip wrongTargetClip;
        [SerializeField] private AudioClip pickupClip;
        [SerializeField] private AudioClip vehicleTurboClip;
        [SerializeField] private AudioClip vehicleRadarClip;
        [SerializeField] private AudioClip vehicleHealClip;
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip uiHoverClip;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        // This library is the recovered authored sound map.  Keep event-to-clip
        // decisions here instead of letting a later fallback silently substitute a sound.
        private AudioLibrary library;

        private AudioClip synthLobbyMusic;
        private AudioClip synthGameplayMusic;
        private AudioClip synthVictoryMusic;
        private AudioClip synthDefeatMusic;

        private AudioClip synthWalk;
        private AudioClip synthRun;
        private AudioClip synthAttack;
        private AudioClip synthFallWater;
        private AudioClip synthDrowningSound;
        private AudioClip synthCorrectTarget;
        private AudioClip synthWrongTarget;
        private AudioClip synthPickup;
        private AudioClip synthTurbo;
        private AudioClip synthRadar;
        private AudioClip synthHeal;
        private AudioClip synthClick;
        private AudioClip synthHover;

        private float lastStepTime;
        private RoundGameLoop activeLoop;
        private ObjectHuntRoundManager activeHunt;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            library = ScriptableObject.CreateInstance<AudioLibrary>();
            library.PopulateDefaultsIfEmpty();
            GenerateProceduralFallbacks();
            EnsureAudioListener();
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName == "RoboSeek_Lobby" || sceneName == "UI_System_Demo")
                PlayLobbyMusic();
            else
                StopMusic();
        }

        private void LoadAudioAssetsFromProject()
        {
#if UNITY_EDITOR
            if (lobbyMusicClip == null)
                lobbyMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_lilvillage_108bpm.wav");
            if (gameplayMusicClip == null)
                gameplayMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_retroevening_108bpm.wav");
            if (victoryMusicClip == null)
                victoryMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_spacewalk_100bpm.wav");
            if (defeatMusicClip == null)
                defeatMusicClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_seacaves_96bpm.wav");

            if (walkClip == null)
                walkClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Footsteps/step1.wav");
            if (runClip == null)
                runClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Footsteps/running-shoes-1.wav");
            if (vehicleTurboClip == null)
                vehicleTurboClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Technology/record_player.wav");
            if (vehicleRadarClip == null)
                vehicleRadarClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Human/attention-whistle.wav");
            if (vehicleHealClip == null)
                vehicleHealClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Human/drink-sip-and-swallow.wav");
            if (pickupClip == null)
                pickupClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Impacts/tap.wav");
            if (correctTargetClip == null)
                correctTargetClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Human/solo-clap.wav");
            if (wrongTargetClip == null)
                wrongTargetClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Owlish Media Sound Effects/Human/cough1.wav");
#endif
        }

        private void Start()
        {
            HookRoundEvents();
        }

        private void Update()
        {
            EnsureAudioListener();
            if (activeLoop == null || activeHunt == null)
            {
                HookRoundEvents();
            }
        }

        // The simplified manager lost this safeguard.  Without an active listener Unity
        // drops every music/SFX source, which made correct clip assignments sound silent.
        private void EnsureAudioListener()
        {
            if (FindFirstObjectByType<AudioListener>() != null) return;
            GameObject target = Camera.main != null ? Camera.main.gameObject : gameObject;
            target.AddComponent<AudioListener>();
        }

        public void HookRoundEvents()
        {
            if (activeLoop == null)
            {
                activeLoop = FindFirstObjectByType<RoundGameLoop>();
                if (activeLoop != null)
                {
                    activeLoop.PhaseChanged -= OnPhaseChanged;
                    activeLoop.PhaseChanged += OnPhaseChanged;
                }
            }
            if (activeHunt == null)
            {
                activeHunt = FindFirstObjectByType<ObjectHuntRoundManager>();
                if (activeHunt != null)
                {
                    activeHunt.RoundCompleted -= OnRoundCompleted;
                    activeHunt.RoundCompleted += OnRoundCompleted;
                    activeHunt.RoundFailed -= OnRoundFailed;
                    activeHunt.RoundFailed += OnRoundFailed;
                }
            }
        }

        private void OnDestroy()
        {
            if (activeLoop != null) activeLoop.PhaseChanged -= OnPhaseChanged;
            if (activeHunt != null)
            {
                activeHunt.RoundCompleted -= OnRoundCompleted;
                activeHunt.RoundFailed -= OnRoundFailed;
            }
        }

        private void OnPhaseChanged(RoundPhase phase)
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            bool isLobbyScene = sceneName == "RoboSeek_Lobby" || sceneName == "UI_System_Demo";

            switch (phase)
            {
                case RoundPhase.Lobby:
                    if (isLobbyScene) PlayLobbyMusic();
                    else StopMusic();
                    break;
                case RoundPhase.Intro:
                case RoundPhase.Targets:
                case RoundPhase.Countdown:
                case RoundPhase.Search:
                    StopMusic();
                    break;
            }
        }

        private void OnRoundCompleted()
        {
            PlayVictoryMusic();
        }

        private void OnRoundFailed()
        {
            PlayDefeatMusic();
        }

        public static bool IsGameStarting = false;

        public void PlayLobbyMusic()
        {
            if (IsGameStarting)
            {
                StopMusic();
                return;
            }
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (sceneName != "RoboSeek_Lobby" && sceneName != "UI_System_Demo")
            {
                StopMusic();
                return;
            }
            AudioClip clip = library != null && library.lobbyMusic != null ? library.lobbyMusic : (lobbyMusicClip != null ? lobbyMusicClip : synthLobbyMusic);
            PlayMusic(clip, 0.55f);
        }

        public void PlayGameplayMusic()
        {
            AudioClip clip = gameplayMusicClip != null ? gameplayMusicClip : synthGameplayMusic;
            PlayMusic(clip, 0.5f);
        }

        public void PlayVictoryMusic()
        {
            AudioClip clip = library != null && library.victoryMusic != null ? library.victoryMusic : (victoryMusicClip != null ? victoryMusicClip : synthVictoryMusic);
            PlayMusic(clip, 0.7f, loop: false);
        }

        public void PlayDefeatMusic()
        {
            AudioClip clip = library != null && library.gameOverMusic != null ? library.gameOverMusic : (defeatMusicClip != null ? defeatMusicClip : synthDefeatMusic);
            PlayMusic(clip, 0.7f, loop: false);
        }

        private void PlayMusic(AudioClip clip, float volume = 0.5f, bool loop = true)
        {
            if (musicSource == null || clip == null) return;
            if (musicSource.clip == clip && musicSource.isPlaying) return;

            musicSource.Stop();
            musicSource.clip = clip;
            musicSource.loop = loop;
            float musicVol = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume * SettingsManager.Instance.MusicVolume : 1.0f;
            musicSource.volume = volume * musicVol;
            musicSource.Play();
        }

        public void RegisterWithAudioSettings()
        {
            var controller = FindFirstObjectByType<AudioSettingsController>();
            if (controller != null)
            {
                controller.musicSources = new[] { musicSource };
                controller.sfxSources = new[] { sfxSource };
                if (SettingsManager.Instance != null)
                {
                    controller.Initialize(SettingsManager.Instance);
                }
            }
        }

        public void PlayFootstep(bool isRunning, Vector3 position = default)
        {
            float interval = isRunning ? 0.28f : 0.42f;
            if (Time.time - lastStepTime < interval) return;
            lastStepTime = Time.time;

            AudioClip clip = isRunning
                ? (runClip != null ? runClip : synthRun)
                : (walkClip != null ? walkClip : synthWalk);

            PlaySFX(clip, isRunning ? 0.5f : 0.4f, position);
        }

        public void PlayFootstep(string surfaceTag, Vector3 position)
        {
            if (library == null) { PlayFootstep(false, position); return; }
            string surface = surfaceTag == null ? string.Empty : surfaceTag.ToLowerInvariant();
            AudioClip[] clips = surface.Contains("wood") || surface.Contains("bridge")
                ? library.footstepWoodClips
                : surface.Contains("stone") || surface.Contains("metal") || surface.Contains("tile")
                    ? library.footstepStoneClips : library.footstepGrassClips;
            if (clips == null || clips.Length == 0) return;
            PlaySFX(clips[UnityEngine.Random.Range(0, clips.Length)], 0.5f, position);
        }

        public void PlayAttack(Vector3 position = default)
        {
            PlaySFX(attackClip != null ? attackClip : synthAttack, 0.7f, position);
        }

        public void PlayFallIntoWater(Vector3 position = default)
        {
            PlaySFX(fallWaterClip != null ? fallWaterClip : synthFallWater, 0.85f, position);
        }

        public void PlayCorrectTarget(Vector3 position = default)
        {
            PlaySFX(correctTargetClip != null ? correctTargetClip : synthCorrectTarget, 0.9f, position);
        }

        public void PlayWrongTarget(Vector3 position = default)
        {
            PlaySFX(wrongTargetClip != null ? wrongTargetClip : synthWrongTarget, 0.8f, position);
        }

        public void PlayPickup(Vector3 position = default)
        {
            PlaySFX(pickupClip != null ? pickupClip : synthPickup, 0.75f, position);
        }

        public void PlayVehicleTurbo(Vector3 position = default)
        {
            PlaySFX(vehicleTurboClip != null ? vehicleTurboClip : synthTurbo, 0.85f, position);
        }

        public void PlayVehicleRadar(Vector3 position = default)
        {
            PlaySFX(vehicleRadarClip != null ? vehicleRadarClip : synthRadar, 0.85f, position);
        }

        public void PlayVehicleHeal(Vector3 position = default)
        {
            PlaySFX(vehicleHealClip != null ? vehicleHealClip : synthHeal, 0.85f, position);
        }

        public void PlayUISelect() => PlayRandom(library != null ? library.uiSelectClips : null, 0.70f);
        public void PlayUIConfirm() => PlaySFX(library != null ? library.uiConfirmClip : null, 0.8f);
        public void PlayUIBack() => PlayRandom(library != null ? library.uiBackClips : null, 0.60f);
        public void PlayUIStartGame() => PlaySFX(library != null ? library.uiStartGameClip : null, 0.85f);
        public void PlayUIMenuOpen() => PlaySFX(library != null ? library.uiMenuOpenClip : null, 0.35f);
        public void PlayUIMenuClose() => PlaySFX(library != null ? library.uiMenuCloseClip : null, 0.30f);
        public void PlayUIError() => PlaySFX(library != null ? library.uiErrorClip : null, 0.65f);
        public void PlayTargetCompleteFanfare() => PlaySFX(library != null ? library.targetCompleteFanfareClip : null, 0.9f);
        public void PlayMusicVictory() => PlayVictoryMusic();
        public void PlayMusicGameOver() => PlayDefeatMusic();
        public void PlayMusicLobby() => PlayLobbyMusic();
        public void StopMusic() { if (musicSource != null) musicSource.Stop(); }
        public AudioClip WaterDrowningClip => library != null && library.waterSplashClip != null ? library.waterSplashClip : (fallWaterClip != null ? fallWaterClip : synthDrowningSound);
        public void PlayPoliceRadarScan(Vector3 pos = default) => PlaySFX(library != null ? library.policeRadarScanClip : vehicleRadarClip, 0.65f, pos);
        public void PlayTaxiTurboBoost(Vector3 pos = default) => PlaySFX(library != null ? library.taxiTurboBoostClip : vehicleTurboClip, 0.85f, pos);
        public void PlayAmbulanceHeal(Vector3 pos = default) => PlaySFX(library != null ? library.ambulanceHealClip : vehicleHealClip, 0.75f, pos);
        public void PlayCountdownTick() => PlaySFX(library != null ? library.countdownTickClip : null, 0.65f);
        public void PlayCountdownGo() => PlaySFX(library != null ? library.countdownGoClip : null, 0.85f);
        public void PlayJump(Vector3 pos) => PlaySFX(library != null ? library.jumpClip : null, 0.7f, pos);
        public void PlayLand(Vector3 pos) => PlaySFX(library != null ? library.landClip : null, 0.75f, pos);
        public void PlayWaterSplash(Vector3 pos) => PlaySFX(library != null ? library.waterSplashClip : fallWaterClip, 0.85f, pos);
        public void PlayRobotDamage() => PlaySFX(library != null ? library.robotDamageClip : null, 0.8f);
        public void PlayRobotKnockout() => PlaySFX(library != null ? library.robotKnockoutClip : null, 0.9f);
        public void PlayTargetScan() => PlaySFX(library != null ? library.targetScanClip : null, 0.65f);
        public void PlayTargetCorrectPickup() => PlaySFX(library != null ? library.targetCorrectPickupClip : correctTargetClip, 0.85f);
        public void PlayTargetWrongPickup() => PlaySFX(library != null ? library.targetWrongPickupClip : wrongTargetClip, 0.75f);
        public void PlayAttackSwing() => PlaySFX(library != null ? library.attackSwingClip : attackClip, 0.6f);
        public void PlayAttackHit() => PlaySFX(library != null ? library.attackHitClip : attackClip, 0.75f);
        public void StopAllSFX() { if (sfxSource != null) sfxSource.Stop(); }

        public void PlayUIClick()
        {
            PlayRandom(library != null ? library.uiClickClips : null, 0.65f, uiClickClip != null ? uiClickClip : synthClick);
        }

        public void PlayUIHover()
        {
            PlayRandom(library != null ? library.uiHoverClips : null, 0.45f, uiHoverClip != null ? uiHoverClip : synthHover);
        }

        private void PlayRandom(AudioClip[] clips, float volume, AudioClip fallback = null)
        {
            if (clips != null && clips.Length > 0) PlaySFX(clips[UnityEngine.Random.Range(0, clips.Length)], volume);
            else PlaySFX(fallback, volume);
        }

        private void PlaySFX(AudioClip clip, float volume = 1.0f, Vector3 position = default)
        {
            if (clip == null) return;
            float masterVol = SettingsManager.Instance != null ? SettingsManager.Instance.MasterVolume * SettingsManager.Instance.SFXVolume : 1.0f;
            float finalVol = volume * masterVol;

            if (position != default)
            {
                AudioSource.PlayClipAtPoint(clip, position, finalVol);
            }
            else if (sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, finalVol);
            }
        }

        private void GenerateProceduralFallbacks()
        {
            // Rich multi-harmonic musical tracks for Lobby, Gameplay, Win (Victory), and Lose (Defeat)
            synthLobbyMusic = CreatePolyphonicTheme(new[] { 261.63f, 329.63f, 392.00f, 523.25f, 392.00f, 329.63f }, 0.35f, 4);
            synthGameplayMusic = CreatePolyphonicTheme(new[] { 220.00f, 261.63f, 329.63f, 293.66f, 349.23f, 440.00f }, 0.4f, 4);
            synthVictoryMusic = CreateFanfareTheme(new[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.3f);
            synthDefeatMusic = CreateMinorTheme(new[] { 349.23f, 329.63f, 293.66f, 220.00f }, 0.45f);

            // Realistic footfall double-thud impacts
            synthWalk = CreateFootstepThud(0.08f, 120f, 60f);
            synthRun = CreateFootstepThud(0.06f, 150f, 70f);

            // Action & interaction audio
            synthAttack = CreateSweep(0.14f, 650f, 120f);
            synthFallWater = CreateSplashSound(0.45f);
            synthDrowningSound = CreateSplashSound(5.0f);
            synthCorrectTarget = CreateArpeggio(new[] { 523.25f, 659.25f, 783.99f, 1046.50f, 1318.51f }, 0.09f);
            synthWrongTarget = CreateToneSequence(new[] { 247.94f, 196.00f }, 0.16f);
            synthPickup = CreateSweep(0.22f, 320f, 950f);

            // Vehicles
            synthTurbo = CreateEngineRev(0.6f);
            synthRadar = CreateSonarPing(0.4f);
            synthHeal = CreateHealingChime(0.6f);

            // UI
            synthClick = CreateTone(0.04f, 880f);
            synthHover = CreateTone(0.03f, 580f);
        }

        private static AudioClip CreateFootstepThud(float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            System.Random rand = new System.Random(1337);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = t / duration;
                float env = MathF.Pow(1.0f - norm, 2.5f);
                float lowFreq = Mathf.Lerp(startFreq, endFreq, norm);
                float thud = Mathf.Sin(2f * Mathf.PI * lowFreq * t) * 0.8f;
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.2f * (1.0f - norm);
                data[i] = (thud + noise) * env;
            }
            AudioClip clip = AudioClip.Create("Synth_Footstep", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSplashSound(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            System.Random rand = new System.Random(2024);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = t / duration;
                float env = MathF.Sin(norm * MathF.PI) * (1.0f - norm * 0.5f);
                float noise = (float)(rand.NextDouble() * 2.0 - 1.0) * 0.6f;
                float bubble = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(300f, 100f, norm) * t) * 0.4f;
                data[i] = (noise + bubble) * env;
            }
            AudioClip clip = AudioClip.Create("Synth_Splash", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateEngineRev(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = t / duration;
                float freq = Mathf.Lerp(150f, 600f, norm * norm);
                float env = MathF.Sin(norm * MathF.PI);
                float engine = Mathf.Sin(2f * Mathf.PI * freq * t) + 0.5f * Mathf.Sin(4f * Mathf.PI * freq * t);
                data[i] = (engine * 0.5f) * env;
            }
            AudioClip clip = AudioClip.Create("Synth_EngineRev", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSonarPing(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = t / duration;
                float env = MathF.Pow(1.0f - norm, 3f);
                float ping = Mathf.Sin(2f * Mathf.PI * 1200f * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * 2400f * t);
                data[i] = ping * 0.5f * env;
            }
            AudioClip clip = AudioClip.Create("Synth_SonarPing", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateHealingChime(float duration)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            float[] freqs = { 440f, 554.37f, 659.25f, 880f };
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = t / duration;
                float env = MathF.Sin(norm * MathF.PI);
                float sample = 0f;
                foreach (var f in freqs)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * f * t) * 0.25f;
                }
                data[i] = sample * env;
            }
            AudioClip clip = AudioClip.Create("Synth_HealingChime", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreatePolyphonicTheme(float[] melodyNotes, float noteLength, int loops)
        {
            int sampleRate = 44100;
            int loopSamples = (int)(sampleRate * noteLength * melodyNotes.Length);
            int totalSamples = loopSamples * loops;
            float[] data = new float[totalSamples];

            for (int l = 0; l < loops; l++)
            {
                int loopOffset = l * loopSamples;
                for (int note = 0; note < melodyNotes.Length; note++)
                {
                    float freq = melodyNotes[note];
                    int noteOffset = loopOffset + (int)(note * noteLength * sampleRate);
                    int noteSamples = (int)(noteLength * sampleRate);

                    for (int i = 0; i < noteSamples && (noteOffset + i) < totalSamples; i++)
                    {
                        float t = (float)i / sampleRate;
                        float env = MathF.Sin((float)i / noteSamples * MathF.PI);
                        float mel = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f;
                        float bass = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.3f;
                        data[noteOffset + i] += (mel + bass) * env * 0.5f;
                    }
                }
            }
            AudioClip clip = AudioClip.Create("Synth_Theme", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateFanfareTheme(float[] notes, float noteLength)
        {
            return CreatePolyphonicTheme(notes, noteLength, 2);
        }

        private static AudioClip CreateMinorTheme(float[] notes, float noteLength)
        {
            return CreatePolyphonicTheme(notes, noteLength, 2);
        }

        private static AudioClip CreateTone(float duration, float frequency)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float env = 1.0f - (t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env;
            }
            AudioClip clip = AudioClip.Create($"Synth_{frequency}Hz", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateSweep(float duration, float startFreq, float endFreq)
        {
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] data = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float norm = t / duration;
                float freq = Mathf.Lerp(startFreq, endFreq, norm);
                float env = Mathf.Sin(norm * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
            }
            AudioClip clip = AudioClip.Create("Synth_Sweep", sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateArpeggio(float[] frequencies, float noteDuration)
        {
            int sampleRate = 44100;
            int totalSamples = (int)(sampleRate * noteDuration * frequencies.Length);
            float[] data = new float[totalSamples];
            int noteSamples = (int)(sampleRate * noteDuration);

            for (int note = 0; note < frequencies.Length; note++)
            {
                float freq = frequencies[note];
                int offset = note * noteSamples;
                for (int i = 0; i < noteSamples && (offset + i) < totalSamples; i++)
                {
                    float t = (float)i / sampleRate;
                    float env = 1.0f - ((float)i / noteSamples);
                    data[offset + i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env;
                }
            }
            AudioClip clip = AudioClip.Create("Synth_Arp", totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateToneSequence(float[] frequencies, float noteDuration)
        {
            return CreateArpeggio(frequencies, noteDuration);
        }
    }
}
