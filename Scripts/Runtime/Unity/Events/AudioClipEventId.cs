using System;
using UnityEngine;

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;
#endif // SCRIPTABLE_OBJECT_COLLECTION

namespace RoyTheunissen.FMODSyntax.UnityAudioSyntax
{
    public sealed class AudioClipEventId : ScriptableObject
#if SCRIPTABLE_OBJECT_COLLECTION
        , ISOCItem
#endif // SCRIPTABLE_OBJECT_COLLECTION
    {
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
