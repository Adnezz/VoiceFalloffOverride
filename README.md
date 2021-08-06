# Voice Falloff Override
### A VRChat mod for MelonLoader by Adnezz

Built against and tested with MelonLoader v0.4.3, and VRChat Build 1121.

Allows local customization of the distance values on player voices, AKA: the voice falloff distance.  
Does NOT change how far other players hear voices travel, only changes what you hear.

Also includes a fix for voice spatialization. A recent VRChat update disabled spatial audio on avatar voices, making voices very hard to localize in a crowd. This may have been fixed as of 1121, so it is not enabled by default, but it's still there if you want to experiment.

## Background
I wrote this because I often felt like I was drowning in voices when hanging out in a crowd in VRChat. I can't tell for certain, but it feels like voice localization and falloff has taken a turn for the worse over recent updates. With this mod, I can lower the distance and stay focused on the conversation I'm having with the people immediately around me.

Could also be used for people in particularly large avatars, so they can hear people on the ground.



## Requirements
* MelonLoader v0.4.3
* UIExpansionKit is recommended.

## Use

If you have UIExpansionKit, you can enable the mod and change the set falloff distance in the Mod Settings panel in VRChat's settings menu. Pinning the VFO Enabled setting to the Quick Menu allows for ease of enabling and disabling the mod.

Otherwise, after running VRChat with the mod installed for the first time, you can edit the falloff distance in VRChat/UserData/MelonPreferences.cfg under the VFO header.

This mod will not function in Game worlds or worlds on emmvrc's blacklist. In worlds with the club tag, you may only reduce the falloff range from the world's default, not increase it.


## Credit
This mod was built up from dave-kun's [RankVolumeControl](https://github.com/dave-kun/RankVolumeControl) and uses NetworkManagerHooks.cs from Knah's [JoinNotifier](https://github.com/knah/VRCMods/tree/master/JoinNotifier)
Special thanks to lil-fluff for assistance in tracking down the cause of VFO failing to work in some worlds.




## WARNING
All modification of the VRChat client is against the VRChat terms of service, and is a potentially bannable offense. Despite the current cease-fire between VRC devs and the modding community, the current state of affairs could change at any time. I have made some effort to ensure that this mod does not change the way the client behaves from the  perspective of VRC Servers in order to minimize risk, but it would be relatively easy for VRChat devs to add in a check for any modification of the client in the future.

So, like any other VRChat mod, **USE AT YOUR OWN RISK.**
