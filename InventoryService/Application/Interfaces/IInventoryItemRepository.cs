using Domain;

namespace Application.Interfaces
{
    public interface IInventoryItemRepository
    {
        Task CreateItemsAsync(List<InventoryItem> items);
        Task<InventoryItem> GetItemByIdAsync(int id);
        Task<List<InventoryItem>> GetItemsByProductIdAsync(int productId);
        Task<List<InventoryItem>> GetAllItemsAsync();
        Task ChangeItemsStatusAsync(List<InventoryItem> inventoryItems);
        Task<int> DeleteItemAsync(int id);
    }
}
