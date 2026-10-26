// Reloads packs, checks missing files, rebuilds the car and records timings.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace SpKnpp {
 public sealed partial class Exporter {
  static Exporter developerPartsOwner;
  readonly HashSet<object> developerLiveBundles=new HashSet<object>();
  readonly Dictionary<ulong,object> developerPackSnapshot=new Dictionary<ulong,object>();
  object developerPartsLoader,developerPartsRoomObject;
  FieldInfo developerScanCacheField;object developerScanCacheOriginal,developerScanCacheMarker;
  object developerPartsHarmony,developerPartsAwaiter,developerPartsGame,developerPartsState,developerPartsOldCar;
  MethodBase developerUnloadMethod,developerCreateMethod;MethodInfo developerCreateHook;
  int developerAllowedMissing;
  int developerPartsStage,developerPartsCarId,developerPartsRoom,developerCreateRequests,developerPackCountBefore,developerExpectedPackFiles,developerBundleKeeps;
  bool developerPartsRequest,developerRebuildArmed,developerSkipRebuild;
  readonly System.Diagnostics.Stopwatch developerTotalTimer=new System.Diagnostics.Stopwatch(),developerStageTimer=new System.Diagnostics.Stopwatch();
  float developerPartsStarted,developerReadySince=-1,developerScanSeconds,developerRebuildSeconds;
  string developerReloadStep="idle",developerReloadError="";
  string developerPartsStatus="Ready · refresh installed .knpp packs without going to the garage.";
  public static void DeveloperPackUnloadPrefix(object __0,ref bool __1) {
   Exporter owner=developerPartsOwner;
   if(object.ReferenceEquals(owner,null) || owner.developerPartsStage!=1 || !owner.developerLiveBundles.Contains(__0)) return;
   __1=false;owner.developerBundleKeeps++;
  }
  public static bool DeveloperShouldRecreateCar(object state,int carId) {
   Exporter owner=developerPartsOwner;
   if(owner==null || !owner.developerRebuildArmed || owner.developerCreateRequests!=0 || carId!=owner.developerPartsCarId || !object.ReferenceEquals(state,owner.developerPartsState)) return false;
   owner.developerCreateRequests++;return true;
  }
  void DeveloperPatch(MethodBase original,MethodInfo hook,string kind) {
   object descriptor=Activator.CreateInstance(FindType("HarmonyLib.HarmonyMethod"),new object[]{hook});
   foreach(MethodInfo patch in developerPartsHarmony.GetType().GetMethods()) {
    if(patch.Name!="Patch") continue;ParameterInfo[] p=patch.GetParameters();int slot=Array.FindIndex(p,x=>x.Name==kind);if(slot<0) continue;
    object[] args=new object[p.Length];args[0]=original;args[slot]=descriptor;patch.Invoke(developerPartsHarmony,args);return;
   }
   throw new MissingMethodException("Harmony "+kind);
  }
  void RemoveDeveloperPatches() {
   developerRebuildArmed=false;
   if(developerPartsHarmony!=null) {
    MethodInfo undo=developerPartsHarmony.GetType().GetMethod("Unpatch",new[]{typeof(MethodBase),typeof(MethodInfo)});
    try {if(developerUnloadMethod!=null) undo.Invoke(developerPartsHarmony,new object[]{developerUnloadMethod,typeof(Exporter).GetMethod("DeveloperPackUnloadPrefix",BindingFlags.Public|BindingFlags.Static)});} catch(Exception e) {Debug.LogWarning("SP-KNPP developer unload cleanup: "+e.GetBaseException().Message);}
    try {if(developerCreateMethod!=null) undo.Invoke(developerPartsHarmony,new object[]{developerCreateMethod,developerCreateHook});} catch(Exception e) {Debug.LogWarning("SP-KNPP developer rebuild cleanup: "+e.GetBaseException().Message);}
   }
   RestoreDeveloperScanCache();developerPartsLoader=null;
   developerPartsHarmony=null;developerUnloadMethod=developerCreateMethod=null;developerCreateHook=null;
   developerLiveBundles.Clear();developerPackSnapshot.Clear();if(developerPartsOwner==this) developerPartsOwner=null;
  }
  void InvalidateDeveloperScanCache(object loader) {
   FieldInfo field=kinoCopyModule.ResolveField(67119448);
   object original=field.GetValue(loader);
   if(!(original is Dictionary<string,string>)) throw new InvalidOperationException("Kino pack file cache is unavailable; reload cancelled.");
   object marker=new Dictionary<string,string>();
   developerScanCacheField=field;developerScanCacheOriginal=original;developerScanCacheMarker=marker;
   field.SetValue(loader,marker);
  }
  void RestoreDeveloperScanCache() {
   try {
    if(developerScanCacheField!=null && developerPartsLoader!=null && object.ReferenceEquals(developerScanCacheField.GetValue(developerPartsLoader),developerScanCacheMarker))
     developerScanCacheField.SetValue(developerPartsLoader,developerScanCacheOriginal);
   } catch(Exception e) {Debug.LogWarning("SP-KNPP file-cache restore: "+e);}
   developerScanCacheField=null;developerScanCacheOriginal=developerScanCacheMarker=null;
  }
  static int CountMissingDeveloperPacks(IDictionary known,IDictionary disk) {
   if(known==null || disk==null) throw new InvalidOperationException("Pack file list unavailable.");
   int missing=0;foreach(object key in known.Keys) if(!disk.Contains(key)) missing++;
   return missing;
  }
  bool DeveloperContextReady(out object car) {
   car=null;
   object game=gameProperty==null?null:gameProperty.GetValue(null,null);
   if(!object.ReferenceEquals(game,developerPartsGame) || !object.ReferenceEquals(R(game,"CurrentRoom"),developerPartsRoomObject)) return false;
   object state=FindType("States").GetProperty("mainCurrent",Flags).GetValue(null,null);
   car=R(state,"userCar")??R(R(game,"LocalPlayer"),"userCar");
   return object.ReferenceEquals(state,developerPartsState) && car!=null && Number(car,"carId")==developerPartsCarId;
  }
  void StartDeveloperPartReload() {
   if(developerPartsStage!=0) throw new InvalidOperationException("Wait for the current reload to finish.");
   developerTotalTimer.Restart();developerStageTimer.Restart();developerScanSeconds=developerRebuildSeconds=0;developerReloadStep="checking Kino";developerReloadError="";ResolveKinoCopy();
   if(Convert.ToBoolean(KinoInvoke(100682045,null))) throw new InvalidOperationException("Kino is already loading car parts. Wait for it to finish.");
   object game=gameProperty.GetValue(null,null),room=R(game,"CurrentRoom"),car=R(R(game,"LocalPlayer"),"userCar");
   object state=FindType("States").GetProperty("mainCurrent",Flags).GetValue(null,null);
   car=R(state,"userCar")??car;
   Type race=FindType("BaseRaceState");
   if(state==null || race==null || !race.IsInstanceOfType(state) || car==null || !object.Equals(R(car,"IsVisualLoaded"),true) || object.Equals(R(state,"carSwapRunning"),true) || object.Equals(R(R(car,"carModel"),"IsCarPartsLoading"),true))
    throw new InvalidOperationException("Enter single-player, training or a multiplayer session and wait for your car to finish loading.");
   object module=kinoCopyModule.ResolveField(67118991).GetValue(null);
   if(module==null) throw new InvalidOperationException("Kino's car-part module is not initialized.");
   object loader=KinoInvoke(100682031,module);developerPartsLoader=loader;
   IDictionary packs=KinoField(67119447,loader) as IDictionary;
   if(packs==null) throw new InvalidOperationException("Kino's pack registry cannot be inspected.");
   var keep=new HashSet<ulong>();developerLiveBundles.Clear();developerPackSnapshot.Clear();
   FieldInfo bundleRefField=kinoCopyModule.ResolveField(67119602),bundleHandleField=kinoCopyModule.ResolveField(67112432);
   foreach(DictionaryEntry pair in packs) {
    developerPackSnapshot[Convert.ToUInt64(pair.Key)]=pair.Value;
    object bundleRef=bundleRefField.GetValue(pair.Value),bundle=bundleRef==null?null:bundleHandleField.GetValue(bundleRef);
    UnityEngine.Object live=bundle as UnityEngine.Object;
    if(live!=null) {developerLiveBundles.Add(bundle);keep.Add(Convert.ToUInt64(pair.Key));}
   }
   developerPartsGame=game;developerPartsState=state;developerPartsRoomObject=room;int roomId;developerPartsRoom=AttachmentLobby.TryReadId(room,R,out roomId)?roomId:-1;developerPartsCarId=Number(car,"carId");developerPartsOldCar=car;
   string folder=Convert.ToString(KinoInvoke(100682908,null));
   if(string.IsNullOrEmpty(folder) || !System.IO.Directory.Exists(folder)) throw new InvalidOperationException("Kino car_parts folder is unavailable.");
   developerExpectedPackFiles=System.IO.Directory.GetFiles(folder,"*.knpp").Length;
   if(developerExpectedPackFiles==0) throw new InvalidOperationException("No .knpp files found; the loaded registry was kept.");
   IDictionary knownFiles=KinoField(67119448,loader) as IDictionary;
   // A missing pack can make the subsequent native rebuild discard its selected
   // part IDs. Refuse that destructive transition rather than save a stock car.
   object[] scanArgs={null};KinoInvoke(100682866,loader,scanArgs);
   IDictionary diskFiles=scanArgs[0] as IDictionary;
   if(diskFiles==null || diskFiles.Count!=developerExpectedPackFiles)
    throw new InvalidOperationException("Pack folder changed or could not be read. Finish exporting/copying packs, then retry.");
   int missingFiles=CountMissingDeveloperPacks(knownFiles,diskFiles);
   if(missingFiles>0) throw new InvalidOperationException(missingFiles+" previously loaded pack file(s) are missing. Restore them before reloading so equipped part selections are not lost. Car kept as-is.");
   developerAllowedMissing=knownFiles==null?0:Math.Max(0,knownFiles.Count-packs.Count);
   developerPackCountBefore=packs.Count;developerCreateRequests=developerBundleKeeps=0;developerSkipRebuild=false;developerReadySince=-1;

   BundledHarmony.EnsureLoaded();developerPartsHarmony=Activator.CreateInstance(FindType("HarmonyLib.Harmony"),new object[]{"rez.spknpp.reload"});
   developerUnloadMethod=KinoMethod(100666791);
   developerPartsOwner=this;developerPartsStage=1;developerPartsStarted=Time.unscaledTime;developerStageTimer.Restart();
   try {
    developerReloadStep="protecting live car assets";DeveloperPatch(developerUnloadMethod,typeof(Exporter).GetMethod("DeveloperPackUnloadPrefix",BindingFlags.Public|BindingFlags.Static),"prefix");
    developerReloadStep="invalidating native pack file cache";InvalidateDeveloperScanCache(loader);
    developerReloadStep="running Kino pack reload";object task=KinoInvoke(100682867,loader,keep);developerPartsAwaiter=Call(task,"GetAwaiter");
    developerPartsStatus="Refreshing Kino's pack registry; preserving existing cars' loaded assets…";
   } catch {developerPartsStage=0;RemoveDeveloperPatches();throw;}
  }
  void StartDeveloperCarRebuild() {
   developerReloadStep="rebuilding current car";
   object current;if(!DeveloperContextReady(out current)) {FinishDeveloperReload("Packs refreshed. Car/room changed, so no car rebuild was requested.");return;}
   Type machine=null;foreach(Type nested in FindType("BaseRaceState").GetNestedTypes(Flags)) if(nested.Name.StartsWith("<CreateUserCarAsync>",StringComparison.Ordinal)) {if(machine!=null) throw new InvalidOperationException("Ambiguous car-create method.");machine=nested;}
   developerCreateMethod=machine==null?null:machine.GetMethod("MoveNext",Flags);
   if(developerCreateMethod==null) throw new MissingMethodException("Native car-create state machine unavailable.");
   Type instruction=developerPartsHarmony.GetType().Assembly.GetType("HarmonyLib.CodeInstruction");
   developerCreateHook=typeof(Exporter).GetMethod("SyncSameModelCreateTranspiler",BindingFlags.Public|BindingFlags.Static).MakeGenericMethod(instruction);
   DeveloperPatch(developerCreateMethod,developerCreateHook,"transpiler");

   developerRebuildArmed=true;developerPartsOldCar=current;

   developerPartsStarted=Time.unscaledTime;developerStageTimer.Restart();
   try {object task=Call(developerPartsState,"SwapCar",developerPartsCarId);developerPartsAwaiter=Call(task,"GetAwaiter");developerPartsStage=2;developerPartsStatus="Packs refreshed. Rebuilding the current car through CarX…";}
   finally { }
  }
  void RecordDeveloperReload(string status) {
   try {if(!string.IsNullOrEmpty(outputRoot)) System.IO.File.WriteAllLines(System.IO.Path.Combine(outputRoot,"sp-knpp-reload.txt"),new[]{"SP-KNPP developer pack reload",status,"Car: "+developerPartsCarId,"Packs before: "+developerPackCountBefore,"Pack files scanned: "+developerExpectedPackFiles,"Bundle unloads preserving live assets: "+developerBundleKeeps,"Same-model rebuild branches: "+developerCreateRequests,"Previously unregistered files: "+developerAllowedMissing,"Pack refresh seconds: "+developerScanSeconds.ToString("0.000"),"Car rebuild seconds: "+developerRebuildSeconds.ToString("0.000"),"Total seconds: "+(developerTotalTimer.Elapsed.TotalSeconds).ToString("0.000"),"Step: "+developerReloadStep,"Error detail: "+developerReloadError,"Remote result: unconfirmed"});}catch(Exception e){Debug.LogWarning("SP-KNPP developer report: "+e.GetBaseException().Message);}
  }
  void FinishDeveloperReload(string status) {
   RecordDeveloperReload(status);
   developerPartsStage=0;developerPartsAwaiter=null;RemoveDeveloperPatches();developerPartsStatus=status;
  }
  void UpdateDeveloperPartReload() {
   try {
    if(developerPartsRequest) {developerPartsRequest=false;StartDeveloperPartReload();}
    if(developerPartsStage==0) return;
    if(developerPartsAwaiter!=null) {
     if(!object.Equals(R(developerPartsAwaiter,"IsCompleted"),true)) {
      if(Time.unscaledTime-developerPartsStarted>120) {developerSkipRebuild=true;developerPartsStatus="Kino/CarX is taking longer than expected. Waiting for its task; further reloads are blocked.";}
      return;
     }
     object awaiter=developerPartsAwaiter;developerPartsAwaiter=null;Call(awaiter,"GetResult");
     if(developerPartsStage==1) {
      developerScanSeconds=(float)developerStageTimer.Elapsed.TotalSeconds;
      object module=kinoCopyModule.ResolveField(67118991).GetValue(null),loader=KinoInvoke(100682031,module);
      IDictionary registered=KinoField(67119447,loader) as IDictionary;
      bool unchanged=registered!=null && registered.Count==developerPackSnapshot.Count;
      if(unchanged) foreach(DictionaryEntry entry in registered) {object before;if(!developerPackSnapshot.TryGetValue(Convert.ToUInt64(entry.Key),out before) || !object.ReferenceEquals(before,entry.Value)) {unchanged=false;break;}}
      if(unchanged) throw new InvalidOperationException("Kino kept the old pack registry. No car-only reload was performed; pack refresh could not be confirmed.");
      if(registered==null || registered.Count<developerExpectedPackFiles-developerAllowedMissing) {FinishDeveloperReload("Pack scan finished with missing/duplicate packs ("+(registered==null?0:registered.Count)+" registered from "+developerExpectedPackFiles+" files). Car rebuild skipped; check Kino errors.");return;}
      developerReloadStep="refreshing Kino part cache";
      object cache=KinoInvoke(100682022,null);
      if(cache==null) throw new InvalidOperationException("Kino part cache is not initialized.");
      KinoInvoke(100682697,cache);KinoInvoke(100665516,null);
      if(!developerSkipRebuild) StartDeveloperCarRebuild();
      else FinishDeveloperReload("Kino pack registry refreshed. Existing cars kept their old assets; switch/rebuild a car to use the updated pack.");
      return;
     }
     developerRebuildArmed=false;developerPartsStage=3;developerReadySince=-1;
    }
    if(developerPartsStage==3) {
     object car;if(!DeveloperContextReady(out car)) {FinishDeveloperReload("Packs refreshed. The session changed during car rebuild; check the current car before retrying.");return;}
     bool ready=car!=null && !object.ReferenceEquals(car,developerPartsOldCar) && !object.Equals(R(developerPartsState,"carSwapRunning"),true) && object.Equals(R(car,"IsVisualLoaded"),true) && !object.Equals(R(R(car,"carModel"),"IsCarPartsLoading"),true);
     object local=KinoStaticProperty(kinoCopyModule.Assembly.GetType("Kino.ModCore.Players",true),"LocalPlayer");
     ready=ready && local!=null && object.ReferenceEquals(R(local,"RaceCar"),car);
     if(!ready) {developerReadySince=-1;if(Time.unscaledTime-developerPartsStarted>180) FinishDeveloperReload("Packs refreshed, but the car did not finish loading. Use CarX's normal car switcher to recover.");return;}
     if(developerReadySince<0) {developerReadySince=Time.unscaledTime;return;}
     if(Time.unscaledTime-developerReadySince<.5f) return;
     if(developerCreateRequests!=1) throw new InvalidOperationException("CarX did not confirm exactly one same-model rebuild.");
     developerRebuildSeconds=(float)developerStageTimer.Elapsed.TotalSeconds;
     FinishDeveloperReload("Packs refreshed and selected parts applied in "+(developerTotalTimer.Elapsed.TotalSeconds).ToString("0.000")+"s. Multiplayer appearance is unconfirmed.");
    }
   } catch(Exception e) {developerReloadError=e.ToString();FinishDeveloperReload("Pack refresh stopped while "+developerReloadStep+": "+e.GetBaseException().Message);Debug.LogWarning("SP-KNPP "+developerPartsStatus+"\n"+developerReloadError);}
  }
  void DrawDeveloperParts() {
   GUILayout.BeginVertical(cardStyle);GUILayout.Label("Reload car-part packs",nameStyle);
   GUILayout.Label("Refresh and apply updated .knpp packs in single-player, training or multiplayer. Finish exporting/copying the pack before reloading.",smallStyle);
   bool enabled=GUI.enabled;GUI.enabled=enabled && developerPartsStage==0;
   GUILayout.Label("Reload rereads installed packs from disk, then rebuilds your car after the registry refresh is confirmed.",smallStyle);
   if(ActionButton("Reload car-part packs",GUILayout.Width(320),GUILayout.Height(40))) developerPartsRequest=true;
   GUI.enabled=enabled;GUILayout.Label(developerPartsStatus,smallStyle);
   if(developerPartsStage>0) GUILayout.Label("A native reload is running. Wait for it to finish before changing cars or starting another operation.",smallStyle);

   GUILayout.Label("Existing car instances keep their old assets until rebuilt. This does not distribute edited packs to other players. Repeated developer reloads can retain old assets; restart after a long editing session.",smallStyle);
   GUILayout.EndVertical();
  }
 }
}
