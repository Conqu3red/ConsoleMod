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

        public static ConsoleMod instance;
        void Awake()
        {
            if (instance == null) instance = this;
            // Use this if you wish to make the mod trigger cheat mode ingame.
            // Set this true if your mod effects physics or allows mods that you can't normally do.
            isCheat = false;
            // Set this to whether the mod is currently enabled or not.
            // Usually you want this to be true by default.


            // Register the mod to PTF, that way it will be able to be configured using PTF ingame.

            modEnabled = Config.Bind(modEnabledDef, true, new ConfigDescription("Enable Mod"));
            modEnabled.SettingChanged += onEnableDisable;

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

                uConsole.RegisterCommand("popup_test", new uConsole.DebugCommand(popup_test));
                uConsole.RegisterCommand("set_cheat", new uConsole.DebugCommand(toggleCheat));
                
                // modded Cinematic Camera
                uConsole.RegisterCommand("cin_add", new uConsole.DebugCommand(cin_add));
                uConsole.RegisterCommand("cin_delete", new uConsole.DebugCommand(cin_delete));
                uConsole.RegisterCommand("cin_modify", new uConsole.DebugCommand(cin_modify));
                uConsole.RegisterCommand("cin_restore", new uConsole.DebugCommand(cin_restore));
                uConsole.RegisterCommand("cin_list", new uConsole.DebugCommand(cin_log));
                
                
                // z modifiers 
                
                uConsole.RegisterCommand("set_z", new uConsole.DebugCommand(set_z));
                uConsole.RegisterCommand("set_z_scale", new uConsole.DebugCommand(set_z_scale));
                uConsole.RegisterCommand("shuffle_z", new uConsole.DebugCommand(shuffle_z));


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
                if (uConsole.IsOn() && !modEnabled.Value)
                {
                    uConsole.TurnOff();
                }
            }
        }

        // Use this method to execute code that will be ran when the mod is enabled.
        public override void enableMod() 
        {
            //Logger.LogInfo("Enabled!");
        }
        // Use this method to execute code that will be ran when the mod is disabled.
        public override void disableMod() 
        {
            //Logger.LogInfo("Disabled!");
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

        private static void toggleCheat(){
            bool val = uConsole.GetBool();
            PolyTechMain.setCheat(instance, val);
            PopUpMessage.DisplayOkOnly("Set Console mod cheat? to " + val.ToString(), null);
        }


        private static void set_z(){
            float z = uConsole.GetFloat();
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                var obj = SandboxSelectionSet.m_Items[i];
                CustomShape component = obj.gameObject.GetComponent<CustomShape>();
                if (component != null){
                    string orig_pos = component.gameObject.transform.position.ToString();
                    Vector3 new_pos = new Vector3(component.gameObject.transform.position.x, component.gameObject.transform.position.y, z);
                    component.gameObject.transform.position = new_pos;
                    uConsole.Log(orig_pos + " -> " + component.gameObject.transform.position.ToString());
                }

            }
        }
        private static void shuffle_z(){
            float z_start = 5f;
            float z_end = 100f;
            float z;
            if (uConsole.GetNumParameters() == 2){
                z_start = uConsole.GetFloat();
                z_end = uConsole.GetFloat();
            }
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                var obj = SandboxSelectionSet.m_Items[i];
                CustomShape component = obj.gameObject.GetComponent<CustomShape>();
                if (component != null){
                    z = UnityEngine.Random.Range(z_start, z_end);
                    string orig_pos = component.gameObject.transform.position.ToString();
                    Vector3 new_pos = new Vector3(component.gameObject.transform.position.x, component.gameObject.transform.position.y, z);
                    component.gameObject.transform.position = new_pos;
                    uConsole.Log(orig_pos + " -> " + component.gameObject.transform.position.ToString());
                }

            }
        }
        private static void set_z_scale(){
            float z = uConsole.GetFloat();
            for (var i = 0; i < SandboxSelectionSet.m_Items.Count; i++){
                var obj = SandboxSelectionSet.m_Items[i];
                CustomShape component = obj.gameObject.GetComponent<CustomShape>();
                if (component != null){
                    string orig_scale = component.gameObject.transform.localScale.ToString();
                    Vector3 new_scale = new Vector3(component.gameObject.transform.localScale.x, component.gameObject.transform.localScale.y, z);
                    component.gameObject.transform.localScale = new_scale;
                    
                    uConsole.Log(orig_scale + " -> " + component.gameObject.transform.localScale.ToString());
                }

            }
        }
        private static void popup_test() 
        {
            GameUI.ShowMessage(ScreenMessageLocation.TOP_LEFT, "hey", 5f);
            PopUpMessage.DisplayOkOnly("ok only", null);
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

        public class CameraKeyFrame {
            public Vector3 m_StartPos;
		    public Quaternion m_StartRot;
		    public Vector3 m_StartPivot;
		    public float m_StartOrthographicSize;
            public float m_DurationSeconds = 5;
            public bool m_Ease = false;

        }
        public static class ModdedCinemaCamera {
            public static List<CameraKeyFrame> keyFrames = new List<CameraKeyFrame> ();
            public static int current_start = 0;
            public static int current_end = 1;

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