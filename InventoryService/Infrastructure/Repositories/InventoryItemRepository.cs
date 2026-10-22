using Application.Interfaces;
using Domain;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class InventoryItemRepository : IInventoryItemRepository
    {
        private readonly ApplicationDbContext _context;

        public InventoryItemRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ChangeItemsStatusAsync(List<InventoryItem> inventoryItems)
        {
            _context.InventoryItems.UpdateRange(inventoryItems);
            await _context.SaveChangesAsync();
        }

        public async Task CreateItemsAsync(List<InventoryItem> inventoryItems)
        {
            await _context.InventoryItems.AddRangeAsync(inventoryItems);
            await _context.SaveChangesAsync();
        }

        public async Task<int> DeleteItemAsync(int id)
        {
            var inventoryItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryItem != null)
            {
                _context.InventoryItems.Remove(inventoryItem);
                await _context.SaveChangesAsync();
                return inventoryItem.Id;
            }

            return 0;
        }

        public async Task<List<InventoryItem>> GetAllItemsAsync()
        {
            var inventoryItem = await _context.InventoryItems.ToListAsync();

            if (inventoryItem.Count > 0)
            {
                return inventoryItem;
            }

            return [];
        }

        public async Task<InventoryItem> GetItemByIdAsync(int id)
        {
            var inventoryItem = await _context.InventoryItems.FirstOrDefaultAsync(i => i.Id == id);

            if (inventoryItem is not null)
            {
                return inventoryItem;
            }

            return new();
        }

        public async Task<List<InventoryItem>> GetItemsByProductIdAsync(int productId)
        {
            var inventoryItems = await _context.InventoryItems.Where(i => i.ProductId == productId).ToListAsync();

            if (inventoryItems.Count > 0)
            {
                return inventoryItems;
            }

            return [];
        }
    }
}
