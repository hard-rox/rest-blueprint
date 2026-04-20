using System.Collections.Concurrent;
using RestBlueprint.Api.Models.Entities;

namespace RestBlueprint.Api;

/// <summary>
/// In-memory data store used as a lightweight stand-in for a real database.
/// </summary>
/// <remarks>
/// <b>⚠ TEMPLATE NOTE — Replace this with a real persistence layer:</b>
/// <para>
/// This static class exists solely to make the template runnable out-of-the-box without a database.
/// Data is lost on every restart.  To add persistence:
/// </para>
/// <list type="number">
///   <item>
///     Add EF Core: install <c>Microsoft.EntityFrameworkCore</c> + a provider package
///     (e.g. <c>Pomelo.EntityFrameworkCore.MySql</c> or <c>Microsoft.EntityFrameworkCore.Sqlite</c>).
///   </item>
///   <item>
///     Create an <c>AppDbContext</c> with <c>DbSet&lt;Article&gt;</c> and <c>DbSet&lt;Comment&gt;</c>.
///   </item>
///   <item>
///     Register it in <c>Program.cs</c>:
///     <code>builder.Services.AddDbContext&lt;AppDbContext&gt;(opt => opt.UseSqlite("Data Source=app.db"));</code>
///   </item>
///   <item>
///     Inject <c>AppDbContext db</c> directly into endpoint handlers (Minimal API supports DI injection
///     in handler arguments) and replace all <c>DataStore.*</c> calls with async EF Core queries.
///   </item>
///   <item>
///     See <c>Services/</c> for the optional service-layer pattern.
///   </item>
/// </list>
/// </remarks>
public static class DataStore
{
    // Thread-safe dictionaries keyed by entity ID.
    public static readonly ConcurrentDictionary<Guid, Article> Articles = new(SeedArticles());
    public static readonly ConcurrentDictionary<Guid, Comment> Comments = new(SeedComments());

    // -------------------------------------------------------------------------
    // Seed data — replace or remove once you connect a real database.
    // -------------------------------------------------------------------------

    private static IEnumerable<KeyValuePair<Guid, Article>> SeedArticles()
    {
        Article[] articles =
        [
            new Article(
                Guid.Parse("11111111-0000-0000-0000-000000000001"),
                "Getting Started with REST APIs",
                "REST (Representational State Transfer) is an architectural style for designing networked applications...",
                "Jane Smith",
                ArticleStatus.Published,
                new DateTime(2025, 1, 10, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAt: null),

            new Article(
                Guid.Parse("11111111-0000-0000-0000-000000000002"),
                "HTTP Methods Explained",
                "Understanding GET, POST, PUT, PATCH, and DELETE is fundamental to building RESTful services...",
                "John Doe",
                ArticleStatus.Published,
                new DateTime(2025, 2, 5, 14, 30, 0, DateTimeKind.Utc),
                UpdatedAt: null),

            new Article(
                Guid.Parse("11111111-0000-0000-0000-000000000003"),
                "Versioning Your REST API",
                "API versioning strategies: URL path, query string, headers, and Accept header approaches...",
                "Alice Brown",
                ArticleStatus.Draft,
                new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc),
                UpdatedAt: null),

            new Article(
                Guid.Parse("11111111-0000-0000-0000-000000000004"),
                "Output Caching Best Practices",
                "Cache strategically: tag-based eviction ensures consistency without sacrificing performance...",
                "Bob Wilson",
                ArticleStatus.Archived,
                new DateTime(2025, 4, 20, 11, 0, 0, DateTimeKind.Utc),
                new DateTime(2025, 6, 15, 9, 0, 0, DateTimeKind.Utc))
        ];

        return articles.Select(a => new KeyValuePair<Guid, Article>(a.Id, a));
    }

    private static IEnumerable<KeyValuePair<Guid, Comment>> SeedComments()
    {
        Comment[] comments =
        [
            new Comment(
                Guid.Parse("22222222-0000-0000-0000-000000000001"),
                Guid.Parse("11111111-0000-0000-0000-000000000001"),
                "Great introduction! Very clear and well-structured.",
                "reader1",
                new DateTime(2025, 1, 11, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAt: null),

            new Comment(
                Guid.Parse("22222222-0000-0000-0000-000000000002"),
                Guid.Parse("11111111-0000-0000-0000-000000000001"),
                "Would love to see a section on HATEOAS.",
                "reader2",
                new DateTime(2025, 1, 12, 15, 0, 0, DateTimeKind.Utc),
                UpdatedAt: null),

            new Comment(
                Guid.Parse("22222222-0000-0000-0000-000000000003"),
                Guid.Parse("11111111-0000-0000-0000-000000000002"),
                "The PATCH vs PUT distinction was very helpful.",
                "reader3",
                new DateTime(2025, 2, 6, 9, 0, 0, DateTimeKind.Utc),
                UpdatedAt: null)
        ];

        return comments.Select(c => new KeyValuePair<Guid, Comment>(c.Id, c));
    }
}
