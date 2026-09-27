namespace ElementalBlacksmithStory.Data
{
    public enum StatModifierType
    {
        Flat, //단순합 ex) 10 + 10
        Add, //합연산 ex) 0.1%+0.1% = 0.2%
        Multiply, //곱연산 ex) 50% * 50% = 25%
    }
    public class StatModifier
    {
        public StatType Type { get; private set; }
        public StatModifierType ModifierType { get; private set; }
        public float Value { get; private set; }

        public StatModifier(StatType type, StatModifierType modifierType, float value)
        {
            Type = type;
            ModifierType = modifierType;
            Value = value;
        }
    }
}