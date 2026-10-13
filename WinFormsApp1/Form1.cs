using System.Diagnostics;
using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private const int StepCount = 16;
        private const int TrackCount = 5;
        private static readonly string[] TrackNames = ["Kick", "Snare", "Closed hi-hat", "Open hi-hat", "808 bass"];
        private readonly CheckBox[,] stepCells = new CheckBox[TrackCount, StepCount];
        private string? currentStyle;
        private bool restoringStyleSelection;
        private bool applyingStylePreset;
        private bool patternHasManualEdits;
        private string? lastAudioPath = null;

        public Form1()
        {
            InitializeComponent();
            BuildStepMatrix();
            currentStyle = cbBeatStyle.SelectedItem?.ToString() ?? "Trap";
            cbBeatStyle.SelectedIndexChanged += (_, _) =>
            {
                if (!restoringStyleSelection)
                {
                    ApplyStylePreset();
                }
            };
            ApplyStylePreset();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Enter))
            {
                btnGenerateBeat.PerformClick();
                return true;
            }

            if (keyData == (Keys.Control | Keys.P))
            {
                btnPlayBeat.PerformClick();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Shift | Keys.F))
            {
                btnOpenFolder.PerformClick();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void BuildStepMatrix()
        {
            var grid = new TableLayoutPanel
            {
                ColumnCount = StepCount + 1,
                RowCount = TrackCount + 1,
                Location = new Point(0, 4),
                Size = new Size(632, 204),
                BackColor = Color.FromArgb(25, 28, 38),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            for (var step = 0; step < StepCount; step++)
            {
                grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
            }
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            for (var track = 0; track < TrackCount; track++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            }

            for (var step = 0; step < StepCount; step++)
            {
                var isDownbeat = step % 4 == 0;
                grid.Controls.Add(new Label
                {
                    Text = (step + 1).ToString("00"),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = isDownbeat ? Color.FromArgb(39, 42, 57) : Color.FromArgb(30, 33, 45),
                    ForeColor = Color.FromArgb(154, 161, 178),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Margin = new Padding(1)
                }, step + 1, 0);
            }

            for (var track = 0; track < TrackCount; track++)
            {
                grid.Controls.Add(new Label
                {
                    Text = TrackNames[track].ToUpperInvariant(),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    ForeColor = Color.FromArgb(190, 195, 207),
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                    Margin = new Padding(2)
                }, 0, track + 1);

                for (var step = 0; step < StepCount; step++)
                {
                    var cell = new CheckBox
                    {
                        Appearance = Appearance.Button,
                        AutoSize = false,
                        Dock = DockStyle.Fill,
                        FlatStyle = FlatStyle.Flat,
                        Text = string.Empty,
                        BackColor = step / 4 % 2 == 0
                            ? Color.FromArgb(37, 41, 54)
                            : Color.FromArgb(31, 35, 47),
                        ForeColor = Color.White,
                        Margin = new Padding(2),
                        Tag = (track, step),
                        AccessibleName = $"{TrackNames[track]}, шаг {step + 1}",
                        AccessibleDescription = "Переключить этот шаг в паттерне",
                        TabStop = true
                    };
                    cell.FlatAppearance.BorderColor = Color.FromArgb(57, 62, 80);
                    cell.FlatAppearance.BorderSize = 1;
                    cell.FlatAppearance.CheckedBackColor = Color.FromArgb(166, 111, 255);
                    cell.CheckedChanged += StepCell_CheckedChanged;
                    stepCells[track, step] = cell;
                    grid.Controls.Add(cell, step + 1, track + 1);
                }
            }

            panelSequencer.Controls.Add(grid);
            panelSequencer.Resize += (_, _) => grid.Left = Math.Max(0, (panelSequencer.ClientSize.Width - grid.Width) / 2);
            grid.Left = Math.Max(0, (panelSequencer.ClientSize.Width - grid.Width) / 2);
        }

        private void StepCell_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox cell) return;
            if (!applyingStylePreset)
            {
                patternHasManualEdits = true;
            }

            cell.FlatAppearance.CheckedBackColor = Color.FromArgb(166, 111, 255);
            if (cell.Checked)
            {
                cell.BackColor = Color.FromArgb(166, 111, 255);
            }
            else if (cell.Tag is ValueTuple<int, int> position)
            {
                var step = position.Item2;
                cell.BackColor = step / 4 % 2 == 0
                    ? Color.FromArgb(37, 41, 54)
                    : Color.FromArgb(31, 35, 47);
            }
        }

        private void ApplyStylePreset()
        {
            var style = cbBeatStyle.SelectedItem?.ToString() ?? "Trap";
            if (patternHasManualEdits && currentStyle is not null && !string.Equals(style, currentStyle, StringComparison.Ordinal))
            {
                var result = MessageBox.Show(
                    this,
                    "Выбор пресета заменит все изменения в матрице. Продолжить?",
                    "Заменить текущий паттерн?",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (result != DialogResult.Yes)
                {
                    restoringStyleSelection = true;
                    try
                    {
                        cbBeatStyle.SelectedItem = currentStyle;
                    }
                    finally
                    {
                        restoringStyleSelection = false;
                    }

                    return;
                }
            }

            currentStyle = style;
            var preset = style switch
            {
                "Boom bap" => new[]
                {
                    new[] { 0, 7, 10 }, new[] { 4, 12 }, new[] { 0, 2, 4, 6, 8, 10, 12, 14 },
                    new[] { 14 }, new[] { 0, 10 }
                },
                "Drill" => new[]
                {
                    new[] { 0, 3, 10, 14 }, new[] { 8 }, new[] { 0, 2, 4, 6, 8, 10, 12, 14 },
                    new[] { 14 }, new[] { 0, 3, 10, 14 }
                },
                _ => new[]
                {
                    new[] { 0, 6, 10, 14 }, new[] { 8 }, new[] { 0, 2, 4, 6, 8, 10, 12, 14 },
                    new[] { 15 }, new[] { 0, 6, 10, 14 }
                }
            };

            applyingStylePreset = true;
            try
            {
                for (var track = 0; track < TrackCount; track++)
                {
                    for (var step = 0; step < StepCount; step++)
                    {
                        stepCells[track, step].Checked = preset[track].Contains(step);
                    }
                }
            }
            finally
            {
                applyingStylePreset = false;
            }

            patternHasManualEdits = false;
        }

        private async void BtnGenerateBeat_Click(object? sender, EventArgs e)
        {
            SetGenerationInProgress(true);
            try
            {
                var bpm = (int)nudBpm.Value;
                var bars = (int)nudBars.Value;
                var style = cbBeatStyle.SelectedItem?.ToString() ?? "Trap";
                var pattern = new bool[TrackCount, StepCount];
                for (var track = 0; track < TrackCount; track++)
                {
                    for (var step = 0; step < StepCount; step++)
                    {
                        pattern[track, step] = stepCells[track, step].Checked;
                    }
                }

                lastAudioPath = await Task.Run(() => CreateBeat(bpm, bars, style, pattern));
                btnPlayBeat.Enabled = true;
                btnOpenFolder.Enabled = true;
                var duration = TimeSpan.FromSeconds(bars * 4 * 60d / bpm);
                var durationText = duration.ToString(@"mm\:ss");
                lblBeatInfo.Text = $"{style.ToUpperInvariant()}\n{bpm} BPM  •  {bars} ТАКТОВ  •  {durationText}\n\nБит готов для записи речитатива.\nСтерео WAV · 44,1 кГц · 16 бит.";
                lblStatus.Text = "●  БИТ ГОТОВ";
            }
            catch (Exception ex)
            {
                lblBeatInfo.Text = ex.Message;
                lblStatus.Text = "●  НЕ УДАЛОСЬ СОЗДАТЬ БИТ";
                lastAudioPath = null;
            }
            finally
            {
                SetGenerationInProgress(false);
            }
        }

        private void SetGenerationInProgress(bool isGenerating)
        {
            btnGenerateBeat.Enabled = !isGenerating;
            cbBeatStyle.Enabled = !isGenerating;
            nudBpm.Enabled = !isGenerating;
            nudBars.Enabled = !isGenerating;
            foreach (var cell in stepCells)
            {
                cell.Enabled = !isGenerating;
            }

            if (isGenerating)
            {
                btnPlayBeat.Enabled = false;
                btnOpenFolder.Enabled = false;
                lblStatus.Text = "●  СОЗДАЮ БИТ...";
            }
            else if (!string.IsNullOrEmpty(lastAudioPath) && File.Exists(lastAudioPath))
            {
                btnPlayBeat.Enabled = true;
                btnOpenFolder.Enabled = true;
            }
        }

        private static string CreateBeat(int bpm, int bars, string style, bool[,] pattern)
        {
            var generatedAt = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var fileName = $"beat-{style.ToLowerInvariant().Replace(' ', '-')}-{bpm}bpm-{bars}bars-{generatedAt}-{uniqueId}.wav";
            var audioPath = Path.Combine(GetOutputDirectory(), fileName);
            WriteBuiltInBeat(audioPath, bpm, bars, style, pattern);
            return audioPath;
        }

        private static string GetOutputDirectory()
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RapStudio");
            Directory.CreateDirectory(path);
            return path;
        }

        private static void WriteBuiltInBeat(string path, int bpm, int bars, string style, bool[,] pattern)
        {
            const int sampleRate = 44100;
            const int channels = 2;
            if (bpm <= 0 || bars <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bpm), "Tempo and bar count must be positive.");
            }

            if (pattern.GetLength(0) != TrackCount || pattern.GetLength(1) != StepCount)
            {
                throw new ArgumentException("Pattern dimensions must match the sequencer grid.", nameof(pattern));
            }

            var beatDuration = 60d / bpm;
            var totalFramesLong = (long)(sampleRate * beatDuration * 4 * bars);
            var sampleCountLong = totalFramesLong * channels;
            if (sampleCountLong > Array.MaxLength)
            {
                throw new ArgumentOutOfRangeException(nameof(bars), "Requested audio is too large to render in memory.");
            }

            var totalFrames = checked((int)totalFramesLong);
            var samples = new float[(int)sampleCountLong];
            var random = Random.Shared;

            void Mix(int frame, float value, double pan = 0)
            {
                var left = Math.Sqrt((1 - pan) * 0.5);
                var right = Math.Sqrt((1 + pan) * 0.5);
                samples[frame * channels] += (float)(value * left);
                samples[frame * channels + 1] += (float)(value * right);
            }

            void AddKick(int start)
            {
                var length = Math.Min((int)(sampleRate * 0.32), totalFrames - start);
                var phase = 0d;
                for (var i = 0; i < length; i++)
                {
                    var time = i / (double)sampleRate;
                    var frequency = 43 + 125 * Math.Exp(-time * 22);
                    phase += 2 * Math.PI * frequency / sampleRate;
                    var envelope = Math.Exp(-time * 10);
                    var body = Math.Sin(phase) * 0.92 + Math.Sin(phase * 2) * 0.08;
                    Mix(start + i, (float)(body * envelope * 0.9));
                }
            }

            void AddSnare(int start, float volume)
            {
                var length = Math.Min((int)(sampleRate * 0.22), totalFrames - start);
                var phase = 0d;
                for (var i = 0; i < length; i++)
                {
                    var time = i / (double)sampleRate;
                    var noiseEnvelope = Math.Exp(-time * 22);
                    var bodyEnvelope = Math.Exp(-time * 15);
                    phase += 2 * Math.PI * 185 / sampleRate;
                    var noise = random.NextDouble() * 2 - 1;
                    var clap = i < sampleRate * 0.012 ? (random.NextDouble() * 2 - 1) * 0.45 : 0;
                    var sound = noise * noiseEnvelope * 0.72 + clap * noiseEnvelope + Math.Sin(phase) * bodyEnvelope * 0.28;
                    Mix(start + i, (float)(sound * volume), 0.04);
                }
            }

            void AddHat(int start, double duration, float volume, double pan)
            {
                var length = Math.Min((int)(sampleRate * duration), totalFrames - start);
                var previousNoise = 0d;
                for (var i = 0; i < length; i++)
                {
                    var envelope = Math.Exp(-i / (sampleRate * duration * 0.23));
                    var noise = random.NextDouble() * 2 - 1;
                    var highPass = noise - previousNoise * 0.82;
                    previousNoise = noise;
                    Mix(start + i, (float)(highPass * envelope * volume), pan);
                }
            }

            void Add808(int start, double frequency, bool slide)
            {
                var length = Math.Min((int)(sampleRate * 0.62), totalFrames - start);
                var phase = 0d;
                for (var i = 0; i < length; i++)
                {
                    var time = i / (double)sampleRate;
                    var targetFrequency = slide ? frequency * (1.16 - 0.16 * Math.Min(time / 0.42, 1)) : frequency;
                    phase += 2 * Math.PI * targetFrequency / sampleRate;
                    var envelope = Math.Exp(-time * 2.4);
                    var fundamental = Math.Sin(phase);
                    var harmonics = Math.Sin(phase * 2) * 0.22 + Math.Sin(phase * 3) * 0.08;
                    Mix(start + i, (float)((fundamental + harmonics) * envelope * 0.42));
                }
            }

            void AddPad(int start, double frequency)
            {
                var duration = beatDuration * 3.8;
                var length = Math.Min((int)(sampleRate * duration), totalFrames - start);
                for (var i = 0; i < length; i++)
                {
                    var time = i / (double)sampleRate;
                    var attack = Math.Min(time / 0.18, 1);
                    var release = Math.Min((duration - time) / 0.35, 1);
                    var envelope = Math.Max(0, Math.Min(attack, release)) * 0.045;
                    var chord = Math.Sin(2 * Math.PI * frequency * time)
                        + Math.Sin(2 * Math.PI * frequency * 1.25 * time) * 0.65
                        + Math.Sin(2 * Math.PI * frequency * 1.5 * time) * 0.45;
                    Mix(start + i, (float)(chord * envelope), -0.12);
                }
            }

            void AddPluck(int start, double frequency, double pan)
            {
                var length = Math.Min((int)(sampleRate * 0.42), totalFrames - start);
                var phase = 0d;
                for (var i = 0; i < length; i++)
                {
                    var time = i / (double)sampleRate;
                    phase += 2 * Math.PI * frequency / sampleRate;
                    var envelope = Math.Exp(-time * 8);
                    var note = Math.Sin(phase) + Math.Sin(phase * 2) * 0.28;
                    Mix(start + i, (float)(note * envelope * 0.055), pan);
                }
            }

            var bassNotes = new[] { 55d, 65.41d, 73.42d, 49d };
            var padNotes = new[] { 220d, 174.61d, 261.63d, 196d };
            var leadNotes = new[] { 440d, 523.25d, 659.25d, 587.33d, 523.25d, 392d, 440d, 659.25d };
            for (var bar = 0; bar < bars; bar++)
            {
                var barStart = (int)(bar * 4 * beatDuration * sampleRate);
                AddPad(barStart, padNotes[(bar / 2) % padNotes.Length]);

                for (var step = 0; step < 16; step++)
                {
                    var start = barStart + (int)(step * beatDuration / 4 * sampleRate);
                    var isFillBar = bar % 4 == 3;
                    var kick = pattern[0, step];
                    var snare = pattern[1, step];
                    var closedHat = pattern[2, step] || (isFillBar && step >= 12 && step % 2 == 1);
                    var openHat = pattern[3, step];
                    var bass = pattern[4, step];
                    var pan = step % 4 < 2 ? -0.22 : 0.22;

                    if (kick)
                    {
                        AddKick(start);
                    }

                    if (bass)
                    {
                        var noteIndex = (bar + (step >= 8 ? 1 : 0)) % bassNotes.Length;
                        Add808(start, bassNotes[noteIndex], style == "Drill" && step % 2 == 1);
                    }

                    if (snare) AddSnare(start, style == "Boom bap" ? 0.72f : 0.64f);
                    if (closedHat)
                    {
                        var velocity = (step % 4 == 0 ? 1f : 0.72f) * (0.9f + (float)random.NextDouble() * 0.2f);
                        AddHat(start, 0.045, 0.085f * velocity, pan);
                    }

                    if (openHat)
                    {
                        AddHat(start, 0.17, 0.19f, pan);
                    }

                    if ((step is 0 or 6 or 10 or 14) && (bar % 2 == 0 || style == "Boom bap"))
                    {
                        var note = leadNotes[(bar * 2 + step / 4) % leadNotes.Length];
                        AddPluck(start, note, pan);
                    }
                }
            }

            var peak = 0f;
            foreach (var sample in samples)
            {
                peak = Math.Max(peak, Math.Abs(sample));
            }

            var dataLengthLong = (long)samples.Length * sizeof(short);
            if (dataLengthLong > uint.MaxValue - 36)
            {
                throw new ArgumentOutOfRangeException(nameof(bars), "Requested audio exceeds the RIFF/WAV format size limit.");
            }

            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath)!;
            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
            try
            {
                using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new BinaryWriter(stream))
                {
                    var dataLength = (int)dataLengthLong;
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write(36 + dataLength);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                    writer.Write(16);
                    writer.Write((short)1);
                    writer.Write((short)channels);
                    writer.Write(sampleRate);
                    writer.Write(sampleRate * channels * sizeof(short));
                    writer.Write((short)(channels * sizeof(short)));
                    writer.Write((short)16);
                    writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                    writer.Write(dataLength);

                    var scale = peak > 0 ? 0.88 / peak : 1;
                    foreach (var sample in samples)
                    {
                        var normalized = Math.Clamp(sample * scale, -1, 1);
                        writer.Write((short)(normalized * short.MaxValue));
                    }
                }

                File.Move(temporaryPath, fullPath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        private void BtnPlayBeat_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lastAudioPath) && File.Exists(lastAudioPath))
            {
                try
                {
                    using var process = Process.Start(new ProcessStartInfo(lastAudioPath) { UseShellExecute = true });
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
                {
                    lblStatus.Text = "●  НЕ УДАЛОСЬ ОТКРЫТЬ WAV-ФАЙЛ";
                    lblBeatInfo.Text = ex.Message;
                }
            }
            else
            {
                lblStatus.Text = "●  СНАЧАЛА СОЗДАЙ ТРЕК";
            }
        }

        private void BtnOpenFolder_Click(object? sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lastAudioPath) && File.Exists(lastAudioPath))
            {
                try
                {
                    using var process = Process.Start(new ProcessStartInfo(GetOutputDirectory()) { UseShellExecute = true });
                }
                catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
                {
                    lblStatus.Text = "●  НЕ УДАЛОСЬ ОТКРЫТЬ ПАПКУ";
                    lblBeatInfo.Text = ex.Message;
                }
            }
            else
            {
                lblStatus.Text = "●  СНАЧАЛА СОЗДАЙ БИТ";
            }
        }

    }
}
