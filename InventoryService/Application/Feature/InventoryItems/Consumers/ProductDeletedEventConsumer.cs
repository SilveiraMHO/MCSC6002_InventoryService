using MassTransit;
using MediatR;
using ResponseLibrary.Models.Requests.Product.Events;

namespace Application.Feature.InventoryItems.Consumers
{
    public class ProductDeletedEventConsumer : IConsumer<ProductDeletedEvent>
    {
        private readonly ISender _sender;

        public ProductDeletedEventConsumer(ISender sender)
        {
            _sender = sender;
        }

        public async Task Consume(ConsumeContext<ProductDeletedEvent> context)
        {
            var productCreatedEvent = context.Message;

            Console.WriteLine($"Message ID: {context.MessageId}\n");

            var response = await _sender.Send(new Commands.ChangeInventoryItemsStatusCommand
            {
                changeInventoryItemStatus = new()
                {
                    Id = productCreatedEvent.Id,
                    Status = "Removed"
                }
            });

            // Create a Log or Notification with the response from the command is a good idea.
        }
    }
}
