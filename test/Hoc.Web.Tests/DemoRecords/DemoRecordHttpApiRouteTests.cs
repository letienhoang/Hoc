using System.Linq;
using Hoc.Authorization;
using Microsoft.AspNetCore.Routing;
using Shouldly;
using Xunit;

namespace Hoc.DemoRecords.Tests;

public class DemoRecordHttpApiRouteTests : RolePermissionWebTestBase
{
    [Fact]
    public void Should_expose_only_the_expected_conventional_demo_record_routes()
    {
        var routes = GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.Contains("demo-records", System.StringComparison.OrdinalIgnoreCase) == true)
            .ToList();

        routes.ShouldContain(endpoint =>
            endpoint.RoutePattern.RawText == "api/demo-records/demo-record" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("GET"));
        routes.ShouldContain(endpoint =>
            endpoint.RoutePattern.RawText == "api/demo-records/demo-record" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("POST"));
        routes.ShouldContain(endpoint =>
            endpoint.RoutePattern.RawText == "api/demo-records/demo-record/{id}" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("GET"));
        routes.ShouldContain(endpoint =>
            endpoint.RoutePattern.RawText == "api/demo-records/demo-record/{id}" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("PUT"));
        routes.ShouldContain(endpoint =>
            endpoint.RoutePattern.RawText == "api/demo-records/demo-record/{id}" &&
            endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Contains("DELETE"));
    }
}
