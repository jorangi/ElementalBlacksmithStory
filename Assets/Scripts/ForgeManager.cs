using ElementalBlacksmithStory.Data;
using R3;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using ElementalBlacksmithStory.Events;
using ElementalBlacksmithStory.Inventory;

namespace ElementalBlacksmithStory.Core
{
    public class ForgeManager
    {
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly ISubscriber<ChangeWeaponEvent> _weaponChangeSubscriber;
        private readonly EquipmentInventory _inventory;

        public Weapon CurrentWeapon {get; private set;}
        [Inject]
        public ForgeManager(
            SO_WeaponDatabase weaponDatabase, 
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber,
            EquipmentInventory inventory)
        {
            _weaponDatabase = weaponDatabase;
            _weaponChangeSubscriber = weaponChangeSubscriber;
            _inventory = inventory;
            _weaponChangeSubscriber.Subscribe(e =>
            {
                UnityEngine.Debug.Log($"[ForgeManager] ChangeWeaponEvent 수신됨 -> [Id]Sprite: [{e.WeaponId}]{_weaponDatabase.GetWeapon(e.WeaponId).weaponName}");
                SetCurrentWeapon(e.Weapon);
                CurrentWeapon.SetData(_weaponDatabase.GetWeapon(e.WeaponId));
                if(e.Weapon.Cost == 0)
                    e.Weapon.PushCost(weaponDatabase.GetWeapon(e.WeaponId).cost);
            });
        }
        public void SetCurrentWeapon(uint weaponId)
        {
            CurrentWeapon = new Weapon(_weaponDatabase.GetWeapon(weaponId));
        }
        public void SetCurrentWeapon(Weapon weapon)
        {
            CurrentWeapon = weapon;
        }
    }
}