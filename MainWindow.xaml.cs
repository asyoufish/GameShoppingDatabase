using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
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

namespace FinalTszFungChan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            clearAllRecords();
            resetGames();
            loadGames();
        }

        private void resetGames()
        {
            using (var context = new GameShoppingDBEntities())
            {
                var fifa = context.Games.Find(1);
                fifa.Stock = 10;
                var cod = context.Games.Find(2);
                cod.Stock = 5;
                var mc = context.Games.Find(3);
                mc.Stock = 1;
                var nfs = context.Games.Find(4);
                nfs.Stock = 0;
                var gtav = context.Games.Find(5);
                gtav.Stock = 2;

                context.SaveChanges();

                var games = context.Games.ToList();

                grdGames.ItemsSource = games;
            }
        }

        private void loadGames()
        {
            using(var context = new GameShoppingDBEntities())
            {
                var games = context.Games.ToList();

                grdGames.ItemsSource = games;
            }
        }

        private void btnPurchase_Click(object sender, RoutedEventArgs e)
        {
            int selectedGameID = grdGames.SelectedIndex + 1;

            if (selectedGameID == 0)
            {
                MessageBox.Show("Please select a game to continue", "Select", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (tbCustName.Text.Equals(""))
            {
                MessageBox.Show("Please enter a name to continue", "Enter Name", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (tbQuan.Text.Equals(""))
            {
                MessageBox.Show("Please enter quantity to continue", "Enter Quantity", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var context = new GameShoppingDBEntities())
            {
                try
                {
                    string custName = tbCustName.Text;
                    int quantity = int.Parse(tbQuan.Text);
                    var game = context.Games.Find(selectedGameID);
                    var lastCust = from customer in context.Customers
                                   orderby customer.CustomerId descending
                                   select customer;
                    var lastOrder = from order in context.Orders
                                    orderby order.OrderId descending
                                    select order;
                    
                    int custID = 1;
                    int orderID = 1;

                    if (lastCust.Any())
                    {
                        custID = lastCust.First().CustomerId + 1;
                    }
                    if (lastOrder.Any())
                    {
                        orderID = lastOrder.First().OrderId + 1;
                    }

                    // Zero Stock Case
                    if (game.Stock == 0)
                    {
                        MessageBox.Show("The stock of this game is 0", "Zero Stock", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    // Not enough Stock Case
                    else if (quantity > game.Stock)
                    {
                        MessageBox.Show("The game doesn't have enough stock", "Stock", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    // Purchase Case
                    else if (quantity <= game.Stock)
                    {
                        // Check for existing Customers
                        var cust = from customer in context.Customers
                                   where customer.CustomerName == custName
                                   select customer;
                        // If it's a new customer, add new customer to DB
                        if (!cust.Any())
                        {
                            Customer newCust = new Customer();
                            newCust.CustomerName = custName;
                            newCust.CustomerId = custID;

                            context.Customers.Add(newCust);
                        }
                        // Else use the existing customer's ID
                        else if (cust.Any())
                        {
                            custID = cust.First().CustomerId;
                        }

                        // Add new order
                        Order order = new Order();
                        order.OrderId = orderID;
                        order.Date = DateTime.Now;
                        order.CustomerId = custID;
                        order.GameId = selectedGameID;
                        order.Quantity = quantity;

                        double total = quantity * context.Games.Find(selectedGameID).Price;

                        // If quantity is equal or more than 5, discount needed
                        if (quantity >= 5)
                        {
                            order.Discount = total * 0.1;
                        }

                        game.Stock = game.Stock - quantity;

                        context.Orders.Add(order);

                        context.SaveChanges();

                        double tax = total * 0.13;

                        tbPrice.Text = "$" + game.Price.ToString();
                        tbQuan1.Text = quantity.ToString();
                        tbTotal.Text = "$" + total.ToString();
                        tbDiscount.Text = "$" + ((decimal)order.Discount).ToString("F");
                        tbTax.Text = "$" + ((decimal)tax).ToString("F");
                        tbNetTotal.Text = "$" + ((decimal)(total - order.Discount + tax)).ToString("F");

                        MessageBox.Show("Thank you for your purchase", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (FormatException ex)
                {
                    MessageBox.Show(ex.Message);
                }

                loadGames();
                loadCustomers();
                loadAllTransaction();
            }
        }

        private void btnDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            clearAllRecords();
            loadCustomers();
            loadAllTransaction();
            grdTransByCust.ItemsSource = null;
            grdAllTrans.ItemsSource = null;
        }

        private void clearAllRecords()
        {
            using (var context = new GameShoppingDBEntities())
            {

                foreach (var record in context.Customers)
                {
                    context.Customers.Remove(record);
                }
                foreach (var record in context.Orders)
                {
                    context.Orders.Remove(record);
                }
                context.Database.ExecuteSqlCommand("DBCC CHECKIDENT('Orders', RESEED, 0)");
                context.Database.ExecuteSqlCommand("DBCC CHECKIDENT('Customers', RESEED, 0)");
                context.SaveChanges();
            }
        }

        private void loadCustomers()
        {
            using (var context = new GameShoppingDBEntities())
            {
                lbCustomers.Items.Clear();
                var customers = from customer in context.Customers
                                select customer;
                foreach (var customer in customers)
                {
                    lbCustomers.Items.Add(customer.CustomerName);
                }
            }
        }

        private void lbCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = lbCustomers.SelectedIndex;

            if (index != -1)
            {
                using (var context = new GameShoppingDBEntities())
                {
                    var cust = from customer in context.Customers
                               where customer.CustomerId == (index + 1)
                               select customer;

                    int customerID = 0;

                    if (cust.Any())
                    {
                        customerID = cust.First().CustomerId;
                        var transactions = from order in context.Orders
                                           where order.CustomerId == customerID
                                           select new
                                           {
                                               order.OrderId,
                                               Date = order.Date.Year + "-" + order.Date.Month + "-" + order.Date.Day,
                                               order.Customer.CustomerName,
                                               order.Game.GameName,
                                               Price = "$" + order.Game.Price,
                                               order.Quantity,
                                               Discount = "$" + order.Discount,
                                               Tax = "$" + order.Quantity * order.Game.Price * 0.13,
                                               Total = "$" + (order.Quantity * order.Game.Price - order.Discount + order.Quantity * order.Game.Price * 0.13)
                                           };

                        grdTransByCust.ItemsSource = transactions.ToList();
                    }
                }
            }
        }

        private void loadAllTransaction()
        {
            using (var context = new GameShoppingDBEntities())
            {
                var transactions = from order in context.Orders
                                   select new 
                                   { 
                                       order.OrderId,
                                       Date = order.Date.Year + "-" + order.Date.Month + "-" + order.Date.Day, 
                                       order.Customer.CustomerName, 
                                       order.Game.GameName, 
                                       Price = "$" + order.Game.Price, 
                                       order.Quantity, 
                                       Discount = "$" + order.Discount, 
                                       Tax = "$" + order.Quantity * order.Game.Price * 0.13,
                                       Total = "$" + (order.Quantity * order.Game.Price - order.Discount + order.Quantity * order.Game.Price * 0.13)
                                   };
                if (transactions.Any())
                {
                    grdAllTrans.ItemsSource = transactions.ToList();
                }
            }
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            tbCustName.Clear();
            tbQuan.Clear();
            tbPrice.Clear();
            tbQuan1.Clear();
            tbTotal.Clear();
            tbDiscount.Clear();
            tbTax.Clear();
            tbNetTotal.Clear();

        }

        private void btnNewQuan_Click(object sender, RoutedEventArgs e)
        {
            using (var context = new GameShoppingDBEntities())
            {
                Random rnd = new Random();
                foreach (var game in context.Games)
                {
                    game.Stock = rnd.Next(20);
                }

                context.SaveChanges();

                var games = context.Games.ToList();

                grdGames.ItemsSource = games;
            }
        }
    }
}
