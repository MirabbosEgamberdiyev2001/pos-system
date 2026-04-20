using Desktop.Extended;
using Microsoft.EntityFrameworkCore;
using POS.Application.Common.DataTransferObjects.CategoryDtos;
using POS.Application.Common.Enums;
using POS.Application.Common.Models;
using POS.Application.Interfaces;

namespace Desktop.Admin.CategoryForms;
public partial class CategoryTable : UserControl
{
    private readonly List<CategoryDto> _categories = new();
    private readonly IBusinessUnit _businessUnit;
    private int selectedId = 0;
    private State selected = State.All;

    // Search debounce timer
    private readonly System.Windows.Forms.Timer _searchDebounce = new() { Interval = 300 };

    public CategoryTable(IBusinessUnit businessUnit)
    {
        InitializeComponent();
        _businessUnit = businessUnit;
        InfoCategory.SelectedIndex = 0;
        _searchDebounce.Tick += async (s, e) =>
        {
            _searchDebounce.Stop();
            await SearchCategories(search_textbox.Text);
        };
    }

    /// <summary>
    /// CategoryTable load event - fill table with categories
    /// </summary>
    private async void CategoryTable_Load(object sender, EventArgs e)
    {
        await Task.Run(() => FillCategories(selected));
    }

    /// <summary>
    /// Fill table with categories
    /// </summary>
    private async Task FillCategories(State selected)
    {
        var list = selected switch
        {
            State.Active => await _businessUnit.CategoryService.GetAllActivesAsync(),
            State.Archive => await _businessUnit.CategoryService.GetAllArchivesAsync(),
            _ => await _businessUnit.CategoryService.GetAllAsync()
        };

        if (IsHandleCreated)
        {
            table.BeginInvoke(() =>
            {
                table.DataSource = list.Select(i =>
                new { Id = i.Id, Nomi = i.Name })
                .ToList();
                _categories.AddRange(list);
            });
        }
    }

    /// <summary>
    /// Add button click event - open AddCategory form
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void addbtn_Click(object sender, EventArgs e)
    {
        AddCategoryForm form = new(_businessUnit);
        var result = form.ShowDialog();
        if (result == DialogResult.OK)
        {
            new Toastr().ShowSuccess();
            await Task.Run(() => FillCategories(selected));
        }
    }

    /// <summary>
    /// Edit button click event - open EditCategory form
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void editbtn_Click(object sender, EventArgs e)
    {
        if (selectedId != 0)
        {
            EditCategoryForm form = new(selectedId, _businessUnit);
            var result = form.ShowDialog();
            if (result == DialogResult.OK)
            {
                new Toastr().ShowSuccess("O'zgarishlar saqlandi!");
                await Task.Run(() => FillCategories(selected));
                selectedId = 0;
            }
        }
        else
        {
            new Toastr().ShowWarning("Kategoriyalardan birini tanlang!");
        }
    }

    /// <summary>
    /// Delete button click event - delete selected category
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void deletebtn_Click(object sender, EventArgs e)
    {
        if (selectedId != 0)
        {
            var result = new Modal().ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    await _businessUnit.CategoryService.DeleteAsync(selectedId);
                    new Toastr().ShowSuccess("Muvoffaqqiyatli o'chirildi");
                }
                catch (DbUpdateException)
                {
                    new Toastr().ShowError("Kategoriyaga tegishli mahsulotlar mavjud!");
                }
                catch (Exception)
                {
                    new Toastr().ShowError("Xatolik yuz berdi!");
                }
                finally
                {
                    await Task.Run(() => FillCategories(selected));
                    selectedId = 0;
                }
            }
        }
        else
        {
            new Toastr().ShowWarning("Kategoriyalardan birini tanlang!");
        }
    }

    /// <summary>
    /// select category id
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void table_CellClick(object sender, DataGridViewCellEventArgs e)
    {
        selectedId = int.Parse(table.SelectedRows[0].Cells[0].Value.ToString());
    }


    private void search_textbox_TextChanged(object sender, EventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private async Task SearchCategories(string text)
    {
        try
        {
            if (!string.IsNullOrEmpty(text))
            {
                var filter = await _businessUnit.CategoryService
                                                .FilterByNameAsync(text, selected);
                var searchResult = filter.Select(i => new { Id = i.Id, Nomi = i.Name }).ToList();
                if (IsHandleCreated)
                    table.BeginInvoke(() =>
                    {
                        table.DataSource = searchResult.Any()
                            ? (object)searchResult
                            : new List<object>();
                        if (!searchResult.Any())
                            new Toastr().ShowWarning("Bunday kategoriya mavjud emas");
                    });
            }
            else
            {
                await Task.Run(() => FillCategories(selected));
            }
        }
        catch (Exception)
        {
            if (IsHandleCreated) table.BeginInvoke(() => new Toastr().ShowError("Xatolik yuz berdi"));
        }
    }



    /// <summary>
    /// Categoriyani arxivga solish 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// 
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
                        await _businessUnit.CategoryService.ActionAsync(selectedId, ActionType.Archive);
                        await Task.Run(() => FillCategories(selected));
                        new Toastr().ShowSuccess("Muvoffaqqiyatli arxivelandi");
                    }
                }
                else
                {
                    if(selectedId != 0)
                    {
                        ArchiveBtn.Text = "Faollashtirish";
                        var result = new Modal("Rostdan ham arxivdan chiqarmoqchimisan?").ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            await _businessUnit.CategoryService.ActionAsync(selectedId, ActionType.Recover);
                            await Task.Run(() => FillCategories(selected));
                            new Toastr().ShowSuccess("Muvoffaqqiyatli arxivdan chiqarildi");
                        }
                    }
                    else
                    {
                        new Toastr().ShowWarning("Foallashtirish uchun bironta kategoriyani tanlang");
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
            if(InfoCategory.Text== "Arxivlangan")
            {
                new Toastr().ShowWarning("Faollashtirish uchun kategoriyalardan birini tanlang!");

            }
            else
            {
                new Toastr().ShowWarning("Arxivelash uchun kategoriyalardan birini tanlang!");

            }
        }
    }

    /// <summary>
    /// ComboBoxni textsini o'zgartirish va selectedId ni 0 ga tenglash 
    /// </summary>
    private void UpdateArchiveButton()
    {
        if (InfoCategory.Text == "Arxivlangan")
        {
            ArchiveBtn.Text = "Faollashtirish";
            ArchiveBtn.Visible = true;
            selectedId = 0;

        }
        else if (InfoCategory.Text == "Aktiv")
        {
            ArchiveBtn.Text = "Arxivlash";
            ArchiveBtn.Visible = true;
            selectedId = 0;

        }
        else
        {
            ArchiveBtn.Visible = false;
            
        }
    }


    private async void InfoCategory_SelectedIndexChangedAsync(object sender, EventArgs e)
    {
        selected = InfoCategory.Text switch
        {
            "Aktiv" => State.Active,
            "Arxivlangan" => State.Archive,
            _ => State.All
        };
        await Task.Run(() => FillCategories(selected));
        UpdateArchiveButton();
    }
}