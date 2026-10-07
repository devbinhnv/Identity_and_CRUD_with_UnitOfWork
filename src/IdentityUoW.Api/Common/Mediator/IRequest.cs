namespace IdentityUoW.Api.Common.Mediator;

/// <summary>Read-only request (query). The mediator never commits for it.</summary>
public interface IRequest<TOutput>
{
}

/// <summary>
/// State-changing request. The mediator commits the Unit of Work once, after the handler
/// returns a successful result: one command = one commit.
/// </summary>
public interface ICommand<TOutput> : IRequest<TOutput>
{
}
