using System.Collections.Generic;

namespace ElementalBlacksmithStory.Data
{
    public class Stat
    {
        private static uint _nextModId = 1;
        private bool isDirty = false;
        private float _value = 0f;
        private float _baseValue = 0f;
        public float BaseValue
        {
            get => _baseValue;
            private set
            {
                _baseValue = value;
                isDirty = true;
            }
        }
        public void SetBaseValue(float baseValue)
        {
            BaseValue = baseValue;
        }
        public float Value
        {
            get
            {
                if (isDirty)
                {
                    _value = CalculateValue();
                    isDirty = false;
                }
                return _value;
            }
        }
        private readonly List<StatModifier> statModifiers = new();
        public Stat(float baseValue, IReadOnlyList<StatModifier> modifiers = null)
        {
            _baseValue = baseValue;
            statModifiers.AddRange(modifiers);
            isDirty = true;
        }
        public ModifierHandle AddModifier(StatModifier modifier)
        {
            modifier.Id = _nextModId++;
            statModifiers.Add(modifier);
            isDirty = true;
            return new ModifierHandle(this, modifier.Id);
        }
        public bool RemoveModifier(uint modId)
        {
            for (int i = 0; i < statModifiers.Count; i++)
            {
                if (statModifiers[i].Id == modId)
                {
                    statModifiers[i] = statModifiers[^1];
                    statModifiers.RemoveAt(statModifiers.Count - 1);
                    isDirty = true;
                    return true;
                }
            }
            return false;
        }
        private float CalculateValue()
        {
            float finalValue = _baseValue;
            float sumPercentAdd = 0f;
            for (int i = 0; i < statModifiers.Count; i++)
            {
                StatModifier mod = statModifiers[i];
                switch (mod.ModifierType)
                {
                    case StatModifierType.Flat:
                        finalValue += mod.Value;
                        break;
                    case StatModifierType.Add:
                        sumPercentAdd += mod.Value;
                        break;
                    case StatModifierType.Multiply:
                        finalValue *= 1f + mod.Value;
                        break;
                }
            }
            finalValue *= 1f + sumPercentAdd;
            return finalValue;
        }
    }
}