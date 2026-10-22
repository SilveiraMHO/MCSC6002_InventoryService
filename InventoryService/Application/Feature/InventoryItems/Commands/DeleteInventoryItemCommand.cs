using Application.Services.Interfaces;
using MediatR;
using ResponseLibrary.Wrappers;
using ResponseLibrary.Wrappers.Interfaces;

namespace Application.Feature.InventoryItems.Commands
{
    public class DeleteInventoryItemCommand : IRequest<IResponseWrapper>
    {
        public int ItemId { get; set; }
    }

    public class DeleteInventoryItemCommandHandler(IInventoryItemService inventoryItemService) : IRequestHandler<DeleteInventoryItemCommand, IResponseWrapper>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<IResponseWrapper> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
        {
            var removedItemId = await _inventoryItemService.DeleteItemAsync(request.ItemId);

            if (removedItemId != 0)
            {
                return await ResponseWrapper.SuccessAsync("Inventory item deleted successfully.");
            }

            return await ResponseWrapper.FailAsync("Failed to delete inventory item.");
        }
    }
}
