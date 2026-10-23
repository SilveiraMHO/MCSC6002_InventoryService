using Application.Services.Interfaces;
using Mapster;
using MediatR;
using ResponseLibrary.Models.Responses.Inventory;
using ResponseLibrary.Wrappers;

namespace Application.Feature.InventoryItems.Queries
{
    public class GetInventoryItemByProductIdQuery : IRequest<ResponseWrapper<List<InventoryItemResponse>>>
    {
        public int ProductId { get; set; }
    }

    public class GetInventoryItemByProductIdQueryHandler(IInventoryItemService inventoryItemService) : IRequestHandler<GetInventoryItemByProductIdQuery, ResponseWrapper<List<InventoryItemResponse>>>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<ResponseWrapper<List<InventoryItemResponse>>> Handle(GetInventoryItemByProductIdQuery request, CancellationToken cancellationToken)
        {
            var items = await _inventoryItemService.GetItemsByProductIdAsync(request.ProductId);

            if (items.Count > 0)
            {
                return await ResponseWrapper<List<InventoryItemResponse>>.SuccessAsync(items.Adapt<List<InventoryItemResponse>>());
            }

            return await ResponseWrapper<List<InventoryItemResponse>>.FailAsync($"Inventory item not found. (Product Id: {request.ProductId})");
        }
    }
}
