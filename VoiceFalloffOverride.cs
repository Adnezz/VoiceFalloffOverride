/*
Voice Falloff Override v0.3
A VRChat modification for MelonLoader
By Adnezz

Modified from Rank Volume Control by dave-kun
https://github.com/dave-kun/RankVolumeControl
Makes use of NetworkManagerHooks.cs from JoinNotifier by Knah
https://github.com/knah/VRCMods/tree/master/JoinNotifier

Both sources, and thus this source, are licensed under GPL-3.0
/*


/*
VRCPlayer[Remote] **ID**
	PlayerAudioManager

field_Private_Single_0 <- Gain
field_Private_Single_1 <- Audio Falloff Range
Method_Private_Void_0 <- Updates state after changing values

Public_Void_PDM_0 <- Appears to make the audio go world space?
*/

//Todo: Research and implement different method of changing audio falloff. 
//  This code currently alters Unity components on the actual Game Objects. 
//  It does not prevent later alteration of the falloff distance.
//  It also does not support properly restoring the distance back to what
//  the world creator intended. 

//  Will need to find a way to proxy or replace the VRChat function that 
//  alters the falloff distance. It's beyond my current skill and knowledge
//  to do so, however. If you're reading this, there's a good chance you 
//  have some idea! Feel free to get in touch or contribute code. :3


using MelonLoader;
using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using VRC;
using VRC.Core;
using HarmonyLib;

namespace VoiceFalloffOverride
{

    public static class BuildInfo
    {
        public const string Name = "Voice Falloff Override";
        public const string Author = "Adnezz";
        public const string Company = null;
        public const string Version = "0.4.1";
        public const string DownloadLink = "https://github.com/Adnezz/VoiceFalloffOverride";
    }

    

    public class VoiceFalloffOverrideMod : MelonMod
    {
        private static int _WorldType = 10;

        //public static distValidator dval;

        public static bool Enabled = true;
        public static bool Initializing = true;
        //public static bool Spatialize = false;
        //public static bool HasBPAC = false;
        //public static GameObject BPAC;
        public static int WorldType
        {
            get { return _WorldType; }
            set { _WorldType = value; } //0; }
        }

        public static float DefaultVoiceRange = 25;
        public static float DefaultNearRange = 0;
        public static float DefaultGain = 15;
        
        public static float VoiceRange = 25;
        public static float VoiceNearRange = 0;
        public static float VoiceGain = 15;

        public static bool PAPassthrough = true;
        public static float PADetectRange = 100;
        
        
        
        

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("VFO", "Voice Falloff Override");
            MelonPreferences.CreateEntry("VFO", "Enabled", false, "VFO Enabled");
            MelonPreferences.CreateEntry<float>("VFO", "Distance", 25, "Falloff Distance", "Range in meters where volume reaches 0% Default: 25");//, false, false, dval);
            MelonPreferences.CreateEntry<float>("VFO", "NearDistance", 0, "Falloff Start Distance", "Range in meters where volume begins dropping off. Default: 0");//, false, false, dval);
            MelonPreferences.CreateEntry<float>("VFO", "Gain", 15, "Gain", "Gain adjustment. Default: 15");//, false, false, dval);
            MelonPreferences.CreateEntry("VFO", "PAPassthrough", true, "Allow PA Systems (Experimental)");
            MelonPreferences.CreateEntry<float>("VFO", "PADetectRange", 100, "PA Range Threshold", "Threshold for detecting a public announcement or whole world voice. Default: 100");
            //MelonPreferences.CreateEntry("VFO", "Spatialize", false, "Voice Spatialization (Experimental)");

            Enabled = MelonPreferences.GetEntryValue<bool>("VFO", "Enabled");
            VoiceRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
            VoiceNearRange = MelonPreferences.GetEntryValue<float>("VFO", "NearDistance");

            VoiceGain = MelonPreferences.GetEntryValue<float>("VFO", "Gain");
            //Spatialize = MelonPreferences.GetEntryValue<bool>("VFO", "Spatialize");

            PAPassthrough = MelonPreferences.GetEntryValue<bool>("VFO", "PAPassthrough");
            PADetectRange = MelonPreferences.GetEntryValue<float>("VFO", "PADetectRange");

            //foreach (MethodInfo method in typeof(PlayerAudioManager).Get) //.GetMethods().Where(mein => mein.Name.StartsWith("Method_Private_Void"))
            //{
            //    typeof(PlayerAudioManager).GetRuntimeMethod().
            //    XrefIn
            //}

            //HarmonyInstance.Patch(typeof(AudioSource).GetMethod("set_maxVolume"), prefix: new HarmonyMethod(typeof(VoiceFalloffOverrideMod).GetMethod("OnSetMaxVol", BindingFlags.Static | BindingFlags.Public)));
            HarmonyInstance.Patch(typeof(PlayerAudioManager).GetMethod("Method_Private_Void_0"), prefix: new HarmonyMethod(typeof(VoiceFalloffOverrideMod).GetMethod("OnUpdateAvAudio", BindingFlags.Static | BindingFlags.Public)));

            MelonCoroutines.Start(Initialize());
        }

        /*public static void OnSetMaxVol(AudioSource __instance)
        {

        }*/

        public static bool OnUpdateAvAudio(PlayerAudioManager __instance)
        {
            if (WorldType >= 2) //Original audio update function will always call when VFO's use is restricted.
                return true;
            if (PAPassthrough && Enabled)
            {
                if (__instance.field_Private_Single_1 >= PADetectRange) //Check if VRC is trying to set the voice range above the PA threshold
                {
                    MelonDebug.Msg("PA Passthrough triggered");
                    return true;
                }
                else //VRC is setting the volume below the PA threshold, set it to VFO preference
                {
                    UpdatePAMVolume(__instance, GetFarRange(), GetNearRange(), GetGain());
                    return false;

                }
                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_0 = gain;
                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_1 = far_range;
                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_2 = near_range;
            }
            return (!Enabled);
        }
        

        private IEnumerator Initialize()
        {
            while (NetworkManager.field_Internal_Static_NetworkManager_0 is null)
                yield return null;

            MelonLogger.Msg("Initializing Voice Falloff Override.");
            NetworkManagerHooks.Initialize();
            NetworkManagerHooks.OnJoin += OnPlayerJoined;
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            switch (buildIndex)
            {
                case -1:
                    _WorldType = 10;
                    Initializing = true;
                    //HasBPAC = false;
                    MelonCoroutines.Start(Utilities.CheckWorld());
                    MelonCoroutines.Start(Utilities.SampleFalloffRange());
                    break;
                case 0:
                case 1:
                    break;
                default:
                    break;
                    
            }
        }

        private void OnPlayerJoined(Player player)
        {
            if (!Initializing)
            {
                if (player != null || player.field_Private_APIUser_0 != null)
                {
                    //if (Spatialize) UpdatePlayerSpatialization(player, true);
                    if ((_WorldType < 2) && Enabled)
                    {
                        UpdatePlayerVolume(player, GetFarRange(), GetNearRange(), GetGain());
                    }
                }
            }
        }

        public override void OnPreferencesSaved()
        {
            bool update = false;
            if (Enabled != MelonPreferences.GetEntryValue<bool>("VFO", "Enabled"))
            {
                Enabled = MelonPreferences.GetEntryValue<bool>("VFO", "Enabled");
                update = true;
            }
            /*if (Spatialize != MelonPreferences.GetEntryValue<bool>("VFO", "Spatialize"))
            {
                Spatialize = MelonPreferences.GetEntryValue<bool>("VFO", "Spatialize");
                if (!Initializing) UpdateAllPlayerSpatializations();
            }*/
            if (Math.Abs(VoiceRange - MelonPreferences.GetEntryValue<float>("VFO", "Distance")) > 0.01)
            {
                VoiceRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
                update = true;
            }
            if (Math.Abs(VoiceNearRange - MelonPreferences.GetEntryValue<float>("VFO", "NearDistance")) > 0.01)
            {
                VoiceNearRange = MelonPreferences.GetEntryValue<float>("VFO", "NearDistance");
                update = true;
            }
            if (Math.Abs(VoiceGain - MelonPreferences.GetEntryValue<float>("VFO", "Gain")) > 0.01)
            {
                VoiceGain = MelonPreferences.GetEntryValue<float>("VFO", "Gain");
                update = true;
            }

            if (WorldType < 2 && !Initializing & update)
                UpdateAllPlayerVolumes();

        }

        public static void FinishInit()
        {
            MelonLogger.Msg($"World Type: {WorldType}, Default Range: {DefaultVoiceRange}");


            //Apply voice ranges.
            if (WorldType < 2 && Enabled)
                UpdateAllPlayerVolumes();
            //if (Spatialize) UpdateAllPlayerSpatializations();
            Initializing = false;
        }

        public static void SetWorldDefaultParameters(float min, float max, float gain)
        {
            VoiceRange = max;
            VoiceNearRange = min;
            VoiceGain = gain;
        }



        public static void UpdateAllPlayerVolumes()
        {
            var Players = PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;
            float far_range = GetFarRange();
            float near_range = GetNearRange();
            float gain = GetGain();

            /*if (HasBPAC)
            {
                //MelonLogger.Msg($"Attempting to {(Enabled ? "disable" : "re-enable")} component...");
                BPAC.active = !Enabled;
            }*/


            for (int i = 0; i < Players.Count; i++)
            {
                
                Player player = Players.System_Collections_IList_get_Item(i).Cast<Player>();
                if (player != null || player.field_Private_APIUser_0 != null)
                {
                    UpdatePlayerVolume(player, far_range, near_range, gain);
                }
            }
        }

        /*public static void UpdateAllPlayerSpatializations()
        {
            var Players = PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;
            for (int i = 0; i < Players.Count; i++)
            {
                Player player = Players.System_Collections_IList_get_Item(i).Cast<Player>();//Players[i];
                if (player != null || player.field_Private_APIUser_0 != null)
                {
                    UpdatePlayerSpatialization(player, Spatialize);
                }
            }
        }*/

        private static float GetFarRange()
        {
            //float DesiredRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
            float ResultRange;
            if (Enabled)
            {
                switch (_WorldType)
                {
                    case 0:
                        ResultRange = VoiceRange;
                        break;
                    case 1:
                        if (VoiceRange < DefaultVoiceRange)
                            ResultRange = VoiceRange;
                        else
                            ResultRange = DefaultVoiceRange;
                        break;
                    default:
                        //Shouldn't ever get here, but just in case.
                        ResultRange = DefaultVoiceRange;
                        break;
                }
            }
            else
            {
                ResultRange = DefaultVoiceRange;
            }
            //MelonLogger.Msg($"Range Set: {ResultRange.ToString()}");
            return ResultRange;
        }

        private static float GetNearRange()
        {
            //float DesiredRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
            float ResultRange;
            if (Enabled)
            {
                switch (_WorldType)
                {
                    case 0:
                        ResultRange = VoiceNearRange;
                        break;
                    case 1:
                        if (VoiceNearRange < DefaultVoiceRange) //Allow increasing near rage to match max range.
                            ResultRange = VoiceNearRange;
                        else
                            ResultRange = DefaultVoiceRange;
                        break;
                    default:
                        //Shouldn't ever get here, but just in case.
                        ResultRange = DefaultNearRange;
                        break;
                }
            }
            else
            {
                ResultRange = DefaultNearRange;
            }
            //MelonLogger.Msg($"Range Set: {ResultRange.ToString()}");
            return ResultRange;
        }

        private static float GetGain()
        {
            //float DesiredRange = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
            float ResultRange;
            if (Enabled)
            {
                switch (_WorldType)
                {
                    case 0:
                        ResultRange = VoiceGain;
                        break;
                    case 1:
                        if (VoiceGain > DefaultGain)
                            ResultRange = VoiceGain;
                        else
                            ResultRange = DefaultGain;
                        break;
                    default:
                        //Shouldn't ever get here, but just in case.
                        ResultRange = DefaultGain;
                        break;
                }
            }
            else
            {
                ResultRange = DefaultGain;
            }
            //MelonLogger.Msg($"Range Set: {ResultRange.ToString()}");
            return ResultRange;
        }

        

        private static void UpdatePlayerVolume(Player player, float far_range, float near_range, float gain)
        {
            UpdatePAMVolume(player.prop_VRCPlayer_0.prop_PlayerAudioManager_0, far_range, near_range, gain);
        }

        private static void UpdatePAMVolume(PlayerAudioManager pam, float far_range, float near_range, float gain)
        {
            if (pam == null)
                MelonDebug.Msg("Null PAM");
            if (!Enabled) pam.Method_Private_Void_0();
            else
            {
                AudioSource audio = pam.field_Private_AudioSource_0;
                if (audio == null)
                    MelonDebug.Msg("Null audio");
                GameObject go = audio.gameObject;
                if (go == null)
                    MelonDebug.Msg("Null game object");
                USpeaker us = go.GetComponent<USpeaker>();
                if (us == null)
                    MelonDebug.Msg("Null uspeaker");
                ONSPAudioSource a2 = us.field_Private_ONSPAudioSource_0;//audio.gameObject.GetComponent<USpeaker>().field_Private_ONSPAudioSource_0;
                if (a2 == null)
                {
                    MelonDebug.Msg("Null ONSP");
                    Utilities.WaitForONSP(us, near_range, far_range, gain);
                    a2 = us.field_Private_ONSPAudioSource_0;
                    
                        
                        audio.minDistance = near_range;
                        audio.maxDistance = far_range;
                        return;
                    
                }
                    
                audio.minDistance = near_range;
                audio.maxDistance = far_range;
                a2.far = far_range * 2;
                a2.near = near_range * 2;
                a2.gain = gain;
                //a2.enableSpatialization = Enabled & Spatialize;

                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_0 = gain;
                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_1 = far_range;
                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_2 = near_range;
                //player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.Method_Private_Void_1(); //Apply Changes?
            }
        }

        /*private static void UpdatePlayerSpatialization(Player player, bool spatialize)
        {
            AudioSource audio = player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_AudioSource_0;
            ONSPAudioSource a2 = audio.gameObject.GetComponent<USpeaker>().field_Private_ONSPAudioSource_0;
            a2.enableSpatialization = spatialize;
        }*/


        



    }
}
