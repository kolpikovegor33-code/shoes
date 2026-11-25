using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using shoes.Models;

namespace shoes
{
    public partial class OrderEditWindow : Window
    {
        private readonly int? editedOrderId;
        private readonly ObservableCollection<OrderItemRow> items = new ObservableCollection<OrderItemRow>();

        public OrderEditWindow(int? orderId)
        {
            InitializeComponent();

            editedOrderId = orderId;
            Title = orderId == null ? "Добавление заказа" : "Редактирование заказа";
            textHeader.Text = Title;
            listItems.ItemsSource = items;

            try
            {
                LoadLists();

                if (orderId != null)
                {
                    LoadOrder(orderId.Value);
                }
                else
                {
                    datePickerOrder.SelectedDate = DateTime.Today;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить данные.\n\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadLists()
        {
            using (AppDbContext db = new AppDbContext())
            {
                comboUser.ItemsSource = db.Users
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToList()
                    .Select(u => new UserItem
                    {
                        UserId = u.UserId,
                        FullName = u.LastName + " " + u.FirstName + (string.IsNullOrWhiteSpace(u.Surname) ? "" : " " + u.Surname)
                    })
                    .ToList();

                comboPickup.ItemsSource = db.PickupPoints
                    .OrderBy(p => p.City)
                    .ThenBy(p => p.Street)
                    .ToList()
                    .Select(p => new PickupItem
                    {
                        PickupPointId = p.PickupPointId,
                        Address = p.Index + ", " + p.City + ", " + p.Street + (p.House == null ? "" : ", " + p.House)
                    })
                    .ToList();

                comboProduct.ItemsSource = db.Products
                    .OrderBy(p => p.ProductName)
                    .ToList()
                    .Select(p => new ProductItem
                    {
                        Article = p.Article,
                        Display = p.ProductName + " (" + p.Article + ")"
                    })
                    .ToList();

                List<string> statuses = db.Orders
                    .Where(o => o.Status != null)
                    .Select(o => o.Status!)
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                if (statuses.Count == 0)
                {
                    statuses.Add("Новый");
                    statuses.Add("Завершен");
                }

                comboStatus.ItemsSource = statuses;
            }
        }

        private void LoadOrder(int orderId)
        {
            using (AppDbContext db = new AppDbContext())
            {
                Order order = db.Orders
                    .Include(o => o.OrderProducts)
                        .ThenInclude(op => op.ArticleNavigation)
                    .First(o => o.OrderId == orderId);

                comboUser.SelectedValue = order.UserId;
                comboPickup.SelectedValue = order.PickupPointId;
                datePickerOrder.SelectedDate = order.OrderDate.ToDateTime(TimeOnly.MinValue);
                datePickerDelivery.SelectedDate = order.DeliveryDate.ToDateTime(TimeOnly.MinValue);
                textCode.Text = order.Code.ToString();
                comboStatus.Text = order.Status ?? "";

                foreach (OrderProduct orderProduct in order.OrderProducts)
                {
                    items.Add(new OrderItemRow
                    {
                        Article = orderProduct.Article,
                        ProductName = orderProduct.ArticleNavigation.ProductName,
                        Count = orderProduct.Count
                    });
                }
            }
        }

        private void ButtonAddItem_Click(object sender, RoutedEventArgs e)
        {
            if (comboProduct.SelectedValue == null)
            {
                MessageBox.Show("Выберите товар из списка, затем нажмите «Добавить».", "Товар не выбран", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int count;
            if (!int.TryParse(textCount.Text, out count) || count <= 0)
            {
                MessageBox.Show("Количество должно быть целым числом больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string article = (string)comboProduct.SelectedValue;
            ProductItem selectedProduct = (ProductItem)comboProduct.SelectedItem;

            OrderItemRow? existingRow = items.FirstOrDefault(row => row.Article == article);

            if (existingRow != null)
            {
                int index = items.IndexOf(existingRow);
                items[index] = new OrderItemRow
                {
                    Article = existingRow.Article,
                    ProductName = existingRow.ProductName,
                    Count = existingRow.Count + count
                };
            }
            else
            {
                items.Add(new OrderItemRow
                {
                    Article = article,
                    ProductName = selectedProduct.Display,
                    Count = count
                });
            }

            textCount.Text = "1";
        }

        private void ButtonRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            OrderItemRow? selectedRow = listItems.SelectedItem as OrderItemRow;

            if (selectedRow == null)
            {
                MessageBox.Show("Выберите в составе заказа товар, который нужно убрать.", "Товар не выбран", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            items.Remove(selectedRow);
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (comboUser.SelectedValue == null || comboPickup.SelectedValue == null)
            {
                MessageBox.Show("Выберите клиента и пункт выдачи.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (datePickerOrder.SelectedDate == null || datePickerDelivery.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату заказа и дату доставки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (datePickerDelivery.SelectedDate < datePickerOrder.SelectedDate)
            {
                MessageBox.Show("Дата доставки не может быть раньше даты заказа. Исправьте даты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int code;
            if (!int.TryParse(textCode.Text, out code) || code < 0)
            {
                MessageBox.Show("Код получения должен быть целым неотрицательным числом.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string status = comboStatus.Text.Trim();
            if (status == "")
            {
                MessageBox.Show("Укажите статус заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (items.Count == 0)
            {
                MessageBox.Show("Добавьте в заказ хотя бы один товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    Order order;

                    if (editedOrderId == null)
                    {
                        order = new Order();
                        order.OrderId = db.Orders.Any() ? db.Orders.Max(o => o.OrderId) + 1 : 1;
                        db.Orders.Add(order);
                    }
                    else
                    {
                        order = db.Orders
                            .Include(o => o.OrderProducts)
                            .First(o => o.OrderId == editedOrderId);

                        db.OrderProducts.RemoveRange(order.OrderProducts);
                    }

                    order.UserId = (int)comboUser.SelectedValue;
                    order.PickupPointId = (int)comboPickup.SelectedValue;
                    order.OrderDate = DateOnly.FromDateTime(datePickerOrder.SelectedDate.Value);
                    order.DeliveryDate = DateOnly.FromDateTime(datePickerDelivery.SelectedDate.Value);
                    order.Code = code;
                    order.Status = status;

                    int nextItemId = db.OrderProducts.Any() ? db.OrderProducts.Max(op => op.OrderArticleId) + 1 : 1;

                    foreach (OrderItemRow row in items)
                    {
                        OrderProduct orderProduct = new OrderProduct();
                        orderProduct.OrderArticleId = nextItemId;
                        orderProduct.OrderId = order.OrderId;
                        orderProduct.Article = row.Article;
                        orderProduct.Count = row.Count;
                        db.OrderProducts.Add(orderProduct);
                        nextItemId++;
                    }

                    db.SaveChanges();
                }

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось сохранить заказ.\n\n" + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class UserItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = "";
    }

    public class PickupItem
    {
        public int PickupPointId { get; set; }
        public string Address { get; set; } = "";
    }

    public class ProductItem
    {
        public string Article { get; set; } = "";
        public string Display { get; set; } = "";
    }

    public class OrderItemRow
    {
        public string Article { get; set; } = "";
        public string ProductName { get; set; } = "";
        public int Count { get; set; }

        public string Display
        {
            get { return ProductName + " × " + Count; }
        }
    }
}