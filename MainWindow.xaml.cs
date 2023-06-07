using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MagazineTelegramBot
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            for(int i = 0; i < 13; i++)
            {
                byte[] img = System.IO.File.ReadAllBytes(@$"E:\рб\Photoshop\Оригиналы\Новая папка (2)\{i}.jpg");
                Database.ItemContext.Add(new Database.Item()
                {
                    Name = $"Test item #{i}",
                    Description = $"Test description #{i}",
                    Category = i < 3 ? "1" : i < 6 ? "2" : "3",
                    Count = 100,
                    Price = 7,
                    Image = img
                });

            }
            Task.Run(()=> MagazineTelegramBot.Module.TelegramBot.Start());
             
        }
        
    }
}
