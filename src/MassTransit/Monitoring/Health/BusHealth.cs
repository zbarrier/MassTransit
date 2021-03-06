namespace MassTransit.Monitoring.Health
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using EndpointConfigurators;
    using Util;


    public class BusHealth :
        IBusObserver,
        IEndpointConfigurationObserver,
        IBusHealth
    {
        readonly EndpointHealth _endpointHealth;

        string _failureMessage = "not started";
        bool _healthy;

        public BusHealth()
            : this("masstransit-bus")
        {
        }

        public BusHealth(string name)
        {
            Name = name;
            _endpointHealth = new EndpointHealth();
        }

        public string Name { get; }

        public HealthResult CheckHealth()
        {
            (var status, var description, IReadOnlyDictionary<string, EndpointHealthResult> results) = _endpointHealth.CheckHealth();

            var exception = results.Where(x => x.Value.Exception != null).Select(x => x.Value.Exception).FirstOrDefault();

            return _healthy && status == BusHealthStatus.Healthy
                ? HealthResult.Healthy("Ready", results)
                : _healthy && status == BusHealthStatus.Degraded
                    ? HealthResult.Degraded(description, exception, results)
                    : HealthResult.Unhealthy($"Not ready: {_failureMessage}", exception, results);
        }

        Task IBusObserver.CreateFaulted(Exception exception)
        {
            return Failure($"create faulted: {exception.Message}");
        }

        Task IBusObserver.PostCreate(IBus bus)
        {
            return TaskUtil.Completed;
        }

        Task IBusObserver.PostStart(IBus bus, Task<BusReady> busReady)
        {
            return Success();
        }

        Task IBusObserver.StartFaulted(IBus bus, Exception exception)
        {
            return Failure($"start faulted: {exception.Message}");
        }

        Task IBusObserver.PreStop(IBus bus)
        {
            return TaskUtil.Completed;
        }

        Task IBusObserver.PostStop(IBus bus)
        {
            return Failure("stopped");
        }

        Task IBusObserver.StopFaulted(IBus bus, Exception exception)
        {
            return Failure($"stop faulted: {exception.Message}");
        }

        Task IBusObserver.PreStart(IBus bus)
        {
            return TaskUtil.Completed;
        }

        void IEndpointConfigurationObserver.EndpointConfigured<T>(T configurator)
        {
            _endpointHealth.EndpointConfigured(configurator);
        }

        Task Failure(string message)
        {
            _healthy = false;
            _endpointHealth.BusHealthy = false;
            _failureMessage = message;

            return TaskUtil.Completed;
        }

        Task Success()
        {
            _healthy = true;
            _endpointHealth.BusHealthy = true;
            _failureMessage = "";

            return TaskUtil.Completed;
        }
    }
}
