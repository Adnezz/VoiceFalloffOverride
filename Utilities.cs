using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;
using VRC.Core;


using SIDictionary = System.Collections.Generic.Dictionary<string, int>;


namespace VoiceFalloffOverride
{
    /*public class distValidator : MelonLoader.Preferences.ValueValidator
    {
        public override object EnsureValid(object value)
        {
            if (value is float)
            {
                if ((float)value >= 0)
                {
                    return value;
                }
                else return new float();
            }
            else return new float();
        }

        public override bool IsValid(object value)
        {
            if (value is float)
            {
                if ((float)value >= 0)
                {
                    return true;
                }
                else return false;
            }
            else return false;
        }
    }*/
    class Utilities
    {
        //Borrowed parts from https://github.com/loukylor/VRC-Mods/blob/main/VRChatUtilityKit/Utilities/VRCUtils.cs
        //And also https://github.com/Psychloor/PlayerRotater/blob/master/PlayerRotater/Utilities.cs

        private static bool alreadyCheckingWorld;
         
        private static SIDictionary checkedWorlds = new SIDictionary();


        //0: Unblocked
        //1: Club World, Range can only be lowered
        //2: Game World, Mod Disabled
        //3: Emm Website Blacklisted, Mod Disabled
        //4: Emm GameObject Blacklisted, Mod Disabled
        //10: Not checked yet.
        internal static System.Collections.IEnumerator CheckWorld()
        {
            if (alreadyCheckingWorld)
            {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }

            //Wait for RoomManager to exist before continuing.
            ApiWorld currentWorld = null;
            while (currentWorld == null)
            {
                currentWorld = RoomManager.field_Internal_Static_ApiWorld_0;
                yield return new WaitForSecondsRealtime(1);
            }


            var worldId = currentWorld.id;
            MelonLogger.Msg($"Checking World with Id {worldId}");

            //Check cache for world, so we keep the number of API calls lower.
            if (checkedWorlds.ContainsKey(worldId))
            {
                checkedWorlds.TryGetValue(worldId, out int outres);
                VoiceFalloffOverrideMod.WorldType = outres;
                    //checkedWorlds[worldId];
                MelonLogger.Msg($"Using cached check {VoiceFalloffOverrideMod.WorldType} for world '{worldId}'");
                yield break;
            }


            //Check for Game Objects first, as it's the lowest cost check.
            if (GameObject.Find("eVRCRiskFuncEnable") != null)
            {
                VoiceFalloffOverrideMod.WorldType = 0;
                checkedWorlds.Add(worldId, 0);
                yield break;
            }
            else if (GameObject.Find("eVRCRiskFuncDisable") != null)
            {
                VoiceFalloffOverrideMod.WorldType = 4;
                checkedWorlds.Add(worldId, 4);
                yield break;
            }

            alreadyCheckingWorld = true;

            // Check if black/whitelisted from EmmVRC - thanks Emilia and the rest of EmmVRC Staff
            var uwr = UnityWebRequest.Get($"https://dl.emmvrc.com/riskyfuncs.php?worldid={worldId}");
            uwr.SendWebRequest();
            while (!uwr.isDone)
                yield return new WaitForEndOfFrame();

            var result = uwr.downloadHandler.text?.Trim().ToLower();
            uwr.Dispose();
            if (!string.IsNullOrWhiteSpace(result))
            {
                switch (result)
                {
                    case "allowed":
                        VoiceFalloffOverrideMod.WorldType = 0;
                        checkedWorlds.Add(worldId, 0);
                        alreadyCheckingWorld = false;
                        //MelonLogger.Msg($"EmmVRC allows world '{worldId}'");
                        yield break;

                    case "denied":
                        VoiceFalloffOverrideMod.WorldType = 3;
                        checkedWorlds.Add(worldId, 3);
                        alreadyCheckingWorld = false;
                        //MelonLogger.Msg($"EmmVRC denies world '{worldId}'");
                        yield break;
                }
            } 


            // no result from server or they're currently down
            // Check tags then. should also be in cache as it just got downloaded
            API.Fetch<ApiWorld>(
                worldId,
                new Action<ApiContainer>(
                    container =>
                    {
                        ApiWorld apiWorld;
                        if ((apiWorld = container.Model.TryCast<ApiWorld>()) != null)
                        {
                            short tagResult = 0;
                            foreach (var worldTag in apiWorld.tags)
                            {
                                if (worldTag.IndexOf("game", StringComparison.OrdinalIgnoreCase) != -1 && worldTag.IndexOf("games", StringComparison.OrdinalIgnoreCase) == -1)
                                {
                                    tagResult = 2;
                                    //MelonLogger.Msg($"Found game tag in world world '{worldId}'");
                                    break;
                                }
                                else if (worldTag.IndexOf("club", StringComparison.OrdinalIgnoreCase) != -1)
                                    tagResult = 1;
                            }
                            VoiceFalloffOverrideMod.WorldType = tagResult;
                            checkedWorlds.Add(worldId, tagResult);
                            alreadyCheckingWorld = false;
                            //MelonLogger.Msg($"Found no game or club tag in world world '{worldId}'");
                        }
                        else
                        {
                            MelonLogger.Error("Failed to cast ApiModel to ApiWorld");
                        }
                    }),
                disableCache: false);
        }

        internal static System.Collections.IEnumerator SampleFalloffRange()
        {
            //Wait for world type to populate and local player to exist.
            while (VoiceFalloffOverrideMod.WorldType == 10 || VRCPlayer.field_Internal_Static_VRCPlayer_0 == null)
                yield return new WaitForSecondsRealtime(2);

            //Assign values at start of world as default.

            var BPAC = GameObject.Find("BetterPlayerAudioController");
            if (BPAC != null)
            {
                VoiceFalloffOverrideMod.HasBPAC = true;
                VoiceFalloffOverrideMod.BPAC = BPAC;
                MelonLogger.Msg("Found BetterPlayerAudioController in this world.");
            }


            var PAM = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_PlayerAudioManager_0;
            VoiceFalloffOverrideMod.SetWorldDefaultParameters(PAM.field_Private_Single_2, PAM.field_Private_Single_1, PAM.field_Private_Single_0);
            VoiceFalloffOverrideMod.FinishInit();
            
            yield break;
        }

        
    }
}
