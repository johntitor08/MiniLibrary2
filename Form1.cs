using System.Data;
using System.Data.SQLite;

namespace MiniLibrary
{
    public partial class Form1 : Form
    {
        // UI
        readonly DataGridView grid = new();
        readonly TextBox txtTitle = new();
        readonly TextBox txtAuthor = new();
        readonly TextBox txtCategory = new();
        readonly CheckBox chkRead = new();
        readonly CheckBox chkFavorite = new();
        readonly Button btnAdd = new();
        readonly Button btnUpdate = new();
        readonly Button btnDelete = new();
        readonly Button btnClear = new();

        // DB
        readonly string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "library.db");
        string ConnStr => $"Data Source={dbPath};Version=3;";

        int? selectedId = null;

        public Form1()
        {
            Text = "Mini Library";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
            InitDb();
            LoadData();
        }

        void BuildUi()
        {
            // Grid
            grid.Dock = DockStyle.Top;
            grid.Height = 330;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            grid.CellClick += Grid_CellClick;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
            Controls.Add(grid);

            // Labels
            var lblTitle = new Label() { Text = "Başlık", Top = 350, Left = 20, Width = 100 };
            var lblAuthor = new Label() { Text = "Yazar", Top = 380, Left = 20, Width = 100 };
            var lblCategory = new Label() { Text = "Kategori", Top = 410, Left = 20, Width = 100 };

            // Inputs
            txtTitle.SetBounds(120, 345, 260, 25);
            txtAuthor.SetBounds(120, 375, 260, 25);
            txtCategory.SetBounds(120, 405, 260, 25);

            chkRead.Text = "Okundu";
            chkRead.SetBounds(400, 345, 100, 25);
            chkFavorite.Text = "Favori";
            chkFavorite.SetBounds(400, 375, 100, 25);

            // Buttons
            btnAdd.Text = "Ekle";
            btnAdd.SetBounds(520, 345, 120, 32);
            btnAdd.Click += (s, e) => { AddBook(); };

            btnUpdate.Text = "Güncelle";
            btnUpdate.SetBounds(520, 385, 120, 32);
            btnUpdate.Click += (s, e) => { UpdateBook(); };

            btnDelete.Text = "Sil";
            btnDelete.SetBounds(520, 425, 120, 32);
            btnDelete.Click += (s, e) => { DeleteBook(); };

            btnClear.Text = "Temizle";
            btnClear.SetBounds(520, 465, 120, 32);
            btnClear.Click += (s, e) => { ClearForm(); };

            Controls.AddRange(new Control[] {
                lblTitle, lblAuthor, lblCategory,
                txtTitle, txtAuthor, txtCategory,
                chkRead, chkFavorite,
                btnAdd, btnUpdate, btnDelete, btnClear
            });
        }

        void InitDb()
        {
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
            }
            using var conn = new SQLiteConnection(ConnStr);
            conn.Open();
            string create = @"
                CREATE TABLE IF NOT EXISTS Books (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Author TEXT NOT NULL,
                    Category TEXT,
                    IsRead INTEGER NOT NULL DEFAULT 0,
                    IsFavorite INTEGER NOT NULL DEFAULT 0,
                    CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
                );
            ";
            using var cmd = new SQLiteCommand(create, conn);
            cmd.ExecuteNonQuery();
        }

        void LoadData()
        {
            using var conn = new SQLiteConnection(ConnStr);
            conn.Open();
            using var da = new SQLiteDataAdapter(
                "SELECT Id, Title AS 'Başlık', Author AS 'Yazar', Category AS 'Kategori', " +
                "CASE IsRead WHEN 1 THEN 'Evet' ELSE 'Hayır' END AS 'Okundu', " +
                "CASE IsFavorite WHEN 1 THEN 'Evet' ELSE 'Hayır' END AS 'Favori', " +
                "CreatedAt AS 'Eklenme Tarihi' FROM Books ORDER BY Id DESC", conn);
            var dt = new DataTable();
            da.Fill(dt);
            grid.DataSource = dt;
            selectedId = null;
        }

        void AddBook()
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                MessageBox.Show("Başlık ve Yazar zorunludur.");
                return;
            }

            using var conn = new SQLiteConnection(ConnStr);
            conn.Open();
            using var cmd = new SQLiteCommand(@"
                INSERT INTO Books(Title, Author, Category, IsRead, IsFavorite)
                VALUES (@Title, @Author, @Category, @IsRead, @IsFavorite);
            ", conn);
            cmd.Parameters.AddWithValue("@Title", txtTitle.Text.Trim());
            cmd.Parameters.AddWithValue("@Author", txtAuthor.Text.Trim());
            cmd.Parameters.AddWithValue("@Category", txtCategory.Text.Trim());
            cmd.Parameters.AddWithValue("@IsRead", chkRead.Checked ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsFavorite", chkFavorite.Checked ? 1 : 0);
            cmd.ExecuteNonQuery();

            ClearForm();
            LoadData();
        }

        void UpdateBook()
        {
            if (selectedId == null)
            {
                MessageBox.Show("Güncellemek için listeden bir kayıt seçin.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txtTitle.Text) || string.IsNullOrWhiteSpace(txtAuthor.Text))
            {
                MessageBox.Show("Başlık ve Yazar zorunludur.");
                return;
            }

            using var conn = new SQLiteConnection(ConnStr);
            conn.Open();
            using var cmd = new SQLiteCommand(@"
                UPDATE Books
                SET Title=@Title, Author=@Author, Category=@Category,
                    IsRead=@IsRead, IsFavorite=@IsFavorite
                WHERE Id=@Id;
            ", conn);
            cmd.Parameters.AddWithValue("@Title", txtTitle.Text.Trim());
            cmd.Parameters.AddWithValue("@Author", txtAuthor.Text.Trim());
            cmd.Parameters.AddWithValue("@Category", txtCategory.Text.Trim());
            cmd.Parameters.AddWithValue("@IsRead", chkRead.Checked ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsFavorite", chkFavorite.Checked ? 1 : 0);
            cmd.Parameters.AddWithValue("@Id", selectedId.Value);
            cmd.ExecuteNonQuery();

            ClearForm();
            LoadData();
        }

        void DeleteBook()
        {
            if (selectedId == null)
            {
                MessageBox.Show("Silmek için listeden bir kayıt seçin.");
                return;
            }
            var confirm = MessageBox.Show("Bu kaydı silmek istiyor musunuz?", "Onay",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            using var conn = new SQLiteConnection(ConnStr);
            conn.Open();
            using var cmd = new SQLiteCommand("DELETE FROM Books WHERE Id=@Id;", conn);
            cmd.Parameters.AddWithValue("@Id", selectedId.Value);
            cmd.ExecuteNonQuery();

            ClearForm();
            LoadData();
        }

        void ClearForm()
        {
            selectedId = null;
            txtTitle.Text = "";
            txtAuthor.Text = "";
            txtCategory.Text = "";
            chkRead.Checked = false;
            chkFavorite.Checked = false;
            grid.ClearSelection();
        }

        void Grid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            if (row.Cells["Id"] == null) return;

            // DataTable sütun adlarını kontrol et (Id gizli olabilir)
            int id;
            try { id = Convert.ToInt32(row.Cells["Id"].Value); }
            catch
            {
                // Grid başlıklarını lokalize ettiğimiz için Id görünmüyor olabilir.
                // DataBoundItem üzerinden yakalayalım.
                if (row.DataBoundItem is DataRowView drv && drv.Row.Table.Columns.Contains("Id"))
                    id = Convert.ToInt32(drv.Row["Id"]);
                else return;
            }

            selectedId = id;

            // Seçili satırdan değerleri TextBox/CheckBox'lara bas
            string title = GetCell(row, "Başlık");
            string author = GetCell(row, "Yazar");
            string category = GetCell(row, "Kategori");
            string read = GetCell(row, "Okundu");
            string fav = GetCell(row, "Favori");

            txtTitle.Text = title;
            txtAuthor.Text = author;
            txtCategory.Text = category;
            chkRead.Checked = string.Equals(read, "Evet", StringComparison.OrdinalIgnoreCase);
            chkFavorite.Checked = string.Equals(fav, "Evet", StringComparison.OrdinalIgnoreCase);
        }

        string GetCell(DataGridViewRow row, string name)
        {
            try
            {
                return row.Cells[name]?.Value?.ToString() ?? "";
            }
            catch
            {
                // Farklı locale/sütun isimleri için fallback
                return "";
            }
        }
    }
}
