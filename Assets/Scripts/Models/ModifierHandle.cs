using System;

namespace ElementalBlacksmithStory.Data
{
    public readonly struct ModifierHandle : IDisposable, IEquatable<ModifierHandle>
    {
        private readonly Stat _stat;
        private readonly uint _id;
        public ModifierHandle(Stat stat, uint id)
        {
            _stat = stat;
            _id = id;
        }
        public void Dispose()
        {
            if (_stat != null)
            {
                _stat.RemoveModifier(_id);
            }
        }
        public bool Equals(ModifierHandle other) => _id == other._id;
        public override bool Equals(object obj) => obj is ModifierHandle other && Equals(other);
        public override int GetHashCode() => (int)_id;
    }
}