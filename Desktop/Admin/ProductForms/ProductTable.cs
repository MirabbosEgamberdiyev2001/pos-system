using Desktop.Extended;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.DataTransferObjects.CategoryDtos;
using POS.Application.Common.DataTransferObjects.ProductDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Models;
using POS.Application.Interfaces;

namespace Desktop.Admin.ProductForms;
public partial class ProductTable : UserControl
{
    private readonly IBusinessUnit _businessUnit;
    private List<CategoryDto> _categories = new();
    private int selectedId = 0;
    private State selected = State.All;
    private int selectedCategoryId = 0;

    // Search debounce timer
    private readonly System.Windows.Forms.Timer _searchDebounce = new() { Interval = 300 };

    public ProductTable(IBusinessUnit businessUnit)
    {
        InitializeComponent();
        _businessUnit = businessUnit;
        ProductCategoryComboBox.SelectedIndex = 0;
        _searchDebounce.Tick += async (s, e) =>
        {
            _searchDebounce.Stop();
            await SearchProducts(search_textbox.Text);
        };
    }

    private async void addbtn_Click(object sender, EventArgs e)
    {
        AddProductForm form = new(_businessUnit);
        var result = form.ShowDialog();
        if (result == DialogResult.OK)
        {
            new Toastr().ShowSuccess();
            await Task.Run(() => FillProducts(selected));
        }
    }

    private async void deletebtn_Click(object sender, EventArgs e)
    {
        if (selectedId != 0)
        {
            var result = new Modal().ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    await _businessUnit.ProductService.ActionAsync(selectedId, ActionType.Remove);
                    new Toastr().ShowSuccess("Muvoffaqqiyatli o'chirildi");
                }
                catch (Exception)
                {
                    new Toastr().ShowError("Xatolik yuz berdi!");
                }
                finally
                {
                    await Task.Run(() => FillProducts(selected));
                    selectedId = 0;
                }
            }
        }
        else
        {
            new Toastr().ShowWarning("Mahsulotlardan birini tanlang!");
        }
    }

    private void table_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        if (table.SelectedRows.Count > 0 &&
            table.SelectedRows[0].Cells[0].Value != null)
            selectedId = int.Parse(table.SelectedRows[0].Cells[0].Value.ToString()!);
    }

    /// <summary>
    /// Mahsulotlar jadvalini to'ldirish
    /// </summary>
    private async Task FillProducts(State selected)
    {
        var list = selected switch
        {
            State.Active  => await _businessUnit.ProductService.GetAllActivesAsync(selectedCategoryId),
            State.Archive => await _businessUnit.ProductService.GetAllArchivesAsync(selectedCategoryId),
            _             => await _businessUnit.ProductService.GetAllAsync()
        };

        if (IsHandleCreated)
        {
            table.BeginInvoke(() =>
            {
                table.DataSource = list.Select(i => new
                {
                    Id           = i.Id,
                    Kodi         = i.Barcode,
                    Nomi         = i.Name,
                    Miqdori      = i.Amount,
                    OlchovTuri   = i.MeasurmentType.ToString(),
                    Kategoriyasi = i.Category?.Name ?? "-"
                }).ToList();
            });
        }
    }

    /// <summary>
    /// Load: kategoriya filter + mahsulotlar
    /// </summary>
    private async void ProductTable_Load(object sender, EventArgs e)
    {
        await Task.Run(() => FillProducts(selected));
        _categories = (await _businessUnit.CategoryService.GetAllAsync()).ToList();

        var comboItems = new List<string> { "Barcha kategoriyalar" };
        comboItems.AddRange(_categories.Select(c => c.Name));
        FilterComboBox.DataSource = comboItems.ToArray();
    }

    private async Task FillProductSelectetCategory(string selectedCategoryName)
    {
        try
        {
            var list = await _businessUnit.ProductService.GetAllAsync();
            var selectedList = list.Where(i => i.Category?.Name == selectedCategoryName).ToList();
            if (IsHandleCreated)
                table.BeginInvoke(() =>
                {
                    table.DataSource = selectedList.Select(i => new
                    {
                        Id           = i.Id,
                        Kodi         = i.Barcode,
                        Nomi         = i.Name,
                        Miqdori      = i.Amount,
                        OlchovTuri   = i.MeasurmentType.ToString(),
                        Kategoriyasi = i.Category?.Name ?? "-"
                    }).ToList();
                });
        }
        catch (Exception)
        {
            new Toastr().ShowError("Xato yuz berdi. Iltimos, qayta urinib ko'ring.");
        }
    }

    private async void FilterComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        try
        {
            string? selectedCategoryName = FilterComboBox.SelectedItem?.ToString();
            if (selectedCategoryName == "Barcha kategoriyalar" || string.IsNullOrEmpty(selectedCategoryName))
            {
                selectedCategoryId = 0;
                await FillProducts(selected);
            }
            else
            {
                var category = _categories.FirstOrDefault(c => c.Name == selectedCategoryName);
                selectedCategoryId = category?.Id ?? 0;
                await FillProductSelectetCategory(selectedCategoryName);
            }
        }
        catch (Exception)
        {
            new Toastr().ShowError("Xatolik yuz berdi! Iltimos, qayta urinib ko'ring.");
        }
    }

    private async void ArchiveBtn_Click(object sender, EventArgs e)
    {
        if (selectedId != 0)
        {
            try
            {
                if (selected == State.Active || selected == State.All)
                {
                    ArchiveBtn.Text = "Arxivlash";
                    var result = new Modal("Rostdan ham arxivlamoqchimisiz?").ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        await _businessUnit.ProductService.ActionAsync(selectedId, ActionType.Archive);
                        await Task.Run(() => FillProducts(selected));
                        new Toastr().ShowSuccess("Muvoffaqqiyatli arxivelandi");
                    }
                }
                else
                {
                    ArchiveBtn.Text = "Faollashtirish";
                    var result = new Modal("Rostdan ham arxivdan chiqarmoqchimisan?").ShowDialog();
                    if (result == DialogResult.OK)
                    {
                        await _businessUnit.ProductService.ActionAsync(selectedId, ActionType.Recover);
                        await Task.Run(() => FillProducts(selected));
                        new Toastr().ShowSuccess("Muvoffaqqiyatli arxivdan chiqarildi");
                    }
                }
            }
            catch (Exception)
            {
                new Toastr().ShowError("Arxivelashda hatolik yuz berdi");
            }
        }
        else
        {
            new Toastr().ShowWarning(
                ProductCategoryComboBox.Text == "Arxivlangan"
                    ? "Faollashtirish uchun kategoriyalardan birini tanlang!"
                    : "Arxivelash uchun kategoriyalardan birini tanlang!");
        }
    }

    private void UpdateArchiveButton()
    {
        if (ProductCategoryComboBox.Text == "Arxivlangan")
        {
            ArchiveBtn.Text = "Faollashtirish";
            ArchiveBtn.Visible = true;
        }
        else if (ProductCategoryComboBox.Text == "Aktiv")
        {
            ArchiveBtn.Text = "Arxivlash";
            ArchiveBtn.Visible = true;
        }
        else
        {
            ArchiveBtn.Visible = false;
        }
    }

    private async void ProductCategoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        selected = ProductCategoryComboBox.Text switch
        {
            "Aktiv"      => State.Active,
            "Arxivlangan" => State.Archive,
            _            => State.All
        };
        await Task.Run(() => FillProducts(selected));
        UpdateArchiveButton();
    }

    // Debounced search
    private void search_textbox_TextChanged(object sender, EventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private async Task SearchProducts(string text)
    {
        try
        {
            if (!string.IsNullOrEmpty(text))
            {
                var filter = await _businessUnit.ProductService
                    .FilterByNameAsync(text, selected, selectedCategoryId);
                var searchResult = filter.Select(i => new
                {
                    Id           = i.Id,
                    Kodi         = i.Barcode,
                    Nomi         = i.Name,
                    Miqdori      = i.Amount,
                    OlchovTuri   = i.MeasurmentType.ToString(),
                    Kategoriyasi = i.Category?.Name ?? "-"
                }).ToList();

                if (IsHandleCreated)
                    table.BeginInvoke(() =>
                    {
                        table.DataSource = (object)searchResult;
                        if (!searchResult.Any())
                            new Toastr().ShowWarning("Bunday mahsulot mavjud emas");
                    });
            }
            else
            {
                await Task.Run(() => FillProducts(selected));
            }
        }
        catch (Exception)
        {
            if (IsHandleCreated) table.BeginInvoke(() => new Toastr().ShowError("Xatolik yuz berdi"));
        }
    }

    private async void editbtn_Click(object sender, EventArgs e)
    {
        if (selectedId != 0)
        {
            EditProductForm form = new(selectedId, _businessUnit);
            var result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                new Toastr().ShowSuccess("O'zgarishlar saqlandi!");
                await Task.Run(() => FillProducts(selected));
                selectedId = 0;
            }
        }
        else
        {
            new Toastr().ShowWarning("Mahsulotlardan birini tanlang!");
        }
    }
}
