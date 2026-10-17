using Microsoft.AspNetCore.Razor.TagHelpers;
using MotoRent.WebApp.Helpers;

namespace MotoRent.WebApp.TagHelpers;

/// <summary>
/// Hiển thị badge trạng thái có màu + chữ tiếng Việt.
/// Cách dùng: &lt;status-badge value="@Model.Status" kind="motorbike" /&gt;
/// kind: motorbike | maintenance  (Dev 3 thêm "rental" trong DisplayHelper khi làm đơn thuê)
/// </summary>
[HtmlTargetElement("status-badge", TagStructure = TagStructure.WithoutEndTag)]
public class StatusBadgeTagHelper : TagHelper
{
    public string? Value { get; set; }
    public string Kind { get; set; } = "motorbike";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var status = Value ?? "";
        var (text, css) = Kind switch
        {
            "maintenance" => (DisplayHelper.MaintenanceStatusText(status), DisplayHelper.MaintenanceStatusBadge(status)),
            _ => (DisplayHelper.MotorbikeStatusText(status), DisplayHelper.MotorbikeStatusBadge(status))
        };

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", $"badge {css}");
        output.Content.SetContent(text);
    }
}
