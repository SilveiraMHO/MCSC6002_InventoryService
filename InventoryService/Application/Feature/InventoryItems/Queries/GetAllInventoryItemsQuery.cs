using Application.Services.Interfaces;
using Mapster;
using MediatR;
using ResponseLibrary.Models.Responses.Inventory;
using ResponseLibrary.Wrappers;

namespace Application.Feature.InventoryItems.Queries
{
    public class GetAllInventoryItemsQuery : IRequest<ResponseWrapper<List<InventoryItemResponse>>>
    {
    }

    public class GetAllInventoryItemsQueryHandler(IInventoryItemService inventoryItemService) : IRequestHandler<GetAllInventoryItemsQuery, ResponseWrapper<List<InventoryItemResponse>>>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<ResponseWrapper<List<InventoryItemResponse>>> Handle(GetAllInventoryItemsQuery request, CancellationToken cancellationToken)
        {
            var inventoryItems = await _inventoryItemService.GetAllItemsAsync();

            if (inventoryItems.Count > 0)
            {
                return await ResponseWrapper<List<InventoryItemResponse>>.SuccessAsync(inventoryItems.Adapt<List<InventoryItemResponse>>());
            }

            return await ResponseWrapper<List<InventoryItemResponse>>.FailAsync("No inventory items found.");
        }
    }
}
