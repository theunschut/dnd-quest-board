namespace EuphoriaInn.Domain.Interfaces;

public interface IEmailRenderService
{
    Task<string> RenderAsync<TComponent>(Dictionary<string, object?> parameters)
        where TComponent : Microsoft.AspNetCore.Components.IComponent;
}
