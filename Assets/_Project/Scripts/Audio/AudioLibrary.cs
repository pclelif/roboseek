using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Robot.Audio
{
    [CreateAssetMenu(fileName = "AudioLibrary", menuName = "Robot/Audio/Audio Library")]
    public class AudioLibrary : ScriptableObject
    {
        [Header("UI Sounds")]
        public AudioClip[] uiHoverClips;
        public AudioClip[] uiClickClips;
        public AudioClip[] uiSelectClips;
        public AudioClip[] uiBackClips;
        public AudioClip uiStartGameClip;
        public AudioClip uiConfirmClip;
        public AudioClip uiErrorClip;
        public AudioClip uiMenuOpenClip;
        public AudioClip uiMenuCloseClip;

        [Header("Robot & Movement Sounds")]
        public AudioClip robotIdleAmbience;
        public AudioClip[] footstepGrassClips;
        public AudioClip[] footstepWoodClips;
        public AudioClip[] footstepStoneClips;
        public AudioClip waterSplashClip;
        public AudioClip jumpClip;
        public AudioClip landClip;
        public AudioClip robotDamageClip;
        public AudioClip robotKnockoutClip;

        [Header("Target Hunt Sounds")]
        public AudioClip targetScanClip;
        public AudioClip targetCorrectPickupClip;
        public AudioClip targetWrongPickupClip;
        public AudioClip targetCompleteFanfareClip;

        [Header("Combat Sounds")]
        public AudioClip attackSwingClip;
        public AudioClip attackHitClip;
        public AudioClip enemyHitClip;
        public AudioClip enemyDeathClip;

        [Header("Vehicle Ability Sounds")]
        public AudioClip ambulanceHealClip;
        public AudioClip policeRadarScanClip;
        public AudioClip taxiTurboBoostClip;

        [Header("Round Flow Sounds")]
        public AudioClip countdownTickClip;
        public AudioClip countdownGoClip;
        public AudioClip roundVictoryClip;
        public AudioClip roundDefeatClip;

        [Header("Music Tracks")]
        public AudioClip lobbyMusic;
        public AudioClip explorationMusic;
        public AudioClip showcaseMusic;
        public AudioClip pressureMusic;
        public AudioClip victoryMusic;
        public AudioClip gameOverMusic;

        public void PopulateDefaultsIfEmpty()
        {
#if UNITY_EDITOR
            // UI - Completely unique clips per event with zero repetition
            if (IsEmpty(uiHoverClips)) uiHoverClips = LoadClips("Assets/_Project/Audio/kenney_interface-sounds/Audio/click_002.ogg", "Assets/_Project/Audio/kenney_interface-sounds/Audio/click_003.ogg");
            if (IsEmpty(uiClickClips)) uiClickClips = LoadClips("Assets/_Project/Audio/kenney_interface-sounds/Audio/click_001.ogg", "Assets/_Project/Audio/kenney_interface-sounds/Audio/click_004.ogg");
            if (IsEmpty(uiSelectClips)) uiSelectClips = LoadClips("Assets/_Project/Audio/kenney_interface-sounds/Audio/select_001.ogg", "Assets/_Project/Audio/kenney_interface-sounds/Audio/select_003.ogg");
            if (IsEmpty(uiBackClips)) uiBackClips = LoadClips("Assets/_Project/Audio/kenney_interface-sounds/Audio/back_001.ogg", "Assets/_Project/Audio/kenney_interface-sounds/Audio/drop_001.ogg");
            if (uiStartGameClip == null) uiStartGameClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/confirmation_001.ogg");
            if (uiConfirmClip == null) uiConfirmClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/confirmation_003.ogg");
            if (uiErrorClip == null) uiErrorClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/error_001.ogg");
            if (uiMenuOpenClip == null) uiMenuOpenClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/open_001.ogg");
            if (uiMenuCloseClip == null) uiMenuCloseClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/close_001.ogg");

            // Movement - Dedicated physical surface & movement SFX
            if (IsEmpty(footstepGrassClips)) footstepGrassClips = LoadClips(
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_grass_001.ogg",
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_grass_002.ogg",
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_grass_003.ogg",
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_grass_004.ogg"
            );
            if (IsEmpty(footstepWoodClips)) footstepWoodClips = LoadClips(
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_wood_001.ogg",
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_wood_002.ogg",
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_wood_003.ogg",
                "Assets/_Project/Audio/kenney_impact-sounds/Audio/footstep_wood_004.ogg"
            );
            if (IsEmpty(footstepStoneClips)) footstepStoneClips = LoadClips(
                "Assets/_Project/Audio/Owlish Media Sound Effects/Footsteps/step1.wav",
                "Assets/_Project/Audio/Owlish Media Sound Effects/Footsteps/step2.wav",
                "Assets/_Project/Audio/Owlish Media Sound Effects/Footsteps/step3.wav",
                "Assets/_Project/Audio/Owlish Media Sound Effects/Footsteps/step4.wav"
            );
            if (waterSplashClip == null) waterSplashClip = LoadClip("Assets/_Project/Audio/Owlish Media Sound Effects/Water/tap-water-1.wav");
            if (jumpClip == null) jumpClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactSoft_heavy_000.ogg");
            if (landClip == null) landClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactSoft_heavy_001.ogg");
            if (robotDamageClip == null) robotDamageClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactWood_medium_001.ogg");
            if (robotKnockoutClip == null) robotKnockoutClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactPlate_heavy_001.ogg");

            // Target Hunt - 100% EXCLUSIVE audio clip for correct toy pickup!
            if (targetScanClip == null) targetScanClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/select_002.ogg");
            if (targetCorrectPickupClip == null) targetCorrectPickupClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/confirmation_002.ogg"); // EXCLUSIVE TO E-KEY PICKUP
            if (targetWrongPickupClip == null) targetWrongPickupClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/drop_002.ogg");
            if (targetCompleteFanfareClip == null) targetCompleteFanfareClip = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_crystalmountain_104bpm.wav");

            // Combat
            if (attackSwingClip == null) attackSwingClip = LoadClip("Assets/_Project/Audio/Owlish Media Sound Effects/Impacts/tap.wav");
            if (attackHitClip == null) attackHitClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactWood_medium_002.ogg");
            if (enemyHitClip == null) enemyHitClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactSoft_heavy_002.ogg");
            if (enemyDeathClip == null) enemyDeathClip = LoadClip("Assets/_Project/Audio/kenney_impact-sounds/Audio/impactPlate_medium_001.ogg");

            // Vehicles - Non-scifi, sweet natural recovery chime & energetic boost chime
            if (ambulanceHealClip == null) ambulanceHealClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/glass_003.ogg"); // NATURAL GENTLE HEAL CHIME
            if (policeRadarScanClip == null) policeRadarScanClip = LoadClip("Assets/_Project/Audio/Owlish Media Sound Effects/Scifi/High pitched bleep.wav"); // POLICE RADAR BLEEP
            if (taxiTurboBoostClip == null) taxiTurboBoostClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/toggle_002.ogg"); // ENERGETIC CUTE BOOST CHIME

            // Round Flow - Distinct tick & GO chimes
            if (countdownTickClip == null) countdownTickClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/tick_001.ogg");
            if (countdownGoClip == null) countdownGoClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/pluck_001.ogg");
            if (roundVictoryClip == null) roundVictoryClip = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_crystalmountain_104bpm.wav");
            if (roundDefeatClip == null) roundDefeatClip = LoadClip("Assets/_Project/Audio/kenney_interface-sounds/Audio/error_007.ogg");

            // Music - Valid, existing music tracks for Lobby, Victory, and Game Over
            if (lobbyMusic == null) lobbyMusic = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_retroevening_108bpm.wav");
            if (explorationMusic == null) explorationMusic = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_seacaves_96bpm.wav");
            if (showcaseMusic == null) showcaseMusic = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_crystalmountain_104bpm.wav");
            if (pressureMusic == null) pressureMusic = LoadClip("Assets/_Project/Audio/Foozle_M0002_Explorer_Chiptunes/Foozle_M0002_Explorer_Chiptunes/foozlecc - Theme #2 (30s).ogg");
            if (victoryMusic == null) victoryMusic = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_lilvillage_108bpm.wav");
            if (gameOverMusic == null) gameOverMusic = LoadClip("Assets/_Project/Audio/tinytunatunes_upbeat+cozy/tinytunatunes_upbeat+cozy/tinytunatunes_cozyexploration_spacewalk_100bpm.wav");
#endif
        }

        private static bool IsEmpty(AudioClip[] array) => array == null || array.Length == 0 || (array.Length == 1 && array[0] == null);

#if UNITY_EDITOR
        private static AudioClip LoadClip(string path)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        private static AudioClip[] LoadClips(params string[] paths)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            foreach (var path in paths)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) list.Add(clip);
            }
            return list.ToArray();
        }
#endif
    }
}
