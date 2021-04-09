using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using EventStore.Client;
using MassTransit.Context;
using MassTransit.Serialization;
using MassTransit.TestFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace MassTransit.EventStoreDbIntegration.Tests
{
    public class Receive_Specs :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_receive()
        {
            TaskCompletionSource<ConsumeContext<EventStoreDbMessage>> taskCompletionSource = GetTask<ConsumeContext<EventStoreDbMessage>>();
            var services = new ServiceCollection();
            services.AddSingleton(taskCompletionSource);

            services.TryAddSingleton<ILoggerFactory>(LoggerFactory);
            services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));

            _ = services.AddSingleton<EventStoreClient>((provider) => {
                var settings = EventStoreClientSettings.Create("esdb://masstransit.eventstore.db:2113?tls=false");
                settings.ConnectionName = "MassTransit Test Connection";

                return new EventStoreClient(settings);
            });

            services.AddMassTransit(x =>
            {
                x.UsingInMemory((context, cfg) => cfg.ConfigureEndpoints(context));
                x.AddRider(rider =>
                {
                    rider.AddConsumer<EventStoreDbMessageConsumer>();

                    rider.UsingEventStoreDB((context, esdb) =>
                    {
                        //esdb.UseExistingClient();
                        //esdb.UseEventStoreDBCheckpointStore();

                        esdb.ReceiveEndpoint(StreamCategory.AllStream, "MassTransit Test Subscription", c =>
                        {
                            c.ConfigureConsumer<EventStoreDbMessageConsumer>(context);
                        });
                    });
                });
            });

            var provider = services.BuildServiceProvider();

            var busControl = provider.GetRequiredService<IBusControl>();

            await busControl.StartAsync(TestCancellationToken);

            try
            {
                using var producer = provider.GetRequiredService<EventStoreClient>();

                var serializer = new JsonMessageSerializer();

                var message = new EventStoreDbMessageClass("test message");
                var context = new MessageSendContext<EventStoreDbMessage>(message);

                var preparedMessage = message.SerializeEvent(Uuid.NewUuid(), new Dictionary<string, object>());

                await producer.AppendToStreamAsync("masstransit_test_stream", StreamState.Any, new List<EventData> { preparedMessage });

                //await using (var stream = new MemoryStream())
                //{
                //    serializer.Serialize(stream, context);
                //    stream.Flush();

                //    var eventData = new EventData(stream.ToArray());
                //    await producer.SendAsync(new[] {eventData});
                //}

                ConsumeContext<EventStoreDbMessage> result = await taskCompletionSource.Task;

                Assert.AreEqual(message.Text, result.Message.Text);
            }
            finally
            {
                await busControl.StopAsync(TestCancellationToken);

                await provider.DisposeAsync();
            }
        }


        public class EventStoreDbMessageClass :
            EventStoreDbMessage
        {
            public EventStoreDbMessageClass(string text)
            {
                Text = text;
            }

            public string Text { get; }
        }


        public class EventStoreDbMessageConsumer :
            IConsumer<EventStoreDbMessage>
        {
            readonly TaskCompletionSource<ConsumeContext<EventStoreDbMessage>> _taskCompletionSource;

            public EventStoreDbMessageConsumer(TaskCompletionSource<ConsumeContext<EventStoreDbMessage>> taskCompletionSource)
            {
                _taskCompletionSource = taskCompletionSource;
            }

            public async Task Consume(ConsumeContext<EventStoreDbMessage> context)
            {
                _taskCompletionSource.TrySetResult(context);
            }
        }


        public interface EventStoreDbMessage
        {
            string Text { get; }
        }
    }
}