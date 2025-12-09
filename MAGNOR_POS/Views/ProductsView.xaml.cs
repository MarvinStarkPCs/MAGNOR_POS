using System.Windows.Controls;
using MAGNOR_POS.ViewModels;

namespace MAGNOR_POS.Views;

public partial class ProductsView : UserControl
{
    public ProductsView()
    {
        InitializeComponent();
        DataContext = new ProductsViewModel();
    }
}
