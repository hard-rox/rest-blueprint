using FluentValidation;
using RestBlueprint.Api.Models.Requests;

namespace RestBlueprint.Api.Validators;

// ============================================================
// CreateArticleRequest validator
// ============================================================

/// <summary>Validates <see cref="CreateArticleRequest"/> before the handler executes.</summary>
internal sealed class CreateArticleRequestValidator : AbstractValidator<CreateArticleRequest>
{
    public CreateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters.");
    }
}

// ============================================================
// UpdateArticleRequest validator  (PUT — full replace)
// ============================================================

/// <summary>Validates <see cref="UpdateArticleRequest"/> for full-replace PUT operations.</summary>
internal sealed class UpdateArticleRequestValidator : AbstractValidator<UpdateArticleRequest>
{
    public UpdateArticleRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Body is required.");
    }
}

// ============================================================
// PatchArticleRequest validator  (PATCH — partial update)
// ============================================================

/// <summary>
/// Validates <see cref="PatchArticleRequest"/> for partial PATCH operations.
/// Only non-null fields are validated.
/// </summary>
internal sealed class PatchArticleRequestValidator : AbstractValidator<PatchArticleRequest>
{
    public PatchArticleRequestValidator()
    {
        // Validate only the fields that were supplied (not null).
        When(x => x.Title is not null, () =>
        {
            RuleFor(x => x.Title!)
                .NotEmpty().WithMessage("Title must not be empty when provided.")
                .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");
        });

        When(x => x.Body is not null, () =>
        {
            RuleFor(x => x.Body!)
                .NotEmpty().WithMessage("Body must not be empty when provided.");
        });
    }
}
