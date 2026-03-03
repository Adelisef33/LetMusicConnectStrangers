using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Xunit;
using LetMusicConnectStrangers.Data;
using LetMusicConnectStrangers.Models;
using LetMusicConnectStrangers.Pages;
using LetMusicConnectStrangers.Services;
using SpotifyAPI.Web;

namespace LetMusicConnectStrangers.Tests.Pages
{
    public class ReviewsModelTests
    {
        #region Input Validation Tests

        [Fact]
        public void InputModel_WithValidData_PassesValidation()
        {
            // Given: Input with all required fields
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 4,
                Comment = "Great song!"
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: No validation errors
            Assert.Empty(validationResults);
        }

        [Fact]
        public void InputModel_WithMissingSpotifyTrackId_FailsValidation()
        {
            // Given: Input with empty SpotifyTrackId
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = string.Empty,
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 4
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: Validation fails with correct error
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a song");
        }

        [Fact]
        public void InputModel_WithNullSpotifyTrackId_FailsValidation()
        {
            // Given: Input with null SpotifyTrackId
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = null!,
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 4
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: Validation fails with correct error
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a song");
        }

        [Fact]
        public void InputModel_WithRatingZero_FailsValidation()
        {
            // Given: Input with rating of zero
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 0
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: Validation fails with correct error
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a rating between 1 and 5");
        }

        [Fact]
        public void InputModel_WithRatingBelowRange_FailsValidation()
        {
            // Given: Input with negative rating
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = -1
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: Validation fails with correct error
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a rating between 1 and 5");
        }

        [Fact]
        public void InputModel_WithRatingAboveRange_FailsValidation()
        {
            // Given: Input with rating above 5
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 6
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: Validation fails with correct error
            Assert.NotEmpty(validationResults);
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a rating between 1 and 5");
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void InputModel_WithValidRating_PassesValidation(int rating)
        {
            // Given: Input with valid rating (1-5)
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = rating
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: No validation errors
            Assert.Empty(validationResults);
        }

        [Fact]
        public void InputModel_WithNullComment_PassesValidation()
        {
            // Given: Input without comment (optional field)
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 4,
                Comment = null
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: No validation errors (comment is optional)
            Assert.Empty(validationResults);
        }

        [Fact]
        public void InputModel_WithEmptyComment_PassesValidation()
        {
            // Given: Input with empty comment (optional field)
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 4,
                Comment = string.Empty
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: No validation errors (comment is optional)
            Assert.Empty(validationResults);
        }

        [Fact]
        public void InputModel_WithMultipleValidationErrors_ReturnsAllErrors()
        {
            // Given: Input missing multiple required fields
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = string.Empty,
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 0
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: Both validation errors are returned
            Assert.Equal(2, validationResults.Count);
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a song");
            Assert.Contains(validationResults, v => v.ErrorMessage == "Please select a rating between 1 and 5");
        }

        [Fact]
        public void InputModel_WithOptionalFields_PassesValidation()
        {
            // Given: Input with only required fields
            var input = new ReviewsModel.InputModel
            {
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                Rating = 3,
                AlbumName = null,
                AlbumImageUrl = null,
                Comment = null
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: No validation errors
            Assert.Empty(validationResults);
        }

        [Fact]
        public void InputModel_WithAllFieldsPopulated_PassesValidation()
        {
            // Given: Input with all fields populated
            var input = new ReviewsModel.InputModel
            {
                ReviewId = 1,
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                AlbumName = "Test Album",
                AlbumImageUrl = "https://example.com/image.jpg",
                Rating = 5,
                Comment = "Amazing track!"
            };

            // When: Validating the input model
            var validationResults = ValidateModel(input);

            // Then: No validation errors
            Assert.Empty(validationResults);
        }

        #endregion

        #region Spotify API Integration Tests

        [Fact]
        public async Task OnPostSearchAsync_CallsSpotifySearchTracksAPI()
        {
            // Given: A page model with mocked Spotify service
            var (pageModel, mockSpotifyService, mockUserManager) = CreatePageModelWithMocks();
            var testUser = CreateTestUser();
            
            // Mock user authentication
            SetupUserManagerMock(mockUserManager, testUser);
            SetupPageContext(pageModel, testUser);
            
            // Setup Spotify API response
            var expectedTracks = CreateMockFullTracks(3);
            mockSpotifyService
                .Setup(s => s.SearchTracks(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(expectedTracks);
            
            pageModel.SearchQuery = "Test Song";

            // When: Performing a search
            await pageModel.OnPostSearchAsync();

            // Then: Spotify SearchTracks API should be called exactly once with correct parameters
            mockSpotifyService.Verify(
                s => s.SearchTracks(
                    It.Is<ApplicationUser>(u => u.Id == testUser.Id),
                    It.Is<string>(q => q == "Test Song"),
                    It.Is<int>(limit => limit == 20)
                ),
                Times.Once,
                "SpotifyService.SearchTracks should be called once with the search query"
            );
        }

        [Fact]
        public async Task OnGetSearchTracksAsync_CallsSpotifySearchTracksAPI()
        {
            // Given: A page model with mocked Spotify service
            var (pageModel, mockSpotifyService, mockUserManager) = CreatePageModelWithMocks();
            var testUser = CreateTestUser();
            
            // Mock user authentication
            SetupUserManagerMock(mockUserManager, testUser);
            SetupPageContext(pageModel, testUser);
            
            // Setup Spotify API response
            var expectedTracks = CreateMockFullTracks(5);
            mockSpotifyService
                .Setup(s => s.SearchTracks(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(expectedTracks);

            // When: Calling the search endpoint
            var result = await pageModel.OnGetSearchTracksAsync("Bohemian Rhapsody");

            // Then: Spotify SearchTracks API should be called exactly once
            mockSpotifyService.Verify(
                s => s.SearchTracks(
                    It.IsAny<ApplicationUser>(),
                    It.Is<string>(q => q == "Bohemian Rhapsody"),
                    It.Is<int>(limit => limit == 20)
                ),
                Times.Once,
                "SpotifyService.SearchTracks should be called once"
            );
            
            // And: Result should be a JsonResult with data
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public async Task OnPostOpenFormAsync_CallsSpotifyRecentlyPlayedAndTopTracksAPIs()
        {
            // Given: A page model with mocked Spotify service
            var (pageModel, mockSpotifyService, mockUserManager) = CreatePageModelWithMocks();
            var testUser = CreateTestUser();
            
            // Mock user authentication
            SetupUserManagerMock(mockUserManager, testUser);
            SetupPageContext(pageModel, testUser);
            
            // Setup Spotify API responses
            var recentTracks = CreateMockPlayHistoryItems(10);
            var topTracks = CreateMockFullTracks(10);
            
            mockSpotifyService
                .Setup(s => s.GetRecentlyPlayed(It.IsAny<ApplicationUser>(), It.IsAny<int>()))
                .ReturnsAsync(recentTracks);
            
            mockSpotifyService
                .Setup(s => s.GetUserTopTracks(It.IsAny<ApplicationUser>(), It.IsAny<int>()))
                .ReturnsAsync(topTracks);

            // When: Opening the form
            await pageModel.OnPostOpenFormAsync();

            // Then: Both Spotify APIs should be called
            mockSpotifyService.Verify(
                s => s.GetRecentlyPlayed(
                    It.Is<ApplicationUser>(u => u.Id == testUser.Id),
                    It.Is<int>(limit => limit == 10)
                ),
                Times.Once,
                "SpotifyService.GetRecentlyPlayed should be called once"
            );
            
            mockSpotifyService.Verify(
                s => s.GetUserTopTracks(
                    It.Is<ApplicationUser>(u => u.Id == testUser.Id),
                    It.Is<int>(limit => limit == 10)
                ),
                Times.Once,
                "SpotifyService.GetUserTopTracks should be called once"
            );
        }

        [Fact]
        public async Task OnGetAsync_WithEditId_CallsSpotifyAPIs()
        {
            // Given: A page model with mocked Spotify service and an existing review
            var (pageModel, mockSpotifyService, mockUserManager) = CreatePageModelWithMocks();
            var testUser = CreateTestUser();
            var existingReview = CreateTestReview(testUser.Id);
            
            // Add review to in-memory database
            var context = pageModel.GetType()
                .GetField("_context", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(pageModel) as LetMusicConnectStrangersContext;
            
            if (context != null)
            {
                context.Reviews.Add(existingReview);
                await context.SaveChangesAsync();
            }
            
            // Mock user authentication
            SetupUserManagerMock(mockUserManager, testUser);
            SetupPageContext(pageModel, testUser);
            
            // Setup Spotify API responses
            var recentTracks = CreateMockPlayHistoryItems(10);
            var topTracks = CreateMockFullTracks(10);
            
            mockSpotifyService
                .Setup(s => s.GetRecentlyPlayed(It.IsAny<ApplicationUser>(), It.IsAny<int>()))
                .ReturnsAsync(recentTracks);
            
            mockSpotifyService
                .Setup(s => s.GetUserTopTracks(It.IsAny<ApplicationUser>(), It.IsAny<int>()))
                .ReturnsAsync(topTracks);
            
            pageModel.EditId = existingReview.ReviewId;

            // When: Loading the page with EditId
            await pageModel.OnGetAsync();

            // Then: Spotify APIs should be called to load track options
            mockSpotifyService.Verify(
                s => s.GetRecentlyPlayed(It.IsAny<ApplicationUser>(), It.IsAny<int>()),
                Times.Once,
                "SpotifyService.GetRecentlyPlayed should be called when editing"
            );
            
            mockSpotifyService.Verify(
                s => s.GetUserTopTracks(It.IsAny<ApplicationUser>(), It.IsAny<int>()),
                Times.Once,
                "SpotifyService.GetUserTopTracks should be called when editing"
            );
        }

        [Fact]
        public async Task OnPostSearchAsync_WithEmptyQuery_DoesNotCallSpotifyAPI()
        {
            // Given: A page model with mocked Spotify service
            var (pageModel, mockSpotifyService, mockUserManager) = CreatePageModelWithMocks();
            var testUser = CreateTestUser();
            
            // Mock user authentication
            SetupUserManagerMock(mockUserManager, testUser);
            SetupPageContext(pageModel, testUser);
            
            // Setup empty responses for the tracks that are always loaded
            mockSpotifyService
                .Setup(s => s.GetRecentlyPlayed(It.IsAny<ApplicationUser>(), It.IsAny<int>()))
                .ReturnsAsync(new List<PlayHistoryItem>());
            
            mockSpotifyService
                .Setup(s => s.GetUserTopTracks(It.IsAny<ApplicationUser>(), It.IsAny<int>()))
                .ReturnsAsync(new List<FullTrack>());
            
            pageModel.SearchQuery = "   "; // Whitespace only

            // When: Performing a search with empty query
            await pageModel.OnPostSearchAsync();

            // Then: Spotify SearchTracks should NOT be called
            mockSpotifyService.Verify(
                s => s.SearchTracks(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Never,
                "SpotifyService.SearchTracks should NOT be called with empty query"
            );
        }

        [Fact]
        public async Task OnGetSearchTracksAsync_WithNullQuery_DoesNotCallSpotifyAPI()
        {
            // Given: A page model with mocked Spotify service
            var (pageModel, mockSpotifyService, mockUserManager) = CreatePageModelWithMocks();
            var testUser = CreateTestUser();
            
            // Mock user authentication
            SetupUserManagerMock(mockUserManager, testUser);
            SetupPageContext(pageModel, testUser);

            // When: Calling search with null query
            var result = await pageModel.OnGetSearchTracksAsync(null);

            // Then: Spotify SearchTracks should NOT be called
            mockSpotifyService.Verify(
                s => s.SearchTracks(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<int>()),
                Times.Never,
                "SpotifyService.SearchTracks should NOT be called with null query"
            );
            
            // And: Should return empty result
            var jsonResult = Assert.IsType<JsonResult>(result);
            var resultList = Assert.IsType<List<object>>(jsonResult.Value);
            Assert.Empty(resultList);
        }

        #endregion

        #region Helper Methods

        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        private (ReviewsModel pageModel, Mock<SpotifyService> mockSpotifyService, Mock<UserManager<ApplicationUser>> mockUserManager) CreatePageModelWithMocks()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<LetMusicConnectStrangersContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new LetMusicConnectStrangersContext(options);

            // Mock UserManager
            var mockUserStore = new Mock<IUserStore<ApplicationUser>>();
            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            // Mock SpotifyService
            var mockSpotifyService = new Mock<SpotifyService>(
                MockBehavior.Loose,
                null, // IHttpClientFactory
                null  // IConfiguration
            );

            // Mock Logger
            var mockLogger = new Mock<ILogger<ReviewsModel>>();

            // Create page model
            var pageModel = new ReviewsModel(
                context,
                mockUserManager.Object,
                mockSpotifyService.Object,
                mockLogger.Object
            );

            return (pageModel, mockSpotifyService, mockUserManager);
        }

        private ApplicationUser CreateTestUser()
        {
            return new ApplicationUser
            {
                Id = "test-user-123",
                UserName = "testuser@example.com",
                Email = "testuser@example.com",
                SpotifyAccessToken = "mock-access-token",
                SpotifyRefreshToken = "mock-refresh-token",
                SpotifyTokenExpiration = DateTime.UtcNow.AddHours(1)
            };
        }

        private Review CreateTestReview(string userId)
        {
            return new Review
            {
                ReviewId = 1,
                UserId = userId,
                SpotifyTrackId = "abc123",
                TrackName = "Test Song",
                ArtistName = "Test Artist",
                AlbumName = "Test Album",
                Rating = 4,
                Comment = "Great song!",
                CreatedAt = DateTime.UtcNow
            };
        }

        private void SetupUserManagerMock(Mock<UserManager<ApplicationUser>> mockUserManager, ApplicationUser user)
        {
            mockUserManager
                .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(user);
        }

        private void SetupPageContext(ReviewsModel pageModel, ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName!)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            pageModel.PageContext = new PageContext
            {
                HttpContext = httpContext
            };
        }

        private List<FullTrack> CreateMockFullTracks(int count)
        {
            var tracks = new List<FullTrack>();
            for (int i = 0; i < count; i++)
            {
                var track = new FullTrack
                {
                    Id = $"track-{i}",
                    Name = $"Test Song {i}",
                    Artists = new List<SimpleArtist>
                    {
                        new SimpleArtist { Name = $"Artist {i}" }
                    },
                    Album = new SimpleAlbum
                    {
                        Name = $"Album {i}",
                        Images = new List<Image>
                        {
                            new Image { Url = $"https://example.com/image{i}.jpg" }
                        }
                    }
                };
                tracks.Add(track);
            }
            return tracks;
        }

        private List<PlayHistoryItem> CreateMockPlayHistoryItems(int count)
        {
            var items = new List<PlayHistoryItem>();
            for (int i = 0; i < count; i++)
            {
                var item = new PlayHistoryItem
                {
                    Track = new FullTrack
                    {
                        Id = $"recent-track-{i}",
                        Name = $"Recent Song {i}",
                        Artists = new List<SimpleArtist>
                        {
                            new SimpleArtist { Name = $"Recent Artist {i}" }
                        },
                        Album = new SimpleAlbum
                        {
                            Name = $"Recent Album {i}",
                            Images = new List<Image>
                            {
                                new Image { Url = $"https://example.com/recent{i}.jpg" }
                            }
                        }
                    },
                    PlayedAt = DateTime.UtcNow.AddHours(-i)
                };
                items.Add(item);
            }
            return items;
        }

        #endregion
    }
}