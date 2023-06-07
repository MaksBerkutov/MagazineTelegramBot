using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace MagazineTelegramBot.Module
{
    class ItemsPage : TelegramPage
    {
        
      

        public int CurrentItem { get; set; }
        public int Size { get; set; }
        public ItemsPage(Database.User Owner)
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
            await botClient.DeleteMessageAsync(Owner.TelegramId,callback.Message.MessageId);
            await this.Close();            
            await TelegramBot.CreateMenu(Owner);

        }

        private async void buy_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            var items = Database.ItemContext.GetElement(CurrentItem);
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
                this.Type = typeof(ItemsPage);
                var curentElemet = Database.ItemContext.GetElement(CurrentItem);
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
