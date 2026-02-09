using Collaboration.Infrastructure.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Collaboration.Presentation;

internal static class HandleCollaboration
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapHub<DocumentHub>("/hubs/document").WithTags(Tags.Collaboration);
    }
}