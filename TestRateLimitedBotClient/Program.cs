using System;
using System.Collections.Generic;
using System.Diagnostics;
using MihaZupan.TelegramBotClients;
using MihaZupan.TelegramBotClients.RateLimitedClient;
using Moq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TestRateLimitedBotClient
{
    public class ChatSentTime
    {
        public string ChatType => ChatId < 0 ? "Group" : "Private";
        public int ChatId { get; set; }
        public List<string> SentTime { get; set; }
    }
    
    class Program
    {
        static void Main()
        {
            int i;
            var mock = new Mock<ITelegramBotClient>();
            var client = new RateLimitedTelegramBotClient(mock.Object, SchedulerSettings.Default);
            var chatSentTimes = new Dictionary<int, ChatSentTime>();
            var halfOfChats = 10;
            var minChatId = -halfOfChats;
            var maxChatId = halfOfChats;

            for (i = minChatId; i <= maxChatId; i++)
            {
                if (i == 0) continue;
                chatSentTimes.Add(i, new ChatSentTime
                {
                    ChatId = i,
                    SentTime = new List<string>()
                });
                ;
            }

            i = 0;

            var clock = Stopwatch.StartNew();
            int chatId;
            while (true)
            {
                i++;
                chatId = i % (halfOfChats << 1) - halfOfChats;
                if (chatId == 0)
                    continue;
                mock.Setup(m => m.SendTextMessageAsync(It.IsAny<ChatId>(), It.IsAny<string>(), default(ParseMode),
                    default, default, default, default, default)).Callback(
                    () => chatSentTimes[chatId].SentTime.Add(clock.Elapsed.TotalSeconds.ToString()));
                client.SendTextMessageAsync(new ChatId(chatId), "123456").GetAwaiter().GetResult();

                if (clock.Elapsed > TimeSpan.FromSeconds(20))
                    break;
            }

            clock.Stop();
            Console.ReadKey();
        }
    }
}