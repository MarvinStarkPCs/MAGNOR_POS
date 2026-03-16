using System.Windows;
using MAGNOR_POS.ViewModels;

namespace MAGNOR_POS.Views;

public partial class TableManagementWindow : Window
{
    public TableManagementWindow()
    {
        InitializeComponent();
        Loaded += async (s, e) =>
        {
            if (DataContext is TableManagementViewModel viewModel)
            {
                await viewModel.LoadDataAsync();
            }
        };
    }
}
