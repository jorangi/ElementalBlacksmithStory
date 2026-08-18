using ElementalBlacksmithStory.Core;
namespace ElementalBlacksmithStory.Events
{
    public readonly struct ChangeWeaponEvent
    {
        public readonly Weapon Weapon { get; }
        public readonly uint WeaponId { get; }
        public readonly ulong TotalCost { get; }
        public ChangeWeaponEvent(Weapon weapon, uint nextWeaponId, ulong totalCost)
        {
            Weapon = weapon;
            WeaponId = nextWeaponId;
            TotalCost = totalCost;
        }
        public ChangeWeaponEvent(uint weaponId)
        {
            WeaponId = weaponId;
            Weapon = null;
            TotalCost = 0;
        }
    }
}