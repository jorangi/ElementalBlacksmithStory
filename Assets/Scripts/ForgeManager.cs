using ElementalBlacksmithStory.Data;
using R3;
using MessagePipe;
using VContainer;
using VContainer.Unity;
using ElementalBlacksmithStory.Events;

namespace ElementalBlacksmithStory.Core
{
    public class ForgeManager
    {
        private readonly SO_WeaponDatabase _weaponDatabase;
        private readonly ISubscriber<ChangeWeaponEvent> _weaponChangeSubscriber;
        public SO_WeaponData CurrentWeapon {get; private set;}
        [Inject]
        public ForgeManager(
            SO_WeaponDatabase weaponDatabase, 
            ISubscriber<ChangeWeaponEvent> weaponChangeSubscriber)
        {
            _weaponDatabase = weaponDatabase;
            _weaponChangeSubscriber = weaponChangeSubscriber;
            SetCurrentWeapon(10001);
            _weaponChangeSubscriber.Subscribe(e =>
            {
                UnityEngine.Debug.Log($"[ForgeManager] ChangeWeaponEvent 수신됨 -> [Id]Sprite: [{e.WeaponId}]{_weaponDatabase.GetWeapon(e.WeaponId).weaponName}");
                SetCurrentWeapon(e.WeaponId); 
            });
        }
        public void SetCurrentWeapon(uint weaponId)
        {
            CurrentWeapon = _weaponDatabase.GetWeapon(weaponId);
            
        }
    }
}