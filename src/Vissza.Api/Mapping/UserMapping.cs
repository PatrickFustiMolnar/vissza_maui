using Vissza.Api.Entities;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;

namespace Vissza.Api.Mapping;

public static class UserMapping
{
    /// <summary>
    /// Entitás -> DTO. Az egyetlen út, ahogy felhasználó kikerül az API-ból,
    /// így a password_hash nem tud véletlenül kiszivárogni.
    /// </summary>
    public static UserDto ToDto(this User user, ImageUrlService imageUrls) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email,
        Phone = user.Phone,
        ProfileImage = imageUrls.ToAbsolute(user.ProfileImage),
        UserRole = user.UserRole,
        Bio = user.Bio,
        DefaultAddress = user.DefaultAddress,
        DefaultLat = user.DefaultLat,
        DefaultLng = user.DefaultLng,
        AverageRating = user.AverageRating,
        TotalRatings = user.TotalRatings,
        SuccessfulDonations = user.SuccessfulDonations,
        SuccessfulCollections = user.SuccessfulCollections,
        NotificationsEnabled = user.NotificationsEnabled,
        NotificationRadius = user.NotificationRadius,
        DarkMode = user.DarkMode,
        CreatedAt = user.CreatedAt,
        LastActivity = user.LastActivity
    };
}
