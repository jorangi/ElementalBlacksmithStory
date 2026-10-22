using ElementalBlacksmithStory.Data;
using ElementalBlacksmithStory.Events;
using MessagePipe;

namespace ElementalBlacksmithStory.Inventory
{
    public interface IInventoryContext
    {
        MaterialInventory MaterialInventory { get; }
        EquipmentInventory EquipmentInventory { get; }
        RuneInventory RuneInventory { get; }
        SO_MaterialDatabase MaterialDatabase { get; }
        IPublisher<AcquireItemEvent> AcquireItemPublisher { get; }
    }
}
