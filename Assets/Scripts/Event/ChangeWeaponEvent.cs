namespace ElementalBlacksmithStory.Events
{
    public readonly struct ChangeWeaponEvent
    {
        public readonly uint WeaponId { get; }
        public ChangeWeaponEvent(uint weaponId)
        {
            WeaponId = weaponId;
        }
    }
}