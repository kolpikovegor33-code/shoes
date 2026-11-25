using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using shoes.Models;

namespace shoes
{
    public partial class OrdersWindow : Window
    {
        private readonly bool isAdmin;

        public OrdersWindow(bool adminMode)
        {
            InitializeComponent();

            isAdmin = adminMode;

            if (isAdmin)
            {
                buttonAddOrder.Visibility = Visibility.Visible;
                buttonDeleteOrder.Visibility = Visibility.Visible;
            }

            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    List<Order> orders = db.Orders
                        .Include(o => o.User)
                        .Include(o => o.PickupPoint)
                        .Include(o => o.OrderProducts)
                            .ThenInclude(op => op.ArticleNavigation)
                        .OrderBy(o => o.OrderId)
                        .ToList();

                    List<OrderViewModel> orderList = orders
                        .Select(CreateViewModel)
                        .ToList();

                    listOrders.ItemsSource = orderList;
                    textNoOrders.Visibility = orderList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить список заказов из базы данных.\n\n" + ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private OrderViewModel CreateViewModel(Order order)
        {
            OrderViewModel viewModel = new OrderViewModel();

            viewModel.OrderId = order.OrderId;
            viewModel.Title = "Заказ № " + order.OrderId;
            viewModel.Status = order.Status ?? "";
            viewModel.Code = order.Code.ToString();
            viewModel.OrderDate = order.OrderDate.ToString("dd.MM.yyyy");
            viewModel.DeliveryDate = order.DeliveryDate.ToString("dd.MM.yyyy");

            viewModel.Client = order.User.LastName + " " + order.User.FirstName;
            if (!string.IsNullOrWhiteSpace(order.User.Surname))
            {
                viewModel.Client = viewModel.Client + " " + order.User.Surname;
            }

            viewModel.Pickup = order.PickupPoint.Index + ", " + order.PickupPoint.City + ", " + order.PickupPoint.Street;
            if (order.PickupPoint.House != null)
            {
                viewModel.Pickup = viewModel.Pickup + ", " + order.PickupPoint.House;
            }

            viewModel.Composition = string.Join("; ", order.OrderProducts
                .Select(op => op.ArticleNavigation.ProductName + " × " + op.Count));

            return viewModel;
        }

        private void OpenOrderWindow(int? orderId)
        {
            OrderEditWindow orderWindow = new OrderEditWindow(orderId);
            orderWindow.Owner = this;

            if (orderWindow.ShowDialog() == true)
            {
                LoadOrders();
            }
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ButtonAddOrder_Click(object sender, RoutedEventArgs e)
        {
            OpenOrderWindow(null);
        }

        private void ListOrders_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!isAdmin)
            {
                return;
            }

            OrderViewModel? selectedOrder = listOrders.SelectedItem as OrderViewModel;

            if (selectedOrder == null)
            {
                return;
            }

            OpenOrderWindow(selectedOrder.OrderId);
        }

        private void ButtonDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            OrderViewModel? selectedOrder = listOrders.SelectedItem as OrderViewModel;

            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите в списке заказ, который нужно удалить, и нажмите кнопку еще раз.", "Заказ не выбран", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult answer = MessageBox.Show("Вы действительно хотите удалить заказ № " + selectedOrder.OrderId + " вместе с его составом? Это действие нельзя отменить.", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    Order? order = db.Orders
                        .Include(o => o.OrderProducts)
                        .FirstOrDefault(o => o.OrderId == selectedOrder.OrderId);

                    if (order != null)
                    {
                        db.OrderProducts.RemoveRange(order.OrderProducts);
                        db.Orders.Remove(order);
                        db.SaveChanges();
                    }
                }

                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить заказ.\n\n" + ex.Message, "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public string Client { get; set; } = "";
        public string Pickup { get; set; } = "";
        public string Composition { get; set; } = "";
        public string Code { get; set; } = "";
        public string OrderDate { get; set; } = "";
        public string DeliveryDate { get; set; } = "";
    }
}