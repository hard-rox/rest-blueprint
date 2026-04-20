using FluentValidation;
using RestBlueprint.Api.Models.Requests;

namespace RestBlueprint.Api.Validators;

// ============================================================
// CreateCommentRequest validator
// ============================================================

/// <summary>Validates <see cref="CreateCommentRequest"/> before the handler executes.</summary>
internal sealed class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(2000).WithMessage("Comment body must not exceed 2000 characters.");

        RuleFor(x => x.Author)
            .NotEmpty().WithMessage("Author is required.")
            .MaximumLength(100).WithMessage("Author must not exceed 100 characters.");
    }
}

// ============================================================
// UpdateCommentRequest validator  (PUT — full replace)
// ============================================================

/// <summary>Validates <see cref="UpdateCommentRequest"/> for full-replace PUT operations.</summary>
internal sealed class UpdateCommentRequestValidator : AbstractValidator<UpdateCommentRequest>
{
    public UpdateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .NotEmpty().WithMessage("Comment body is required.")
            .MaximumLength(2000).WithMessage("Comment body must not exceed 2000 characters.");
    }
}

// ============================================================
// PatchCommentRequest validator  (PATCH — partial update)
// ============================================================

/// <summary>Validates <see cref="PatchCommentRequest"/> for partial PATCH operations.</summary>
internal sealed class PatchCommentRequestValidator : AbstractValidator<PatchCommentRequest>
{
    public PatchCommentRequestValidator()
    {
        When(x => x.Body is not null, () =>
        {
            RuleFor(x => x.Body!)
                .NotEmpty().WithMessage("Body must not be empty when provided.")
                .MaximumLength(2000).WithMessage("Body must not exceed 2000 characters.");
        });
    }
}
