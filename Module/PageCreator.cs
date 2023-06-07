using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace MagazineTelegramBot.Module
{
    class ButtonPage
    {
        public InlineKeyboardButton Button;
       
        public ButtonPage(string Name, string Callback)
        {
            Button = new InlineKeyboardButton(Name) { CallbackData = Callback };
        }
        public void Trigger(CallbackQuery callback) => OnClicked?.Invoke(this, callback);
        public static List<List<InlineKeyboardButton>>  Create(List<List<ButtonPage>> Buttons)
        {
            List<List<InlineKeyboardButton>> result = new List<List<InlineKeyboardButton>>();
            foreach(var Button in Buttons)
            {
                result.Add(new List<InlineKeyboardButton>());
                foreach (var ButtonPage in Button)
                    result[result.Count-1].Add(ButtonPage.Button);
            }
            return result;
        }
       
        public delegate void Clicked(ButtonPage button, CallbackQuery callback);
       
        public event Clicked OnClicked;
    }
    interface ITelegramPage
    {
        Type Type { get; set; }
        Database.User Owner { get; set; }
        [JsonIgnore]
        List<List<ButtonPage>> Buttons { get; set; }
        byte[] Image { get; set; }
        string Text { get; set; }
        Task Create(int MessgaeId = -1);
        Task Save();
        Task Close();
        Task HandlerCallback(CallbackQuery callback);
    }
    abstract class TelegramPage : ITelegramPage
    {
        protected ITelegramBotClient botClient = TelegramBot.BotClient;
        public Type Type { get; set; }
        public Database.User Owner { get; set; }
        public List<List<ButtonPage>> Buttons { get; set; }
        public byte[] Image { get; set; }
        public string Text { get; set; }
        public abstract Task Create(int MessgaeId = -1);
        public virtual async Task Close()
        {
            Owner.SavePage = string.Empty;
            Owner = await Database.UserContext.UpdateAsync(Owner);
        }
        public virtual async Task HandlerCallback(CallbackQuery callback)
        {
            await Task.Run(() =>
            {
                if (callback.Data != null)
                    foreach (var Button in Buttons)
                        foreach (var ButtonPage in Button)
                            if (ButtonPage.Button.CallbackData == callback.Data)
                            {
                                ButtonPage.Trigger(callback);
                                return;
                            }
            });

        }
        public virtual async Task Save()
        {
            Owner.SavePage = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            Owner = await Database.UserContext.UpdateAsync(Owner);
        }
    }
    class CategoryPage : TelegramPage
    {
        public List<string>AllCategory { get; set; }
        public int lastIndex { get; set; } = 0;
        private const int countItems = 5;
        public CategoryPage(Database.User user)
        {
            this.Type = typeof(CategoryPage);
            this.Owner = user;
            AllCategory = Database.ItemContext.GetCategory();
            this.Buttons = new List<List<ButtonPage>>();
        }
        public override async Task HandlerCallback(CallbackQuery callback)
        {
            LoadBut();
            await Task.Run(() =>
            {
                if (callback.Data != null)
                    foreach (var Button in Buttons)
                        foreach (var ButtonPage in Button)
                            if (ButtonPage.Button.CallbackData == callback.Data)
                            {
                                ButtonPage.Trigger(callback);
                                return;
                            }
            });

        }
        public void LoadBut()
        {
            Buttons.Clear();
            for (int i = lastIndex; i < AllCategory.Count && i < lastIndex + countItems; i++)
            {
                var button = new ButtonPage(AllCategory[i], i.ToString());
                button.OnClicked += Button_OnClicked; ;
                Buttons.Add(new List<ButtonPage>() { button });
            }
            AddNavigateButton();
        }

        private async void Button_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            await botClient.DeleteMessageAsync(Owner.TelegramId, callback.Message.MessageId);
            await this.Close();
            await new CategoryItemPage(Owner) { Category = AllCategory[int.Parse(callback.Data)] }.Create();
        }

        private void AddNavigateButton()
        {
            var right = new ButtonPage(">>", "MoveRight");
            right.OnClicked += right_OnClicked;
            var left = new ButtonPage("<<", "MoveLeft");
            left.OnClicked += left_OnClicked;
            var back = new ButtonPage("Назад", "ButBack");
            back.OnClicked += back_OnClicked;
            Buttons.Add(new List<ButtonPage> { left, right });
            Buttons.Add(new List<ButtonPage> { back });

        }
        private async void back_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            await botClient.DeleteMessageAsync(Owner.TelegramId, callback.Message.MessageId);
            await this.Close();
            await TelegramBot.CreateMenu(Owner);

        }

        private async void left_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            //--
            if (lastIndex - countItems < 0)
                lastIndex = 0;
            else lastIndex -= countItems;
            await Create(callback.Message.MessageId);
        }

        private async void right_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            if (lastIndex + countItems >= AllCategory.Count)
                lastIndex = AllCategory.Count;
            else lastIndex += countItems;
            await Create(callback.Message.MessageId);
        }
        public override async Task Create(int MessgaeId = -1)
        {
            try
            {
                LoadBut();
                if (AllCategory.Count == 0)
                {
                    return;
                }

                if (MessgaeId != -1)
                {
                    await botClient.DeleteMessageAsync(Owner.TelegramId, MessgaeId);

                }
                await botClient.SendTextMessageAsync(Owner.TelegramId, "Категории",
                         replyMarkup: new InlineKeyboardMarkup(ButtonPage.Create(Buttons).ToArray()));
                await Save();

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }
    }
    class CategoryItemPage: TelegramPage
    {
        public int CurrentItem { get; set; }
        public int Size { get; set; }
        public string Category { get; set; }
        public CategoryItemPage(Database.User Owner)
        {
            this.Owner = Owner;
            var right = new ButtonPage(">>", "MoveRight");
            right.OnClicked += right_OnClicked;
            var left = new ButtonPage("<<", "MoveLeft");
            left.OnClicked += left_OnClicked;
            var buy = new ButtonPage("Купить", "ButBuy");
            buy.OnClicked += buy_OnClicked;
            var back = new ButtonPage("Назад", "ButBack");
            back.OnClicked += back_OnClicked;
            Buttons = new List<List<ButtonPage>>()
            {
                new List<ButtonPage>(){left,right },
                new List<ButtonPage>(){buy },
                new List<ButtonPage>(){back }
            };

        }

        private async void back_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            await botClient.DeleteMessageAsync(Owner.TelegramId, callback.Message.MessageId);
            await this.Close();
            var CategoryPage = new CategoryPage(Owner);
            await CategoryPage.Create();

        }

        private async void buy_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            var items = Database.ItemContext.GetElement(CurrentItem, Category);
            await Database.BasketContext.AddToBasket(items.Item1, Owner);
            await botClient.AnswerCallbackQueryAsync(callback.Id, $"Добавлаенно в корзину {items.Item1.Name}");
        }

        private async void left_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            //--
            if (CurrentItem == 0)
                CurrentItem = Size - 1;
            else
                CurrentItem--;
            await Create(callback.Message.MessageId);
        }

        private async void right_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            //++
            if (CurrentItem == Size - 1)
                CurrentItem = 0;
            else
                CurrentItem++;
            await Create(callback.Message.MessageId);
        }

        public override async Task Create(int MessgaeId = -1)
        {
            try
            {
                this.Type = typeof(CategoryItemPage);
                var curentElemet = Database.ItemContext.GetElement(CurrentItem,Category);
                Size = curentElemet.Item2;
                if (Size == -1 && Size == 0)
                {
                    await botClient.SendTextMessageAsync(Owner.TelegramId, "На данный момент в магазине нет товаров",
                        replyMarkup: new InlineKeyboardMarkup(new InlineKeyboardButton[] { Buttons[2][0].Button }));
                }
                if (MessgaeId != -1) await botClient.DeleteMessageAsync(Owner.TelegramId, MessgaeId);
                Image = curentElemet.Item1.Image;

                using (var stream = new MemoryStream(Image))
                {
                    var photo = new InputOnlineFile(stream);
                    await botClient.SendPhotoAsync(Owner.TelegramId, photo
                       , curentElemet.Item1.ToString(), replyMarkup: new InlineKeyboardMarkup(ButtonPage.Create(Buttons).ToArray()));

                }
                await Save();
            }
            catch (Exception)
            {


            }

        }
        public override async Task Save()
        {
            Owner.SavePage = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            await Database.UserContext.UpdateAsync(Owner);
        }
    }

}
