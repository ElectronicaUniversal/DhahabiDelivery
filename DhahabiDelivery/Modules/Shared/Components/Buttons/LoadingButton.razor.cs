using Microsoft.AspNetCore.Components;

namespace DhahabiDelivery.Modules.Shared.Components.Buttons;

public partial class LoadingButton
{
    public enum State
    {
        Normal,
        Loading,
        Error,
        Success
    }

    [Parameter] public State ButtonState { get; set; } = State.Normal;
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public EventCallback OnClick { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? InputAttributes { get; set; }

    private string GetButtonClass()
    {
        return ButtonState switch
        {
            State.Success => "success",
            State.Loading => "loading",
            State.Normal => "normal",
            State.Error => "error",
            _ => "error"
        };
    }
}