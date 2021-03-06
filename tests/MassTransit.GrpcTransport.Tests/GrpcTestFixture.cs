namespace MassTransit.GrpcTransport.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using NUnit.Framework.Internal;
    using TestFramework;
    using Testing;
    using Util;


    public class GrpcTestFixture :
        BusTestFixture
    {
        TestExecutionContext _fixtureContext;

        public GrpcTestFixture()
            : this(new GrpcTestHarness())
        {
        }

        public GrpcTestFixture(GrpcTestHarness harness)
            : base(harness)
        {
            GrpcTestHarness = harness;
            GrpcTestHarness.OnConfigureGrpcBus += ConfigureGrpcBus;
            GrpcTestHarness.OnConfigureGrpcReceiveEndpoint += ConfigureGrpcReceiveEndpoint;
        }

        protected GrpcTestHarness GrpcTestHarness { get; }

        protected string InputQueueName => GrpcTestHarness.InputQueueName;

        protected Uri BaseAddress => GrpcTestHarness.BaseAddress;

        /// <summary>
        /// The sending endpoint for the InputQueue
        /// </summary>
        protected ISendEndpoint InputQueueSendEndpoint => GrpcTestHarness.InputQueueSendEndpoint;

        /// <summary>
        /// The sending endpoint for the Bus
        /// </summary>
        protected ISendEndpoint BusSendEndpoint => GrpcTestHarness.BusSendEndpoint;

        protected Uri BusAddress => GrpcTestHarness.BusAddress;

        protected Uri InputQueueAddress => GrpcTestHarness.InputQueueAddress;

        [SetUp]
        public Task SetupGrpcTest()
        {
            return TaskUtil.Completed;
        }

        [TearDown]
        public Task TearDownGrpcTest()
        {
            return TaskUtil.Completed;
        }

        protected IRequestClient<TRequest> CreateRequestClient<TRequest>()
            where TRequest : class
        {
            return GrpcTestHarness.CreateRequestClient<TRequest>();
        }

        protected IRequestClient<TRequest> CreateRequestClient<TRequest>(Uri destinationAddress)
            where TRequest : class
        {
            return GrpcTestHarness.CreateRequestClient<TRequest>(destinationAddress);
        }

        protected Task<IRequestClient<TRequest>> ConnectRequestClient<TRequest>()
            where TRequest : class
        {
            return GrpcTestHarness.ConnectRequestClient<TRequest>();
        }

        [OneTimeSetUp]
        public Task SetupGrpcTestFixture()
        {
            _fixtureContext = TestExecutionContext.CurrentContext;

            LoggerFactory.Current = _fixtureContext;

            return GrpcTestHarness.Start();
        }

        protected Task<ISendEndpoint> GetSendEndpoint(Uri address)
        {
            return GrpcTestHarness.GetSendEndpoint(address);
        }

        [OneTimeTearDown]
        public async Task TearDownGrpcTestFixture()
        {
            LoggerFactory.Current = _fixtureContext;

            await GrpcTestHarness.Stop().ConfigureAwait(false);

            GrpcTestHarness.Dispose();
        }

        protected virtual void ConfigureGrpcBus(IGrpcBusFactoryConfigurator configurator)
        {
        }

        protected virtual void ConfigureGrpcReceiveEndpoint(IGrpcReceiveEndpointConfigurator configurator)
        {
        }
    }
}
