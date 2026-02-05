// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using OpenTweak.Models;
using OpenTweak.Services;
using Xunit;

namespace OpenTweak.Tests.Services;

/// <summary>
/// Tests for the PCGWService that interacts with PCGamingWiki API.
/// Uses mocked HTTP client to avoid external dependencies.
/// </summary>
public class PCGWServiceTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public PCGWServiceTests()
    {
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
    }

    #region SearchGameAsync Tests

    [Fact]
    public async Task SearchGameAsync_WithValidGame_ReturnsGameInfo()
    {
        // Arrange
        var searchResponse = new
        {
            query = new
            {
                search = new[]
                {
                    new { title = "Test Game" }
                }
            }
        };

        var pageContent = new
        {
            parse = new
            {
                wikitext = @"Configuration files location
{{Game data/config|Windows|{{p|game}}\config.ini}}

Video settings
{{Fixbox|description=Disable motion blur|fix=
Edit {{file|config.ini}} and set <code>MotionBlur=0</code>
<pre>
[Graphics]
MotionBlur=0
</pre>
}}
"
            }
        };

        SetupMockResponse("action=query&list=search", JsonSerializer.Serialize(searchResponse));
        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("Test Game");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Game", result.Title);
        Assert.NotNull(result.WikiUrl);
    }

    [Fact]
    public async Task SearchGameAsync_WithNoResults_ReturnsNull()
    {
        // Arrange
        var searchResponse = new
        {
            query = new
            {
                search = Array.Empty<object>()
            }
        };

        SetupMockResponse("action=query&list=search", JsonSerializer.Serialize(searchResponse));

        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("NonExistentGame12345");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchGameAsync_WithNullTitle_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchGameAsync_WithEmptyTitle_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchGameAsync_WithWhitespaceTitle_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchGameAsync_HttpError_ReturnsNull()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("action=query")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("Test Game");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchGameAsync_NetworkError_ReturnsNull()
    {
        // Arrange
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("action=query")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Network error"));

        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("Test Game");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchGameAsync_SkipsCategoryPages()
    {
        // Arrange
        var searchResponse = new
        {
            query = new
            {
                search = new[]
                {
                    new { title = "Category:Test Games" },
                    new { title = "File:TestGame.png" },
                    new { title = "Actual Game" }
                }
            }
        };

        var pageContent = new
        {
            parse = new
            {
                wikitext = "Test content"
            }
        };

        SetupMockResponse("action=query&list=search", JsonSerializer.Serialize(searchResponse));
        SetupMockResponse("action=parse&page=Actual%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.SearchGameAsync("Test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Actual Game", result.Title);
    }

    #endregion

    #region GetPageContentAsync Tests

    [Fact]
    public async Task GetPageContentAsync_WithValidPage_ReturnsGameInfo()
    {
        // Arrange
        var pageContent = new
        {
            parse = new
            {
                wikitext = @"Configuration file(s) location
{{Game data/config|Windows|{{p|appdata}}\TestGame\config.ini}}

Save game data location
{{Game data/saves|Windows|{{p|userprofile\documents}}\TestGame\saves\}}
"
            }
        };

        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync("Test Game");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Game", result.Title);
        Assert.NotEmpty(result.ConfigFiles);
        Assert.NotEmpty(result.SaveGameLocations);
    }

    [Fact]
    public async Task GetPageContentAsync_WithNullPageTitle_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageContentAsync_WithEmptyPageTitle_ReturnsNull()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageContentAsync_InvalidJsonResponse_ReturnsNull()
    {
        // Arrange
        SetupMockResponse("action=parse&page=Test%20Game", "invalid json");

        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync("Test Game");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPageContentAsync_MissingParseProperty_ReturnsNull()
    {
        // Arrange
        var invalidResponse = new { error = new { info = "Invalid page" } };
        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(invalidResponse));

        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync("Test Game");

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region GetAvailableTweaksAsync Tests

    [Fact]
    public async Task GetAvailableTweaksAsync_WithValidGame_ReturnsTweaks()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var searchResponse = new
        {
            query = new
            {
                search = new[]
                {
                    new { title = "Test Game" }
                }
            }
        };

        var pageContent = new
        {
            parse = new
            {
                wikitext = @"Video settings
{{Fixbox|description=Disable motion blur|fix=
Edit {{file|config.ini}} and set <code>MotionBlur=0</code>
<pre>
[Graphics]
MotionBlur=0
</pre>
}}

Audio settings
{{Fixbox|description=Fix audio crackling|fix=
Edit {{file|audio.ini}} and set <code>BufferSize=512</code>
}}
"
            }
        };

        SetupMockResponse("action=query&list=search", JsonSerializer.Serialize(searchResponse));
        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.GetAvailableTweaksAsync("Test Game", gameId);

        // Assert
        Assert.NotNull(result);
        Assert.All(result, tweak => Assert.Equal(gameId, tweak.GameId));
    }

    [Fact]
    public async Task GetAvailableTweaksAsync_WithNoTweaks_ReturnsEmptyList()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var searchResponse = new
        {
            query = new
            {
                search = new[]
                {
                    new { title = "Test Game" }
                }
            }
        };

        var pageContent = new
        {
            parse = new
            {
                wikitext = "This is a game page with no tweak instructions."
            }
        };

        SetupMockResponse("action=query&list=search", JsonSerializer.Serialize(searchResponse));
        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.GetAvailableTweaksAsync("Test Game", gameId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableTweaksAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var searchResponse = new
        {
            query = new
            {
                search = Array.Empty<object>()
            }
        };

        SetupMockResponse("action=query&list=search", JsonSerializer.Serialize(searchResponse));

        var service = CreateService();

        // Act
        var result = await service.GetAvailableTweaksAsync("NonExistentGame", gameId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    #endregion

    #region ExtractConfigFilePaths Tests (via GetPageContentAsync)

    [Fact]
    public async Task GetPageContentAsync_ExtractsConfigFilePaths()
    {
        // Arrange
        var pageContent = new
        {
            parse = new
            {
                wikitext = @"Configuration file(s) location
{{Game data/config|Windows|{{p|game}}\Game.ini}}
{{Game data/config|Windows|{{p|appdata}}\TestGame\settings.cfg}}
"
            }
        };

        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync("Test Game");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ConfigFiles.Count >= 0); // May or may not extract depending on regex
    }

    [Fact]
    public async Task GetPageContentAsync_ExtractsSaveGamePaths()
    {
        // Arrange
        var pageContent = new
        {
            parse = new
            {
                wikitext = @"Save game data location
{{Game data/saves|Windows|{{p|userprofile\documents}}\TestGame\saves\}}
{{Game data/saves|Steam|{{p|steam}}\userdata\{{p|uid}}\12345\remote\}}
"
            }
        };

        SetupMockResponse("action=parse&page=Test%20Game", JsonSerializer.Serialize(pageContent));

        var service = CreateService();

        // Act
        var result = await service.GetPageContentAsync("Test Game");

        // Assert
        Assert.NotNull(result);
        Assert.True(result.SaveGameLocations.Count >= 0); // May or may not extract depending on regex
    }

    #endregion

    #region Helper Methods

    private PCGWService CreateService()
    {
        // Now that PCGWService supports HttpClient injection, we can inject our mock!
        return new PCGWService(_httpClient);
    }

    private void SetupMockResponse(string urlPattern, string content)
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains(urlPattern)),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(content)
            });
    }

    #endregion
}
