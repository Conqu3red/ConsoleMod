using System;
using BepInEx;
using Logger = BepInEx.Logging.Logger;
using PolyTechFramework;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using BepInEx.Configuration;

namespace ConsoleMod
{
	[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
	// Specify the mod as a dependency of PTF
	[BepInDependency(PolyTechMain.PluginGuid, BepInDependency.DependencyFlags.HardDependency)]
	// This Changes from BaseUnityPlugin to PolyTechMod.
	// This superclass is functionally identical to BaseUnityPlugin, so existing documentation for it will still work.
	public class ConsoleMod : PolyTechMod
	{
		public new const string
			PluginGuid = "org.bepinex.plugins.ConsoleMod",
			PluginName = "Console Mod",
			PluginVersion = "0.1.0.0";
		public static ConfigDefinition
			modEnabledDef = new ConfigDefinition("Console", "Enabled");
		public static ConfigEntry<bool>
			modEnabled;
		public static bool _enabled = true;//modEnabled.Value;
		void Awake()
		{
			// Use this if you wish to make the mod trigger cheat mode ingame.
			// Set this true if your mod effects physics or allows mods that you can't normally do.
			this.isCheat = false;
			// Set this to whether the mod is currently enabled or not.
			// Usually you want this to be true by default.
			this.isEnabled = true;


			// Register the mod to PTF, that way it will be able to be configured using PTF ingame.

			modEnabled = Config.Bind(modEnabledDef, true, new ConfigDescription("Enable Mod"));
			modEnabled.SettingChanged += onEnableDisable;
			_enabled = modEnabled.Value;

			harmony = new Harmony("org.bepinex.plugins.ConsoleCinematicCamera");
			harmony.PatchAll(Assembly.GetExecutingAssembly());
			Logger.LogInfo("Console Initiated.");
			// ConsoleCommands.Init();
			PolyTechMain.registerMod(this);

			
		}

		public void onEnableDisable(object sender, EventArgs e)
		{
			this.isEnabled = modEnabled.Value;

			if (modEnabled.Value)
			{
				enableMod();
			}
			else
			{
				disableMod();
			}
		}


		[HarmonyPatch(typeof(Main))]
		[HarmonyPatch("InstantiateGameUI")]
		//[HarmonyPatch("Awake")]
		public class PatchMain
		{
			static void Postfix()
            {
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Main.m_Instance.m_ConsoleUI, Vector3.zero, Quaternion.identity);
				if (gameObject != null)
				{
					DontDestroyOnLoad(gameObject);
					gameObject.name = Main.m_Instance.m_ConsoleUI.name;
				}
				uConsole.UnRegisterCommand("version");
				uConsole.RegisterCommand("version", "show uConsole version", new uConsole.DebugCommand(mod_ver));

				uConsole.RegisterCommand("popup", new uConsole.DebugCommand(popup));
				
				uConsole.RegisterCommand("bridge_hide", new uConsole.DebugCommand(bridge_hide));
				uConsole.RegisterCommand("bridge_reveal", new uConsole.DebugCommand(bridge_reveal));
				uConsole.RegisterCommand("cam_info", new uConsole.DebugCommand(cam_info));
				uConsole.RegisterCommand("cin_start", new uConsole.DebugCommand(cin_start));
				uConsole.RegisterCommand("cin_start_restore", new uConsole.DebugCommand(cin_start_restore));
				uConsole.RegisterCommand("cin_ease", new uConsole.DebugCommand(cin_ease));
				uConsole.RegisterCommand("cin_end", new uConsole.DebugCommand(cin_end));
				uConsole.RegisterCommand("cin_end_restore", new uConsole.DebugCommand(cin_end_restore));
				uConsole.RegisterCommand("cin_duration", new uConsole.DebugCommand(cin_duration));
				uConsole.RegisterCommand("vehicle_show_polygon_shapes", new uConsole.DebugCommand(vehicle_show_polygon_shapes));
				
			}
		}
		[HarmonyPatch(typeof(uConsoleGUI))]
		[HarmonyPatch("Update")]
		public class PatchConsole
        {
			 static void Prefix ()
            {
				if (uConsole.IsOn() && !_enabled)
                {
					uConsole.TurnOff();
                }
            }
        }

		// Use this method to execute code that will be ran when the mod is enabled.
		public override void enableMod() 
		{
			//Logger.LogInfo("Enabled!");
			_enabled = true;
		}
		// Use this method to execute code that will be ran when the mod is disabled.
		public override void disableMod() 
		{
			//Logger.LogInfo("Disabled!");
			_enabled = false;
		}

		// I have no idea how either of this functions work,
		// so just talk to MoonlitJolty if you wanna know what to do this this.
		// This returns a stringified version of the current mod settings.
		public override string getSettings() { return ""; }
		// This takes a stringified version of the mod settings and updates the settings to that.
		public override void setSettings(string settings) { }

		private void InstantiateConsole()
		{
			GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(Main.m_Instance.m_ConsoleUI, Vector3.zero, Quaternion.identity);
			if (gameObject != null)
			{
                DontDestroyOnLoad(gameObject);
				gameObject.name = Main.m_Instance.m_ConsoleUI.name;
			}
		}

		private static void mod_ver()
        {
			uConsoleCommands.ShowVersion();
			uConsole.Log(PluginName + " | Version " + PluginVersion);
			uConsole.Log("Mod Created by Conqu3red");
        }

		private static void popup() 
		{
			if (uConsole.GetNumParameters() == 1)
            {
				
				PopUpMessage.DisplayOkOnly(uConsole.GetString(), null);
            }
		}

		private static void bridge_hide()
		{
			Bridge.Hide();
		}

		// Token: 0x060058DD RID: 22749
		private static void bridge_reveal()
		{
			float intervalSeconds = 0.1f;
			if (uConsole.GetNumParameters() == 1)
			{
				intervalSeconds = uConsole.GetFloat();
			}
			Bridge.Reveal(intervalSeconds);
			uConsole.TurnOff();
		}

		// Token: 0x060058DE RID: 22750
		private static void cam_info()
		{
			uConsole.Log("PIVOT" + PointsOfView.m_Pivot.ToString());
			uConsole.Log("OFFSET FROM PIVOT: " + 
			(
			Camera.main.transform.position - PointsOfView.m_Pivot
			).ToString());
			uConsole.Log("ScreenCenterToWorld " + 
			(Camera.main.ScreenToWorldPoint(new Vector3((float)(Screen.width / 2), 
			(float)(Screen.height / 2), 
			GameSettings.CamDistFromPivot())
			).ToString()));

			uConsole.Log("POS: " + 
			(
			Camera.main.transform.position.ToString()
			));
			uConsole.Log("ROT: " + 
			(
			Camera.main.transform.eulerAngles.ToString()
			));
			uConsole.Log("SIZE: " + 
			(
			Camera.main.orthographicSize.ToString()
			));
		}

		// Token: 0x060058DF RID: 22751
		private static void cin_start()
		{
			Vehicle vehicle = null;
			if (uConsole.GetNumParameters() == 1)
			{
				string @string = uConsole.GetString();
				vehicle = Vehicles.FindVehicleWithLabel(@string);
				if (vehicle == null)
				{
					uConsole.Log("Could not find vehicle with label " + @string);
					return;
				}
				uConsole.Log("Following " + Localize.Get(vehicle.m_DisplayNameLocKey).ToString() + vehicle.GetTextMeshString().ToString());
			}
			CinemaCamera.SaveStart(vehicle);
		}

		// Token: 0x060058E0 RID: 22752
		private static void cin_start_restore()
		{
			CinemaCamera.RestoreStart();
		}

		// Token: 0x060058E1 RID: 22753
		private static void cin_ease()
		{
			if (uConsole.GetNumParameters() == 0)
			{
				CinemaCamera.m_Ease = !CinemaCamera.m_Ease;
			}
			if (uConsole.GetNumParameters() == 1)
			{
				CinemaCamera.m_Ease = uConsole.GetBool();
			}
			if (uConsole.GetNumParameters() == 0 || uConsole.GetNumParameters() == 1)
			{
				Profile.Save();
				uConsole.Log("Cin Camera Ease " + (CinemaCamera.m_Ease ? "ENABLED" : "DISABLED"));
			}
		}

		// Token: 0x060058E2 RID: 22754
		private static void cin_end()
		{
			CinemaCamera.SaveEnd();
		}

		// Token: 0x060058E3 RID: 22755
		private static void cin_end_restore()
		{
			CinemaCamera.RestoreEnd();
		}

		// Token: 0x060058E4 RID: 22756
		private static void cin_duration()
		{
			if (uConsole.GetNumParameters() == 0)
			{
				uConsole.Log("Duration is: " + CinemaCamera.m_DurationSeconds.ToString() + "s");
				return;
			}
			if (uConsole.GetNumParameters() != 1)
			{
				uConsole.Log("Usage is cin_duration <seconds>");
			}
			CinemaCamera.m_DurationSeconds = uConsole.GetFloat();
		}

		// Token: 0x060058E5 RID: 22757
		private static void vehicle_show_polygon_shapes()
		{
			if (uConsole.GetNumParameters() == 0)
			{
				Bridge.m_DebugVisualizePolygonShapesForVehicles = !Bridge.m_DebugVisualizePolygonShapesForVehicles;
			}
			else
			{
				if (uConsole.GetNumParameters() != 1)
				{
					uConsole.Log("Usage: vehicle_show_polygon_shapes [true | false]");
					return;
				}
				Bridge.m_DebugVisualizePolygonShapesForVehicles = uConsole.GetBool();
			}
			uConsole.Log("Vehicle show polygon shapes " + (Bridge.m_DebugVisualizePolygonShapesForVehicles ? "Enabled" : "Disabled"));
		}
		
		Harmony harmony;
	}
	
}