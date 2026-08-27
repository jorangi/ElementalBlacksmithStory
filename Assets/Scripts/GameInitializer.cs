using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Inventory;
using VContainer;
using VContainer.Unity;

namespace ElementalBlacksmithStory.Core
{
    public class GameInitializer : IStartable
    {
        private readonly MaterialInventory _materialInventory;
        private readonly SO_MaterialDatabase _materialDatabase;
        [Inject]
        public GameInitializer(
            MaterialInventory materialInventory,
            SO_MaterialDatabase materialDatabase)
        {
            _materialInventory = materialInventory;
            _materialDatabase = materialDatabase;
        }
        public void Start()
        {
            _materialInventory.Add(_materialDatabase.GetMaterial(30001), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30002), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30003), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30004), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30005), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30006), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30007), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30008), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30009), 500);
            _materialInventory.Add(_materialDatabase.GetMaterial(30010), 500);
        }
    }
}