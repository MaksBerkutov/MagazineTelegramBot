using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MagazineTelegramBot.Database
{
    public class Item
    {
        public int Id { get; set; }
        public byte[] Image { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int Price { get; set; }
        public int Count { get; set; }
        public override string ToString() => $"{Name}\n{Description}\n{Category}\n\n\t{Price}$";
    }

    public class ItemContext : DbContext
    {
        public DbSet<Item> Items { get; set; }
       
        public ItemContext() : base("DbConnection")
        { }
        public static List<Item> GetAllItemCategory(string Category)
        {
            using (var context = new ItemContext())
                return context.Items.ToList().Where(x => x.Category == Category).ToList();
        }
        public static (Item obj ,bool Next,int size) MoveNext(int CurrentId)
        {
            using (var context = new ItemContext())
            {
                if(CurrentId+1 >= context.Items.ToList().Count)return (null,false, context.Items.ToList().Count);
                return (context.Items.ToList()[++CurrentId], CurrentId + 1 == context.Items.ToList().Count, context.Items.ToList().Count);
            }
        }
        public static (Item,int) GetElement(int index)
        {
            using (var context = new ItemContext())
            {
                var items = context.Items.ToList();
                if (index >= items.Count) return (null,-1);
                return (items[index],items.Count);
            }
        }
        public static (Item, int) GetElement(int index, string Category)
        {
            using (var context = new ItemContext())
            {
                var items = context.Items.ToList().Where(x => x.Category == Category).ToList();
                if (index >= items.Count) return (null, -1);
                return (items[index], items.Count);
            }
        }
        public static async void Add(Item item)
        {
            using (var context = new ItemContext())
            {
                context.Items.Add(item);
                await context.SaveChangesAsync();
            }
        }
        public static int GetSize() => new ItemContext().Items.ToList().Count; 
        public static List<string> GetCategory( )
        {
            using (var context = new ItemContext())
            {
                return context.Items.ToList().Select(x => x.Category).Distinct().ToList();
            }
        }

    }




}
