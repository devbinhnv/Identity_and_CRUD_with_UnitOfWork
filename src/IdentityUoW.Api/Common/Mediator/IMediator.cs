namespace IdentityUoW.Api.Common.Mediator;

public interface IMediator
{
    Task<TOutput> SendAsync<TOutput>(IRequest<TOutput> request, CancellationToken cancellationToken = default);
}
