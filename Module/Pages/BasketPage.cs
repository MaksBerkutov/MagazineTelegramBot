using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace MagazineTelegramBot.Module
{
    class BasketPage : TelegramPage
    {
        public List<(int,int)> BasketItems { get; set; }
        public int lastIndex { get; set; } = 0;
        public int FocusId { get; set; } = -1;
        private const int countItems = 5;
        private void AddNavigateButton()
        {
            var right = new ButtonPage(">>", "MoveRight");
            right.OnClicked += right_OnClicked;
            var left = new ButtonPage("<<", "MoveLeft");
            left.OnClicked += left_OnClicked;
            var back = new ButtonPage("Назад", "ButBack");
            back.OnClicked += back_OnClicked;
            Buttons.Add(new List<ButtonPage> { left, right });
            Buttons.Add(new List<ButtonPage> { back});
           
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
            else lastIndex-=countItems;
            await Create(callback.Message.MessageId);
        }

        private async void right_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            if (lastIndex + countItems >= BasketItems.Count)
                lastIndex = BasketItems.Count;
            else lastIndex += countItems;
            await Create(callback.Message.MessageId);
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
            var rightAdd = new ButtonPage("+1", "AddMoveRight");
            rightAdd.OnClicked += RightAdd_OnClicked;
            var leftAdd = new ButtonPage("1-", "AddMoveLeft");
            leftAdd.OnClicked += LeftAdd_OnClicked;

            Buttons.Clear();
            for (int i = lastIndex; i < BasketItems.Count && i < lastIndex + countItems; i++)
            {
                var button = new ButtonPage($"{Database.ItemContext.GetElement(i).Item1.Name} x{BasketItems[i].Item2}", BasketItems[i].Item1.ToString());
                button.OnClicked += Button_OnClicked;
                if (i == FocusId)
                    Buttons.Add(new List<ButtonPage>() { leftAdd, button, rightAdd });
                else
                    Buttons.Add(new List<ButtonPage>() { button });
            }
            AddNavigateButton();
        }
        public override async Task Create(int MessgaeId = -1)
        {
            try
            {
                LoadBut();
                if (BasketItems.Count == 0)
                {
                    return;
                }
               
                if (MessgaeId != -1)
                {
                    await botClient.DeleteMessageAsync(Owner.TelegramId, MessgaeId);

                }
                await botClient.SendTextMessageAsync(Owner.TelegramId, "Корзина",
                         replyMarkup: new InlineKeyboardMarkup(ButtonPage.Create(Buttons).ToArray()));
                await Save();

            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
            

        }

        private async void Button_OnClicked(ButtonPage inoutPageBut, CallbackQuery callback)
        {
            FocusId = BasketItems.FindIndex(x => x.Item1 == int.Parse(callback.Data));
            await Create(callback.Message.MessageId);
        }

        private async void LeftAdd_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            //--
            if (this.FocusId != -1)
            {
                if (BasketItems[FocusId].Item2 - 1 <= 0)
                {
                    BasketItems.RemoveAt(FocusId);
                    FocusId = -1;
                }
                else
                {
                    BasketItems[FocusId] = (BasketItems[FocusId].Item1, BasketItems[FocusId].Item2 - 1);
                }
                await Database.BasketContext.SaveBasketAsync(new Database.Basket() 
                { BasketSerilazed = JsonConvert.SerializeObject(BasketItems), TID = Owner.TelegramId });
                await Create(callback.Message.MessageId);  

            }
        }

        private async void RightAdd_OnClicked(ButtonPage button, CallbackQuery callback)
        {
            //++
            if (this.FocusId != -1)
            {
                BasketItems[FocusId] = (BasketItems[FocusId].Item1, BasketItems[FocusId].Item2 + 1);
                await Database.BasketContext.SaveBasketAsync(new Database.Basket()
                { BasketSerilazed = JsonConvert.SerializeObject(BasketItems), TID = Owner.TelegramId });
                await Create(callback.Message.MessageId);
            }
        }

        public BasketPage(Database.User Owner)
        {
            this.Owner = Owner;
            this.Type = typeof(BasketPage);
            BasketItems = Database.BasketContext.GetBasketUser(Owner).Result.Deserilazed();
            Buttons = new List<List<ButtonPage>>();
        }
    }

}
