using Domain;
using ResponseLibrary.Models.Responses.Product;
using ResponseLibrary.Wrappers;

namespace Application.Services.Interfaces
{
    public interface IInventoryItemService
    {
        Task CreateItemsAsync(List<InventoryItem> items);
        Task<InventoryItem> GetItemByIdAsync(int id);
        Task<List<InventoryItem>> GetItemsByProductIdAsync(int productId);
        Task<List<InventoryItem>> GetAllItemsAsync();
        Task ChangeItemsStatusAsync(List<InventoryItem> inventoryItems);
        Task<int> DeleteItemAsync(int id);

        Task<ResponseWrapper<ProductResponse>> GetByProductIdAsync(int productId);
    }
}
