using Application.Pipelines.Interfaces;
using Application.Services.Interfaces;
using MediatR;
using ResponseLibrary.Models.Requests.Inventory;
using ResponseLibrary.Wrappers;
using ResponseLibrary.Wrappers.Interfaces;

namespace Application.Feature.InventoryItems.Commands
{
    public class ChangeInventoryItemStatusCommand : IRequest<IResponseWrapper>, IValidateMe
    {
        public ChangeInventoryItemStatusRequest changeInventoryItemStatus { get; set; }
    }

    public class ChangeInventoryItemStatusCommandHandler(IInventoryItemService inventoryItemService) : IRequestHandler<ChangeInventoryItemStatusCommand, IResponseWrapper>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<IResponseWrapper> Handle(ChangeInventoryItemStatusCommand request, CancellationToken cancellationToken)
        {
            var inventoryItem = await _inventoryItemService.GetItemByIdAsync(request.changeInventoryItemStatus.Id);

            if (inventoryItem.Id != 0)
            {
                inventoryItem.Status = request.changeInventoryItemStatus.Status;

                await _inventoryItemService.ChangeItemsStatusAsync([inventoryItem]);

                return await ResponseWrapper.SuccessAsync($"Inventory item status marked as '{request.changeInventoryItemStatus.Status}'.");
            }

            return await ResponseWrapper.FailAsync($"Inventory item not found. (Id: {request.changeInventoryItemStatus.Id})");
        }
    }
}
