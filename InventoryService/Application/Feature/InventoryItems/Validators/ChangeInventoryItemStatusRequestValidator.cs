using Application.Feature.InventoryItems.Commands;
using FluentValidation;
using ResponseLibrary.Models.Requests.Inventory;

namespace Application.Feature.InventoryItems.Validators
{
    public class ChangeInventoryItemStatusCommandValidator : AbstractValidator<ChangeInventoryItemStatusCommand>
    {
        public ChangeInventoryItemStatusCommandValidator()
        {
            RuleFor(command => command.changeInventoryItemStatus)
                .NotNull()
                .SetValidator(new ChangeInventoryItemStatusRequestValidator());
        }
    }

    public class ChangeInventoryItemStatusRequestValidator : AbstractValidator<ChangeInventoryItemStatusRequest>
    {
        public static readonly string[] Statuses = ["Available", "Sold", "Removed"];

        public ChangeInventoryItemStatusRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0);
            RuleFor(x => x.Status)
                .Must(status => Statuses.Contains(status))
                    .WithMessage($"Status must be one of: {string.Join(", ", Statuses)}.");
        }
    }
}
