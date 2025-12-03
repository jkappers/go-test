using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace sample.Tests;

[TestClass]
public class ProgramTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [TestInitialize]
    public void Setup()
    {
        _factory = new WebApplicationFactory<Program>();
        _client = _factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [TestMethod]
    public async Task RootEndpoint_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task RootEndpoint_ReturnsExpectedContent()
    {
        var response = await _client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.IsTrue(content.Contains("Hello from"));
        Assert.IsTrue(content.Length > 0);
    }

    [TestMethod]
    public async Task HealthEndpoint_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task HealthEndpoint_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/health");

        Assert.IsTrue(response.IsSuccessStatusCode);
    }

    [TestMethod]
    public async Task RootEndpoint_ContentTypeIsPlainText()
    {
        var response = await _client.GetAsync("/");

        Assert.IsNotNull(response.Content.Headers.ContentType);
        Assert.IsTrue(response.Content.Headers.ContentType.MediaType?.Contains("text/plain") ?? false);
    }

    [TestMethod]
    public async Task HealthEndpoint_HasCorrectStatusCode()
    {
        var response = await _client.GetAsync("/health");

        Assert.AreEqual(200, (int)response.StatusCode);
    }

    [TestMethod]
    public async Task MultipleRequests_AllSucceed()
    {
        for (int i = 0; i < 5; i++)
        {
            var response = await _client.GetAsync("/");
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [TestMethod]
    public async Task InvalidEndpoint_Returns404()
    {
        var response = await _client.GetAsync("/nonexistent");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }
}
