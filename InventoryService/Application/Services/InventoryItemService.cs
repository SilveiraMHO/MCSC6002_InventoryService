using Application.Interfaces;
using Application.Services.Interfaces;
using Domain;
using ResponseLibrary.Models.Responses.Product;
using ResponseLibrary.Wrappers;

namespace Application.Services
{
    public class InventoryItemService : IInventoryItemService
    {
        private readonly IInventoryItemRepository _inventoryItemRepository;
        private IHttpClientFactory _httpClientFactory;

        public InventoryItemService(IInventoryItemRepository inventoryItemRepository, IHttpClientFactory httpClientFactory)
        {
            _inventoryItemRepository = inventoryItemRepository;
            _httpClientFactory = httpClientFactory;
        }

        public async Task ChangeItemsStatusAsync(List<InventoryItem> inventoryItems)
        {
            await _inventoryItemRepository.ChangeItemsStatusAsync(inventoryItems);
        }

        public async Task CreateItemsAsync(List<InventoryItem> items)
        {
            await _inventoryItemRepository.CreateItemsAsync(items);
        }

        public async Task<int> DeleteItemAsync(int id)
        {
            return await _inventoryItemRepository.DeleteItemAsync(id);
        }

        public async Task<List<InventoryItem>> GetAllItemsAsync()
        {
            return await _inventoryItemRepository.GetAllItemsAsync();
        }

        public async Task<InventoryItem> GetItemByIdAsync(int id)
        {
            return await _inventoryItemRepository.GetItemByIdAsync(id);
        }

        public async Task<List<InventoryItem>> GetItemsByProductIdAsync(int productId)
        {
            return await _inventoryItemRepository.GetItemsByProductIdAsync(productId);
        }

        public async Task<ResponseWrapper<ProductResponse>> GetByProductIdAsync(int productId)
        {
            var client = _httpClientFactory.CreateClient("ProductServiceClient");

            var response = await client.GetAsync($"/product-service/products/{productId}");

            return await response.ToResponseAsync<ProductResponse>();
        }
    }
}
