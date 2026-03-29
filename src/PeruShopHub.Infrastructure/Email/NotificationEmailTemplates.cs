using PeruShopHub.Core.Enums;

namespace PeruShopHub.Infrastructure.Email;

public static class NotificationEmailTemplates
{
    public static NotificationType? MapNotificationType(string type)
    {
        return type switch
        {
            "new_sale" or "sale" => NotificationType.NewSale,
            "stock" or "stock_alert" => NotificationType.LowStock,
            "margin" or "margin_alert" => NotificationType.MarginAlert,
            "token_renewal_failed" or "connection" or "connection_error" => NotificationType.MLConnectionError,
            "product_sync_errors" or "stock_reconciliation" or "sync_error" or "shipping_update" => NotificationType.SyncError,
            "unanswered_question" => NotificationType.UnansweredQuestion,
            "unanswered_message" => NotificationType.UnansweredMessage,
            _ => null
        };
    }

    public static string BuildEmailBody(string title, string description, string? navigationTarget, string baseUrl)
    {
        var body = EmailTemplateBuilder.Heading(title);
        body += EmailTemplateBuilder.Paragraph(description);

        if (!string.IsNullOrEmpty(navigationTarget))
        {
            var fullUrl = $"{baseUrl.TrimEnd('/')}{navigationTarget}";
            body += EmailTemplateBuilder.Button("Ver Detalhes", fullUrl);
        }

        body += BuildUnsubscribeFooter(baseUrl);

        return EmailTemplateBuilder.BuildLayout($"PeruShopHub — {title}", body);
    }

    private static string BuildUnsubscribeFooter(string baseUrl)
    {
        var settingsUrl = $"{baseUrl.TrimEnd('/')}/configuracoes?tab=notificacoes";
        return $@"<div style='margin-top:32px;padding-top:16px;border-top:1px solid #eee;text-align:center'>
  <p style='font-size:12px;color:#999;margin:0'>
    Você pode gerenciar suas preferências de notificação a qualquer momento.
  </p>
  <p style='font-size:12px;margin:4px 0 0'>
    <a href='{settingsUrl}' style='color:#1A237E;text-decoration:underline'>Cancelar inscrição / Gerenciar notificações</a>
  </p>
</div>";
    }
}
