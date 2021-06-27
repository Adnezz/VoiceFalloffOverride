using MelonLoader;
using System;
using System.Collections;
using Il2CppSystem.Collections.Generic;
using UnityEngine;
using VRC;
using VRC.Core;

namespace VoiceFalloffOverride
{
    class Utilities
    {
        //Borrowed parts from https://github.com/loukylor/VRC-Mods/blob/main/VRChatUtilityKit/Utilities/VRCUtils.cs
        //And also https://github.com/Psychloor/PlayerRotater/blob/master/PlayerRotater/Utilities.cs

        private static bool alreadyCheckingWorld;
        private static Dictionary<string, int> checkedWorlds = new Dictionary<string, int>();
        internal static IEnumerator CheckWorld()
        {
            if (alreadyCheckingWorld)
            {
                MelonLogger.Error("Attempted to check for world multiple times");
                yield break;
            }


            ApiWorld currentWorld = null;
            while (currentWorld == null)
            {
                currentWorld = RoomManager.field_Internal_Static_ApiWorld_0;
                yield return new WaitForSecondsRealtime(1);
            }
            var worldId = currentWorld.id;
            MelonLogger.Msg($"Checking World with Id {worldId}");

            if (checkedWorlds.ContainsKey(worldId))
            {
                VoiceFalloffOverrideMod.WorldType = checkedWorlds[worldId];
                MelonLogger.Msg($"Using cached check {checkedWorlds[worldId]} for world '{worldId}'");
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
            var www = new WWW($"https://dl.emmvrc.com/riskyfuncs.php?worldid={worldId}", null, new Dictionary<string, string>());

            while (!www.isDone)
                yield return new WaitForEndOfFrame();

            var result = www.text?.Trim().ToLower();
            www.Dispose();
            if (!string.IsNullOrWhiteSpace(result))
            {
                switch (result)
                {
                    case "allowed":
                        VoiceFalloffOverrideMod.WorldType = 0;
                        checkedWorlds.Add(worldId, 0);
                        alreadyCheckingWorld = false;
                        MelonLogger.Msg($"EmmVRC allows world '{worldId}'");
                        yield break;

                    case "denied":
                        VoiceFalloffOverrideMod.WorldType = 3;
                        checkedWorlds.Add(worldId, 3);
                        alreadyCheckingWorld = false;
                        MelonLogger.Msg($"EmmVRC denies world '{worldId}'");
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
                                if (worldTag.IndexOf("game", StringComparison.OrdinalIgnoreCase) != -1)
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

        internal static IEnumerator SampleFalloffRange()
        {
            while (VoiceFalloffOverrideMod.WorldType == 10)
                yield return new WaitForSecondsRealtime(1);
            VoiceFalloffOverrideMod.DefaultVoiceRange = VRCPlayer.field_Internal_Static_VRCPlayer_0.prop_PlayerAudioManager_0.field_Private_Single_1;
            MelonLogger.Msg($"World Type: {VoiceFalloffOverrideMod.WorldType}, Default Range: {VoiceFalloffOverrideMod.DefaultVoiceRange}");
            if (VoiceFalloffOverrideMod.WorldType < 2 && VoiceFalloffOverrideMod.Enabled)
                VoiceFalloffOverrideMod.UpdateAllPlayerVolumes();
            VoiceFalloffOverrideMod.Initializing = false;
            yield break;
        }
    }
}
