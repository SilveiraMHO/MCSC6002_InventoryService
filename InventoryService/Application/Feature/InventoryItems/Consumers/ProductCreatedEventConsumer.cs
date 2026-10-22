using Application.Feature.InventoryItems.Commands;
using MassTransit;
using MediatR;
using ResponseLibrary.Models.Requests.Inventory;
using ResponseLibrary.Models.Requests.Product.Events;

namespace Application.Feature.InventoryItems.Consumers
{
    public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
    {
        private readonly ISender _sender;

        public ProductCreatedEventConsumer(ISender sender)
        {
            _sender = sender;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var productCreatedEvent = context.Message;

            Console.WriteLine($"Message ID: {context.MessageId}\n");
            Console.WriteLine($"Product ID: {productCreatedEvent.Id}\nName: {productCreatedEvent.Name}\nQuantity: {productCreatedEvent.Quantity}\n");

            var response = await _sender.Send(new CreateInventoryItemCommand
            {
                CreateInventoryItems = GenerateCreateInventoryItems(productCreatedEvent)
            });

            // Create a Log or Notification with the response from the command is a good idea.
        }

        private static List<CreateInventoryItemRequest> GenerateCreateInventoryItems(ProductCreatedEvent productCreatedEvent)
        {
            var inventoryItems = new List<CreateInventoryItemRequest>();

            for (int i = 0; i < productCreatedEvent.Quantity; i++)
            {
                inventoryItems.Add(new CreateInventoryItemRequest
                {
                    ProductId = productCreatedEvent.Id,
                    Status = "Available",
                    Barcode = GenerateBarcode(productCreatedEvent.Id)
                });
            }

            return inventoryItems;
        }

        private static string GenerateBarcode(int productId)
        {
            string uniquePart = $"{DateTime.UtcNow.ToString("yyyyMMddHHmmssfffffff")}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";

            return $"{productId}-{uniquePart}";
        }
    }
}
