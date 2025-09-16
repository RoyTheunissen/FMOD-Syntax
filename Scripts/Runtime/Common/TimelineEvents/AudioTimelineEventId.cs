using System;
using UnityEngine;

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;
#endif // SCRIPTABLE_OBJECT_COLLECTION

namespace RoyTheunissen.FMODSyntax.TimelineEvents
{
    /// <summary>
    /// Scriptable Object that functions as an ID for a timeline event. Both in the FMOD and the Unity solutions you can
    /// define specific times within an audio event at which a "timeline event" is dispatched. You could refer to it by
    /// name but it's smarter to refer to it via a strongly typed Scriptable Object instead so the reference doesn't
    /// break so easily (you can for example rename the event just by renaming the ID asset).
    ///
    /// Optionally you can use the Scriptable Object Collection package by Bruno Mikoski to manage these identifiers,
    /// then as a bonus you can have static access code generated so that you can access events from C# code using
    /// syntax like `AudioTimelineEventIds.End`. This part is optional so as not to have a hard dependency on another
    /// package. I can recommend doing it this way though.
    /// </summary>
#if !SCRIPTABLE_OBJECT_COLLECTION
    [CreateAssetMenu(fileName = "AudioTimelineEventId", menuName = MenuPaths.CreateScriptableObject + "Audio Timeline Event ID")]
#endif // !SCRIPTABLE_OBJECT_COLLECTION
    public sealed class AudioTimelineEventId : ScriptableObject
#if SCRIPTABLE_OBJECT_COLLECTION
        , ISOCItem
#endif // SCRIPTABLE_OBJECT_COLLECTION
    {
        public string Id => name;
        
#if SCRIPTABLE_OBJECT_COLLECTION
        [SerializeField, HideInInspector]
        private LongGuid guid;
        public LongGuid GUID
        {
            get
            {
                if (guid.IsValid())
                    return guid;
                
                GenerateNewGUID();
                return guid;
            }
        }
        
        [SerializeField, CollectionReferenceLongGuid]
        private LongGuid collectionGUID;

        [NonSerialized]
        private bool hasCachedCollection;
        [NonSerialized]
        private ScriptableObjectCollection cachedCollection;
        public ScriptableObjectCollection Collection
        {
            get
            {
                if (!hasCachedCollection)
                {
                    if (collectionGUID.IsValid())
                    {
                        cachedCollection = CollectionsRegistry.Instance.GetCollectionByGUID(collectionGUID);
                    }
                    else
                    {
                        CollectionsRegistry.Instance.TryGetCollectionFromItemType(GetType(), out cachedCollection);
                        if (cachedCollection != null)
                        {
                            collectionGUID = cachedCollection.GUID;
                            ObjectUtility.SetDirty(this);
                        }
                    }

                    hasCachedCollection = cachedCollection != null;
                }
                
                return cachedCollection;
            }
        }

        public void SetCollection(ScriptableObjectCollection collection)
        {
            cachedCollection = collection;
            collectionGUID = cachedCollection.GUID;
            ObjectUtility.SetDirty(this);
        }
        
        public void ClearCollection()
        {
            cachedCollection = null;
            hasCachedCollection = false;
            collectionGUID = default;
            ObjectUtility.SetDirty(this);
        }
        
        public void GenerateNewGUID()
        {
            guid = LongGuid.NewGuid();
            ObjectUtility.SetDirty(this);
        }
#endif // SCRIPTABLE_OBJECT_COLLECTION
    }
}
