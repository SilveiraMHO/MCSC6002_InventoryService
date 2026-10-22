using Application.Services.Interfaces;
using Mapster;
using MediatR;
using ResponseLibrary.Models.Responses.Inventory;
using ResponseLibrary.Wrappers;

namespace Application.Feature.InventoryItems.Queries
{
    public class GetInventoryItemByIdQuery : IRequest<ResponseWrapper<InventoryItemResponse>>
    {
        public int ItemId { get; set; }
    }

    public class GetInventoryItemByIdQueryHandler(IInventoryItemService inventoryItemService) : IRequestHandler<GetInventoryItemByIdQuery, ResponseWrapper<InventoryItemResponse>>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<ResponseWrapper<InventoryItemResponse>> Handle(GetInventoryItemByIdQuery request, CancellationToken cancellationToken)
        {
            var item = await _inventoryItemService.GetItemByIdAsync(request.ItemId);

            if (item.Id != 0)
            {
                var mappedItem = item.Adapt<InventoryItemResponse>();

                var productResponse = await _inventoryItemService.GetByProductIdAsync(item.ProductId); // Data from Product Service.

                if (productResponse.IsSuccessful)
                {
                    mappedItem.Product = productResponse.Data;

                    return await ResponseWrapper<InventoryItemResponse>.SuccessAsync(mappedItem);
                }

                return await ResponseWrapper<InventoryItemResponse>.SuccessAsync(mappedItem, productResponse.Messages);
            }

            return await ResponseWrapper<InventoryItemResponse>.FailAsync($"Inventory item not found. (Item Id: {request.ItemId})");
        }
    }
}
