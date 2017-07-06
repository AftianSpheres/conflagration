using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using MovementEffects;

namespace CnfBattleSys
{
    /// <summary>
    /// Loads in audio event and FX event resolver tables
    /// and stores references to those for other objects to use.
    /// </summary>
    public class BattleEventResolverTablesLoader : MonoBehaviour
    {
        /// <summary>
        /// States the table loader can be in,
        /// indicating which phase of the load process this is.
        /// </summary>
        public enum LoadingState
        {
            NoLoadStarted,
            Loading,
            LoadCompleted
        }
        private event Action onAnyLoadFinished;

        public static BattleEventResolverTablesLoader instance { get; private set; }
        public LoadingState loadingState { get; private set; }
        private Dictionary<AudioEventResolverTableType, AudioEventResolverTable> audioEventResolverTables;
        private Dictionary<SignedFXEventType, BattleFXController[]> fxEventResolverTable;
        private LinkedList<AudioEventResolverTableType> audioEventResolverTablesInLoading;
        private LinkedList<SignedFXEventType> fxEventsInLoading;
        private string thisTag;
        
        /// <summary>
        /// MonoBehaviour.Awake ()
        /// </summary>
        void Awake ()
        {
            thisTag = GetInstanceID().ToString();
            audioEventResolverTables = new Dictionary<AudioEventResolverTableType, AudioEventResolverTable>(32);
            audioEventResolverTablesInLoading = new LinkedList<AudioEventResolverTableType>();
            fxEventResolverTable = new Dictionary<SignedFXEventType, BattleFXController[]>(32);
            fxEventsInLoading = new LinkedList<SignedFXEventType>();
        }

        /// <summary>
        /// MonoBehaviour.OnDestroy ()
        /// </summary>
        void OnDestroy ()
        {
            if (instance == this) instance = null;
            Timing.KillCoroutines(thisTag);
        }

        /// <summary>
        /// Coroutine: Call the given Action once an instance of TableLoader exists.
        /// </summary>
        public static IEnumerator<float> _OnceAvailable(Action callOnceTableLoaderExists)
        {
            while (instance == null) yield return 0;
            callOnceTableLoaderExists();
        }

        /// <summary>
        /// Get the specified AudioEventResolverTable.
        /// This will crash if it's not loaded!
        /// </summary>
        public AudioEventResolverTable GetTable (AudioEventResolverTableType tableType)
        {
            if (!audioEventResolverTables.ContainsKey(tableType)) Util.Crash(tableType + " isn't loaded");
            return audioEventResolverTables[tableType];
        }

        /// <summary>
        /// Once the current batch of table loading operations is completed,
        /// set loadingState to LoadingState.LoadCompleted.
        /// </summary>
        public void MarkLoadFinishedOnceRequestsClear ()
        {
            Action evt = () => { if (audioEventResolverTablesInLoading.Count == 0 && fxEventsInLoading.Count == 0) loadingState = LoadingState.LoadCompleted; onAnyLoadFinished = null; };
            // If we're calling this at a point when loading has already completed, onAnyLoadFinished would never be called, so just setting the event wouldn't accomplish anything.
            // We run evt once here in order to make sure we haven't already finished loading.
            // If we have, that's that; otherwise, we hook into onAnyLoadFinished, and load coroutines will call that
            // until we finally run out of live requests, at which point it'll set our load state and unhook itself.
            evt(); 
            if (loadingState != LoadingState.LoadCompleted) onAnyLoadFinished = evt;
        }

        /// <summary>
        /// Adds the given fx type to the list of fx prefabs to load in
        /// and instantiate at the first opportunity. If it's already been requested,
        /// this does nothing silently, so you don't need to worry about checking for
        /// what's already been loaded - if you need an FxEventType, fire off
        /// RequestFXLoad() and it'll start loading if it's not loaded.
        /// </summary>
        public void RequestFXLoad (FXEvent fxEvent, Action callback)
        {
            if (loadingState == LoadingState.LoadCompleted) Util.Crash("Can't request fx load: loading is finished!");
            else
            {
                if (loadingState == LoadingState.NoLoadStarted) loadingState = LoadingState.Loading;
                if (!fxEventsInLoading.Contains(fxEvent.signedFXEventType) && !fxEventResolverTable.ContainsKey(fxEvent.signedFXEventType)) Timing.RunCoroutine(_LoadFXEvent(fxEvent, callback));
            }
        }

        /// <summary>
        /// Coroutine: Load in the given table type, if necessary. Wait until the table is loaded, if it hasn't finished loading yet.
        /// Call the given Action once the table is loaded.
        /// </summary>
        public void RequestAudioEventResolverTableLoad (AudioEventResolverTableType tableType, Action callback)
        {
            if (loadingState == LoadingState.LoadCompleted) Util.Crash("Can't request aert load: loading is finished!");
            else
            {
                if (loadingState == LoadingState.NoLoadStarted) loadingState = LoadingState.Loading;
                if (!audioEventResolverTables.ContainsKey(tableType) && !audioEventResolverTablesInLoading.Contains(tableType)) Timing.RunCoroutine(_LoadAudioEventResolverTable(tableType, callback), thisTag);
            }

        }

        /// <summary>
        /// Generates and populates fx containers for all fx targets.
        /// </summary>
        private void GenerateContainersFromResolverTable ()
        {
            BattleStage.instance.AcquireFXContainer();
            for (int i = 0; i < BattleOverseer.currentBattle.allBattlers.Length; i++) BattleOverseer.currentBattle.allBattlers[i].puppet.AcquireFXContainer();
        }

        /// <summary>
        /// Coroutine: Load in the AudioEventResolverTable for the given table type.
        /// </summary>
        private IEnumerator<float> _LoadAudioEventResolverTable (AudioEventResolverTableType tableType, Action callback = null)
        {
            const string clipsPath = "Audio/Battle/";
            const string xmlPath = "Battle/AudioEventResolverTypes/";
            audioEventResolverTablesInLoading.AddLast(tableType);
            ResourceRequest request = Resources.LoadAsync<TextAsset>(xmlPath + tableType);
            while (request.progress < 1.0f) yield return 0;
            if (request.asset == null)
            {
                Util.Crash("No XML file for AudioEventResolverTableType of " + tableType);
                yield break;
            }
            TextAsset file = (TextAsset)request.asset;
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(file.ToString());
            XmlNode baseNode = doc.DocumentElement;
            if (baseNode == null) Util.Crash("No document element in AudioEventResolverTable definition: " + tableType);
            XmlNode workingNode;
            XmlNodeList resolvers = doc.SelectNodes("resolve");
            AudioEventType[] audioEventTypes = new AudioEventType[resolvers.Count];
            AudioClip[][] clipSets = new AudioClip[resolvers.Count][];
            if (resolvers.Count < 1) Util.Crash("No resolvers in AudioEventResolverTable definition: " + tableType);
            for (int r = 0; r < resolvers.Count; r++)
            {
                workingNode = resolvers[r].Attributes.GetNamedItem("type");
                if (workingNode == null) Util.Crash("No audio event type on resolver " + r + " attached to AudioEventResolverTable definition " + tableType);
                audioEventTypes[r] = (AudioEventType)Enum.Parse(typeof(AudioEventType), workingNode.InnerText);
                XmlNodeList clipDefs = resolvers[r].SelectNodes("clip");
                clipSets[r] = new AudioClip[clipDefs.Count];
                if (clipDefs.Count < 1) Util.Crash("No clips defined for resolver of type " + audioEventTypes[r] + " attached to AudioEventResolverTable definition " + tableType);
                for (int c = 0; c < clipDefs.Count; c++)
                {
                    workingNode = clipDefs[c];
                    if (workingNode.InnerText == "None" || workingNode.InnerText == "none") continue; // idk, you might want to have a "no clip" entry at some point
                    request = Resources.LoadAsync<AudioClip>(clipsPath + workingNode.InnerText);
                    while (request.progress < 1.0f) yield return 0;
                    if (request.asset == null)
                    {
                        Util.Crash("No audio clip at " + clipsPath + workingNode.InnerText);
                        yield break;
                    }
                    clipSets[r][c] = (AudioClip)request.asset;
                }
            }
            audioEventResolverTables.Add(tableType, new AudioEventResolverTable(audioEventTypes, clipSets));
            audioEventResolverTablesInLoading.Remove(tableType);
            if (callback != null) callback();
            onAnyLoadFinished();
        }

        /// <summary>
        /// Coroutine: Loads in the prefab for the given fx event, instantiates however many we need, and attaches them to the appropriate gameObjects
        /// to allow that fx event to be resolved during the battle.
        /// </summary>
        private IEnumerator<float> _LoadFXEvent(FXEvent fxEvent, Action callback = null)
        {
            const string prefabsPath = "Battle/Prefabs/FX/";
            fxEventsInLoading.AddLast(fxEvent.signedFXEventType);
            ResourceRequest request = Resources.LoadAsync<BattleFXController>(prefabsPath + fxEvent.fxEventType);
            while (request.progress < 1.0f) yield return request.progress;
            if (request.asset == null)
            {
                Util.Crash("No fx controller prefab for fx id of " + fxEvent.fxEventType);
                yield break;
            }
            Battler[] allBattlers = BattleOverseer.currentBattle.allBattlers;
            BattleFXController prefab = (BattleFXController)request.asset;
            int len = 0;
            len += allBattlers.Length; // controllers attached to all battlers 
            len++; // controller attached to stage 
            BattleFXController[] controllers = new BattleFXController[len];
            BattleFXController newController;
            for (int i = 0; i < allBattlers.Length; i++)
            {
                newController = Instantiate(prefab, allBattlers[i].puppet.fxControllersParent);
                if (fxEvent.isScalable) newController.transform.localScale *= allBattlers[i].fxScale;
                controllers[i] = newController;
            }
            // Don't forget about the stage copy!
            newController = Instantiate(prefab, BattleStage.instance.fxControllersParent);
            if (fxEvent.isScalable) newController.transform.localScale *= BattleStage.instance.fxScale;
            controllers[controllers.Length - 1] = newController;
            // And we're done
            fxEventResolverTable.Add(fxEvent.signedFXEventType, controllers);
            fxEventsInLoading.Remove(fxEvent.signedFXEventType);
            if (callback != null) callback();
            onAnyLoadFinished();
        }
    }
}