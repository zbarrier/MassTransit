﻿using GreenPipes;
using MassTransit.Registration;
using MassTransit.Transports;

namespace MassTransit.EventStoreDbIntegration.Specifications
{
    public interface IEventStoreDbCatchupSubscriptionSpecification :
        IReceiveEndpointObserverConnector,
        ISpecification
    {
        /// <summary>
        /// EventStoreDB stream category with subscription name appended onto the end.
        /// </summary>
        string EndpointName { get; }

        ReceiveEndpoint CreateReceiveEndpoint(IBusInstance busInstance);
    }
}