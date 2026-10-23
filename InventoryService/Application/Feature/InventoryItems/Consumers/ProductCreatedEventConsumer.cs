using Application.Feature.InventoryItems.Commands;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using ResponseLibrary.Models.Requests.Inventory;
using ResponseLibrary.Models.Requests.Product.Events;

namespace Application.Feature.InventoryItems.Consumers
{
    public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
    {
        private const int MaxQuantity = 1000; // Same cap as Product's validator.

        private readonly ISender _sender;
        private readonly ILogger<ProductCreatedEventConsumer> _logger;

        public ProductCreatedEventConsumer(ISender sender, ILogger<ProductCreatedEventConsumer> logger)
        {
            _sender = sender;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
        {
            var productCreatedEvent = context.Message;

            // Defense in depth: one row per unit.
            if (productCreatedEvent.Quantity is < 1 or > MaxQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(productCreatedEvent.Quantity), productCreatedEvent.Quantity, $"Quantity must be between 1 and {MaxQuantity}.");
            }

            _logger.LogInformation("Product {ProductId} created: adding {Quantity} stock items (message {MessageId})", productCreatedEvent.Id, productCreatedEvent.Quantity, context.MessageId);

            var response = await _sender.Send(new CreateInventoryItemCommand
            {
                CreateInventoryItems = GenerateCreateInventoryItems(productCreatedEvent)
            });

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
