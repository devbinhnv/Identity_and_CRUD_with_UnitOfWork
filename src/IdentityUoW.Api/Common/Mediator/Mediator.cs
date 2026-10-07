using System.Reflection;
using IdentityUoW.Api.Common.Models;
using IdentityUoW.Api.Common.Repositories;
using IdentityUoW.Api.Persistence;

namespace IdentityUoW.Api.Common.Mediator;

/// <summary>
/// Resolves the handler of a request and acts as the Unit of Work boundary:
/// <list type="bullet">
/// <item>Query (<see cref="IRequest{TOutput}"/>): just returns the result.</item>
/// <item>Command (<see cref="ICommand{TOutput}"/>) succeeded: commits everything tracked, once.</item>
/// <item>Command failed (<c>ApiResult.IsSucceeded == false</c>) or threw: nothing is committed.</item>
/// </list>
/// The commit happens before the controller writes the HTTP response.
/// </summary>
public sealed class Mediator(IServiceProvider serviceProvider, IUnitOfWork<AppDbContext> unitOfWork) : IMediator
{
    public async Task<TOutput> SendAsync<TOutput>(IRequest<TOutput> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TOutput));
        var handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        var method = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TOutput>, TOutput>.HandleAsync))!;
        var output = await (Task<TOutput>)method.Invoke(handler, [request, cancellationToken])!;

        if (request is ICommand<TOutput> && output is not ApiResult { IsSucceeded: false })
        {
            await unitOfWork.CommitAsync(cancellationToken);
        }

        return output;
    }
}

public static class MediatorExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => (Service: i, Implementation: t)));

        foreach (var (service, implementation) in handlerTypes)
        {
            services.AddScoped(service, implementation);
        }

        services.AddScoped<IMediator, Mediator>();

        return services;
    }
}
