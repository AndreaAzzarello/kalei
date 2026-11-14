using Kalei.Core.Ipc;

namespace Kalei.Worker.Builtin;

/// <summary>Does the work of one request and returns the final frame (an answer or an Error). The Modules plug in here.</summary>
internal interface IRequestHandler
{
    Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken cancellationToken);
}

/// <summary>No engine exists yet (the Modules arrive with later tickets): every request is "not supported".</summary>
internal sealed class UnavailableHandler : IRequestHandler
{
    public Task<IpcMessage> HandleAsync(IpcMessage request, CancellationToken cancellationToken) =>
        Task.FromResult<IpcMessage>(new ErrorMessage { Code = ErrorCode.Format });
}
