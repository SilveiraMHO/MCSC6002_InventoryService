using Application.Services.Interfaces;
using Mapster;
using MediatR;
using ResponseLibrary.Models.Responses.Inventory;
using ResponseLibrary.Wrappers;

namespace Application.Feature.InventoryItems.Queries
{
    public class GetInventoryItemByProductIdQuery : IRequest<ResponseWrapper<InventoryItemResponse>>
    {
        public int ProductId { get; set; }
    }

    public class GetInventoryItemByProductIdQueryHandler(IInventoryItemService inventoryItemService) : IRequestHandler<GetInventoryItemByProductIdQuery, ResponseWrapper<InventoryItemResponse>>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<ResponseWrapper<InventoryItemResponse>> Handle(GetInventoryItemByProductIdQuery request, CancellationToken cancellationToken)
        {
            var item = await _inventoryItemService.GetItemsByProductIdAsync(request.ProductId);

            if (item.Count > 0)
            {
                return await ResponseWrapper<InventoryItemResponse>.SuccessAsync(item.Adapt<InventoryItemResponse>());
            }

            return await ResponseWrapper<InventoryItemResponse>.FailAsync($"Inventory item not found. (Product Id: {request.ProductId})");
        }
    }
}
