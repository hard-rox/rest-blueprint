namespace RestBlueprint.Api.Models.Entities;

/// <summary>The lifecycle status of an article.</summary>
public enum ArticleStatus
{
    /// <summary>Work in progress — not publicly visible.</summary>
    Draft,

    /// <summary>Live and publicly accessible.</summary>
    Published,

    /// <summary>Soft-removed; no longer listed but data is retained.</summary>
    Archived
}
