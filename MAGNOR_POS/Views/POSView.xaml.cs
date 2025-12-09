using System.Windows.Controls;
using MAGNOR_POS.ViewModels;

namespace MAGNOR_POS.Views;

public partial class POSView : UserControl
{
    public POSView()
    {
        InitializeComponent();
        DataContext = new POSViewModel();
    }
}
