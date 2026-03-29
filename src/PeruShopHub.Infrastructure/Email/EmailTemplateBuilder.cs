namespace PeruShopHub.Infrastructure.Email;

/// <summary>
/// Builds branded HTML email templates with PeruShopHub styling.
/// </summary>
public static class EmailTemplateBuilder
{
    private const string PrimaryColor = "#1A237E";
    private const string AccentColor = "#FF6F00";

    /// <summary>
    /// Wraps content in the standard PeruShopHub email layout.
    /// </summary>
    public static string BuildLayout(string title, string bodyHtml)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8' />
  <meta name='viewport' content='width=device-width, initial-scale=1.0' />
  <title>{title}</title>
</head>
<body style='font-family:Inter,Arial,sans-serif;color:#1a1a2e;margin:0;padding:0;background:#f5f5f5'>
  <div style='max-width:600px;margin:0 auto;padding:20px'>
    <div style='background:{PrimaryColor};color:white;padding:24px;border-radius:8px 8px 0 0;text-align:center'>
      <h1 style='margin:0;font-size:22px;font-weight:700'>PeruShopHub</h1>
    </div>
    <div style='background:white;padding:32px;border-radius:0 0 8px 8px'>
      {bodyHtml}
    </div>
    <div style='text-align:center;padding:16px;font-size:12px;color:#999'>
      <p style='margin:0'>Este email foi enviado automaticamente pelo PeruShopHub.</p>
      <p style='margin:4px 0 0'>Não responda a este email.</p>
    </div>
  </div>
</body>
</html>";
    }

    /// <summary>
    /// Creates a styled CTA button.
    /// </summary>
    public static string Button(string text, string url)
    {
        return $@"<div style='text-align:center;margin:24px 0'>
  <a href='{url}' style='display:inline-block;background:{AccentColor};color:white;text-decoration:none;padding:12px 32px;border-radius:6px;font-weight:600;font-size:14px'>{text}</a>
</div>";
    }

    /// <summary>
    /// Creates a section heading.
    /// </summary>
    public static string Heading(string text)
    {
        return $"<h2 style='margin:24px 0 12px;font-size:18px;color:{PrimaryColor}'>{text}</h2>";
    }

    /// <summary>
    /// Creates a paragraph.
    /// </summary>
    public static string Paragraph(string text)
    {
        return $"<p style='margin:0 0 16px;line-height:1.6;font-size:14px;color:#333'>{text}</p>";
    }

    /// <summary>
    /// Creates a bulleted list from items.
    /// </summary>
    public static string List(params string[] items)
    {
        var listItems = string.Join("", items.Select(i => $"<li style='margin:4px 0;line-height:1.6;font-size:14px;color:#333'>{i}</li>"));
        return $"<ul style='margin:0 0 16px;padding-left:20px'>{listItems}</ul>";
    }
}
