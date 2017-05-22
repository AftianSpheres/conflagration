using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using CnfBattleSys;

/// <summary>
/// Static class containing scene metadata definitions.
/// </summary>
public static class SceneDatatable
{
    /// <summary>
    /// Metadata definitions for scenes in ring GlobalScenes.
    /// </summary>
    public static class GlobalScenes
    {
        private const SceneRing ring = SceneRing.GlobalScenes;
        public static readonly SceneMetadata loadingScreen = new SceneMetadata("Scenes/LoadingScreenScene", ring);

        /// <summary>
        /// Gets all global scene metadata.
        /// This is sort of expensive, so you should only do it once, really.
        /// </summary>
        public static SceneMetadata[] GetAll ()
        {
            FieldInfo[] fields = typeof(GlobalScenes).GetFields();
            List<SceneMetadata> list = new List<SceneMetadata>(fields.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType == typeof(SceneMetadata)) list.Add((SceneMetadata)(fields[i].GetValue(fields[i]))); 
            }
            return list.ToArray();
        }
    }

    /// <summary>
    /// Metadata definitions for scenes in ring SystemScenes.
    /// </summary>
    public static class SystemScenes
    {
        private const SceneRing ring = SceneRing.SystemScenes;
        public static readonly SceneMetadata battleSystem = new SceneMetadata("Scenes/BattleSystemScene", ring);
        public static readonly SceneMetadata testMenu = new SceneMetadata("Scenes/TestMenuScene", ring);
    }

    /// <summary>
    /// Metadata definitions for scenes in ring VenueScenes.
    /// </summary>
    public static class VenueScenes
    {
        private const SceneRing ring = SceneRing.VenueScenes;
        private const int specialVenuesCount = 1; // venues below index 0: invalid venue, that's it
        private const string venueScenesPath = "Scenes/BattleVenues/";
        private readonly static SceneMetadata[] venueScenesArray;

        /// <summary>
        /// Build the venue scenes table.
        /// </summary>
        static VenueScenes()
        {
            SceneMetadata[] metadataTable = new SceneMetadata[Enum.GetValues(typeof(VenueType)).Length - specialVenuesCount];
            for (int i = 1; i < metadataTable.Length; i++) metadataTable[i] = new SceneMetadata(venueScenesPath + (VenueType)i, ring);
            venueScenesArray = metadataTable;
        }

        /// <summary>
        /// Get scene metadata for venue scene corresponding to venue type.
        /// </summary>
        public static SceneMetadata Get (VenueType venueType)
        {
            return venueScenesArray[(int)venueType];
        }
    }

    /// <summary>
    /// HACK: We need to force the scene datatable to load before ExtendedSceneManager does anything
    /// so this function just exists to poke at its children and get them to initialize their static fields.
    /// </summary>
    public static void Bootstrap ()
    {
        SceneRing throwaway = GlobalScenes.loadingScreen.sceneRing;
        throwaway = SystemScenes.battleSystem.sceneRing;
        throwaway = VenueScenes.Get(VenueType.TestVenue).sceneRing;
        // you're worthless and idgaf what happens to you.
    }
}