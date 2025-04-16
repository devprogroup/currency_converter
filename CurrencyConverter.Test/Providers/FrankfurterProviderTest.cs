using CurrencyConverter.DTOs;
using CurrencyConverter.Porviders;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Moq;
using System.Net;
using System.Text.Json;

public class FrankfurterProviderTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<ILogger<IExchangeRateProvider>> _loggerMock;
    private readonly FrankfurterProvider _provider;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;

    public FrankfurterProviderTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _cacheMock = new Mock<IMemoryCache>();
        _loggerMock = new Mock<ILogger<IExchangeRateProvider>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        var client = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://api.frankfurter.app")
        };

        _httpClientFactoryMock.Setup(f => f.CreateClient("frankfurter")).Returns(client);

        _provider = new FrankfurterProvider(_httpClientFactoryMock.Object, _cacheMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldReturnFromCache_IfExists()
    {
        // Arrange
        var expected = new LatestExchangeRateResponseDto
        {
            Base = "USD",
            Date = DateTime.Today,
            Rates = new Dictionary<string, decimal> { { "EUR", 0.9m } }
        };

        object cached = expected;

        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out cached)).Returns(true);

        // Act
        var result = await _provider.GetLatestRatesAsync("USD");

        // Assert
        Assert.Equal(expected.Base, result.Base);
        Assert.Equal(expected.Rates["EUR"], result.Rates["EUR"]);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldFetchAndCache_IfNotInCache()
    {
        // Arrange
        var responseDto = new LatestExchangeRateResponseDto
        {
            Base = "USD",
            Date = DateTime.Today,
            Rates = new Dictionary<string, decimal> { { "EUR", 0.9m } }
        };

        var json = JsonSerializer.Serialize(responseDto);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        object dummy;
        _cacheMock.Setup(c => c.TryGetValue(It.IsAny<object>(), out dummy)).Returns(false);

        // Mock CreateEntry to return a mocked cache entry
        var cacheEntryMock = new Mock<ICacheEntry>();
        object cachedValue = null;

        // Capture the value being set into cache
        cacheEntryMock.SetupSet(m => m.Value = It.IsAny<object>())
                      .Callback<object>(val => cachedValue = val);

        cacheEntryMock.SetupAllProperties();
        _cacheMock.Setup(c => c.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

        // Act
        var result = await _provider.GetLatestRatesAsync("USD");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("USD", result.Base);
        Assert.True(result.Rates.ContainsKey("EUR"));
    }

    [Fact]
    public async Task ConvertCurrencyAsync_ShouldReturnConvertedAmount()
    {
        // Arrange
        var expected = new ConvertCurrencyResponseDto
        {
            Base = "USD",
            Amount = 100,
            Rates = new Dictionary<string, decimal> { { "EUR", 90 } },
            Date = DateTime.Today
        };

        var json = JsonSerializer.Serialize(expected);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _provider.ConvertCurrencyAsync("USD", "EUR", 100);

        // Assert
        Assert.Equal(100, result.Amount);
        Assert.Equal("USD", result.Base);
        Assert.Equal(90, result.Rates["EUR"]);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldReturnPagedRates()
    {
        // Arrange
        var rawRates = new Dictionary<string, Dictionary<string, decimal>>
        {
            { "2025-01-01", new Dictionary<string, decimal> { { "EUR", 0.85m } } },
            { "2025-01-02", new Dictionary<string, decimal> { { "EUR", 0.86m } } }
        };

        var fullResponse = new HistoricalRatesResponseDto
        {
            Base = "USD",
            Amount = 1,
            Start_Date = DateTime.Today.AddDays(-10),
            End_Date = DateTime.Today,
            Rates = rawRates
        };

        var json = JsonSerializer.Serialize(fullResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        object outObj = null; // For simulating cache miss
        _cacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out outObj)).Returns(false);

        // Setup CreateEntry to allow storing cache entries
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupAllProperties(); // Allows .Value and expiration to be set
        _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(cacheEntryMock.Object);

        // Mock the HttpMessageHandler to simulate HTTP response
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _provider.GetHistoricalRatesAsync("USD", new DateTime(2025, 1, 1), new DateTime(2025, 1, 2), 1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Rates);
        Assert.Equal(0.85m, result.Rates["2025-01-01"]["EUR"]);
    }
}
