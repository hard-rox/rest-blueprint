using RestBlueprint.Api.Models.Entities;
using RestBlueprint.Api.Models.Requests;
using RestBlueprint.Api.Models.Responses;
using RestBlueprint.Api.QueryParams;

namespace RestBlueprint.Api.Services;

// =============================================================================
// TEMPLATE NOTE — ArticleService stub
// =============================================================================
// This is an EMPTY implementation stub.  All methods throw NotImplementedException.
// Fill in the bodies once you have connected a real data store (e.g. EF Core DbContext).
//
// TYPICAL EF CORE IMPLEMENTATION (sketch):
//
//   private readonly AppDbContext _db;
//   public ArticleService(AppDbContext db) => _db = db;
//
//   public async Task<PagedResult<ArticleResponse>> ListAsync(ArticleQueryParams query, CancellationToken ct)
//   {
//       IQueryable<Article> q = _db.Articles.AsNoTracking();
//       if (!string.IsNullOrWhiteSpace(query.Search))
//           q = q.Where(a => a.Title.Contains(query.Search));
//       if (query.Status.HasValue)
//           q = q.Where(a => a.Status == query.Status.Value);
//
//       int total = await q.CountAsync(ct);
//       List<ArticleResponse> items = await q
//           .OrderByDescending(a => a.CreatedAt)
//           .Skip((query.PageNumber - 1) * query.PageSize)
//           .Take(query.PageSize)
//           .Select(a => ArticleResponse.FromArticle(a))
//           .ToListAsync(ct);
//
//       return new PagedResult<ArticleResponse>(items, total, query.PageNumber, query.PageSize);
//   }
// =============================================================================

/// <summary>
/// Stub implementation of <see cref="IArticleService"/>.
/// Replace the <c>throw</c> bodies with real data access logic.
/// </summary>
internal sealed class ArticleService : IArticleService
{
    public Task<PagedResult<ArticleResponse>> ListAsync(ArticleQueryParams query, CancellationToken ct)
        => throw new NotImplementedException("Replace with real data access.");

    public Task<ArticleResponse?> GetByIdAsync(Guid id, CancellationToken ct)
        => throw new NotImplementedException("Replace with real data access.");

    public Task<ArticleResponse> CreateAsync(CreateArticleRequest request, CancellationToken ct)
        => throw new NotImplementedException("Replace with real data access.");

    public Task<ArticleResponse?> ReplaceAsync(Guid id, UpdateArticleRequest request, CancellationToken ct)
        => throw new NotImplementedException("Replace with real data access.");

    public Task<ArticleResponse?> PatchAsync(Guid id, PatchArticleRequest request, CancellationToken ct)
        => throw new NotImplementedException("Replace with real data access.");

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct)
        => throw new NotImplementedException("Replace with real data access.");
}
