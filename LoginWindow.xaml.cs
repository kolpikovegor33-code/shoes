using Microsoft.EntityFrameworkCore;
using shoes.Models;
using System;
using System.Linq;
using System.Windows;

namespace shoes
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void ButtonLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = textLogin.Text.Trim();
            string password = passwordBox.Password;

            if (login == "" || password == "")
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    User? user = db.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Login == login && u.Password == password);

                    if (user == null)
                    {
                        MessageBox.Show("Неверный логин или пароль");
                        passwordBox.Clear();
                        return;
                    }

                    OpenMainWindow(user);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения к базе данных: " + ex.Message);
            }
        }

        private void ButtonGuest_Click(object sender, RoutedEventArgs e)
        {
            OpenMainWindow(null);
        }

        private void OpenMainWindow(User? user)
        {
            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();
            Close();
        }
    }
}