using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ResponseLibrary.Models.Requests.Product.Events;

namespace Application.Feature.InventoryItems.Consumers
{
    public class ProductDeletedEventConsumer : IConsumer<ProductDeletedEvent>
    {
        private readonly ISender _sender;
        private readonly ILogger<ProductDeletedEventConsumer> _logger;

        public ProductDeletedEventConsumer(ISender sender, ILogger<ProductDeletedEventConsumer> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
        {
            var productCreatedEvent = context.Message;

            _logger.LogInformation("Product {ProductId} deleted: removing its stock items (message {MessageId})", productCreatedEvent.Id, context.MessageId);

            var response = await _sender.Send(new Commands.ChangeInventoryItemsStatusCommand
            {
                changeInventoryItemStatus = new()
                {
                    Id = productCreatedEvent.Id,
                    Status = "Removed"
                }
            });

        }
    }
}
