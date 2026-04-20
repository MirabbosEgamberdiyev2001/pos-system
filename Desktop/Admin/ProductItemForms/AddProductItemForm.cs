using Desktop.Extended;
using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.DataTransferObjects.WarehouseItemDtos;
using POS.Application.Common.Exceptions;
using POS.Application.Interfaces;

namespace Desktop.Admin.ProductItemForms;
public partial class AddProductItemForm : Form
{
    private readonly IBusinessUnit _businessUnit;
    private readonly int _adminId;
    public List<ProductItemDto> _productItems = new();
    public List<ProductDto> Products { get; set; } = new();

    public AddProductItemForm(IBusinessUnit businessUnit, int adminId = 0)
    {
        InitializeComponent();
        _businessUnit = businessUnit;
        _adminId = adminId;
    }

    private void CanselBtn_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private async void Save_btn_Click_1(object sender, EventArgs e)
    {
        try
        {
            _productItems = await _businessUnit.ProductItemService.GetAllAsync();

            var matchItem = _productItems.FirstOrDefault(x => x.ProductName == mahsulotlar.Text);
            if (matchItem == null)
            {
                new Toastr().ShowError("Mahsulot tanlanmagan!");
                return;
            }

            if (!decimal.TryParse(Miqdori.Text, out var amount) || amount <= 0)
            {
                new Toastr().ShowError("Miqdor noto'g'ri kiritilgan!");
                return;
            }
            if (!decimal.TryParse(Outcame_price.Text, out var buyingPrice) || buyingPrice <= 0)
            {
                new Toastr().ShowError("Xarid narxi noto'g'ri kiritilgan!");
                return;
            }
            if (!decimal.TryParse(Income_price.Text, out var sellingPrice) || sellingPrice <= 0)
            {
                new Toastr().ShowError("Sotuv narxi noto'g'ri kiritilgan!");
                return;
            }
            if (!DateTime.TryParse(Income_date.Text, out var broughtDate))
            {
                new Toastr().ShowError("Sana noto'g'ri kiritilgan!");
                return;
            }

            var dto = new AddProductItemDto
            {
                ProductId    = matchItem.ProductId,
                Amount       = amount,
                BuyingPrice  = buyingPrice,
                SellingPrice = sellingPrice,
                BroughtDate  = broughtDate,
                AdminId      = _adminId
            };

            await _businessUnit.ProductItemService.AddAsync(dto);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (MarketException ex)
        {
            new Toastr().ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            new Toastr().ShowError(ex.Message);
        }
    }

    private async void AddProductItemForm_Load(object sender, EventArgs e)
    {
        var products = await _businessUnit.ProductService.GetAllAsync();
        mahsulotlar.DataSource = products.Select(x => x.Name).ToArray();
    }
}
