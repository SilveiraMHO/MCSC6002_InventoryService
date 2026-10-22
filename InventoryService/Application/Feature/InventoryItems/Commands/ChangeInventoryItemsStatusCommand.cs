using Application.Services.Interfaces;
using MediatR;
using ResponseLibrary.Models.Requests.Inventory;
using ResponseLibrary.Wrappers;
using ResponseLibrary.Wrappers.Interfaces;

namespace Application.Feature.InventoryItems.Commands
{
    public class ChangeInventoryItemsStatusCommand : IRequest<IResponseWrapper>
    {
        public ChangeInventoryItemStatusRequest changeInventoryItemStatus { get; set; }
    }

    public class ChangeInventoryItemsStatusCommandHandler(IInventoryItemService inventoryItemService) : IRequestHandler<ChangeInventoryItemsStatusCommand, IResponseWrapper>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<IResponseWrapper> Handle(ChangeInventoryItemsStatusCommand request, CancellationToken cancellationToken)
        {
            var inventoryItem = await _inventoryItemService.GetItemsByProductIdAsync(request.changeInventoryItemStatus.Id);

            if (inventoryItem.Count > 0)
            {
                foreach (var item in inventoryItem)
                {
                    item.Status = request.changeInventoryItemStatus.Status;
                }

                await _inventoryItemService.ChangeItemsStatusAsync(inventoryItem);

                return await ResponseWrapper.SuccessAsync("Inventory items status marked as removed.");
            }

            return await ResponseWrapper.FailAsync($"Inventory items not found. (Product Id: {request.changeInventoryItemStatus.Id})");
        }
    }
}
