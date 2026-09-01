using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TmsApi.Application.DTOs;
using TmsApi.Application.DTOs.CourseDtos;
using Xunit;

namespace TmsApi.Tests.Integration;

public class CoursesApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CoursesApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        factory.SeedData();
    }

    [Fact]
    public async Task GetCourses_V1_ReturnsSuccessAndPagedCourses()
    {
        var response = await _client.GetAsync("/api/v1/courses?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedData = await response.Content.ReadFromJsonAsync<PagedResponse<CourseDto>>();

        pagedData.Should().NotBeNull();
        pagedData!.Items.Should().NotBeEmpty();
        pagedData.Items.Should().Contain(c => c.Code == "CS101");
    }

    [Fact]
    public async Task GetCourses_V2_ReturnsSuccessAndCachedCourses()
    {
        var response = await _client.GetAsync("/api/v2/courses?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var pagedData = await response.Content.ReadFromJsonAsync<PagedResponse<CourseDto>>();

        pagedData.Should().NotBeNull();
        pagedData!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCourses_WithDataShaping_ReturnsRequestedFields()
    {
        var response = await _client.GetAsync("/api/v1/courses?fields=code,title");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        root.TryGetProperty("items", out var itemsElement).Should().BeTrue();
        itemsElement.GetArrayLength().Should().BeGreaterThan(0);

        var firstItem = itemsElement[0];
        firstItem.TryGetProperty("Code", out _).Should().BeTrue();
        firstItem.TryGetProperty("Title", out _).Should().BeTrue();
    }
}
