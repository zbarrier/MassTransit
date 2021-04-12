﻿using MassTransit.Configuration;
using MassTransit.EventStoreDbIntegration.Serializers;
using MassTransit.Transports;

namespace MassTransit.EventStoreDbIntegration.Contexts
{
    public class ProcessorContextSupervisor :
        TransportPipeContextSupervisor<ProcessorContext>,
        IProcessorContextSupervisor
    {
        public ProcessorContextSupervisor(IConnectionContextSupervisor supervisor, IHostConfiguration hostConfiguration, ReceiveSettings receiveSettings,
            IHeadersDeserializer headersDeserializer)
            : base(new ProcessorContextFactory(supervisor, hostConfiguration, receiveSettings, headersDeserializer))
        {
            supervisor.AddConsumeAgent(this);
        }
    }
}
