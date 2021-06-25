/*
Voice Falloff Override v0.01
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
using System.Collections;
using UnityEngine;
using VRC;
using VRC.Core;

namespace VoiceFalloffOverride
{

    public static class BuildInfo
    {
        public const string Name = "Voice Falloff Override";
        public const string Author = "Adnezz";
        public const string Company = null;
        public const string Version = "0.01";
        public const string DownloadLink = "https://github.com/Adnezz/VoiceFalloffOverride";
    }

    public class VoiceFalloffOverrideMod : MelonMod
    {



        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory("VFO", "Voice Falloff Override");
            MelonPreferences.CreateEntry("VFO", "Enabled", true, "VFO Enabled", false);
            MelonPreferences.CreateEntry<float>("VFO", "Distance", 25, "Falloff Distance", false);
            MelonCoroutines.Start(Initialize());
        }

        private IEnumerator Initialize()
        {
            while (ReferenceEquals(NetworkManager.field_Internal_Static_NetworkManager_0, null))
                yield return null;

            MelonLogger.Msg("Initializing Voice Falloff Override.");
            NetworkManagerHooks.Initialize();
            NetworkManagerHooks.OnJoin += OnPlayerJoined;
        }

        private void OnPlayerJoined(Player player)
        {
            if (player != null || player.field_Private_APIUser_0 != null)
            {
                UpdatePlayerVolume(player);
            }
        }

        public override void OnPreferencesSaved()
        {
            var Players = PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0;
            for (int i = 0; i < Players.Count; i++)
            {
                Player player = Players[i];
                if(player != null || player.field_Private_APIUser_0 != null)
                {
                    UpdatePlayerVolume(player);
                    
                }
            }
        }

        private void UpdatePlayerVolume(Player player)
        {
            if (MelonPreferences.GetEntryValue<bool>("VFO", "Enabled"))
            {
                player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_1 = MelonPreferences.GetEntryValue<float>("VFO", "Distance");
            }
            else
            {
                //TODO: Restore some original value here.
                player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_1 = 25;
            }
            player.prop_VRCPlayer_0.prop_PlayerAudioManager_0.Method_Private_Void_0(); //Apply Changes?
        }



    }
}
