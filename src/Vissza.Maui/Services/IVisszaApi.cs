using Refit;
using Vissza.Shared.Dtos;

namespace Vissza.Maui.Services;

/// <summary>
/// Az API teljes felülete, tipizáltan. A DTO-k a Vissza.Shared projektből
/// jönnek - ugyanazok, amiket a szerver használ, tehát egy elgépelt mezőnév
/// itt fordítási hiba, nem futásidejű 500-as.
///
/// A query paraméterek AliasAs-t kapnak, mert a szerver snake_case neveket
/// vár, a C# tulajdonságnevek viszont PascalCase-ek.
/// </summary>
public interface IVisszaApi
{
    // --- auth (4) ---

    [Post("/api/auth/register")]
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    [Post("/api/auth/login")]
    Task<AuthResponse> LoginAsync(LoginRequest request);

    [Get("/api/auth/me")]
    Task<UserDto> GetMeAsync();

    [Put("/api/auth/me")]
    Task<UserDto> UpdateMeAsync(UpdateProfileRequest request);

    // --- offers (5) ---

    [Get("/api/offers")]
    Task<IReadOnlyList<OfferDto>> GetOffersAsync(
        [AliasAs("status")] string? status = null,
        [AliasAs("donor_id")] int? donorId = null,
        [AliasAs("bottle_type")] string? bottleType = null,
        [AliasAs("min_quantity")] int? minQuantity = null);

    [Get("/api/offers/{id}")]
    Task<OfferDto> GetOfferAsync(int id);

    [Post("/api/offers")]
    Task<OfferDto> CreateOfferAsync(CreateOfferRequest request);

    [Put("/api/offers/{id}")]
    Task<OfferDto> UpdateOfferAsync(int id, UpdateOfferRequest request);

    [Delete("/api/offers/{id}")]
    Task<MessageResponse> DeleteOfferAsync(int id);

    // --- collection requests (3) ---

    [Get("/api/collection-requests")]
    Task<IReadOnlyList<CollectionRequestDto>> GetCollectionRequestsAsync(
        [AliasAs("offer_id")] int? offerId = null,
        [AliasAs("collector_id")] int? collectorId = null,
        [AliasAs("status")] string? status = null);

    [Post("/api/collection-requests")]
    Task<CollectionRequestDto> CreateCollectionRequestAsync(CreateCollectionRequestRequest request);

    [Put("/api/collection-requests/{id}")]
    Task<CollectionRequestDto> UpdateCollectionRequestAsync(int id, UpdateCollectionRequestRequest request);

    // --- transactions (4) ---

    [Get("/api/transactions")]
    Task<IReadOnlyList<TransactionDto>> GetTransactionsAsync(
        [AliasAs("offer_id")] int? offerId = null,
        [AliasAs("donor_id")] int? donorId = null,
        [AliasAs("collector_id")] int? collectorId = null,
        [AliasAs("status")] string? status = null);

    [Get("/api/transactions/{id}")]
    Task<TransactionDto> GetTransactionAsync(int id);

    [Post("/api/transactions")]
    Task<TransactionDto> CreateTransactionAsync(CreateTransactionRequest request);

    [Put("/api/transactions/{id}")]
    Task<TransactionDto> UpdateTransactionAsync(int id, UpdateTransactionRequest request);

    // --- ratings (5) ---

    [Get("/api/ratings")]
    Task<IReadOnlyList<RatingDto>> GetRatingsAsync(
        [AliasAs("rated_id")] int? ratedId = null,
        [AliasAs("rater_id")] int? raterId = null,
        [AliasAs("transaction_id")] int? transactionId = null);

    [Get("/api/ratings/{id}")]
    Task<RatingDto> GetRatingAsync(int id);

    [Post("/api/ratings")]
    Task<RatingDto> CreateRatingAsync(CreateRatingRequest request);

    [Put("/api/ratings/{id}")]
    Task<RatingDto> UpdateRatingAsync(int id, UpdateRatingRequest request);

    [Delete("/api/ratings/{id}")]
    Task<MessageResponse> DeleteRatingAsync(int id);

    // --- messages (6) ---

    [Get("/api/messages")]
    Task<IReadOnlyList<ChatMessageDto>> GetMessagesAsync(
        [AliasAs("offer_id")] int? offerId = null,
        [AliasAs("sender_id")] int? senderId = null,
        [AliasAs("receiver_id")] int? receiverId = null,
        [AliasAs("partner_id")] int? partnerId = null);

    [Post("/api/messages")]
    Task<ChatMessageDto> SendMessageAsync(SendMessageRequest request);

    [Get("/api/messages/unread-count")]
    Task<UnreadCountResponse> GetUnreadCountAsync();

    [Get("/api/messages/conversations")]
    Task<IReadOnlyList<ConversationDto>> GetConversationsAsync();

    [Put("/api/messages/{id}/read")]
    Task<ChatMessageDto> MarkMessageReadAsync(int id);

    [Delete("/api/messages/{id}")]
    Task<MessageResponse> DeleteMessageAsync(int id);

    // --- users (1) ---

    [Get("/api/users/{id}")]
    Task<UserProfileDto> GetUserAsync(int id);

    // --- return locations (2) ---

    [Get("/api/return-locations")]
    Task<IReadOnlyList<ReturnLocationDto>> GetReturnLocationsAsync(
        [AliasAs("type")] string? type = null);

    [Get("/api/return-locations/{id}")]
    Task<ReturnLocationDto> GetReturnLocationAsync(int id);

    // --- upload (1) ---

    [Multipart]
    [Post("/api/upload")]
    Task<UploadResponse> UploadAsync([AliasAs("file")] StreamPart file);
}
