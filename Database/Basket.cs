using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagazineTelegramBot.Database
{
    public class Basket
    {
        public int Id { get; set; }
        public long TID { get; set; }
        public string BasketSerilazed { get; set; } = Newtonsoft.Json.JsonConvert.SerializeObject(new List<(int,int)>());
        public List<(int, int)> Deserilazed()=>Newtonsoft.Json.JsonConvert.DeserializeObject<List<(int, int)>>(BasketSerilazed);
    }
    public class BasketContext : DbContext
    {
        public DbSet<Basket> Items { get; set; }

        public BasketContext() : base("DbConnection")
        { }
        public static async Task<Basket> GetBasketUser(User user)
        {
            using (var context = new BasketContext())
            {
                var finded = context.Items.ToList().Find(x=>x.TID==user.TelegramId);
                if (finded == null){
                    finded = new Basket() { TID = user.TelegramId };
                    context.Items.Add(finded);  
                }
                await context.SaveChangesAsync();
                return finded;
            }
        }
        public static async Task SaveBasketAsync(Basket basket)
        {
            using (var context = new BasketContext())
            {
                var finded = context.Items.ToList().Find(x => x.TID == basket.TID);
                if (finded == null) return;
                finded.BasketSerilazed = basket.BasketSerilazed;
                await context.SaveChangesAsync();
            }
        }
        public static async Task AddToBasket(Item item,User user)
        {
            using (var context = new BasketContext())
            {
                var Basket = await GetBasketUser(user);
                var BasketItems = Basket.Deserilazed();
                var BasketElement = BasketItems.FindIndex(x => x.Item1 == item.Id);
                if (BasketElement == -1)
                    BasketItems.Add((item.Id, 1));
                else
                    BasketItems[BasketElement] = (BasketItems[BasketElement].Item1, BasketItems[BasketElement].Item2 + 1);
                Basket.BasketSerilazed = Newtonsoft.Json.JsonConvert.SerializeObject(BasketItems);
                await SaveBasketAsync(Basket);
                await context.SaveChangesAsync();
               
            }
        }
        

    }

}




