using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Zyara.Web.Middleware;

public class CspMiddleware
{
    private readonly RequestDelegate _next;
    public CspMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                // Development allows Browser Link noise; production uses strict policy
                var policy = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true
                    ? "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'; form-action 'self'; base-uri 'self'; connect-src 'self' ws://localhost:* wss://localhost:* http://localhost:* https://localhost:*; font-src 'self';"
                    : "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'; form-action 'self'; base-uri 'self'; connect-src 'self'; font-src 'self';";
                context.Response.Headers.Append("Content-Security-Policy", policy);
            }
            return Task.CompletedTask;
        });
        await _next(context);
    }
}
