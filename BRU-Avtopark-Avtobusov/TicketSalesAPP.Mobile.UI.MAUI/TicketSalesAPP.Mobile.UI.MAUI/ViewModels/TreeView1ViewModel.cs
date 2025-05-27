using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class TreeView1ViewModel : ObservableObject
    {
        TreeViewNode root;
        public ObservableCollection<TreeViewNode> Nodes => root.Nodes;

        [ObservableProperty]
        string? filterString;

        [ObservableProperty]
        int checkedNodesCount;

        public TreeView1ViewModel()
        {
            root = TreeViewNode.Generate();
        }

        [RelayCommand]
        async Task HandleActionAsync(TreeViewNode node)
        {
            if (!node.IsFolder)
                await Shell.Current.DisplayAlert(node.Name, "Action is executed", "OK");
        }
        [RelayCommand]
        private void TextChanged(string text)
        {
            FilterString = $"Contains([Name], '{text}')";
        }
    }

    public partial class TreeViewNode : ObservableObject
    {
        public static TreeViewNode Generate()
        {
            var root = new TreeViewNode();

            var customers = new TreeViewNode() { Name = "Customers", IsFolder = true };
            var orders = new TreeViewNode() { Name = "Orders", IsFolder = true };
            orders.AddNode(new TreeViewNode() { Name = "Detail.pdf", IsFolder = false });
            orders.AddNode(new TreeViewNode() { Name = "Summary.pdf", IsFolder = false });
            customers.AddNode(orders);
            customers.AddNode(new TreeViewNode() { Name = "Balance sheet.pdf", IsFolder = false });
            customers.AddNode(new TreeViewNode() { Name = "Revenue by company.pdf", IsFolder = false });

            var employees = new TreeViewNode() { Name = "Employees", IsFolder = true };
            employees.AddNode(new TreeViewNode() { Name = "Arrival card.pdf", IsFolder = false });
            employees.AddNode(new TreeViewNode() { Name = "Employee comparison.pdf", IsFolder = false });
            employees.AddNode(new TreeViewNode() { Name = "Employee location.pdf", IsFolder = false });
            employees.AddNode(new TreeViewNode() { Name = "Employee performance review.pdf", IsFolder = false });
            employees.AddNode(new TreeViewNode() { Name = "Letter.pdf", IsFolder = false });

            var marketResearch = new TreeViewNode() { Name = "Market Research", IsFolder = true };
            marketResearch.AddNode(new TreeViewNode() { Name = "Market share.pdf", IsFolder = false });
            marketResearch.AddNode(new TreeViewNode() { Name = "Polling.pdf", IsFolder = false });
            marketResearch.AddNode(new TreeViewNode() { Name = "Population.pdf", IsFolder = false });
            marketResearch.AddNode(new TreeViewNode() { Name = "Profit and loss.pdf", IsFolder = false });

            var products = new TreeViewNode() { Name = "Products", IsFolder = true };
            var barCodes = new TreeViewNode() { Name = "Bar Codes", IsFolder = true };
            barCodes.AddNode(new TreeViewNode() { Name = "All product labels.pdf", IsFolder = false });
            barCodes.AddNode(new TreeViewNode() { Name = "Code types.pdf", IsFolder = false });
            barCodes.AddNode(new TreeViewNode() { Name = "Product label.pdf", IsFolder = false });
            var crossBand = new TreeViewNode() { Name = "Cross Band", IsFolder = true };
            crossBand.AddNode(new TreeViewNode() { Name = "Invoice.pdf", IsFolder = false });
            crossBand.AddNode(new TreeViewNode() { Name = "Product list.pdf", IsFolder = false });
            var realLife = new TreeViewNode() { Name = "Real-Life", IsFolder = true };
            realLife.AddNode(new TreeViewNode() { Name = "Restaurant menu.pdf", IsFolder = false });
            realLife.AddNode(new TreeViewNode() { Name = "Roll paper.pdf", IsFolder = false });
            products.AddNode(barCodes);
            products.AddNode(crossBand);
            products.AddNode(realLife);
            products.AddNode(new TreeViewNode() { Name = "Sorting products.pdf", IsFolder = false });

            root.AddNode(customers);
            root.AddNode(employees);
            root.AddNode(marketResearch);
            root.AddNode(products);

            return root;
        }

        public bool IsFolder { get; set; }
        public string Name { get; set; }

        public ObservableCollection<TreeViewNode> Nodes { get; }
        public TreeViewNode? Parent { get; private set; }

        public TreeViewNode()
        {
            Nodes = new();
            Name = "";
        }
        public void AddNode(TreeViewNode node)
        {
            Nodes.Add(node);
            node.Parent = this;
        }
    }
}