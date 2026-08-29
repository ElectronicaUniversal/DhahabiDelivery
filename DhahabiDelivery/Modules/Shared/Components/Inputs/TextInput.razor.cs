using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Shared.Components.Inputs;

public partial class TextInput
{
    private bool _showPassword;
    [Parameter] [EditorRequired] public Expression<Func<string>> ValidationFor { get; set; } = default!;
    [Parameter] public string Placeholder { get; set; } = string.Empty;

    private DataType? InputType { get; set; } = DataType.Text; // Default to text

    private static string GetPropertyNameFromExpression(Expression<Func<string>> expression)
    {
        if (expression.Body is MemberExpression memberExpression) return memberExpression.Member.Name;

        return string.Empty;
    }

    protected override void OnParametersSet()
    {
        var model = EditContext.Model;
        var propertyName = GetPropertyNameFromExpression(ValidationFor);
        var property = model.GetType().GetProperty(propertyName);
        if (property == null) return;
        var attribute = property.GetCustomAttributes(typeof(DataTypeAttribute), false)
            .Cast<DataTypeAttribute>()
            .FirstOrDefault();
        InputType = attribute?.DataType;
    }

    private static string IsValid(string? content)
    {
        return string.IsNullOrEmpty(content) ? "form-input empty-input" : "form-input full-input";
    }

    private void TogglePassword()
    {
        _showPassword = !_showPassword;
    }
}