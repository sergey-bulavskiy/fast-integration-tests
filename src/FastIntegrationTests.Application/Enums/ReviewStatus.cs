namespace FastIntegrationTests.Application.Enums;

/// <summary>Статус отзыва.</summary>
public enum ReviewStatus
{
    /// <summary>На проверке.</summary>
    Pending = 0,

    /// <summary>Одобрен.</summary>
    Approved = 1,

    /// <summary>Отклонён.</summary>
    Rejected = 2,
}
