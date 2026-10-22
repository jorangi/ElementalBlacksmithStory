using ElementalBlacksmithStory.Core;
using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;
using VContainer;

namespace ElementalBlacksmithStory.Inventory
{
    /// <summary>
    /// 재료 아이템 전용 인벤토리
    /// </summary>
    public class MaterialInventory : BaseInventory<SO_MaterialData>
    {
        private readonly SO_MaterialDatabase _materialDatabase;

        [Inject]
        public MaterialInventory(ISubscriber<AcquireItemEvent> acquireSubscriber, SO_MaterialDatabase materialDatabase)
        {
            _materialDatabase = materialDatabase;
            SubscribeAcquire(acquireSubscriber);
        }

        protected override ItemCategory Category => ItemCategory.Material;

        protected override SO_MaterialData Resolve(uint id) => _materialDatabase != null ? _materialDatabase.Get(id) : null;
    }
}