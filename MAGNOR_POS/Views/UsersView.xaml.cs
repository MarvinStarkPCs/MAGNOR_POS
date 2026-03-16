using System.Windows.Controls;
using MAGNOR_POS.ViewModels;

namespace MAGNOR_POS.Views;

public partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();
    }

    private async void UserControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is UsersViewModel viewModel)
        {
            await viewModel.LoadUsersAsync();
        }
    }
}
