using POS.Application.Common.Enums;
using POS.Application.Interfaces;

namespace Desktop.Admin.ProductItemForms;
public partial class ProductItemTable : UserControl
{
    private readonly IBusinessUnit _businessUnit;
    private State _selectedState = State.All;

    public ProductItemTable(IBusinessUnit businessUnit)
    {
        InitializeComponent();
        _businessUnit = businessUnit;
    }

    private async void ProductItemTable_Load(object sender, EventArgs e)
    {
        await Task.Run(() => FillProductItems(_selectedState));
    }

    /// <summary>
    /// Ombor mahsulotlari jadvalini to'ldirish
    /// </summary>
    private async Task FillProductItems(State selected)
    {
        var list = await _businessUnit.ProductItemService.GetAllAsync();

        if (IsHandleCreated)
        {
            table.BeginInvoke(() =>
            {
                table.DataSource = list.Select(i => new
                {
                    Id = i.Id,
                    Mahsulot = i.ProductName,
                    Miqdori = i.Amount,
                    Olish_Narxi = i.BuyingPrice,
                    Sotish_Narxi = i.SellingPrice,
                    Sana = i.AddedDate
                }).ToList();
            });
        }
    }

    private async void addbtn_Click(object sender, EventArgs e)
    {
        var form = new AddProductItemForm(_businessUnit);
        var result = form.ShowDialog();
        if (result == DialogResult.OK)
        {
            await Task.Run(() => FillProductItems(_selectedState));
        }
    }

    private void editbtn_Click(object sender, EventArgs e)
    {
        // TODO: Edit form implement qilish
    }
}
