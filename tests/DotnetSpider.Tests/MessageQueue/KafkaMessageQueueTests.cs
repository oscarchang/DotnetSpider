using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotnetSpider.Core;
using DotnetSpider.Kafka;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotnetSpider.Tests.MessageQueue
{
	public class KafkaMessageQueueTests : TestBase
	{
		[Fact(DisplayName = "PubAndSub")]
		public async Task PubAndSub()
		{
			if (Directory.Exists("/home/vsts/work"))
			{
				return;
			}

			int count = 0;
			var options = SpiderFactory.GetRequiredService<ISpiderOptions>();
			var logger = SpiderFactory.GetRequiredService<ILogger<KafkaMessageQueue>>();
			var mq = new KafkaMessageQueue(options, logger);
			mq.Subscribe("PubAndSub", msg => { Interlocked.Increment(ref count); });
			for (int i = 0; i < 100; ++i)
			{
				await mq.PublishAsync("PubAndSub", "a");
			}

			int j = 0;
			while (count < 100 && j < 150)
			{
				Thread.Sleep(500);
				++j;
			}

			Assert.Equal(100, count);
		}

		[Fact(DisplayName = "ParallelPubAndSub")]
		public void ParallelPubAndSub()
		{
			if (Directory.Exists("/home/vsts/work"))
			{
				return;
			}

			int count = 0;
			var options = SpiderFactory.GetRequiredService<ISpiderOptions>();
			var logger = SpiderFactory.GetRequiredService<ILogger<KafkaMessageQueue>>();
			var mq = new KafkaMessageQueue(options, logger);
			mq.Subscribe("ParallelPubAndSub", msg => { Interlocked.Increment(ref count); });

			Parallel.For(0, 100, async (i) => { await mq.PublishAsync("ParallelPubAndSub", "a"); });
			int j = 0;
			while (count < 100 && j < 150)
			{
				Thread.Sleep(500);
				++j;
			}

			Assert.Equal(100, count);
		}

		[Fact(DisplayName = "PubAndUnSub")]
		public async Task PubAndUnSub()
		{
			if (Directory.Exists("/home/vsts/work"))
			{
				return;
			}

			int count = 0;
			var options = SpiderFactory.GetRequiredService<ISpiderOptions>();
			var logger = SpiderFactory.GetRequiredService<ILogger<KafkaMessageQueue>>();
			var mq = new KafkaMessageQueue(options, logger);
			mq.Subscribe("PubAndUnSub", msg => { Interlocked.Increment(ref count); });

			int i = 0;
			Task.Factory.StartNew(async () =>
			{
				for (; i < 50; ++i)
				{
					await mq.PublishAsync("PubAndUnSub", "a");
					await Task.Delay(100);
				}
			}).ConfigureAwait(false).GetAwaiter();
			await Task.Delay(1500);
			mq.Unsubscribe("PubAndUnSub");

			while (i < 50)
			{
				await Task.Delay(100);
			}

			Assert.True(count < 100);
		}
	}
}