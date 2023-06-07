
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.IO;
using System.Timers;
using System.Text.RegularExpressions;
using System.Configuration;


namespace MagazineTelegramBot.Module
{
    class TelegramBot
    {

        public static List<List<(string, Type)>> MainMenu = new List<List<(string, Type)>>()
        {
            new List<(string, Type)> {("Корзина",typeof(BasketPage))},
            new List<(string, Type)> {("Товары по категориям",typeof(CategoryPage)),("Товары", typeof(ItemsPage))},
            new List<(string, Type)> {("О магазине",typeof(ItemsPage))}
        };
        private static ITelegramBotClient botClient;
        public static ITelegramBotClient BotClient => botClient;
        public static async Task Start()
        {
            await Task.Run(() =>
            {



                botClient = new TelegramBotClient("5823270058:AAGcrxMYi4jZS9jH9hqNTtMDLKcccJolaS4");
               

                var cts = new CancellationTokenSource();
                var cancellationToken = cts.Token;
                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { }, // receive all update types
                };
                botClient.StartReceiving(
                    HandleUpdateAsync,
                    HandleErrorAsync,

                    receiverOptions,
                    cancellationToken
                );
                Console.ReadLine();
            });



        }
        private static bool IsMenu(string text, out Type type)
        {
            type=null;  
            foreach(var i in MainMenu)
                foreach(var j in i)
                    if(j.Item1 == text)
                    {
                        type = j.Item2; 
                        return true;
                    }
                       
            return  false;
        }
        private static KeyboardButton[][] CreateMenu()
        {
            KeyboardButton[][] result = new KeyboardButton[MainMenu.Count][];
            for (var i = 0; i < result.Length; i++)
            {
                var tmp = new List<KeyboardButton>();
                foreach (var item in MainMenu[i]) tmp.Add(item.Item1);
                result[i] = tmp.ToArray();
            }
            return result;
        }
        public static async Task CreateMenu(Database.User user)
        {
            user.IdMainMessage = (await botClient.SendTextMessageAsync(user.TelegramId, "Menu",
                            ParseMode.Html, replyMarkup: new ReplyKeyboardMarkup(CreateMenu()))).MessageId;
            await Database.UserContext.UpdateMessage(user);
        }
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            try
            {
                if (update.Message != null)
                {
                    var user = Database.UserContext.GetOrAddUser(update.Message.From.Id);
                    if ("clear" == update.Message.Text)
                    {
                        user.SavePage = "";
                        user.IdMainMessage = -1;
                        await Database.UserContext.UpdateAsync(user);
                        await Database.UserContext.UpdateMessage(user);
                    }
                       
                    if (IsMenu(update.Message.Text,out var TypeMenu))
                    {
                        

                        if (user.SavePage == string.Empty)
                        {

                            if (user.IdMainMessage != -1)
                                await botClient.DeleteMessageAsync(update.Message.Chat, user.IdMainMessage);
                            user.IdMainMessage = -1;
                            await Database.UserContext.UpdateMessage(user);
                            ITelegramPage instance = (ITelegramPage)Activator.CreateInstance(TypeMenu, user);
                            await instance.Create();
                        }
                        
                    }
                    await botClient.DeleteMessageAsync(update.Message.Chat, update.Message.MessageId);
                    if (user.IdMainMessage == -1&& user.SavePage == string.Empty)
                       await CreateMenu(user);
                        
                    
                        




                }

                if (update.Type == UpdateType.CallbackQuery)
                {
                    CallbackQuery callbackQuery = update.CallbackQuery;
                    var user = Database.UserContext.GetOrAddUser(update.CallbackQuery.From.Id);

                    if (callbackQuery != null && user != null && user.SavePage != string.Empty)
                    {
                        var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(user.SavePage);
                        Type deserializedType;
                        if (jsonObject.ContainsKey("Type"))
                        {
                            string typeName = jsonObject["Type"].ToString();
                            deserializedType = Type.GetType(typeName);

                        }
                        else return;
                        //ITelegramPage page = (ITelegramPage)Activator.CreateInstance(deserializedType);

                        ITelegramPage page = (ITelegramPage)Newtonsoft.Json.JsonConvert.DeserializeObject(user.SavePage,deserializedType);
                        await page.HandlerCallback(callbackQuery);
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

        }
        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            await Task.Run(() => Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception)));
        }

    }
}
