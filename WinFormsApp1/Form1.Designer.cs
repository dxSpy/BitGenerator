namespace WinFormsApp1
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;
        private Button btnGenerateBeat;
        private Button btnPlayBeat;
        private Button btnOpenFolder;
        private Label lblStatus;
        private NumericUpDown nudBpm;
        private NumericUpDown nudBars;
        private ComboBox cbBeatStyle;
        private Label lblBeatInfo;
        private Panel panelSequencer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            var background = Color.FromArgb(15, 17, 24);
            var surface = Color.FromArgb(25, 28, 38);
            var muted = Color.FromArgb(154, 161, 178);
            var accent = Color.FromArgb(166, 111, 255);

            btnGenerateBeat = new Button();
            btnPlayBeat = new Button();
            btnOpenFolder = new Button();
            lblStatus = new Label();
            nudBpm = new NumericUpDown();
            nudBars = new NumericUpDown();
            cbBeatStyle = new ComboBox();
            lblBeatInfo = new Label();
            panelSequencer = new Panel();
            components = new System.ComponentModel.Container();
            var toolTip = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)nudBpm).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudBars).BeginInit();
            SuspendLayout();

            Text = "Rap Studio";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(1020, 620);
            ClientSize = new Size(1120, 700);
            BackColor = background;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 10F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = background,
                Padding = new Padding(28),
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            var header = new Panel { Dock = DockStyle.Fill };
            var logo = new Label
            {
                Text = "R",
                Dock = DockStyle.Left,
                Width = 54,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 22F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = accent
            };
            var titleStack = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 0, 0) };
            titleStack.Controls.Add(new Label
            {
                Text = "RAP STUDIO",
                Dock = DockStyle.Top,
                Height = 34,
                Font = new Font("Segoe UI", 19F, FontStyle.Bold),
                ForeColor = Color.White
            });
            titleStack.Controls.Add(new Label
            {
                Text = "Инструментальные биты — без слов и вокала.",
                Dock = DockStyle.Top,
                Height = 26,
                ForeColor = muted
            });
            header.Controls.Add(titleStack);
            header.Controls.Add(logo);
            root.Controls.Add(header, 0, 0);

            var workspace = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = background
            };
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
            workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var settings = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Padding = new Padding(20)
            };
            settings.Controls.Add(new Label
            {
                Text = "НАСТРОЙКИ БИТА",
                Dock = DockStyle.Top,
                Height = 42,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = muted
            });

            var settingsFields = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 228,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = surface
            };
            settingsFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            settingsFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            settingsFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            settingsFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            settingsFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            settingsFields.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            settingsFields.Controls.Add(CreateSettingLabel("Стиль"), 0, 0);
            cbBeatStyle.Items.AddRange(["Trap", "Boom bap", "Drill"]);
            cbBeatStyle.DropDownStyle = ComboBoxStyle.DropDownList;
            cbBeatStyle.SelectedIndex = 0;
            cbBeatStyle.Dock = DockStyle.Fill;
            cbBeatStyle.BackColor = Color.FromArgb(34, 38, 50);
            cbBeatStyle.ForeColor = Color.White;
            cbBeatStyle.FlatStyle = FlatStyle.Flat;
            settingsFields.Controls.Add(cbBeatStyle, 0, 1);
            settingsFields.Controls.Add(CreateSettingLabel("Темп (BPM)"), 0, 2);
            nudBpm.Minimum = 60;
            nudBpm.Maximum = 180;
            nudBpm.Value = 90;
            StyleNumber(nudBpm, surface);
            settingsFields.Controls.Add(nudBpm, 0, 3);
            settingsFields.Controls.Add(CreateSettingLabel("Длина бита (такты)"), 0, 4);
            nudBars.Minimum = 4;
            nudBars.Maximum = 32;
            nudBars.Value = 8;
            StyleNumber(nudBars, surface);
            settingsFields.Controls.Add(nudBars, 0, 5);
            settings.Controls.Add(settingsFields);

            btnGenerateBeat.Text = "СОЗДАТЬ БИТ";
            btnGenerateBeat.Dock = DockStyle.Bottom;
            btnGenerateBeat.Height = 48;
            btnGenerateBeat.FlatStyle = FlatStyle.Flat;
            btnGenerateBeat.FlatAppearance.BorderSize = 0;
            btnGenerateBeat.BackColor = accent;
            btnGenerateBeat.ForeColor = Color.White;
            btnGenerateBeat.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnGenerateBeat.Cursor = Cursors.Hand;
            toolTip.SetToolTip(btnGenerateBeat, "Создать WAV (Ctrl+Enter)");
            btnGenerateBeat.Click += BtnGenerateBeat_Click;
            settings.Controls.Add(btnGenerateBeat);
            workspace.Controls.Add(settings, 0, 0);

            var editor = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 0, 0, 0),
                ColumnCount = 1,
                RowCount = 3,
                BackColor = background
            };
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            editor.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            editor.Controls.Add(new Label
            {
                Text = "ИНСТРУМЕНТАЛЬНАЯ СЕССИЯ",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = muted
            }, 0, 0);

            var beatStage = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = surface,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 2
            };
            beatStage.RowStyles.Add(new RowStyle(SizeType.Absolute, 224));
            beatStage.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panelSequencer.Dock = DockStyle.Fill;
            beatStage.Controls.Add(panelSequencer, 0, 0);
            lblBeatInfo.Text = "Матрица 16 шагов · кликни ячейки, чтобы менять рисунок.\nСтерео WAV для записи речитатива.";
            lblBeatInfo.Dock = DockStyle.Fill;
            lblBeatInfo.TextAlign = ContentAlignment.MiddleLeft;
            lblBeatInfo.ForeColor = Color.FromArgb(190, 195, 207);
            lblBeatInfo.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            beatStage.Controls.Add(lblBeatInfo, 0, 1);
            editor.Controls.Add(beatStage, 0, 1);

            var actions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = background
            };
            StyleActionButton(btnPlayBeat, "▶  СЛУШАТЬ БИТ", surface, Color.White);
            btnPlayBeat.Enabled = false;
            toolTip.SetToolTip(btnPlayBeat, "Открыть последний созданный WAV (Ctrl+P)");
            btnPlayBeat.Click += BtnPlayBeat_Click;
            StyleActionButton(btnOpenFolder, "ОТКРЫТЬ ПАПКУ", surface, Color.White);
            btnOpenFolder.Enabled = false;
            toolTip.SetToolTip(btnOpenFolder, "Открыть папку с битами (Ctrl+Shift+F)");
            btnOpenFolder.Click += BtnOpenFolder_Click;
            actions.Controls.Add(btnPlayBeat);
            actions.Controls.Add(btnOpenFolder);
            editor.Controls.Add(actions, 0, 2);
            workspace.Controls.Add(editor, 1, 0);
            root.Controls.Add(workspace, 0, 1);

            lblStatus.Text = "●  ГОТОВО К РАБОТЕ";
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.ForeColor = Color.FromArgb(129, 214, 172);
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            root.Controls.Add(lblStatus, 0, 2);

            Controls.Add(root);
            ((System.ComponentModel.ISupportInitialize)nudBpm).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudBars).EndInit();
            ResumeLayout(false);
        }

        private static Label CreateSettingLabel(string text) => new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(190, 195, 207),
            Font = new Font("Segoe UI", 9F)
        };

        private static void StyleNumber(NumericUpDown control, Color background)
        {
            control.Dock = DockStyle.Fill;
            control.BackColor = Color.FromArgb(34, 38, 50);
            control.ForeColor = Color.White;
            control.BorderStyle = BorderStyle.FixedSingle;
            control.Font = new Font("Segoe UI", 11F);
        }

        private static void StyleActionButton(Button button, string text, Color background, Color foreground)
        {
            button.Text = text;
            button.Size = new Size(170, 38);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(55, 60, 75);
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = background;
            button.ForeColor = foreground;
            button.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
        }
    }
}