using Application.Feature.InventoryItems.Commands;
using Application.Feature.InventoryItems.Queries;
using AuthLibrary.Attributes;
using AuthLibrary.Constants.Authentication;
using Microsoft.AspNetCore.Mvc;
using ResponseLibrary.Models.Requests.Inventory;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    public class InventoryItemsController : AppBaseController
    {
        [HttpPut("change-item-status")]
        [HasPermission(AppService.Inventory, AppFeature.InventoryItems, AppAction.Update)]
        public async Task<IActionResult> ChangeItemStatus([FromBody] ChangeInventoryItemStatusRequest changeInventoryItemStatus)
        {
            var response = await Sender.Send(new ChangeInventoryItemStatusCommand { changeInventoryItemStatus = changeInventoryItemStatus });

            if (response.IsSuccessful) return Ok(response);

            return NotFound(response);
        }

        [HttpGet("all")]
        [HasPermission(AppService.Inventory, AppFeature.InventoryItems, AppAction.Read)]
        public async Task<IActionResult> GetAllAsync()
        {
            var response = await Sender.Send(new GetAllInventoryItemsQuery());

            if (response.IsSuccessful) return Ok(response);

            return NotFound(response);
        }

        [HttpGet("{id}")]
        [HasPermission(AppService.Inventory, AppFeature.InventoryItems, AppAction.Read)]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var response = await Sender.Send(new GetInventoryItemByIdQuery { ItemId = id });

            if (response.IsSuccessful) return Ok(response);

            return NotFound(response);
        }

        [HttpGet("get-by-product-id/{productId}")]
        [HasPermission(AppService.Inventory, AppFeature.InventoryItems, AppAction.Read)]
        public async Task<IActionResult> GetByProductIdAsync(int productId)
        {
            var response = await Sender.Send(new GetInventoryItemByProductIdQuery { ProductId = productId });

            if (response.IsSuccessful) return Ok(response);

            return NotFound(response);
        }

        [HttpDelete("delete/{id}")]
        [HasPermission(AppService.Inventory, AppFeature.InventoryItems, AppAction.Delete)]
        public async Task<IActionResult> DeleteByIdAsync(int id)
        {
            var response = await Sender.Send(new DeleteInventoryItemCommand { ItemId = id });

            if (response.IsSuccessful) return Ok(response);

            return NotFound(response);
        }
    }
}
