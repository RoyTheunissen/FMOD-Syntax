using System;
using FMOD.Studio;
using FMODUnity;

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;
#endif // SCRIPTABLE_OBJECT_COLLECTION

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Utility for setting FMOD parameters from code.
    /// </summary>
    public abstract class Parameter
    {
        protected PARAMETER_ID id;
        protected bool isGlobal;

        protected EventInstance instance;

        public Parameter(PARAMETER_ID id, bool isGlobal)
        {
            this.id = id;
            this.isGlobal = isGlobal;
        }

        public void InitializeAsEventParameter(EventInstance instance)
        {
            this.instance = instance;
        }
    }
    
    public abstract class ParameterGeneric<ValueType> : Parameter
    {
        private float floatValue;
        public ValueType Value
        {
            get
            {
                if (isGlobal)
                    RuntimeManager.StudioSystem.getParameterByID(id, out floatValue);
                else if (instance.isValid())
                    instance.getParameterByID(id, out floatValue);
                return GetValueFromFloat(floatValue);
            }
            set
            {
                floatValue = GetFloatFromValue(value);
                if (isGlobal)
                    RuntimeManager.StudioSystem.setParameterByID(id, floatValue);
                else if (instance.isValid())
                    instance.setParameterByID(id, floatValue);
            }
        }

        protected abstract ValueType GetValueFromFloat(float value);
        protected abstract float GetFloatFromValue(ValueType value);

        protected ParameterGeneric(PARAMETER_ID id, bool isGlobal)
            : base(id, isGlobal)
        {
        }
    }
    
    public sealed class ParameterFloat : ParameterGeneric<float>
    {
        public ParameterFloat(PARAMETER_ID id, bool isGlobal) : base(id, isGlobal)
        {
        }

        protected override float GetValueFromFloat(float value)
        {
            return value;
        }

        protected override float GetFloatFromValue(float value)
        {
            return value;
        }
    }
    
    public sealed class ParameterInt : ParameterGeneric<int>
    {
        public ParameterInt(PARAMETER_ID id, bool isGlobal) : base(id, isGlobal)
        {
        }

        protected override int GetValueFromFloat(float value)
        {
            return (int)value;
        }

        protected override float GetFloatFromValue(int value)
        {
            return value;
        }
    }
    
    public sealed class ParameterEnum<EnumType> : ParameterGeneric<EnumType>
        where EnumType : Enum
    {
        public ParameterEnum(PARAMETER_ID id, bool isGlobal) : base(id, isGlobal)
        {
        }

        protected override EnumType GetValueFromFloat(float value)
        {
            return (EnumType)(object)(int)value;
        }

        protected override float GetFloatFromValue(EnumType value)
        {
            return (int)(object)value;
        }
    }
    
#if SCRIPTABLE_OBJECT_COLLECTION
    public sealed class ParameterScriptableObjectCollectionItem<ItemType> : ParameterGeneric<ItemType>
        where ItemType : ScriptableObjectCollectionItem
    {
        public ParameterScriptableObjectCollectionItem(PARAMETER_ID id, bool isGlobal) : base(id, isGlobal)
        {
        }

        protected override ItemType GetValueFromFloat(float value)
        {
            return ScriptableObjectCollection<ItemType>.Values[(int)value];
        }

        protected override float GetFloatFromValue(ItemType value)
        {
            return value.Index;
        }
    }
#endif // SCRIPTABLE_OBJECT_COLLECTION
    
    public sealed class ParameterBool : ParameterGeneric<bool>
    {
        public ParameterBool(PARAMETER_ID id, bool isGlobal) : base(id, isGlobal)
        {
        }

        protected override bool GetValueFromFloat(float value)
        {
            return value > 0.5f;
        }

        protected override float GetFloatFromValue(bool value)
        {
            return value ? 1 : 0;
        }
    }
}
