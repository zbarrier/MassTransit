﻿using System;
using System.Collections.Generic;
using EventStore.Client;
using GreenPipes;
using GreenPipes.Configurators;
using MassTransit.Configuration;
using MassTransit.EventStoreDbIntegration.Contexts;
using MassTransit.EventStoreDbIntegration.Filters;
using MassTransit.EventStoreDbIntegration.Serializers;
using MassTransit.Registration;
using MassTransit.Transports;

namespace MassTransit.EventStoreDbIntegration.Configurators
{
    public class EventStoreDbCatchupSubscriptionConfigurator :
        ReceiverConfiguration,
        IEventStoreDbCatchupSubscriptionConfigurator,
        ReceiveSettings
    {
        readonly IBusInstance _busInstance;
        readonly IReceiveEndpointConfiguration _endpointConfiguration;
        readonly IEventStoreDbHostConfiguration _hostConfiguration;
        readonly PipeConfigurator<ProcessorContext> _processorConfigurator;
        IHeadersDeserializer _headersDeserializer;
        Action<SubscriptionFilterOptions> _filterOptions;
        Action<UserCredentials> _userCredentials;
        CheckpointStoreFactory _checkpointStoreFactory;

        public EventStoreDbCatchupSubscriptionConfigurator(IEventStoreDbHostConfiguration hostConfiguration, StreamCategory streamCategory, string subscriptionName,
            IBusInstance busInstance, IReceiveEndpointConfiguration endpointConfiguration, IHeadersDeserializer headersDeserializer)
            : base(endpointConfiguration)
        {
            StreamCategory = streamCategory;
            SubscriptionName = subscriptionName;
            _hostConfiguration = hostConfiguration;
            _busInstance = busInstance;
            _endpointConfiguration = endpointConfiguration;

            SetHeadersDeserializer(headersDeserializer);

            CheckpointInterval = TimeSpan.FromMinutes(1);
            CheckpointMessageCount = 5000;
            ConcurrencyLimit = 1;

            _processorConfigurator = new PipeConfigurator<ProcessorContext>();
        }

        public void SetHeadersDeserializer(IHeadersDeserializer deserializer)
        {
            _headersDeserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
        }

        public TimeSpan CheckpointInterval { get; set; }

        public ushort CheckpointMessageCount { get; set; }

        public int ConcurrencyLimit { get; }

        public Action<SubscriptionFilterOptions> FilterOptions
        {
            set => _filterOptions = value ?? throw new ArgumentNullException(nameof(value));
        }

        public Action<UserCredentials> UserCredentials
        {
            set => _userCredentials = value ?? throw new ArgumentNullException(nameof(value));
        }

        public StreamCategory StreamCategory { get; }

        public string SubscriptionName { get; }

        public void SetCheckpointStore(CheckpointStoreFactory checkpointStoreFactory)
        {
            _checkpointStoreFactory = checkpointStoreFactory ?? throw new ArgumentNullException(nameof(checkpointStoreFactory));
        }

        public override IEnumerable<ValidationResult> Validate()
        {
            if (_headersDeserializer == null)
                yield return this.Failure("HeadersDeserializer", "should not be null");

            foreach (var result in base.Validate())
                yield return result;
        }

        public ReceiveEndpoint Build()
        {
            IEventStoreDbReceiveEndpointContext CreateContext()
            {
                var builder = new EventStoreDbReceiveEndpointBuilder(_hostConfiguration, _busInstance, _endpointConfiguration, this, _headersDeserializer);

                foreach (var specification in Specifications)
                    specification.Configure(builder);

                return builder.CreateReceiveEndpointContext();
            }

            var context = CreateContext();

            _processorConfigurator.UseFilter(new EventStoreDbConsumerFilter(context));

            IPipe<ProcessorContext> processorPipe = _processorConfigurator.Build();

            var transport = new ReceiveTransport<ProcessorContext>(_busInstance.HostConfiguration, context, () => context.ContextSupervisor,
                processorPipe);

            return new ReceiveEndpoint(transport, context);
        }
    }
}
