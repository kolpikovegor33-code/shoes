using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using shoes.Models;

namespace shoes
{
    public partial class MainWindow : Window
    {
        private const string PlaceholderPhoto = "picture.png";
        private const string AllSuppliers = "Все поставщики";

        private readonly User? currentUser;

        private readonly Brush highDiscountBrush = (Brush)new BrushConverter().ConvertFromString("#2E8B57")!;
        private readonly Brush noStockBrush = Brushes.LightBlue;

        private List<Product> allProducts = new List<Product>();

        private bool isReady = false;
        private bool isAdmin = false;

        public MainWindow(User? user)
        {
            InitializeComponent();

            currentUser = user;

            ConfigureInterface();
            LoadProducts();
            FillSupplierFilter();

            isReady = true;
            ApplyFilters();
        }

        private void ConfigureInterface()
        {
            if (currentUser == null)
            {
                textUserName.Text = "Гость";
                textUserRole.Text = "";
                return;
            }

            textUserName.Text = GetFullName(currentUser);

            string roleName = currentUser.Role?.Role1 ?? "";
            textUserRole.Text = roleName;

            isAdmin = roleName.Contains("Администратор", StringComparison.OrdinalIgnoreCase);
            bool isManager = roleName.Contains("Менеджер", StringComparison.OrdinalIgnoreCase);

            if (isAdmin || isManager)
            {
                panelActions.Visibility = Visibility.Visible;
                buttonOrders.Visibility = Visibility.Visible;
                panelFilters.Visibility = Visibility.Visible;
            }

            if (isAdmin)
            {
                buttonAddProduct.Visibility = Visibility.Visible;
                buttonDeleteProduct.Visibility = Visibility.Visible;
            }
        }

        private string GetFullName(User user)
        {
            string fullName = user.LastName + " " + user.FirstName;

            if (!string.IsNullOrWhiteSpace(user.Surname))
            {
                fullName = fullName + " " + user.Surname;
            }

            return fullName;
        }

        private void LoadProducts()
        {
            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    allProducts = db.Products
                        .Include(p => p.Manufacturer)
                        .Include(p => p.Provider)
                        .Include(p => p.ProductCategory)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось загрузить список товаров из базы данных.\n\n" + ex.Message, "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FillSupplierFilter()
        {
            List<string> suppliers = allProducts
                .Select(p => p.Provider?.Provider1)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            comboSupplier.Items.Clear();
            comboSupplier.Items.Add(AllSuppliers);

            foreach (string supplier in suppliers)
            {
                comboSupplier.Items.Add(supplier);
            }

            comboSupplier.SelectedIndex = 0;
        }

        private void RefreshProducts()
        {
            string? selectedSupplier = comboSupplier.SelectedItem as string;

            isReady = false;

            LoadProducts();
            FillSupplierFilter();

            if (selectedSupplier != null && comboSupplier.Items.Contains(selectedSupplier))
            {
                comboSupplier.SelectedItem = selectedSupplier;
            }

            isReady = true;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (!isReady)
            {
                return;
            }

            IEnumerable<Product> result = allProducts;

            string? selectedSupplier = comboSupplier.SelectedItem as string;

            if (!string.IsNullOrEmpty(selectedSupplier) && selectedSupplier != AllSuppliers)
            {
                result = result.Where(p => p.Provider?.Provider1 == selectedSupplier);
            }

            string[] words = textSearch.Text
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length > 0)
            {
                result = result.Where(p => MatchesSearch(p, words));
            }

            switch (comboSort.SelectedIndex)
            {
                case 1:
                    result = result.OrderBy(p => p.StockQuantity);
                    break;
                case 2:
                    result = result.OrderByDescending(p => p.StockQuantity);
                    break;
            }

            List<ProductViewModel> productList = result
                .Select(CreateViewModel)
                .ToList();

            listProducts.ItemsSource = productList;
            textNoResults.Visibility = productList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private bool MatchesSearch(Product product, string[] words)
        {
            string[] fields =
            {
                product.Article ?? "",
                product.ProductName ?? "",
                product.ProductCategory?.ProductCategory1 ?? "",
                product.Description ?? "",
                product.Manufacturer?.Manufacturer1 ?? "",
                product.Provider?.Provider1 ?? "",
                product.Unit ?? ""
            };

            foreach (string word in words)
            {
                bool found = fields.Any(f => f.Contains(word, StringComparison.OrdinalIgnoreCase));

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private ProductViewModel CreateViewModel(Product product)
        {
            ProductViewModel newProduct = new ProductViewModel();

            newProduct.Article = product.Article;
            newProduct.Title = product.ProductName;
            newProduct.Category = product.ProductCategory?.ProductCategory1 ?? "";
            newProduct.Description = product.Description ?? "";
            newProduct.Manufacturer = product.Manufacturer?.Manufacturer1 ?? "";
            newProduct.Supplier = product.Provider?.Provider1 ?? "";
            newProduct.Unit = product.Unit;
            newProduct.Stock = product.StockQuantity.ToString();
            newProduct.Photo = LoadPhoto(product.Photo);

            decimal finalPrice = product.Price - (product.Price * product.CurrentDiscount / 100);

            newProduct.OldPrice = product.Price.ToString("N2") + " руб.";
            newProduct.FinalPrice = finalPrice.ToString("N2") + " руб.";
            newProduct.DiscountText = product.CurrentDiscount + "%";

            if (product.CurrentDiscount > 0)
            {
                newProduct.OldPriceVisibility = Visibility.Visible;
            }
            else
            {
                newProduct.OldPriceVisibility = Visibility.Collapsed;
            }

            newProduct.Background = GetRowBrush(product);

            return newProduct;
        }

        private Brush GetRowBrush(Product product)
        {
            if (product.StockQuantity == 0)
            {
                return noStockBrush;
            }

            if (product.CurrentDiscount > 15)
            {
                return highDiscountBrush;
            }

            return Brushes.White;
        }

        private ImageSource LoadPhoto(string? photoName)
        {
            if (!string.IsNullOrWhiteSpace(photoName))
            {
                try
                {
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image", photoName);

                    if (File.Exists(filePath))
                    {
                        BitmapImage fileImage = new BitmapImage();
                        fileImage.BeginInit();
                        fileImage.CacheOption = BitmapCacheOption.OnLoad;
                        fileImage.UriSource = new Uri(filePath);
                        fileImage.EndInit();
                        return fileImage;
                    }

                    return new BitmapImage(new Uri("pack://application:,,,/Image/" + photoName));
                }
                catch
                {
                }
            }

            return new BitmapImage(new Uri("pack://application:,,,/Image/" + PlaceholderPhoto));
        }

        private void OpenProductWindow(string? article)
        {
            ProductWindow productWindow = new ProductWindow(article);
            productWindow.Owner = this;

            if (productWindow.ShowDialog() == true)
            {
                RefreshProducts();
            }
        }

        private void TextSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ComboSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ListProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!isAdmin)
            {
                return;
            }

            ProductViewModel? selectedProduct = listProducts.SelectedItem as ProductViewModel;

            if (selectedProduct == null)
            {
                return;
            }

            OpenProductWindow(selectedProduct.Article);
        }

        private void ButtonAddProduct_Click(object sender, RoutedEventArgs e)
        {
            OpenProductWindow(null);
        }
        private void ButtonOrders_Click(object sender, RoutedEventArgs e)
        {
            OrdersWindow ordersWindow = new OrdersWindow(isAdmin);
            ordersWindow.Owner = this;
            ordersWindow.ShowDialog();
        }
        private void ButtonDeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            ProductViewModel? selectedProduct = listProducts.SelectedItem as ProductViewModel;

            if (selectedProduct == null)
            {
                MessageBox.Show("Выберите в списке товар, который нужно удалить, и нажмите кнопку еще раз.", "Товар не выбран", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (AppDbContext db = new AppDbContext())
                {
                    bool isInOrder = db.OrderProducts.Any(op => op.Article == selectedProduct.Article);

                    if (isInOrder)
                    {
                        MessageBox.Show("Товар «" + selectedProduct.Title + "» нельзя удалить, потому что он есть в заказах. Сначала удалите его из заказов.", "Удаление невозможно", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    MessageBoxResult answer = MessageBox.Show("Вы действительно хотите удалить товар «" + selectedProduct.Title + "»? Это действие нельзя отменить.", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (answer != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    Product? product = db.Products.FirstOrDefault(p => p.Article == selectedProduct.Article);

                    if (product == null)
                    {
                        MessageBox.Show("Товар уже удален из базы данных. Список будет обновлен.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        db.Products.Remove(product);
                        db.SaveChanges();
                    }
                }

                RefreshProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось удалить товар.\n\n" + ex.Message, "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ButtonLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }

    public class ProductViewModel
    {
        public string Article { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public string Manufacturer { get; set; } = "";
        public string Supplier { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Stock { get; set; } = "";
        public ImageSource? Photo { get; set; }
        public string OldPrice { get; set; } = "";
        public string FinalPrice { get; set; } = "";
        public string DiscountText { get; set; } = "";

        public Visibility OldPriceVisibility { get; set; }

        public Brush Background { get; set; } = Brushes.White;
    }
}