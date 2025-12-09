using MAGNOR_POS.Models.Inventory;

namespace MAGNOR_POS.ViewModels;

public class CartItem : ViewModelBase
{
    private Product _product = null!;
    private decimal _quantity;

    public Product Product
    {
        get => _product;
        set
        {
            if (SetProperty(ref _product, value))
            {
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Tax));
                OnPropertyChanged(nameof(Total));
            }
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                OnPropertyChanged(nameof(Subtotal));
                OnPropertyChanged(nameof(Tax));
                OnPropertyChanged(nameof(Total));
            }
        }
    }

    public decimal Subtotal => Quantity * Product.SalePrice;
    public decimal Tax => Subtotal * Product.TaxRate;
    public decimal Total => Subtotal + Tax;
}
