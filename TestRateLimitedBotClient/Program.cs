using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using MihaZupan.TelegramBotClients;
using MihaZupan.TelegramBotClients.RateLimitedClient;
using Moq;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TestRateLimitedBotClient
{
    public class ChatSentTime
    {
        // ReSharper disable once UnusedMember.Global
        public string ChatType => ChatId < 0 ? "Group" : "Private";
        public int ChatId { get; set; }
        // ReSharper disable once CollectionNeverQueried.Global
        public List<string> SentTimes { get; set; }
    }

    static class Program
    {
        private static readonly Mock<ITelegramBotClient> ClientMock = new Mock<ITelegramBotClient>();

        private static RateLimitedTelegramBotClient _rateLimitedClient;

        private static void AddSendTime(IReadOnlyDictionary<int, ChatSentTime> sentTimes, int chatId, double elapsedSeconds)
        {
            sentTimes[chatId].SentTimes.Add(elapsedSeconds.ToString(CultureInfo.InvariantCulture));
        }
        
        private static Dictionary<int, ChatSentTime> GetSentTimes(int minChatId, int maxChatId)
        {
            _rateLimitedClient =
                new RateLimitedTelegramBotClient(ClientMock.Object, SchedulerSettings.Default);
            var i = minChatId;
            var clock = Stopwatch.StartNew();
            var chatSentTimes = new Dictionary<int, ChatSentTime>();
            ClientMock.Setup(m => m.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), default,
                default, default, default, default, default)).Callback(
                // ReSharper disable once AccessToModifiedClosure
                () => AddSendTime(chatSentTimes, i, clock.Elapsed.TotalSeconds));

            for (i = minChatId; i <= maxChatId; i++)
            {
                if (i == 0) continue;
                chatSentTimes.Add(i, new ChatSentTime
                {
                    ChatId = i,
                    SentTimes = new List<string>()
                });
            }
            while (true)
            {
                i++;
                if (i >= maxChatId)
                    i = minChatId;
                if (i == 0)
                    continue;
                _rateLimitedClient.SendTextMessageAsync(new ChatId(i), "123456").GetAwaiter().GetResult();
                if (clock.Elapsed > TimeSpan.FromSeconds(10))
                    break;
            }
            clock.Stop();
            return chatSentTimes;
        }
        
        static void Main()
        {
            var sendTimes = GetSentTimes(0, 10);
            var sendTimesUsers = JsonConvert.SerializeObject(sendTimes);
            sendTimes = GetSentTimes(-10, 0);
            var sendTimesGroups = JsonConvert.SerializeObject(sendTimes);
            sendTimes = GetSentTimes(-10, 10);
            var sendTimesMixed = JsonConvert.SerializeObject(sendTimes);
            Console.WriteLine($"{sendTimesUsers}\r\n\r\n{sendTimesGroups}\r\n\r\n{sendTimesMixed}");
            Console.ReadKey();
        }
    }
}