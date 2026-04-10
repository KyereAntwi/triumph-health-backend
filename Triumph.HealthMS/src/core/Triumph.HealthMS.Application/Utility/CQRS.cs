namespace Triumph.HealthMS.Application.Utility;

public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface IQuery<out TResponse> : IRequest<TResponse>;