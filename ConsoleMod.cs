using System;
using BepInEx;
using Logger = BepInEx.Logging.Logger;
using PolyTechFramework;
using UnityEngine;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using BepInEx.Configuration;
using PolyPhysics.Viewers;
using Poly.Physics;
using Poly.Math;
using PolyPhysics;
using Common.Class;
using Common.Extension;
using TMPro;
using UnityEngine.UI;
using Vectrosity;
using UnityEngine.Networking;
//using UnityEngine.UnityWebRequestModule;

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
            PluginVersion = "0.3.1";
        public static ConfigDefinition
            modEnabledDef = new ConfigDefinition("Console", "Enable/Disable Mod"),
            recenterEnabledDef = new ConfigDefinition("Console", "Enable/Disable Recenter button"),
            frameByFrameDef = new ConfigDefinition("Smooth Playback", "Frame By Frame Mode"),
            stepFrameDef = new ConfigDefinition("Smooth Playback", "Step Frame"),
            PauseOnSimStartDef = new ConfigDefinition("Smooth Playback", "Pause On Sim Start");

        public static ConfigEntry<bool>
            modEnabled,
            recenterEnabled,
            frameByFrame,
            PauseOnSimStart,
            instantTrace,
            constrainMovement,
            superZoom;
        public static ConfigEntry<BepInEx.Configuration.KeyboardShortcut>
            stepFrame;
        public static ConfigEntry<float> movementPrecision;

        public static ConsoleMod instance;

        public static bool PauseNextFrame = false;
        public static MethodInfo
            CalculateTargetPos,
            RefreshEdgeTransforms,
            IsJointAtInvalidLocation,
            AllEdgesValidLength;
        public static ConfigEntry<BepInEx.Configuration.KeyboardShortcut> consoleShortcut;
        void Awake()
        {
			if (instance == null) instance = this;
            // Use this if you wish to make the mod trigger cheat mode ingame.
            // Set this true if your mod effects physics or allows mods that you can't normally do.
            isCheat = false;
            //shouldSaveData = true;
            // Set this to whether the mod is currently enabled or not.
            // Usually you want this to be true by default.


            // Register the mod to PTF, that way it will be able to be configured using PTF ingame.

            modEnabled = Config.Bind(modEnabledDef, true, new ConfigDescription("Enable Mod"));
            modEnabled.SettingChanged += onEnableDisable;

            recenterEnabled = Config.Bind(recenterEnabledDef, true, new ConfigDescription("Enable or disable the recnter button"));
            frameByFrame = Config.Bind(frameByFrameDef, false, new ConfigDescription("Frame By Frame Mode"));
            stepFrame = Config.Bind(stepFrameDef, new BepInEx.Configuration.KeyboardShortcut(KeyCode.L), new ConfigDescription("Step Frame"));
            PauseOnSimStart = Config.Bind(PauseOnSimStartDef, false, new ConfigDescription("Pause Simulation Straight away upon simulation start"));
            instantTrace = Config.Bind("Miscellaneous", "Instant Trace Fill", false, "If enabled makes the trace tool fill instantly");
            constrainMovement = Config.Bind("Miscellaneous", "Contrain Movement", true, "Whether or not to constrain sandbox item movement (pb2 default is true)");
            superZoom = Config.Bind("Miscellaneous", "Super Zoom", false, "Infinite Camera Zoom");
            movementPrecision = Config.Bind("Miscellaneous", "Movement Precision", 0.01f, "Node Movement Precision");
            
            consoleShortcut = Config.Bind("Console", "Console Keybind", new BepInEx.Configuration.KeyboardShortcut(KeyCode.BackQuote), "Keybind to open the console");
            consoleShortcut.SettingChanged += (o, e) => {uConsole.m_Instance.m_Activate = consoleShortcut.Value.MainKey;};

            harmony = new Harmony("org.bepinex.plugins.ConsoleMod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo("Console Initiated.");
            // ConsoleCommands.Init();
            PolyTechMain.registerMod(this);
            this.isEnabled = modEnabled.Value;
        }

        //public override string getSettings(){
        //    return "aaa";
        //}
        //public override void setSettings(string settings)
        //{
        //    Logger.LogInfo("settings: " + settings);
        //}
        //public override byte[] saveData(){
        //    return new byte[] {1,2,3,4};
        //}
        //public override void loadData(byte[] bytes)
        //{
        //    Logger.LogInfo("custom save data:");
        //    foreach (byte b in bytes){
        //        Logger.LogInfo(b);
        //    }
        //}

        public bool flag = false;

        void Update(){
            //Theme.GreenScreenOn();
            //if (flag){
            //    Cameras.DisableThemePostProcessing();
            //    Theme.m_Instance.m_SunLight.gameObject.SetActive(false);
            //    Theme.m_Instance.m_BridgeLight.gameObject.SetActive(false);
            //    Theme.m_Instance.m_ThemeStub.m_BeautifyProfile.saturate = 0f;
            //}
            

        }


        [HarmonyPatch(typeof(GameManager), "StartManual")]
        public static class StartPatch {
            public static void GameStartPostfix(){
                //SandboxInputField test =  GameUI.m_Instance.m_SandboxEditCustomShape.gameObject.AddComponent<SandboxInputField>() as SandboxInputField;
                //test.name = "injected"; // doesn't work
            }
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

                uConsole.RegisterCommand("popup_test", new uConsole.DebugCommand(popup_test));
                //uConsole.RegisterCommand("set_cheat", new uConsole.DebugCommand(toggleCheat));
                
                // modded Cinematic Camera
                uConsole.RegisterCommand("cin_add", new uConsole.DebugCommand(cin_add));
                uConsole.RegisterCommand("cin_delete", new uConsole.DebugCommand(cin_delete));
                uConsole.RegisterCommand("cin_modify", new uConsole.DebugCommand(cin_modify));
                uConsole.RegisterCommand("cin_restore", new uConsole.DebugCommand(cin_restore));
                uConsole.RegisterCommand("cin_list", new uConsole.DebugCommand(cin_log));
                uConsole.RegisterCommand("cin_duration", new uConsole.DebugCommand(cin_duration));
                uConsole.RegisterCommand("cin_mode", new uConsole.DebugCommand(cin_mode));

                // custom shape modifiers
                
                uConsole.RegisterCommand("set_pos", new uConsole.DebugCommand(set_pos));
                uConsole.RegisterCommand("add_pos", new uConsole.DebugCommand(add_pos));
                uConsole.RegisterCommand("shuffle_pos", new uConsole.DebugCommand(shuffle_pos));
                
                uConsole.RegisterCommand("set_scale", new uConsole.DebugCommand(set_scale));
                uConsole.RegisterCommand("add_scale", new uConsole.DebugCommand(add_scale));
                uConsole.RegisterCommand("shuffle_scale", new uConsole.DebugCommand(shuffle_scale));

                uConsole.RegisterCommand("set_rot", new uConsole.DebugCommand(set_rot));
                uConsole.RegisterCommand("add_rot", new uConsole.DebugCommand(add_rot));
                uConsole.RegisterCommand("shuffle_rot", new uConsole.DebugCommand(shuffle_rot));

                uConsole.RegisterCommand("set_color", new uConsole.DebugCommand(set_color));

                uConsole.RegisterCommand("create_concrete_pillar", new uConsole.DebugCommand(create_support_pillar));
                uConsole.RegisterCommand("user_info", new uConsole.DebugCommand(getUserInfo));
                
                
                uConsole.RegisterCommand("bridge_hide", new uConsole.DebugCommand(bridge_hide));
                uConsole.RegisterCommand("bridge_reveal", new uConsole.DebugCommand(bridge_reveal));
                //uConsole.RegisterCommand("cam_info", new uConsole.DebugCommand(cam_info));
                //uConsole.RegisterCommand("cin_start", new uConsole.DebugCommand(cin_start));
                //uConsole.RegisterCommand("cin_start_restore", new uConsole.DebugCommand(cin_start_restore));
                //uConsole.RegisterCommand("cin_ease", new uConsole.DebugCommand(cin_ease));
                //uConsole.RegisterCommand("cin_end", new uConsole.DebugCommand(cin_end));
                //uConsole.RegisterCommand("cin_end_restore", new uConsole.DebugCommand(cin_end_restore));
                //uConsole.RegisterCommand("cin_duration", new uConsole.DebugCommand(cin_duration));
                uConsole.RegisterCommand("vehicle_show_polygon_shapes", new uConsole.DebugCommand(vehicle_show_polygon_shapes));
                instance.flag = true;
                uConsole.m_Instance.m_Activate = consoleShortcut.Value.MainKey;
                
                CalculateTargetPos = typeof(BridgeJointMovement).GetMethod("CalculateTargetPos", BindingFlags.NonPublic | BindingFlags.Static);
                RefreshEdgeTransforms = typeof(BridgeJointMovement).GetMethod("RefreshEdgeTransforms", BindingFlags.NonPublic | BindingFlags.Static);
                IsJointAtInvalidLocation = typeof(BridgeJointMovement).GetMethod("IsJointAtInvalidLocation", BindingFlags.NonPublic | BindingFlags.Static);
                AllEdgesValidLength = typeof(BridgeJointMovement).GetMethod("AllEdgesValidLength", BindingFlags.NonPublic | BindingFlags.Static);
            }
        }

        /*[HarmonyPatch(typeof(BridgeMaterials), "CreateMaterial")] // this is just a patch for getting some material data - keeping for reference
        public static class t {
            public static void Postfix(GameObject prefab){
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
	            if (gameObject == null)
	            {
	            	//instance.Logger.LogInfo("null gameObject");
                    return;
	            }
	            BridgeMaterial component = gameObject.GetComponent<BridgeMaterial>();
	            if (component == null)
	            {
	            	//instance.Logger.LogInfo("null component");
                    return;
	            }
            instance.Logger.LogInfo($"{prefab.name} {component.m_EdgeMaterial.baseMass}");
            //instance.Logger.LogInfo($"{prefab.name} max length: {component.m_MaxLength} mass per meter: {component.m_EdgeMaterial.massPerMeter} price per meter: {component.m_PricePerMeter} strength: {component.m_EdgeMaterial.strength}");
            }
        }*/

        /*[HarmonyPatch(typeof(ZedAxisVehicle), "StartSimulation")]
        public static class ZedV { // gives area of zed axis vehicle outlines - keeping for reference

            static float CalculateArea(List<Vector2> list){
                float temp = 0;
                int i = 0 ;
                for(; i < list.Count ; i++){
                if(i != list.Count - 1){
                        float mulA = list[i].x * list[i+1].y;
                        float mulB = list[i+1].x * list[i].y;
                        temp = temp + ( mulA - mulB );
                    }else{
                        float mulA = list[i].x * list[0].y;
                        float mulB = list[0].x * list[i].y;
                        temp = temp + ( mulA - mulB );
                    }
                }
                temp *= 0.5f;
                return Mathf.Abs(temp);
            }
            public static void Postfix(ZedAxisVehicle __instance, VectorLine ___m_Outline){
                List<Vector2> points = new List<Vector2>();
                foreach (Vector3 p in ___m_Outline.points3){
                    points.Add(new Vector2(p.x, p.y));
                }
                float area = CalculateArea(points);
                instance.Logger.LogInfo($"{__instance.name} {area}");
                //foreach (Vector3 p in ___m_Outline.points3){
                //    instance.Logger.LogInfo($"  {p}");
                //}
            }
        }*/

        [HarmonyPatch(typeof(GameSettings), "MinOrthographicSize")]
        public static class SuperZoomPatch {
            public static void Postfix(ref float __result){
                if (!superZoom.Value) return;
                __result = 0f;
            }
        }
        [HarmonyPatch(typeof(BridgeJointMovement), "MoveSelectedJoint")]
        public static class MovementPrecisionPatch {
            public static void Prefix(BridgeJoint moveJoint, Vector2 mouseScreenPos, Vector2 ___m_OffsetFromPointer){
                List<BridgeEdge> edgesConnectedToJoint = BridgeEdges.GetEdgesConnectedToJoint(moveJoint);
	            Vector3 vector = (Vector3)CalculateTargetPos.Invoke(null, new object[] {GameUI.SnapPosToGrid(Utils.V2toV3(Utils.GetWorldPointFromScreenPos(mouseScreenPos + ___m_OffsetFromPointer))), moveJoint, edgesConnectedToJoint}) - moveJoint.transform.position;
	            Vector3 normalized = vector.normalized;
	            moveJoint.m_MoveStartPos = moveJoint.transform.position;
	            float num = Math.Max(0.000001f, movementPrecision.Value);
	            float num2 = 0f;
	            float magnitude = vector.magnitude;
	            for (float num3 = 0f; num3 < magnitude; num3 += num)
	            {
	            	moveJoint.transform.position = GameUI.SnapPosToGrid(moveJoint.m_MoveStartPos + normalized * num3);
	            	RefreshEdgeTransforms.Invoke(null, new object[] {edgesConnectedToJoint});
	            	Budget.UpdateBridgeCost();
	            	if (!(bool)IsJointAtInvalidLocation.Invoke(null, new object[] {moveJoint, edgesConnectedToJoint}) && (bool)AllEdgesValidLength.Invoke(null, new object[] {edgesConnectedToJoint}) && Budget.CanAffordToBuild())
	            	{
	            		num2 = num3;
	            	}
	            }
	            if (Mathf.Approximately(num2, 0f))
	            {
	            	moveJoint.transform.position = moveJoint.m_MoveStartPos;
	            	RefreshEdgeTransforms.Invoke(null, new object[] {edgesConnectedToJoint});
	            	Budget.UpdateBridgeCost();
	            	return;
	            }
	            moveJoint.transform.position = GameUI.SnapPosToGrid(moveJoint.m_MoveStartPos + normalized * num2);
	            RefreshEdgeTransforms.Invoke(null, new object[] {edgesConnectedToJoint});
	            Budget.UpdateBridgeCost();
	            foreach (BridgeEdge bridgeEdge in edgesConnectedToJoint)
	            {
	            	bridgeEdge.UpdateJointSelectors();
	            	bridgeEdge.ResolveJointSelectorOverlap();
	            }
	            GameStateBuild.ClearFirstBreakAttachedToJoint(moveJoint.m_Guid);
            }
        }


        [HarmonyPatch(typeof(uConsoleGUI))]
        [HarmonyPatch("Update")]
        public class PatchConsole
        {
             static void Prefix ()
            {
                if (uConsole.IsOn() && !modEnabled.Value)
                {
                    uConsole.TurnOff();
                }
            }
        }

        // Use this method to execute code that will be ran when the mod is enabled.
        public override void enableMod() 
        {
            modEnabled.Value = true;
        }
        // Use this method to execute code that will be ran when the mod is disabled.
        public override void disableMod() 
        {
            modEnabled.Value = false;
        }

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

        private static void toggleCheat(){
            bool val = uConsole.GetBool();
            PolyTechMain.setCheat(instance, val);
            PopUpMessage.DisplayOkOnly("Set Console mod cheat? to " + val.ToString(), null);
        }

        public static void getUserInfo(){
            string id;
            if (uConsole.GetNumParameters() == 1) id = uConsole.GetString();
            else {
                id = Workshop.GetLocalPlayerId();
            }
            Command.commandDefinitions[Command.Action.UserRead] = new Command.CommandDefinition(
			    "user/{0}/read", 
                Command.CommandDefinition.Verb.Get,
                typeof(ResponseData.User)
            );
            Command command = new Command(Command.Action.UserRead, id);
            string uri = DCServices.DCManager.apiAuthedPrefix + command.GetAPIUrl();
            UnityWebRequest request = UnityWebRequest.Get(uri);
            request.SetRequestHeader("Authorization", "Bearer " + DCServices.DCManager.m_Instance.m_CachedToken);
	        request.timeout = 30;
	        request.SendWebRequest().completed += processUserInfoRequest;
            
        }
        public static void processUserInfoRequest(AsyncOperation asyncOperation){
            UnityWebRequestAsyncOperation unityWebRequestAsyncOperation = (UnityWebRequestAsyncOperation)asyncOperation;
			ResponseData.User responseData = new ResponseData.User();
			if (unityWebRequestAsyncOperation.webRequest.isNetworkError || unityWebRequestAsyncOperation.webRequest.isHttpError)
			{
				if (unityWebRequestAsyncOperation.webRequest.isNetworkError)
				{
					Debug.LogWarningFormat("FAILURE CODE: Network Error REASON: {0}", new object[]
					{
						unityWebRequestAsyncOperation.webRequest.error
					});
				}
				if (unityWebRequestAsyncOperation.webRequest.isHttpError)
				{
					Debug.LogWarningFormat("FAILURE CODE: {0} REASON: {1}", new object[]
					{
						unityWebRequestAsyncOperation.webRequest.responseCode,
						(unityWebRequestAsyncOperation.webRequest.downloadHandler != null && !string.IsNullOrEmpty(unityWebRequestAsyncOperation.webRequest.downloadHandler.text)) ? unityWebRequestAsyncOperation.webRequest.downloadHandler.text : "UNKNOWN"
					});
				}
				getUserInfoFailed(null, unityWebRequestAsyncOperation.webRequest.responseCode);
				return;
			}
			Debug.LogFormat("Repsonse Code: {0}", new object[]
			{
				unityWebRequestAsyncOperation.webRequest.responseCode
			});
			bool flag = false;
			if (unityWebRequestAsyncOperation.webRequest.downloadHandler != null && unityWebRequestAsyncOperation.webRequest.downloadHandler.text != null)
			{
				JSONObject jSONObject = new JSONObject(unityWebRequestAsyncOperation.webRequest.downloadHandler.text, -2, false, false);
				try
				{
					responseData.Populate(jSONObject);
				}
				catch (Exception ex)
				{
					Debug.LogWarningFormat("Exception parsing reponse data: {0}", new object[]
					{
						ex.Message.ToString()
					});
					flag = true;
				    getUserInfoFailed(null, unityWebRequestAsyncOperation.webRequest.responseCode);
				}
			}
			if (!flag)
			{
				getUserInfoComplete(responseData, unityWebRequestAsyncOperation.webRequest.responseCode);
			}
        }

        public static void getUserInfoComplete(ResponseData responseData, long responseCode){
            ResponseData.User data = (ResponseData.User)responseData;
            uConsole.Log($"Display Name: {data.displayName}");
            uConsole.Log($"ID: {data.id}");
            uConsole.Log($"Followers: {data.followers}");
            uConsole.Log($"Platform: {data.platform}");
            uConsole.Log($"Is banned: {data.isBanned}");
        }
        public static void getUserInfoFailed(ResponseData responseData, long responseCode){
            uConsole.Log($"Failed, response code: {responseCode}");
        }


        public static void UpdateSandboxPositionUI(SandboxItem item)
	    {
	    	switch (item.m_Type)
	    	{
	    	case SandboxItemType.TERRAIN:
	    		if (GameUI.m_Instance.m_SandboxEditTerrain.gameObject.activeInHierarchy)
	    		{
	    			TerrainIsland component = item.GetComponent<TerrainIsland>();
	    			GameUI.m_Instance.m_SandboxEditTerrain.RefreshPosition(component);
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.ANCHOR:
	    		if (GameUI.m_Instance.m_SandboxEditAnchor.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditAnchor.RefreshPosition(item.GetComponent<BridgeJoint>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.VEHICLE:
	    		if (GameUI.m_Instance.m_SandboxEditVehicle.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditVehicle.RefreshPosition(item.GetComponent<Vehicle>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.VEHICLE_STOP_TRIGGER:
	    		if (GameUI.m_Instance.m_SandboxEditVehicleStopTrigger.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditVehicleStopTrigger.RefreshPosition(item.GetComponent<VehicleStopTrigger>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.WATER:
	    	case SandboxItemType.HYDRAULICS_PHASE:
	    	case SandboxItemType.VEHICLE_RESTART_PHASE:
	    		break;
	    	case SandboxItemType.CHECKPOINT:
	    		if (GameUI.m_Instance.m_SandboxEditCheckpoint.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditCheckpoint.RefreshPosition(item.GetComponent<Checkpoint>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.PLATFORM:
	    		if (GameUI.m_Instance.m_SandboxEditPlatform.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditPlatform.RefreshPosition(item.GetComponent<Platform>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.RAMP:
	    		if (GameUI.m_Instance.m_SandboxEditRamp.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditRamp.RefreshPosition(item.GetComponent<Ramp>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.FLYING_OBJECT:
	    		if (GameUI.m_Instance.m_SandboxEditFlyingObject.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditFlyingObject.RefreshPosition(item.GetComponent<FlyingObject>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.ROCK:
	    		if (GameUI.m_Instance.m_SandboxEditRock.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditRock.RefreshPosition(item.GetComponent<Rock>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.ZED_AXIS_VEHICLE:
	    		if (GameUI.m_Instance.m_SandboxEditZedAxisVehicle.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditZedAxisVehicle.RefreshPosition(item.GetComponent<ZedAxisVehicle>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.CUSTOM_SHAPE:
	    		if (GameUI.m_Instance.m_SandboxEditCustomShape.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditCustomShape.RefreshPosition(item.GetComponent<CustomShape>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.SUPPORT_PILLAR:
	    		if (GameUI.m_Instance.m_SandboxEditSupportPillar.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditSupportPillar.RefreshPosition(item.GetComponent<SupportPillar>());
	    			return;
	    		}
	    		break;
	    	case SandboxItemType.PILLAR:
	    		if (GameUI.m_Instance.m_SandboxEditPillar.gameObject.activeInHierarchy)
	    		{
	    			GameUI.m_Instance.m_SandboxEditPillar.RefreshPosition(item.GetComponent<Pillar>());
	    			return;
	    		}
	    		break;
	    	default:
	    		Debug.LogWarningFormat("UpdateSandboxPositionUI called with unsupported SandboxItemType {0}", new object[]
	    		{
	    			item.m_Type
	    		});
	    		break;
	    	}
            if (item){
                item.SetOffsetFromPointer(Input.mousePosition);
            }
	    }

        public enum SandboxCommandType {
            SET,
            ADD,
            SHUFFLE
        }
        private static void processSandboxCommand(string commandName, SandboxCommandType commandType){
            float x = -1e16f, y = -1e16f, z = -1e16f;
            string axis;
            if (commandType == SandboxCommandType.SHUFFLE){
                float start, end;
                if (uConsole.GetNumParameters() == 0 || uConsole.GetNumParameters() == 2){
                    uConsole.Log($"Usage:\n{commandName} <axis> <value>");
                    return;
                }
                start = uConsole.GetFloat();
                end = uConsole.GetFloat();
                axis = uConsole.GetString().ToLower();

                if (axis == "x") x = UnityEngine.Random.Range(Math.Min(start, end), Math.Max(start, end));
                if (axis == "y") y = UnityEngine.Random.Range(Math.Min(start, end), Math.Max(start, end));
                if (axis == "z") z = UnityEngine.Random.Range(Math.Min(start, end), Math.Max(start, end));

                }
            else {
                if (
                    (!uConsole.NextParameterIsFloat() && uConsole.GetNumParameters() < 2) ||
                    (uConsole.NextParameterIsFloat() && uConsole.GetNumParameters() < 3)
                ){
                    uConsole.Log($"Usage:\n{commandName} <axis> <value>\n{commandName} <x> <y> <z>");
                    return;
                }
                if (!uConsole.NextParameterIsFloat()){
                    axis = uConsole.GetString().ToLower();
                    if (axis == "x"){
                        x = uConsole.GetFloat();
                    }
                    if (axis == "y"){
                        y = uConsole.GetFloat();
                    }
                    if (axis == "z"){
                        z = uConsole.GetFloat();
                    }
                }
                else {
                    x = uConsole.GetFloat();
                    y = uConsole.GetFloat();
                    z = uConsole.GetFloat();
                }
            }

            Vector3 v = new Vector3(x,y,z);
            if (commandName.EndsWith("pos")) changePos(v, commandType);
            if (commandName.EndsWith("scale")) changeScale(v, commandType);
            if (commandName.EndsWith("rot")) changeRot(v, commandType);

        }

        private static void changePos(Vector3 pos, SandboxCommandType commandType){
            
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                SandboxItem sandboxItem = SandboxSelectionSet.m_Items[i];
                Vector3 new_pos;
                string orig_pos = sandboxItem.transform.position.ToString();
                if (commandType == SandboxCommandType.ADD){
                    new_pos = new Vector3(
                        pos.x == -1e16f ? sandboxItem.transform.position.x : sandboxItem.transform.position.x + pos.x, 
                        pos.y == -1e16f ? sandboxItem.transform.position.y : sandboxItem.transform.position.y + pos.y, 
                        pos.z == -1e16f ? sandboxItem.transform.position.z : sandboxItem.transform.position.z + pos.z
                    );
                }
                else {
                    new_pos = new Vector3(
                        pos.x == -1e16f ? sandboxItem.transform.position.x : pos.x, 
                        pos.y == -1e16f ? sandboxItem.transform.position.y : pos.y, 
                        pos.z == -1e16f ? sandboxItem.transform.position.z : pos.z
                    );
                }
                
                sandboxItem.transform.position = new_pos;
                uConsole.Log(orig_pos + " -> " + sandboxItem.gameObject.transform.position.ToString());
                
                if (sandboxItem.m_Type == SandboxItemType.ANCHOR || sandboxItem.m_Type == SandboxItemType.CUSTOM_SHAPE)
			    {
			    	BridgeEdges.UpdateTransforms();
			    }
			    if (sandboxItem.m_Type == SandboxItemType.TERRAIN && sandboxItem.GetComponent<TerrainIsland>().m_TerrainIslandType == TerrainIslandType.Bookend)
			    {
			    	WorldBounds.Calculate(GameSettings.WorldWidth(), GameSettings.WorldMinY(), GameSettings.WorldMaxY());
			    }
                UpdateSandboxPositionUI(sandboxItem);

            }
        }

        private static void changeScale(Vector3 scale, SandboxCommandType commandType){
            
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                SandboxItem sandboxItem = SandboxSelectionSet.m_Items[i];

                
                if (sandboxItem.m_Type == SandboxItemType.CUSTOM_SHAPE ||
                    sandboxItem.m_Type == SandboxItemType.ROCK || 
                    sandboxItem.m_Type == SandboxItemType.FLYING_OBJECT ||
                    sandboxItem.m_Type == SandboxItemType.SUPPORT_PILLAR
                ){
                    Vector3 new_scale;
                    string orig_scale = sandboxItem.transform.localScale.ToString();
                    if (commandType == SandboxCommandType.ADD){
                        new_scale = new Vector3(
                            scale.x == -1e16f ? sandboxItem.transform.localScale.x : sandboxItem.transform.localScale.x + scale.x, 
                            scale.y == -1e16f ? sandboxItem.transform.localScale.y : sandboxItem.transform.localScale.y + scale.y, 
                            scale.z == -1e16f ? sandboxItem.transform.localScale.z : sandboxItem.transform.localScale.z + scale.z
                        );
                    }
                    else {
                        new_scale = new Vector3(
                            scale.x == -1e16f ? sandboxItem.transform.localScale.x : scale.x, 
                            scale.y == -1e16f ? sandboxItem.transform.localScale.y : scale.y, 
                            scale.z == -1e16f ? sandboxItem.transform.localScale.z : scale.z
                        );
                    }

                    sandboxItem.transform.localScale = new_scale;
                    uConsole.Log(orig_scale + " -> " + sandboxItem.gameObject.transform.localScale.ToString());

                    if (sandboxItem.m_Type == SandboxItemType.ANCHOR || sandboxItem.m_Type == SandboxItemType.CUSTOM_SHAPE)
			        {
			        	BridgeEdges.UpdateTransforms();
			        }
			        if (sandboxItem.m_Type == SandboxItemType.TERRAIN && sandboxItem.GetComponent<TerrainIsland>().m_TerrainIslandType == TerrainIslandType.Bookend)
			        {
			        	WorldBounds.Calculate(GameSettings.WorldWidth(), GameSettings.WorldMinY(), GameSettings.WorldMaxY());
			        }
                    UpdateSandboxPositionUI(sandboxItem);
                }
            }
        }

        private static void changeRot(Vector3 rot, SandboxCommandType commandType){
            float x = rot.x;
            float y = rot.y;
            float z = rot.z;
            
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                SandboxItem sandboxItem = SandboxSelectionSet.m_Items[i];
                
                if (sandboxItem.m_Type == SandboxItemType.CUSTOM_SHAPE || sandboxItem.m_Type == SandboxItemType.VEHICLE){
                    string orig_rot = sandboxItem.transform.rotation.eulerAngles.ToString();
                    Quaternion new_rot;
                    if (commandType == SandboxCommandType.ADD){
                        new_rot = Quaternion.Euler(
                            x == -1e16f ? sandboxItem.transform.rotation.eulerAngles.x : sandboxItem.transform.rotation.eulerAngles.x + x, 
                            y == -1e16f ? sandboxItem.transform.rotation.eulerAngles.y : sandboxItem.transform.rotation.eulerAngles.y + y, 
                            z == -1e16f ? sandboxItem.transform.rotation.eulerAngles.z : sandboxItem.transform.rotation.eulerAngles.z + z
                        );
                    }
                    else {
                        new_rot = Quaternion.Euler(
                            x == -1e16f ? sandboxItem.transform.rotation.eulerAngles.x : x, 
                            y == -1e16f ? sandboxItem.transform.rotation.eulerAngles.y : y, 
                            z == -1e16f ? sandboxItem.transform.rotation.eulerAngles.z : z
                        );
                    }
                    sandboxItem.transform.rotation = new_rot;
                    uConsole.Log(orig_rot + " -> " + sandboxItem.gameObject.transform.rotation.eulerAngles.ToString());
                }
                if (sandboxItem.m_Type == SandboxItemType.ANCHOR || sandboxItem.m_Type == SandboxItemType.CUSTOM_SHAPE)
			    {
			    	BridgeEdges.UpdateTransforms();
			    }
			    if (sandboxItem.m_Type == SandboxItemType.TERRAIN && sandboxItem.GetComponent<TerrainIsland>().m_TerrainIslandType == TerrainIslandType.Bookend)
			    {
			    	WorldBounds.Calculate(GameSettings.WorldWidth(), GameSettings.WorldMinY(), GameSettings.WorldMaxY());
			    }
                UpdateSandboxPositionUI(sandboxItem);

            }
        }

        public static void changeColor(Color color, SandboxCommandType commandType){
            
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                CustomShape sandboxItem = SandboxSelectionSet.m_Items[i].GetComponent<CustomShape>();
                
                if (sandboxItem){
                    string orig_color = sandboxItem.transform.rotation.ToString();
                    Color new_color;
                    if (commandType == SandboxCommandType.ADD){
                        new_color = new Color(
                            color.r == -1e16f ? sandboxItem.m_Color.r : sandboxItem.m_Color.r + color.r, 
                            color.g == -1e16f ? sandboxItem.m_Color.g : sandboxItem.m_Color.g + color.g, 
                            color.b == -1e16f ? sandboxItem.m_Color.b : sandboxItem.m_Color.b + color.b
                        );
                    }
                    else {
                        new_color = new Color(
                            color.r == -1e16f ? sandboxItem.m_Color.r : color.r, 
                            color.g == -1e16f ? sandboxItem.m_Color.g : color.g, 
                            color.b == -1e16f ? sandboxItem.m_Color.b : color.b
                        );
                    }
                    sandboxItem.SetColor(new_color);
			        sandboxItem.m_OriginalColor = new_color;
                    uConsole.Log(orig_color + " -> " + new_color.ToString());
                }
            }
        }
          
        
        
        private static void set_pos(){
            // Usage:
            // set_pos <axis> <value>
            // set_pos <x> <y> <z>
            processSandboxCommand("set_pos", SandboxCommandType.SET);
        }

        private static void add_pos(){
            // Usage:
            // add_pos <axis> <value>
            // add_pos <x> <y> <z>
            processSandboxCommand("add_pos", SandboxCommandType.ADD);
        }

        private static void shuffle_pos(){
            // Usage:
            // shuffle_pos <axis> <start> <end>
            processSandboxCommand("shuffle_pos", SandboxCommandType.SHUFFLE);
        }

        private static void set_scale(){
            // Usage:
            // set_scale <axis> <value>
            // set_scale <x> <y> <z>
            processSandboxCommand("set_scale", SandboxCommandType.SET);
            
        }

        private static void add_scale(){
            // Usage:
            // add_scale <axis> <value>
            // add_scale <x> <y> <z>
            processSandboxCommand("add_scale", SandboxCommandType.ADD);
        }

        private static void shuffle_scale(){
            // Usage:
            // shuffle_scale <axis> <start> <end>
            processSandboxCommand("shuffle_scale", SandboxCommandType.SHUFFLE);
            
        }

        private static void set_rot(){
            // Usage:
            // set_rot <axis> <value>
            // set_rot <x> <y> <z>
            processSandboxCommand("set_rot", SandboxCommandType.SET);
        }

        private static void add_rot(){
            // Usage:
            // add_rot <axis> <value>
            // add_rot <x> <y> <z>
            processSandboxCommand("add_rot", SandboxCommandType.ADD);
        }

        private static void shuffle_rot(){
            // Usage:
            // shuffle_rot <axis> <value>
            // shuffle_rot <x> <y> <z>
            processSandboxCommand("shuffle_rot", SandboxCommandType.SHUFFLE);
        }

        private static void set_color(){
            // Usage:
            // set_color <hex_color>
            if (uConsole.GetNumParameters() != 1){
                uConsole.Log("Usage: set_color <hex_color>");
                return;
            }
            string hex_color = uConsole.GetString();
            Color color;
            if (!ColorUtility.TryParseHtmlString(hex_color, out color)){
                uConsole.Log("Usage: set_color <hex_color>");
                return;
            }
            changeColor(color, SandboxCommandType.SET);
        }

        private static void create_support_pillar(){
            string prefabName = "ConcretePillar_Prefab";
            if (GameStateManager.GetState() != GameState.SANDBOX) return;
            Vector3 pos = Cameras.MainCamera().ScreenToWorldPoint(Input.mousePosition);
            SupportPillar pillar = SupportPillars.CreateSupportPillar(Prefabs.m_PrefabsDict[prefabName], pos, new Quaternion(0,0,0,1));
            UpdateSandboxPositionUI(pillar.m_SandboxItem);
            uConsole.Log("Created concrete pillar at mouse position");
        }

        [HarmonyPatch(typeof(SandboxSelectionSet), "ConstrainTargetPos")]
        private static class ConstrainTargetPosPatch {
            private static bool Prefix(SandboxItem item, Vector3 currentPos, Vector3 targetPos, ref Vector3 __result){
                if (constrainMovement.Value) return true;
                targetPos.z = 0;
                __result = targetPos;
                return false;
            }
        }

        
        private static void popup_test() 
        {
            GameUI.ShowMessage(ScreenMessageLocation.TOP_LEFT, "hey", 5f);
            PopUpMessage.DisplayOkOnly("ok only", null);
            PopUpMessage.Display("ok only", null);
            PopUpMessage.Display("ok and cancel", null, () => {});
            PopUpWarning.Display("PopUpWarning");
            PopUpTwoChoices.Display(
                "PopUpTwoChoices",
                "aaa",
                "bbb",
                () => 
            {
                return;
            },
            () => 
            {
                return;
            });
            PopupInputField.Display(
                "PopUpInputField",
                "default",
                (result) => 
                {
                    uConsole.Log("You typed: "+ result);
                }
            );
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



        [HarmonyPatch(typeof(BridgeJoints), "ApplySimulationResults")]
        public static class JointRenderPath {
            public static bool Prefix(){
                if (frameByFrame.Value && modEnabled.Value){
                    foreach (BridgeJoint bridgeJoint in BridgeJoints.m_Joints)
		            {
		            	if (bridgeJoint.gameObject.activeInHierarchy && bridgeJoint.m_PhysicsNode && bridgeJoint.m_PhysicsNode.handle && bridgeJoint.m_PhysicsNode.handle.world)
		            	{
		            		bridgeJoint.transform.position = bridgeJoint.m_PhysicsNode.pos;
		            	}
		            }
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Vehicle), "SyncVisual")]
        public static class VehicleRenderPath {
            public static bool Prefix(
                ref Vehicle __instance, 
                ref PolyPhysics.Vehicle ___m_Physics, 
                ref VehicleSyncTarget[] ___m_SyncTargets
            ){
                if (frameByFrame.Value && modEnabled.Value){
                    if (___m_Physics && ___m_SyncTargets != null && Bridge.IsSimulating())
		            {
		            	foreach (VehicleSyncTarget vehicleSyncTarget in ___m_SyncTargets)
		            	{
		            		if (vehicleSyncTarget && (vehicleSyncTarget.m_type == VehicleSyncTarget.Type.VisualMesh || vehicleSyncTarget.m_type == VehicleSyncTarget.Type.Invalid))
		            		{
		            			vehicleSyncTarget.Sync(false);
		            		}
		            	}
		            }
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(BridgeEdge), "UpdateManual")]
        public static class BridgeEdgeRenderPatch {
            public static bool Prefix(ref BridgeEdge __instance, ref bool ___m_ForceDisabled){
                if (frameByFrame.Value && modEnabled.Value){
                    Vector3 position = __instance.m_JointA.transform.position;
		            Vector3 position2 = __instance.m_JointB.transform.position;
		            Vector3 toDirection = position2 - position;
		            float magnitude = toDirection.magnitude;
		            if (__instance.m_PhysicsEdge && __instance.m_PhysicsEdge.handle && __instance.m_PhysicsEdge.node0 && __instance.m_PhysicsEdge.node0.handle && __instance.m_PhysicsEdge.node0.handle.world && __instance.m_PhysicsEdge.node1 && __instance.m_PhysicsEdge.node1.handle && __instance.m_PhysicsEdge.node1.handle.world)
		            {
		            	WorldObjectImpl handle = __instance.m_PhysicsEdge.handle;
		            	//float currentFractionOfFixedFrame = SingletonMonoBehaviour<World>.instance.currentFractionOfFixedFrame;
		            	Vec2 smoothPos = __instance.m_PhysicsEdge.node0.pos; //  Modifed from SmoothPos to pos
		            	Vec2 smoothPos2 = __instance.m_PhysicsEdge.node1.pos; // Modifed from SmoothPos to pos
		            	__instance.transform.position = 0.5f * (smoothPos + smoothPos2);
		            	if (__instance.m_PhysicsEdge.areNodesReversedInPhysics)
		            	{
		            		Values.Swap<Vec2>(ref smoothPos, ref smoothPos2);
		            	}
		            	Vec2 vec = smoothPos2 - smoothPos;
		            	float z = Mathf.Atan2(vec.y, vec.x) * 57.29578f;
		            	__instance.transform.rotation = Quaternion.Euler(0f, 0f, z);
		            	float magnitude2 = vec.magnitude;
		            	__instance.m_MeshRenderer.transform.SetLocalScaleX(magnitude2);
		            	__instance.m_HighlightFX.transform.SetLocalScaleX(magnitude2);
		            	if (!handle.isEnabled && !___m_ForceDisabled)
		            	{
		            		if ((__instance.m_Material.m_MaterialType == BridgeMaterialType.ROPE || __instance.m_Material.m_MaterialType == BridgeMaterialType.CABLE) && BridgeRopes.m_BridgeRopes.Count > 0)
		            		{
		            			BridgeRopes.DisableRopeForEdge(__instance);
		            		}
		            		__instance.ForceDisable();
		            	}
		            }
		            else if (!float.IsInfinity(magnitude))
		            {
		            	__instance.transform.position = 0.5f * (position + position2);
		            	__instance.transform.rotation = Quaternion.FromToRotation(Vector3.right, toDirection);
		            	__instance.m_MeshRenderer.transform.SetLocalScaleX(magnitude);
		            	__instance.m_HighlightFX.transform.SetLocalScaleX(magnitude);
		            }
		            if (__instance.m_SpringCoilVisualization != null)
		            {
		            	__instance.m_SpringCoilVisualization.UpdateLinks();
		            }
		            if (__instance.m_HydraulicEdgeVisualization != null)
		            {
		            	__instance.m_HydraulicEdgeVisualization.UpdateTransform_Manual(__instance);
		            }
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(GameStateSim), "Enter")]
        public static class StartSimPatch {
            public static void Postfix(GameState prevState){
                if (modEnabled.Value && PauseOnSimStart.Value){
                    SingletonMonoBehaviour<World>.instance.fastForwardToFrame = SingletonMonoBehaviour<World>.instance.frameCount + 1;
                    PauseNextFrame = true;
                }
            }
        }


        [HarmonyPatch(typeof(GameStateSim), "UpdateManual")]
        public static class StepFramePatch {
            public static void Postfix(){
                if (modEnabled.Value){
                    if (stepFrame.Value.IsUp()){
                        //GameUI.m_Instance.m_TopBar.OnPauseSim();
                        //Time.timeScale = 0f;
                        GameUI.m_Instance.m_TopBar.OnUnPauseSim();
                        SingletonMonoBehaviour<World>.instance.fastForwardToFrame = SingletonMonoBehaviour<World>.instance.frameCount + 1;
                        //GameUI.m_Instance.m_TopBar.OnPauseSim();
                        //GameUI.m_Instance.m_TopBar.OnUnPauseSim();
                        PauseNextFrame = true;
                    }
                }
            }
        }
        [HarmonyPatch(typeof(PolyPhysics.World), "FixedUpdate_Manual")]
        public static class PausePatch {
            public static void Postfix(ref PolyPhysics.World __instance){
                if (modEnabled.Value && Bridge.IsSimulating()){
                    if (PauseNextFrame && __instance.frameCount -1 == __instance.fastForwardToFrame){
                        GameUI.m_Instance.m_TopBar.OnPauseSim();
                        PauseNextFrame = false;
                    }
                }
            }
        }







        [HarmonyPatch(typeof(GameStateManager), "UpdateManual")]
        public static class RecenterPatch {
            public static void Postfix(){
                if ((
                    GameStateManager.GetState() == GameState.SANDBOX || 
                    GameStateManager.GetState() == GameState.BUILD || 
                    GameStateManager.GetState() == GameState.SIM
                    ) && ConsoleMod.modEnabled.Value && !ConsoleMod.recenterEnabled.Value
                    )
		        {
			        GameUI.m_Instance.m_Recenter.gameObject.SetActive(false);
		        }
            }
        }

        public class CameraKeyFrame {
            public Vector3 m_StartPos;
		    public Quaternion m_StartRot;
		    public Vector3 m_StartPivot;
		    public float m_StartOrthographicSize;
            public float m_DurationSeconds = 5;
            public bool m_Ease = false;

        }
        public enum CinemaCameraModes {
            ONE_DURATION,
            MULTI_DURATION
        }

        public static class ModdedCinemaCamera {
            public static List<CameraKeyFrame> keyFrames = new List<CameraKeyFrame> ();
            public static int current_start = 0;
            public static int current_end = 1;

            public static CinemaCameraModes mode = CinemaCameraModes.ONE_DURATION;
            public static float duration = 5;

            public static CatmullRomSpline InterpolateHandler = new CatmullRomSpline();

            public static CatmullRomSpline PivotHandler = new CatmullRomSpline();
            public static CameraKeyFrame currentCamera(){
                CameraKeyFrame cam = new CameraKeyFrame();
                cam.m_StartPos = Cameras.MainCamera().transform.position;
		        cam.m_StartRot = Cameras.MainCamera().transform.rotation;
		        cam.m_StartPivot = PointsOfView.m_Pivot;
		        cam.m_StartOrthographicSize = Cameras.GetOrthographicSize();
                return cam;
            }

            public static void StartInterpolate(){
                if (mode == CinemaCameraModes.ONE_DURATION){
                    computeFromOneDuration();
                }
                restore(current_start);
                CameraKeyFrame target = keyFrames[current_end];
			    CameraInterpolate.SlerpTo(
                    target.m_StartPivot, 
                    target.m_StartPos, 
                    target.m_StartRot, 
                    target.m_StartOrthographicSize, 
                    target.m_DurationSeconds, 
                    target.m_Ease
                );
            }
            public static void restore(int pos){
                CameraKeyFrame cam = keyFrames[pos];
                Cameras.MainCamera().transform.position = cam.m_StartPos;
		        Cameras.MainCamera().transform.rotation = cam.m_StartRot;
		        PointsOfView.UpdatePivotBasedOnCamera();
		        Cameras.SetOrthographicSize(cam.m_StartOrthographicSize);
            }
            public static float distTo(int pos){
                float dist = 0;
                Vector3 current;
                Vector3 prev = new Vector3();
                for (var i = 0; i < 50; i++){
                    if (i != 0){
                        current = InterpolateHandler.Interpolate(pos, i/50);
                        dist += Vector3.Distance(current, prev);
                        prev = current;
                    }
                    else {
                        prev = keyFrames[pos-1].m_StartPos;
                    }
                }
                return dist;
            }

            public static void computeFromOneDuration(){
                float total_dist = 0;
                float small_dist = 0;
                for (var i = 1; i < keyFrames.Count; i++){
                    total_dist += distTo(i);
                }
                for (var i = 1; i < keyFrames.Count; i++){
                    CameraKeyFrame endFrame = keyFrames[i];
                    small_dist = distTo(i);
                    //Debug.Log($"{i} {small_dist} {total_dist} {(small_dist/total_dist)*duration}");
                    keyFrames[i].m_DurationSeconds = (small_dist/total_dist)*duration;
                }
            }
        }

        [HarmonyPatch(typeof(CinemaCamera), "StartInterpolate")]
        public static class StartInterpolatePath {
            public static bool Prefix(){
                ModdedCinemaCamera.StartInterpolate();
                return false;
            }
        }
        
        [HarmonyPatch(typeof(CameraInterpolate), "UpdateSlerp")]
        public static class UpdateSlerpPatch {
            public static bool Prefix(
                ref Vector3 ___m_StartPos,
                ref Vector3 ___m_StartPivot,
                ref float ___m_StartOrthographicSize,
                ref Vector3 ___m_EndPos,
                ref Vector3 ___m_EndPivot,
                ref float ___m_EndOrthographicSize,
                ref Quaternion ___m_EndRot,

                ref float ___m_ElapsedSeconds, 
                ref float ___m_TransitionSeconds, 
                ref bool ___m_Slerping
            ){
                if (CinemaCamera.Activated()){
                    if (ModdedCinemaCamera.keyFrames.Count == 0){
                        CameraInterpolate.Cancel();
                        return false;
                    }
                    else if (ModdedCinemaCamera.keyFrames.Count < 2){
                        PopUpWarning.Display("You must add at least 2 keyframes to use the cinematic camera!");
                        CameraInterpolate.Cancel();
                        return false;
                    }
                    ___m_ElapsedSeconds += Time.unscaledDeltaTime;
		            float num = Mathf.Clamp01(___m_ElapsedSeconds / ___m_TransitionSeconds);
                    

                    Vector3 vector = ModdedCinemaCamera.PivotHandler.Interpolate(ModdedCinemaCamera.current_start, num);
                    Vector3 normalized3 = ModdedCinemaCamera.InterpolateHandler.Interpolate(ModdedCinemaCamera.current_start, num).normalized;
                    // testing smooth movement
                    PointsOfView.m_Pivot = vector;
                    
                    Cameras.MainCamera().transform.position = vector + normalized3 * GameSettings.CamDistFromPivot();
		            Cameras.MainCamera().transform.LookAt(vector);
		            Cameras.SetOrthographicSize(Mathf.SmoothStep(___m_StartOrthographicSize, ___m_EndOrthographicSize, num));
		            Bridge.RefreshZoomDependentVisibility();
		            if (Mathf.Approximately(num, 1f))
		            {
                        ModdedCinemaCamera.current_start += 1;
                        ModdedCinemaCamera.current_end += 1;
                        if (ModdedCinemaCamera.current_end >= ModdedCinemaCamera.keyFrames.Count){
                            CameraInterpolate.Cancel();
                        }
                        else {
                            CameraKeyFrame target = ModdedCinemaCamera.keyFrames[ModdedCinemaCamera.current_end];
			                CameraInterpolate.SlerpTo(
                                target.m_StartPivot, 
                                target.m_StartPos, 
                                target.m_StartRot, 
                                target.m_StartOrthographicSize, 
                                target.m_DurationSeconds, 
                                target.m_Ease
                            );
                        }
		            }
		            CameraControl.RegisterTransformUpdate();

                    return false;
                }
                return true;
            }
        }

        

        [HarmonyPatch(typeof(CameraInterpolate), "Cancel")]
        public static class CameraInterpolateCancel {
            public static void Postfix(){
                ModdedCinemaCamera.current_start = 0;
                ModdedCinemaCamera.current_end = 1;
            }
        }

        
        
        
        private static void cin_add(){
            CinemaCamera.SaveStart(null); // we do this...
            CinemaCamera.SaveEnd(); //   to trick the main Cinema Camera into thinking it's initiated
            // usage: cin_add <float m_DurationSeconds> <bool m_Ease> <int index>
            float m_DurationSeconds = 5;
            bool m_Ease = false;
            int index = -1;
            if (uConsole.NextParameterIsFloat()) m_DurationSeconds = uConsole.GetFloat();
            if (uConsole.NextParameterIsBool()) m_Ease = uConsole.GetBool();
            if (uConsole.GetNumParameters() == 3 && uConsole.NextParameterIsInt()){
                index = Math.Max(0, uConsole.GetInt());
            }

            CameraKeyFrame cam = ModdedCinemaCamera.currentCamera();
            cam.m_DurationSeconds = m_DurationSeconds;
            cam.m_Ease = m_Ease;
            if (index != -1){
                ModdedCinemaCamera.keyFrames.Insert(index, cam);
                ModdedCinemaCamera.InterpolateHandler.controlPointsList.Insert(index, cam.m_StartPos.normalized);
                ModdedCinemaCamera.PivotHandler.controlPointsList.Insert(index, cam.m_StartPivot);
                uConsole.Log($"Inserted keyframe into position {index} of list.");
            }
            else {
                ModdedCinemaCamera.keyFrames.Add(cam);
                ModdedCinemaCamera.InterpolateHandler.controlPointsList.Add(cam.m_StartPos.normalized);
                ModdedCinemaCamera.PivotHandler.controlPointsList.Add(cam.m_StartPivot);
                uConsole.Log("Added keyframe to end of list.");
            }
            
        }
        private static void cin_delete(){
            if (uConsole.GetNumParameters() == 0){
                ModdedCinemaCamera.keyFrames.Clear();
                ModdedCinemaCamera.InterpolateHandler.controlPointsList.Clear();
                ModdedCinemaCamera.PivotHandler.controlPointsList.Clear();
                uConsole.Log("Cleared keyframes.");
            }
            else {
                int index = Mathf.Clamp(uConsole.GetInt(), 0, ModdedCinemaCamera.keyFrames.Count - 1);
                ModdedCinemaCamera.keyFrames.RemoveAt(index);
                ModdedCinemaCamera.InterpolateHandler.controlPointsList.RemoveAt(index);
                ModdedCinemaCamera.PivotHandler.controlPointsList.RemoveAt(index);
                uConsole.Log($"Deleted keyframe at position {index}");
            }
        }

        private static void cin_modify(){
            CinemaCamera.SaveStart(null); // we do this...
            CinemaCamera.SaveEnd(); //   to trick the main Cinema Camera into thinking it's initiated
            // usage: cin_add <float m_DurationSeconds> <bool m_Ease> <int index>
            int index = ModdedCinemaCamera.keyFrames.Count - 1;
            if (uConsole.NextParameterIsInt()) index = Mathf.Clamp(uConsole.GetInt(), 0, ModdedCinemaCamera.keyFrames.Count - 1);
            CameraKeyFrame cam = ModdedCinemaCamera.keyFrames[index];
            
            if (uConsole.NextParameterIsFloat()) cam.m_DurationSeconds = uConsole.GetFloat();
            if (uConsole.NextParameterIsBool()) cam.m_Ease = uConsole.GetBool();
            uConsole.Log($"Modified keyframe {index}");
            
        }

        private static void cin_restore(){
            int index = uConsole.GetInt();
            ModdedCinemaCamera.restore(index);
        }
        private static void cin_log(){
            for (var i = 0; i < ModdedCinemaCamera.keyFrames.Count; i++){
                CameraKeyFrame cam = ModdedCinemaCamera.keyFrames[i];
                uConsole.Log($"{i} - Duration to get to from previous: {cam.m_DurationSeconds}");
            }
            
        }

        private static void cin_mode(){
            switch (ModdedCinemaCamera.mode){
                case (CinemaCameraModes.MULTI_DURATION):
                    ModdedCinemaCamera.mode = CinemaCameraModes.ONE_DURATION;
                    uConsole.Log("Set mode to ONE_DURATION (smoother)");
                    return;
                case (CinemaCameraModes.ONE_DURATION):
                    ModdedCinemaCamera.mode = CinemaCameraModes.MULTI_DURATION;
                    uConsole.Log("Set mode to MULTI_DURATION (control time taken to reach each point)");
                    return;
            }
        }
        private static void cin_duration()
        {
            if (ModdedCinemaCamera.mode == CinemaCameraModes.MULTI_DURATION){
                uConsole.Log("Cannot set single duration when camera mode is MULTI_DURATION");
            }
            if (uConsole.GetNumParameters() == 0)
            {
                uConsole.Log("Duration is: " + ModdedCinemaCamera.duration.ToString() + "s");
                return;
            }
            if (uConsole.GetNumParameters() != 1)
            {
                uConsole.Log("Usage is cin_duration <seconds>");
            }
            ModdedCinemaCamera.duration = uConsole.GetFloat();
        }

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
        //private static void cin_duration()
        //{
        //    if (uConsole.GetNumParameters() == 0)
        //    {
        //        uConsole.Log("Duration is: " + CinemaCamera.m_DurationSeconds.ToString() + "s");
        //        return;
        //    }
        //    if (uConsole.GetNumParameters() != 1)
        //    {
        //        uConsole.Log("Usage is cin_duration <seconds>");
        //    }
        //    CinemaCamera.m_DurationSeconds = uConsole.GetFloat();
        //}

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

        [HarmonyPatch(typeof(BridgeTrace), "Fill")]
        public static class TracePatch {
            public static void Postfix(float maxEdgeLength){
                if (modEnabled.Value && instantTrace.Value) BridgeTrace.CompleteFillingInstantly();
            }
        }
        
        Harmony harmony;
    }
    
}