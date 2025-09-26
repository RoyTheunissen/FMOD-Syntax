using System;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Built whenever the addressables are built to map audio event paths to their respective address.
    /// This is necessary to be able to load audio via their path at runtime, which is used by lazy loading.
    /// </summary>
    [Serializable]
    public sealed class AudioEventPathToAddress
    {
        [SerializeField] private string audioEventPath;
        public string AudioEventPath => audioEventPath;
        
        [SerializeField] private string address;
        public string Address => address;
    }
}
