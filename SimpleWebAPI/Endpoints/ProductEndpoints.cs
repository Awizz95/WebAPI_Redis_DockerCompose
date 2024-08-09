using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Products.Api.Contracts;
using Products.Api.Database;
using Products.Api.Entities;

namespace Products.Api.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("products", async (
            CreateProductRequest request,
            ApplicationDbContext context,
            CancellationToken ct) =>
        {
            var product = new Product
            {
                Name = request.Name,
                Price = request.Price
            };

            context.Add(product);

            await context.SaveChangesAsync(ct);

            return Results.Ok(product);
        });

        app.MapGet("products", async (
            ApplicationDbContext context,
            CancellationToken ct,
            int page = 1,
            int pageSize = 10) =>
        {
            var products = await context.Products
                .AsNoTracking()
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return Results.Ok(products);
        });

        app.MapGet("products/{id}", async (
            int id,
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var product = await cache.GetAsync($"products-{id}",
                async token =>
                {
                    var product = await context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == id, token);

                    return product;
                },
                CacheOptions.DefaultExpiration,
                ct);

            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        app.MapPut("products/{id}", async (
            int id,
            UpdateProductRequest request,
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product is null) return Results.NotFound();

            product.Name = request.Name;
            product.Price = request.Price;

            await context.SaveChangesAsync(ct);

            await cache.RemoveAsync($"products-{id}");

            return Results.NoContent();
        });

        app.MapDelete("products/{id}", async (
            int id,
            ApplicationDbContext context,
            IDistributedCache cache,
            CancellationToken ct) =>
        {
            var product = await context.Products
                .FirstOrDefaultAsync(p => p.Id == id, ct);

            if (product is null) return Results.NotFound();

            context.Remove(product);

            await context.SaveChangesAsync(ct);

            await cache.RemoveAsync("products-{id}", ct);

            return Results.NoContent();
        });
    }
}
