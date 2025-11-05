using FluentValidation;
using GeneralReservationSystem.Application.DTOs;

namespace GeneralReservationSystem.Application.Validators
{
    public class PagedSearchRequestDtoValidator : AppValidator<PagedSearchRequestDto>
    {
        public PagedSearchRequestDtoValidator()
        {
            _ = RuleFor(x => x.Page)
                .GreaterThan(0);
            _ = RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 1000);
        }
    }
}