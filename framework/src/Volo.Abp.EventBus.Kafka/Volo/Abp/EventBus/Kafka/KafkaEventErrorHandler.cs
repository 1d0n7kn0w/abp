﻿using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Kafka;

namespace Volo.Abp.EventBus.Kafka
{
    public class KafkaEventErrorHandler : EventErrorHandlerBase, ISingletonDependency
    {
        public const string HeadersKey = "headers";
        public const string RetryIndexKey = "retryIndex";

        protected IKafkaSerializer Serializer { get; }
        protected KafkaDistributedEventBus EventBus { get; }

        public KafkaEventErrorHandler(
            IOptions<AbpEventBusOptions> options,
            IKafkaSerializer serializer,
            KafkaDistributedEventBus eventBus) : base(options)
        {
            Serializer = serializer;
            EventBus = eventBus;
        }

        protected override async Task Retry(EventExecutionErrorContext context)
        {
            if (Options.RetryStrategyOptions.IntervalMillisecond > 0)
            {
                await Task.Delay(Options.RetryStrategyOptions.IntervalMillisecond);
            }

            var headers = context.GetProperty<Headers>(HeadersKey) ?? new Headers();
            var index = Serializer.Deserialize<int>(headers.GetLastBytes(RetryIndexKey));

            headers.Remove(RetryIndexKey);
            headers.Add(RetryIndexKey, Serializer.Serialize(++index));

            await EventBus.PublishAsync(context.EventType, context.EventData, headers);
        }

        protected override async Task MoveToDeadLetter(EventExecutionErrorContext context)
        {
            await EventBus.PublishToDeadLetterAsync(context.EventType, context.EventData, new Headers
            {
                {"exceptions", Serializer.Serialize(context.Exceptions)}
            });
        }

        protected override bool ShouldRetry(EventExecutionErrorContext context)
        {
            if (!base.ShouldRetry(context))
            {
                return false;
            }

            var headers = context.GetProperty<Headers>(HeadersKey);

            if (headers == null)
            {
                return true;
            }

            var index = Serializer.Deserialize<int>(headers.GetLastBytes(RetryIndexKey));

            return Options.RetryStrategyOptions.MaxRetryAttempts < index;
        }
    }
}
