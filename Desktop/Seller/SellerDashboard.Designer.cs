namespace Desktop.Seller;

partial class SellerDashboard
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        // Form settings
        this.Text = "Sotuvchi paneli — POS System";
        this.Size = new Size(1200, 750);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(1000, 600);
        this.BackColor = Color.FromArgb(245, 247, 250);

        // --- Left panel: Products ---
        var pnlProducts = new Panel
        {
            Dock = DockStyle.Left,
            Width = 520,
            Padding = new Padding(10),
            BackColor = Color.White
        };

        var lblProductsHeader = new Label
        {
            Text = "MAHSULOTLAR",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 50, 50),
            Dock = DockStyle.Top,
            Height = 35
        };

        txtSearch = new TextBox
        {
            PlaceholderText = "Mahsulot qidirish...",
            Dock = DockStyle.Top,
            Height = 32,
            Font = new Font("Segoe UI", 10),
            Margin = new Padding(0, 5, 0, 5)
        };
        txtSearch.TextChanged += txtSearch_TextChanged;

        lstProducts = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill
        };
        lstProducts.Columns.Add("Mahsulot nomi", 220);
        lstProducts.Columns.Add("Miqdor", 80);
        lstProducts.Columns.Add("Narx", 100);

        var pnlAddToCart = new Panel { Dock = DockStyle.Bottom, Height = 80, Padding = new Padding(0, 5, 0, 0) };

        var lblQty = new Label { Text = "Miqdor:", Location = new Point(0, 10), Size = new Size(55, 22), Font = new Font("Segoe UI", 9) };
        txtQuantity = new TextBox { Location = new Point(60, 7), Size = new Size(70, 28), Text = "1", Font = new Font("Segoe UI", 9) };

        var lblPrice = new Label { Text = "Narx:", Location = new Point(145, 10), Size = new Size(40, 22), Font = new Font("Segoe UI", 9) };
        txtPrice = new TextBox { Location = new Point(188, 7), Size = new Size(100, 28), Font = new Font("Segoe UI", 9) };

        btnAddToCart = new Button
        {
            Text = "+ Savatga",
            Location = new Point(300, 5),
            Size = new Size(100, 32),
            BackColor = Color.FromArgb(52, 152, 219),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnAddToCart.FlatAppearance.BorderSize = 0;
        btnAddToCart.Click += btnAddToCart_Click;

        pnlAddToCart.Controls.AddRange(new Control[] { lblQty, txtQuantity, lblPrice, txtPrice, btnAddToCart });
        pnlProducts.Controls.AddRange(new Control[] { lblProductsHeader, txtSearch, lstProducts, pnlAddToCart });

        // --- Right panel: Cart ---
        var pnlCart = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = Color.White
        };

        var lblCartHeader = new Label
        {
            Text = "SAVAT",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(50, 50, 50),
            Dock = DockStyle.Top,
            Height = 35
        };

        lstCart = new ListView
        {
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill
        };
        lstCart.Columns.Add("Mahsulot", 200);
        lstCart.Columns.Add("Miqdor", 70);
        lstCart.Columns.Add("Narx", 100);
        lstCart.Columns.Add("Jami", 120);

        var pnlPayment = new Panel { Dock = DockStyle.Bottom, Height = 170, Padding = new Padding(0, 10, 0, 0) };

        lblTotal = new Label
        {
            Text = "Jami: 0 so'm",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(44, 62, 80),
            Dock = DockStyle.Top,
            Height = 30
        };

        var pnlPaymentInputs = new Panel { Dock = DockStyle.Top, Height = 80 };

        var lblCash = new Label { Text = "Naqd:", Location = new Point(0, 10), Size = new Size(50, 22), Font = new Font("Segoe UI", 9) };
        txtPaidCash = new TextBox { Location = new Point(55, 7), Size = new Size(140, 28), Font = new Font("Segoe UI", 9), PlaceholderText = "0" };

        var lblCard = new Label { Text = "Karta:", Location = new Point(215, 10), Size = new Size(45, 22), Font = new Font("Segoe UI", 9) };
        txtPaidCard = new TextBox { Location = new Point(265, 7), Size = new Size(140, 28), Font = new Font("Segoe UI", 9), PlaceholderText = "0" };

        pnlPaymentInputs.Controls.AddRange(new Control[] { lblCash, txtPaidCash, lblCard, txtPaidCard });

        var pnlButtons = new Panel { Dock = DockStyle.Bottom, Height = 50 };

        btnRemoveFromCart = new Button
        {
            Text = "O'chirish",
            Location = new Point(0, 8),
            Size = new Size(100, 34),
            BackColor = Color.FromArgb(231, 76, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnRemoveFromCart.FlatAppearance.BorderSize = 0;
        btnRemoveFromCart.Click += btnRemoveFromCart_Click;

        btnClearCart = new Button
        {
            Text = "Savatni tozalash",
            Location = new Point(110, 8),
            Size = new Size(130, 34),
            BackColor = Color.FromArgb(149, 165, 166),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };
        btnClearCart.FlatAppearance.BorderSize = 0;
        btnClearCart.Click += btnClearCart_Click;

        btnCheckout = new Button
        {
            Text = "✓ Sotish",
            Location = new Point(255, 5),
            Size = new Size(155, 40),
            BackColor = Color.FromArgb(39, 174, 96),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11, FontStyle.Bold)
        };
        btnCheckout.FlatAppearance.BorderSize = 0;
        btnCheckout.Click += btnCheckout_Click;

        pnlButtons.Controls.AddRange(new Control[] { btnRemoveFromCart, btnClearCart, btnCheckout });

        pnlPayment.Controls.AddRange(new Control[] { lblTotal, pnlPaymentInputs, pnlButtons });
        pnlCart.Controls.AddRange(new Control[] { lblCartHeader, lstCart, pnlPayment });

        // Divider
        var splitter = new Splitter { Dock = DockStyle.Left, Width = 6, BackColor = Color.FromArgb(230, 230, 230) };

        this.Controls.AddRange(new Control[] { pnlCart, splitter, pnlProducts });

        this.Load += SellerDashboard_Load;
    }

    // Controls
    private TextBox txtSearch = null!;
    private ListView lstProducts = null!;
    private TextBox txtQuantity = null!;
    private TextBox txtPrice = null!;
    private Button btnAddToCart = null!;

    private ListView lstCart = null!;
    private Label lblTotal = null!;
    private TextBox txtPaidCash = null!;
    private TextBox txtPaidCard = null!;
    private Button btnRemoveFromCart = null!;
    private Button btnClearCart = null!;
    private Button btnCheckout = null!;
}
