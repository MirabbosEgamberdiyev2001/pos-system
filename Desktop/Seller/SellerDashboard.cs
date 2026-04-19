using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.DataTransferObjects.ReceiptDtos;
using POS.Application.Common.DataTransferObjects.TransactionDtos;
using POS.Application.Interfaces;

namespace Desktop.Seller;

public partial class SellerDashboard : Form
{
    private readonly IBusinessUnit _businessUnit;
    private readonly int _sellerId;
    private List<ReceiptItemDto> _cart = new();
    private List<ProductDto> _allProducts = new();

    public SellerDashboard(IBusinessUnit businessUnit, int sellerId)
    {
        _businessUnit = businessUnit;
        _sellerId = sellerId;
        InitializeComponent();
    }

    private async void SellerDashboard_Load(object sender, EventArgs e)
    {
        await LoadProductsAsync();
        UpdateCartTotal();
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            _allProducts = (await _businessUnit.ProductService.GetAllActivesAsync(0)).ToList();
            RefreshProductList(_allProducts);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Mahsulotlarni yuklashda xato: {ex.Message}", "Xato",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshProductList(IEnumerable<ProductDto> products)
    {
        lstProducts.Items.Clear();
        foreach (var p in products.Where(p => p.Amount > 0))
        {
            lstProducts.Items.Add(new ListViewItem(new[]
            {
                p.Name,
                p.Amount.ToString("N2"),
                GetProductPrice(p).ToString("N0") + " so'm"
            })
            { Tag = p });
        }
    }

    private decimal GetProductPrice(ProductDto p) => 0; // ProductItem orqali keladi

    private void txtSearch_TextChanged(object sender, EventArgs e)
    {
        var query = txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(query)
            ? _allProducts
            : _allProducts.Where(p => p.Name.ToLower().Contains(query));
        RefreshProductList(filtered);
    }

    private void btnAddToCart_Click(object sender, EventArgs e)
    {
        if (lstProducts.SelectedItems.Count == 0)
        {
            MessageBox.Show("Mahsulot tanlang!", "Diqqat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(txtQuantity.Text, out var qty) || qty <= 0)
        {
            MessageBox.Show("To'g'ri miqdor kiriting!", "Diqqat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(txtPrice.Text, out var price) || price <= 0)
        {
            MessageBox.Show("To'g'ri narx kiriting!", "Diqqat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var product = (ProductDto)lstProducts.SelectedItems[0].Tag!;

        if (qty > product.Amount)
        {
            MessageBox.Show($"Omborda faqat {product.Amount:N2} {product.MeasurmentType} mavjud!",
                "Yetarli emas", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var existing = _cart.FirstOrDefault(c => c.ProductId == product.Id);
        if (existing != null)
        {
            existing.Quantity += (int)qty;
            existing.TotalPrice = existing.ProductPrice * existing.Quantity;
        }
        else
        {
            _cart.Add(new ReceiptItemDto
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductPrice = price,
                Quantity = (int)qty,
                TotalPrice = price * qty
            });
        }

        UpdateCartDisplay();
        UpdateCartTotal();
    }

    private void btnRemoveFromCart_Click(object sender, EventArgs e)
    {
        if (lstCart.SelectedItems.Count == 0) return;

        var index = lstCart.SelectedItems[0].Index;
        if (index >= 0 && index < _cart.Count)
        {
            _cart.RemoveAt(index);
            UpdateCartDisplay();
            UpdateCartTotal();
        }
    }

    private void UpdateCartDisplay()
    {
        lstCart.Items.Clear();
        foreach (var item in _cart)
        {
            lstCart.Items.Add(new ListViewItem(new[]
            {
                item.ProductName,
                item.Quantity.ToString(),
                item.ProductPrice.ToString("N0"),
                item.TotalPrice.ToString("N0") + " so'm"
            }));
        }
    }

    private void UpdateCartTotal()
    {
        var total = _cart.Sum(i => i.TotalPrice);
        lblTotal.Text = $"Jami: {total:N0} so'm";
    }

    private async void btnCheckout_Click(object sender, EventArgs e)
    {
        if (_cart.Count == 0)
        {
            MessageBox.Show("Savat bo'sh!", "Diqqat", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!decimal.TryParse(txtPaidCash.Text, out var cash)) cash = 0;
        if (!decimal.TryParse(txtPaidCard.Text, out var card)) card = 0;

        var total = _cart.Sum(i => i.TotalPrice);
        if (cash + card < total)
        {
            MessageBox.Show($"To'lov yetarli emas!\nKerak: {total:N0} so'm\nKiritildi: {cash + card:N0} so'm",
                "To'lov xatosi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            btnCheckout.Enabled = false;
            var receiptDto = new AddReceiptDto
            {
                SellerId = _sellerId,
                PaidCash = cash,
                PaidCard = card
            };

            var receipt = await _businessUnit.ReceiptService.AddAsync(receiptDto, _cart);
            var change = cash + card - receipt.TotalPrice;

            MessageBox.Show(
                $"Sotuv muvaffaqiyatli!\n\nChek raqami: #{receipt.Id}\nJami: {receipt.TotalPrice:N0} so'm\nQaytim: {change:N0} so'm",
                "Muvaffaqiyat", MessageBoxButtons.OK, MessageBoxIcon.Information);

            _cart.Clear();
            UpdateCartDisplay();
            UpdateCartTotal();
            txtPaidCash.Text = "";
            txtPaidCard.Text = "";
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Xato: {ex.Message}", "Xato", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnCheckout.Enabled = true;
        }
    }

    private void btnClearCart_Click(object sender, EventArgs e)
    {
        if (_cart.Count == 0) return;
        if (MessageBox.Show("Savatni tozalash?", "Tasdiqlash",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _cart.Clear();
            UpdateCartDisplay();
            UpdateCartTotal();
        }
    }
}
