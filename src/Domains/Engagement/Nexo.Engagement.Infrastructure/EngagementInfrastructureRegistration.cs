using Microsoft.Extensions.DependencyInjection;

namespace Nexo.Engagement.Infrastructure;

public static class EngagementInfrastructureRegistration
{
    /// <summary>Adaptadores de Engagement; recibe la base keyed y nunca un cliente global.</summary>
    public static IServiceCollection AddEngagementInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<Nexo.Engagement.Contracts.IInterestSummaryContract, InterestSummaryContract>();
        services.AddSingleton<Nexo.Engagement.Application.Interests.IInterestStore, MongoInterestStore>();
        services.AddScoped<Nexo.Engagement.Application.Interests.InterestService>();
        services.AddSingleton<Nexo.Engagement.Application.Orders.IOrderStore, MongoOrderStore>();
        services.AddScoped<Nexo.Common.Accounts.IAccountDataLifecycle, EngagementAccountLifecycle>();
        services.AddSingleton<IOrderAddressKeyWrapper, AzureOrderAddressKeyWrapper>();
        services.AddSingleton<Nexo.Engagement.Application.Orders.IOrderAddressProtection, OrderAddressProtection>();
        services.AddScoped<Nexo.Engagement.Application.Orders.OrderService>();
        services.AddSingleton<Nexo.Engagement.Application.Reviews.IReviewStore, MongoReviewStore>();
        services.AddScoped<Nexo.Engagement.Application.Reviews.ReviewService>();
        services.AddSingleton<Nexo.Engagement.Application.Conversations.IConversationStore, MongoConversationStore>();
        services.AddScoped<Nexo.Engagement.Application.Conversations.ConversationService>();
        services.AddSingleton<Nexo.Engagement.Application.Conversations.IChatKeyStore, MongoChatKeyStore>();
        services.AddScoped<Nexo.Engagement.Application.Conversations.ChatKeyService>();
        services.AddScoped<Nexo.Engagement.Application.Conversations.EncryptedChatService>();
        services.AddSingleton<Nexo.Engagement.Application.Conversations.IPrivateChatAttachments, AzurePrivateChatAttachments>();
        services.AddSingleton<Nexo.Engagement.Application.Conversations.IChatAttachmentOwners, MongoChatAttachmentOwners>();
        services.AddSingleton<Nexo.Engagement.Application.Safety.IContactReportStore, MongoContactReportStore>();
        services.AddScoped<Nexo.Engagement.Application.Safety.ContactReportService>();
        services.AddSingleton<Nexo.Engagement.Application.Notifications.INotificationStore, Notifications.MongoNotificationStore>();
        services.AddSingleton<Nexo.Engagement.Application.Notifications.IPushSender, Notifications.FcmPushSender>();
        services.AddScoped<Nexo.Engagement.Application.Notifications.NotificationService>();
        services.AddScoped<Notifications.EngagementOutboxHandler>();
        return services;
    }
}
