using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace SoundOfSilence;

[BepInPlugin(ModGUID, ModName, ModVersion)]
[BepInIncompatibility("org.bepinex.plugins.valheim_plus")]
public class SoundOfSilence : BaseUnityPlugin
{
	private const string ModName = "Sound of Silence";
	private const string ModVersion = "1.0.0";
	private const string ModGUID = "org.bepinex.plugins.soundofsilence";

	private static ConfigEntry<string> muteSounds = null!;
	private static readonly HashSet<string> muteSoundsList = new();
	private static ConfigEntry<Toggle> dumpSounds = null!;

	private enum Toggle
	{
		On = 1,
		Off = 0
	}

	private static SoundOfSilence selfReference = null!;
	private static ManualLogSource logger => selfReference.Logger;
	
	public void Awake()
	{
		selfReference = this;

		dumpSounds = Config.Bind("1 - General", "Dump Sounds", Toggle.Off, "If on, the name of sounds played by the game will be written to the log. Can be used to find the name of a specific sound. Might spam the log a lot, so turn it back off, once you found the name of the sound you were looking for.");
		muteSounds = Config.Bind("1 - General", "Mute Sounds", "", "Comma separated list of the sound effect names you want to mute. E.g. to mute the puking sound, put 'sfx_Puke_male, sfx_Puke_female' here. Or 'sfx_eat' to mute the eating sound.");
		muteSounds.SettingChanged += UpdateMuteList;

		Assembly assembly = Assembly.GetExecutingAssembly();
		Harmony harmony = new(ModGUID);
		harmony.PatchAll(assembly);
		
		UpdateMuteList(null, null);
	}
	
	private static void UpdateMuteList(object? sender, EventArgs? e)
	{
		muteSoundsList.Clear();
		foreach (string s in muteSounds.Value.Split(','))
		{
			muteSoundsList.Add(s.Trim());
		}
	}

	[HarmonyPatch(typeof(ZSFX), nameof(ZSFX.Awake))]
	private static class RemoveSounds
	{
		private static void Postfix(ZSFX __instance)
		{
			if (muteSoundsList.Contains(Utils.GetPrefabName(__instance.gameObject)))
			{
				if (dumpSounds.Value == Toggle.On)
				{
					logger.LogInfo($"The game tried to play the sound effect {Utils.GetPrefabName(__instance.gameObject)}, but the sound was muted.");
				}
				ZNetScene.instance.Destroy(__instance.gameObject);
			}
			else if (dumpSounds.Value == Toggle.On)
			{
				logger.LogInfo($"The game played the sound effect {Utils.GetPrefabName(__instance.gameObject)}.");
			}
		}
	}
}
