using Application.Services.Interfaces;
using Domain;
using Mapster;
using MediatR;
using ResponseLibrary.Models.Requests.Inventory;
using ResponseLibrary.Wrappers;
using ResponseLibrary.Wrappers.Interfaces;

namespace Application.Feature.InventoryItems.Commands
{
    public class CreateInventoryItemCommand : IRequest<IResponseWrapper>
    {
        public List<CreateInventoryItemRequest> CreateInventoryItems { get; set; }
    }

    public class CreateInventoryItemCommandHandler(IInventoryItemService inventoryItemService) : IRequestHandler<CreateInventoryItemCommand, IResponseWrapper>
    {
        private readonly IInventoryItemService _inventoryItemService = inventoryItemService;

        public async Task<IResponseWrapper> Handle(CreateInventoryItemCommand request, CancellationToken cancellationToken)
        {
            var mappedItems = request.CreateInventoryItems.Adapt<List<InventoryItem>>();

            var dateTimeNow = DateTime.UtcNow;

            foreach (var item in mappedItems)
            {
                item.CreatedOn = dateTimeNow;
            }

            await _inventoryItemService.CreateItemsAsync(mappedItems);

            return await ResponseWrapper.SuccessAsync("Inventory items created successfully.");
        }
    }
}
