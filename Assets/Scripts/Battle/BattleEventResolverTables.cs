using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using MovementEffects;

namespace CnfBattleSys
{
    /// <summary>
    /// Loads in audio event (etc.) resolver tables
    /// and stores references to those for puppets to use.
    /// </summary>
    public class BattleEventResolverTables : MonoBehaviour
    {
        public static BattleEventResolverTables instance { get; private set; }
        public bool loading { get { return fxEventsInLoading.Count > 0 || audioEventResolverTablesInLoading.Count > 0; } }
        private Dictionary<AudioEventResolverTableType, AudioEventResolverTable> audioEventResolverTables;
        private Dictionary<FXEventType, BattleFXController[]> fxEventResolverTable;
        private LinkedList<AudioEventResolverTableType> audioEventResolverTablesInLoading;
        private LinkedList<FXEventType> fxEventsInLoading;
        private string thisTag;
        

        /// <summary>
        /// MonoBehaviour.Awake ()
        /// </summary>
        void Awake ()
        {
            thisTag = GetInstanceID().ToString();
            audioEventResolverTables = new Dictionary<AudioEventResolverTableType, AudioEventResolverTable>(32);
            audioEventResolverTablesInLoading = new LinkedList<AudioEventResolverTableType>();
            fxEventsInLoading = new LinkedList<FXEventType>();
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

        public bool FireOffFXEventOn (FXEvent fxEvent, BattlerPuppet puppet)
        {
            bool resolveable = false;
            if (fxEventResolverTable.ContainsKey(fxEvent.fxEventType))
            {
                for (int i = 0; i < fxEventResolverTable[fxEvent.fxEventType].Length; i++)
                {
                    if (fxEventResolverTable[fxEvent.fxEventType][i].transform.parent == puppet.fxControllersParent)
                    {
                        resolveable = true;
                        fxEventResolverTable[fxEvent.fxEventType][i].Commence();
                        break;
                    }
                }
            }
            if (!resolveable && fxEvent.isMandatory) Util.Crash("Failed to resolve mandatory FX event of type " + fxEvent.fxEventType);
            return resolveable;
        }

        /// <summary>
        /// Adds the given fx type to the list of fx prefabs to load in
        /// and instantiate at the first opportunity. If it's already been requested,
        /// this does nothing silently, so you don't need to worry about checking for
        /// what's already been loaded - if you need an FxEventType, fire off
        /// RequestFXLoad() and it'll start loading if it's not loaded.
        /// </summary>
        public void RequestFXLoad(FXEventType fxEventType)
        {
            if (!fxEventsInLoading.Contains(fxEventType)) fxEventsInLoading.AddLast(fxEventType);
        }

        /// <summary>
        /// Coroutine: Load in the given table type, if necessary. Wait until the table is loaded, if it hasn't finished loading yet.
        /// Call the given Action once the table is loaded.
        /// </summary>
        public IEnumerator<float> _AwaitAudioEventResolverTableLoad (AudioEventResolverTableType tableType, Action callOnceTableLoaded)
        {
            if (!audioEventResolverTables.ContainsKey(tableType) && !audioEventResolverTablesInLoading.Contains(tableType)) Timing.RunCoroutine(_LoadAudioEventResolverTable(tableType), thisTag);
            while (audioEventResolverTablesInLoading.Contains(tableType)) yield return 0;
            callOnceTableLoaded();
        }

        /// <summary>
        /// Coroutine: Load in the AudioEventResolverTable for the given table type.
        /// </summary>
        private IEnumerator<float> _LoadAudioEventResolverTable (AudioEventResolverTableType tableType)
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
            audioEventResolverTables[tableType] = new AudioEventResolverTable(audioEventTypes, clipSets);
            audioEventResolverTablesInLoading.Remove(tableType);
        }

        /// <summary>
        /// Loads in the prefab for the given fx event, instantiates however many we need, and attaches them to the appropriate gameObjects
        /// to allow that fx event to be resolved during the battle.
        /// </summary>
        private IEnumerator<float> _LoadFXEvent(FXEvent fxEvent)
        {
            const string prefabsPath = "Battle/Prefabs/FX/";
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
            fxEventResolverTable[fxEvent.fxEventType] = controllers;
        }
    }
}