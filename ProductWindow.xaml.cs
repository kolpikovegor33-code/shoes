using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using shoes.Models;

namespace shoes
{
    public partial class ProductWindow : Window
    {
        private const int MaxPhotoWidth = 300;
        private const int MaxPhotoHeight = 200;
        private const string PlaceholderPhoto = "picture.png";

        private string? editedArticle;
        private string? newPhoto;
        private bool isSaved = false;
        private AppDbContext db = new AppDbContext();

        private static string PhotoFolder
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image"); }
        }

        public ProductWindow(string? article)
        {
            InitializeComponent();
            editedArticle = article;
            Title = "Добавление товара";
            textHeader.Text = Title;
            LoadPhoto(null);
            try
            {
                comboCategory.ItemsSource = db.ProductCategories.OrderBy(item => item.ProductCategory1).ToList();
                comboManufacturer.ItemsSource = db.Manufacturers.OrderBy(item => item.Manufacturer1).ToList();
                comboProvider.ItemsSource = db.Providers.OrderBy(item => item.Provider1).ToList();
                if (editedArticle != null)
                {
                    Title = "Редактирование товара";
                    textHeader.Text = Title;
                    LoadProduct();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить данные", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProduct()
        {
            Product product = db.Products.First(item => item.Article == editedArticle);
            textArticle.Text = product.Article;
            textArticle.IsReadOnly = true;
            textName.Text = product.ProductName;
            comboCategory.SelectedValue = product.ProductCategoryId;
            textDescription.Text = product.Description;
            comboManufacturer.SelectedValue = product.ManufacturerId;
            comboProvider.SelectedValue = product.ProviderId;
            textUnit.Text = product.Unit;
            textPrice.Text = product.Price.ToString("0.00");
            textDiscount.Text = product.CurrentDiscount.ToString();
            textStock.Text = product.StockQuantity.ToString();
            LoadPhoto(product.Photo);
        }

        private void LoadPhoto(string? photoName)
        {
            imagePhoto.Source = CreateImage(photoName);
        }

        private BitmapImage CreateImage(string? photoName)
        {
            if (!string.IsNullOrWhiteSpace(photoName))
            {
                try
                {
                    string filePath = Path.Combine(PhotoFolder, photoName);
                    if (File.Exists(filePath))
                    {
                        return LoadFromFile(filePath);
                    }

                    return new BitmapImage(new Uri("pack://application:,,,/Image/" + photoName));
                }
                catch (Exception)
                {
                }
            }

            return new BitmapImage(new Uri("pack://application:,,,/Image/" + PlaceholderPhoto));
        }

        private BitmapImage LoadFromFile(string filePath)
        {
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(filePath);
            image.EndInit();
            return image;
        }

        private void ButtonChoosePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Title = "Выбор фото товара";
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(PhotoFolder);
                string fileName = Guid.NewGuid().ToString("N") + ".png";
                SaveResizedPhoto(dialog.FileName, Path.Combine(PhotoFolder, fileName));

                DeletePhotoFile(newPhoto);
                newPhoto = fileName;
                LoadPhoto(newPhoto);
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось загрузить изображение. Выберите файл в формате jpg, png или bmp.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveResizedPhoto(string sourcePath, string targetPath)
        {
            BitmapImage source = LoadFromFile(sourcePath);
            BitmapSource result = source;

            double scale = Math.Min(1.0, Math.Min((double)MaxPhotoWidth / source.PixelWidth, (double)MaxPhotoHeight / source.PixelHeight));
            if (scale < 1.0)
            {
                result = new TransformedBitmap(source, new ScaleTransform(scale, scale));
            }

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(result));
            using (FileStream stream = new FileStream(targetPath, FileMode.Create))
            {
                encoder.Save(stream);
            }
        }

        private void DeletePhotoFile(string? photoName)
        {
            if (string.IsNullOrWhiteSpace(photoName) || photoName == PlaceholderPhoto)
            {
                return;
            }

            try
            {
                string filePath = Path.Combine(PhotoFolder, photoName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception)
            {
            }
        }

        private void ButtonBack_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            if (!isSaved)
            {
                DeletePhotoFile(newPhoto);
            }

            db.Dispose();
            base.OnClosed(e);
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            string article = textArticle.Text.Trim();
            string name = textName.Text.Trim();
            string description = textDescription.Text.Trim();
            string unit = textUnit.Text.Trim();
            if (article == "" || name == "" || description == "" || unit == "" || comboCategory.SelectedValue == null
                || comboManufacturer.SelectedValue == null || comboProvider.SelectedValue == null)
            {
                MessageBox.Show("Заполните артикул, наименование, категорию, описание, производителя, поставщика и единицу измерения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            decimal price;
            if (!decimal.TryParse(textPrice.Text.Replace('.', ','), out price) || price < 0 || Math.Round(price, 2) != price)
            {
                MessageBox.Show("Цена должна быть числом не меньше 0 с копейками не более двух знаков", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int discount;
            if (!int.TryParse(textDiscount.Text, out discount) || discount < 0 || discount > 100)
            {
                MessageBox.Show("Скидка должна быть целым числом от 0 до 100", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            short stock;
            if (!short.TryParse(textStock.Text, out stock) || stock < 0)
            {
                MessageBox.Show("Количество на складе должно быть целым числом от 0 до 32767", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                Product product;
                if (editedArticle == null)
                {
                    if (db.Products.Any(item => item.Article == article))
                    {
                        MessageBox.Show("Товар с таким артикулом уже есть", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    product = new Product();
                    product.Article = article;
                    db.Products.Add(product);
                }
                else
                {
                    product = db.Products.First(item => item.Article == editedArticle);
                }
                product.ProductName = name;
                product.ProductCategoryId = (int)comboCategory.SelectedValue;
                product.Description = description;
                product.ManufacturerId = (int)comboManufacturer.SelectedValue;
                product.ProviderId = (int)comboProvider.SelectedValue;
                product.Unit = unit;
                product.Price = price;
                product.CurrentDiscount = discount;
                product.StockQuantity = stock;

                string? oldPhoto = product.Photo;
                if (newPhoto != null)
                {
                    product.Photo = newPhoto;
                }

                db.SaveChanges();

                if (newPhoto != null)
                {
                    DeletePhotoFile(oldPhoto);
                }

                isSaved = true;
                DialogResult = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось сохранить товар", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}