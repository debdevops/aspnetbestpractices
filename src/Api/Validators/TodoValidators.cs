using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public sealed class CreateTodoDtoValidator : AbstractValidator<CreateTodoDto>
{
    public CreateTodoDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}

public sealed class UpdateTodoDtoValidator : AbstractValidator<UpdateTodoDto>
{
    public UpdateTodoDtoValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        When(x => x.IsComplete, () =>
        {
            RuleFor(x => x.Notes).NotEmpty().WithMessage("Notes required when marking complete.");
        });
    }
}
