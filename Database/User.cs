using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace MagazineTelegramBot.Database
{
    public class User
    {
        public int Id { get; set; }
        public long TelegramId { get; set; }
        public string SavePage { get; set; } = string.Empty;
        public int IdMainMessage { get; set; } = -1;
    }

    public class UserContext : DbContext
    {
        public DbSet<User> Items { get; set; }

        public UserContext() : base("DbConnection")
        { }

        public static async Task<User> GetOrAddUserAsync(long TelegramId)
        {
            using (var context = new UserContext())
            {
                var finded = (await context.Items.ToListAsync()).Find(x => x.TelegramId == TelegramId);
                if (finded == null) finded = context.Items.Add(new User() { TelegramId = TelegramId });
                await context.SaveChangesAsync();
                return finded;
            }
        }
        public static User GetOrAddUser(long TelegramId)
        {
            using (var context = new UserContext())
            {
                //var finded = context.Items.Add(new User() { TelegramId = TelegramId });
                var finded = context.Items.ToList().Find(x => x.TelegramId == TelegramId);
                if (finded == null) finded = context.Items.Add(new User() { TelegramId = TelegramId });
                context.SaveChanges();
                return finded;
            }
        }
        public static async Task<User> UpdateAsync(User user)
        {
            using (var context = new UserContext())
            {
                var finded =  context.Items.ToList().Find(x => x.TelegramId == user.TelegramId);
                if (finded == null) return null;
                finded.SavePage = user.SavePage;
                await context.SaveChangesAsync();
                return finded;
            }
        }
        public static async Task<User> UpdateMessage(User user)
        {
            using (var context = new UserContext())
            {
                var finded = context.Items.ToList().Find(x => x.TelegramId == user.TelegramId);
                if (finded == null) return null;
                finded.IdMainMessage = user.IdMainMessage;
                await context.SaveChangesAsync();
                return finded;
            }
        }
        public static User Update (User user)
        {
            using (var context = new UserContext())
            {
                var finded = context.Items.ToList().Find(x => x.TelegramId == user.TelegramId);
                if (finded == null) return null;
                finded = user;
                context.SaveChanges();
                return finded;
            }
        }

    }




}
