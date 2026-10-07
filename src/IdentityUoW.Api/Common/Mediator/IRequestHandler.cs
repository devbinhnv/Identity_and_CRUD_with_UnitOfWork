namespace IdentityUoW.Api.Common.Mediator;

/// <remarks>Handlers only change tracked state. They never call SaveChanges.</remarks>
public interface IRequestHandler<in TRequest, TOutput>
    where TRequest : IRequest<TOutput>
{
    Task<TOutput> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
