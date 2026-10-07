using ElementalBlacksmithStory.Data;

namespace ElementalBlacksmithStory.Inventory
{
    public interface IInventoryContext
    {
        MaterialInventory MaterialInventory { get; }
        EquipmentInventory EquipmentInventory { get; }
        RuneInventory RuneInventory { get; }
        SO_MaterialDatabase MaterialDatabase { get; }
    }
}
