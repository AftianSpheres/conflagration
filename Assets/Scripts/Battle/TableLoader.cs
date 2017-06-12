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
    public class TableLoader : MonoBehaviour
    {
        public static TableLoader instance { get; private set; }
        private Dictionary<AudioEventResolverTableType, AudioEventResolverTable> audioEventResolverTables;
        private LinkedList<AudioEventResolverTableType> tablesInLoading;
        private string thisTag;

        /// <summary>
        /// MonoBehaviour.Awake ()
        /// </summary>
        void Awake ()
        {
            thisTag = GetInstanceID().ToString();
            audioEventResolverTables = new Dictionary<AudioEventResolverTableType, AudioEventResolverTable>(32);
            tablesInLoading = new LinkedList<AudioEventResolverTableType>();
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
        /// Coroutine: Load in the given table type, if necessary. Wait until the table is loaded, if it hasn't finished loading yet.
        /// Call the given Action once the table is loaded.
        /// </summary>
        public IEnumerator<float> _AwaitTableLoad (AudioEventResolverTableType tableType, Action callOnceTableLoaded)
        {
            if (!audioEventResolverTables.ContainsKey(tableType) && !tablesInLoading.Contains(tableType)) Timing.RunCoroutine(_LoadTable(tableType), thisTag);
            while (tablesInLoading.Contains(tableType)) yield return 0;
            callOnceTableLoaded();
        }

        /// <summary>
        /// Coroutine: Load in the AudioEventResolverTable for the given table type.
        /// </summary>
        private IEnumerator<float> _LoadTable (AudioEventResolverTableType tableType)
        {
            const string clipsPath = "Audio/Battle/";
            const string xmlPath = "Battle/AudioEventResolverTypes/";
            tablesInLoading.AddLast(tableType);
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
            tablesInLoading.Remove(tableType);
        }
    }
}