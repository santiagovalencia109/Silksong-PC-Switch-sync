using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MediaDevices;
using System.Text.Json;

namespace SwitchFileSync
{
    public partial class MainForm : Form
    {


        private MediaDevice? switchDevice;
        private AppConfig config;
        private double? pcPlaytime = null;
        private double? switchPlaytime = null;

        public MainForm()
        {
            InitializeComponent();


            this.Load += (s, e) =>
            {
                float scale = this.DeviceDpi / 96f;

                if (scale > 1.4f) // ~150% o más
                {
                    this.MinimumSize = new System.Drawing.Size(800, 600);
                }
                else if (scale > 1.2f) // ~125%
                {
                    this.MinimumSize = new System.Drawing.Size(600, 500);
                }
                else // 100%
                {
                    this.MinimumSize = new System.Drawing.Size(600, 450);
                }
            };

            // 🔹 cargar configuración
            config = AppConfig.Load();
            txtPcPath.Text = config.PcPath;
            txtSwitchPath.Text = config.SwitchPath;
            progressBar.Visible = false;

            DetectSwitch();

            if (switchDevice == null || !switchDevice.IsConnected)
            {

                this.Close();
                return;
            }

            LoadSwitchExplorer();
            treeSwitchExplorer.BeforeExpand += treeSwitchExplorer_BeforeExpand;

            if (!string.IsNullOrWhiteSpace(txtPcPath.Text))
                LoadPlaytimeFromPc(txtPcPath.Text);

            if (!string.IsNullOrWhiteSpace(txtSwitchPath.Text))
                LoadPlaytimeFromSwitch(txtSwitchPath.Text);

        }

        private void DetectSwitch()
        {
            var devices = MediaDevice.GetDevices();
            switchDevice = devices.FirstOrDefault();

            if (switchDevice != null)
            {
                switchDevice.Connect();
                MessageBox.Show("Switch detected vía MTP.");
            }
            else
            {
                MessageBox.Show("Switch is not connected. Please connect it via USB in MTP mode.");
            }
        }

        private void btnBrowsePc_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtPcPath.Text = fbd.SelectedPath;
                LoadPlaytimeFromPc(fbd.SelectedPath);
            }
        }

        private void btnBrowseSwitch_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtSwitchPath.Text = fbd.SelectedPath;
                LoadPlaytimeFromSwitch(fbd.SelectedPath);
            }
        }

        private async void btnSendToSwitch_Click(object sender, EventArgs e)
        {
            try
            {
                if (switchDevice == null || !switchDevice.IsConnected)
                {
                    MessageBox.Show("Switch is not connected. Please connect it via USB.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string pcPath = txtPcPath.Text;
                string switchPath = txtSwitchPath.Text;
                if (!Directory.Exists(pcPath))
                {
                    MessageBox.Show("Invalid PC path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (switchPlaytime.Value == pcPlaytime.Value)
                {
                    MessageBox.Show("The saves are already synchronized.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (pcPlaytime.HasValue && switchPlaytime.HasValue && pcPlaytime.Value < switchPlaytime.Value)
                {
                    var result = MessageBox.Show(
                        "Warning: You are about to overwrite the Switch save with an older PC save.\nDo you want to continue?",
                        "Older Save Detected",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                        return;
                }

                progressBar.Visible = true;
                progressBar.Value = 0;


                // 🔹 Backup
                string backupRoot = CreateBackupRoot();
                BackupFromPc(pcPath, backupRoot);
                BackupFromSwitch(switchPath, backupRoot);

                // 🔹 Subir

                int total = Directory.GetFiles(pcPath, "*.dat", SearchOption.AllDirectories).Length;
                int count = 0;

                // 🔹 Copiar en segundo plano con progreso
                await Task.Run(() =>
                {
                    CopyFromPcRecursive(pcPath, switchPath,
                        progress => this.Invoke(new Action(() => progressBar.Value = progress)),
                        ref count, total);
                });

                LoadPlaytimeFromPc(txtPcPath.Text);
                LoadPlaytimeFromSwitch(txtSwitchPath.Text);

                MessageBox.Show("Files uploaded to Switch (backup created).", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during upload: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }finally
            {
                progressBar.Visible = false;
            }
        }


        private async void btnSendToPc_Click(object sender, EventArgs e)
        {
            try
            {
                if (switchDevice == null || !switchDevice.IsConnected)
                {
                    MessageBox.Show("Switch is not connected. Please connect it via USB.",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string pcPath = txtPcPath.Text;
                string switchPath = txtSwitchPath.Text;

                if (!Directory.Exists(pcPath))
                {
                    MessageBox.Show("Invalid PC path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 🔹 Contar archivos antes
                int total = CountSwitchFiles(switchPath);
                if (total == 0)
                {
                    MessageBox.Show("No save files found on Switch.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                int count = 0;

                progressBar.Visible = true;
                progressBar.Value = 0;

                // 🔹 Backup
                string backupRoot = CreateBackupRoot();
                BackupFromPc(pcPath, backupRoot);
                BackupFromSwitch(switchPath, backupRoot);

                // 🔹 Copiar en segundo plano con progreso
                await Task.Run(() =>
                {
                    CopyFromSwitchRecursive(switchPath, pcPath,
                        progress => this.Invoke(new Action(() => progressBar.Value = progress)),
                        ref count, total);
                });

                LoadPlaytimeFromPc(txtPcPath.Text);
                LoadPlaytimeFromSwitch(txtSwitchPath.Text);

                MessageBox.Show("Files downloaded from Switch (backup created).", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during download: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar.Visible = false;
            }
        }



        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // 🔹 guardar configuración
            config.PcPath = txtPcPath.Text;
            config.SwitchPath = txtSwitchPath.Text;
            config.Save();
        }

        private void LoadSwitchExplorer()
        {
            if (switchDevice == null) return;

            treeSwitchExplorer.Nodes.Clear();

            var rootNode = new TreeNode("Switch (MTP)") { Tag = "\\" };
            treeSwitchExplorer.Nodes.Add(rootNode);

            LoadDirectories(rootNode);
        }

        private void LoadDirectories(TreeNode node)
        {
            string path = node.Tag.ToString();

            try
            {
                var dirs = switchDevice.GetDirectories(path);
                foreach (var dir in dirs)
                {
                    var child = new TreeNode(Path.GetFileName(dir)) { Tag = dir };
                    // Agregar un nodo vacío para mostrar el "+" expandible
                    child.Nodes.Add(new TreeNode("Loading..."));
                    node.Nodes.Add(child);
                }
            }
            catch
            {
                // Si no se puede acceder a la carpeta, la ignoramos
            }
        }

        private void treeSwitchExplorer_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "Loading...")
            {
                e.Node.Nodes.Clear();
                LoadDirectories(e.Node);
            }
        }

        private void treeSwitchExplorer_AfterSelect(object sender, TreeViewEventArgs e)
        {
            txtSwitchPath.Text = e.Node.Tag.ToString();
            LoadPlaytimeFromSwitch(txtSwitchPath.Text);
        }

        // Copiar recursivamente de Switch → PC
        private void CopyFromSwitchRecursive(string switchPath, string pcPath, Action<int> reportProgress, ref int count, int total)
        {
            Directory.CreateDirectory(pcPath);

            // Copiar archivos de la carpeta actual
            var files = switchDevice.GetFiles(switchPath);
            foreach (var file in files.Where(f => f.EndsWith(".dat")))
            {
                string localFile = Path.Combine(pcPath, Path.GetFileName(file));

                using (var fs = File.Create(localFile))
                {
                    switchDevice.DownloadFile(file, fs);
                }

                // (opcional) encriptar aquí si corresponde
                string json = File.ReadAllText(localFile);
                SaveFileEncoder.EncodeDatFile(json, localFile);

                // 🔹 Reportar progreso
                count++;
                int progress = (int)((count / (float)total) * 100);
                reportProgress(progress);
            }

            // Procesar subcarpetas
            var dirs = switchDevice.GetDirectories(switchPath);
            foreach (var dir in dirs)
            {
                string localSubDir = Path.Combine(pcPath, Path.GetFileName(dir));
                CopyFromSwitchRecursive(dir, localSubDir, reportProgress, ref count, total);
            }
        }





        // Copiar recursivamente de PC → Switch
        private void CopyFromPcRecursive(string pcPath, string switchPath, Action<int> reportProgress, ref int count, int total)
        {
            // Copiar archivos locales
            foreach (string file in Directory.GetFiles(pcPath, "*.dat"))
            {
                string json = SaveFileEncoder.DecodeDatFile(file);
                string tempFile = Path.Combine(Path.GetTempPath(), Path.GetFileName(file));
                File.WriteAllText(tempFile, json);

                string targetFile = Path.Combine(switchPath, Path.GetFileName(file)).Replace("\\", "/");

                if (switchDevice.FileExists(targetFile))
                {
                    switchDevice.DeleteFile(targetFile);
                }

                using (FileStream fs = File.OpenRead(tempFile))
                {
                    switchDevice.UploadFile(fs, targetFile);
                }

                // 🔹 Actualizar progreso
                count++;
                int progress = (int)((count / (float)total) * 100);
                reportProgress(progress);
            }

            // Procesar subcarpetas
            foreach (string dir in Directory.GetDirectories(pcPath))
            {
                string subDirName = Path.GetFileName(dir);
                string switchSubDir = Path.Combine(switchPath, subDirName).Replace("\\", "/");

                if (!switchDevice.DirectoryExists(switchSubDir))
                {
                    switchDevice.CreateDirectory(switchSubDir);
                }

                CopyFromPcRecursive(dir, switchSubDir, reportProgress, ref count, total);
            }
        }

        private int CountSwitchFiles(string switchPath)
        {
            int count = switchDevice.GetFiles(switchPath).Count(f => f.EndsWith(".dat"));

            foreach (var dir in switchDevice.GetDirectories(switchPath))
            {
                count += CountSwitchFiles(dir);
            }

            return count;
        }


        private void LoadPlaytimeFromPc(string pcPath)
        {
            string filePath = Path.Combine(pcPath, "user1.dat");
            if (!File.Exists(filePath))
            {
                pcPlaytime = null;
                lblPlaytimePc.Text = "Playtime PC: N/A";
                ComparePlaytimes();
                return;
            }

            try
            {
                string json = SaveFileEncoder.DecodeDatFile(filePath);
                var pt = TryGetPlaytime(json);

                if (pt.HasValue)
                {
                    pcPlaytime = pt.Value;
                    lblPlaytimePc.Text = $"Playtime PC: {FormatSecondsAsHMS(pcPlaytime.Value)}";
                }
                else
                {
                    pcPlaytime = null;
                    lblPlaytimePc.Text = "Playtime PC: not found";
                }
            }
            catch
            {
                pcPlaytime = null;
                lblPlaytimePc.Text = "Playtime PC: error";
            }

            ComparePlaytimes();
        }

        // Leer playTime desde Switch
        private void LoadPlaytimeFromSwitch(string switchPath)
        {

            string filePath = Path.Combine(switchPath, "user1.dat").Replace("\\", "/");
            if (switchDevice == null || !switchDevice.IsConnected || !switchDevice.FileExists(filePath))
            {
                switchPlaytime = null;
                lblPlaytimeSwitch.Text = "Playtime Switch: N/A";
                ComparePlaytimes();
                return;
            }

            try
            {
                using var ms = new MemoryStream();
                switchDevice.DownloadFile(filePath, ms);
                ms.Position = 0;
                using var reader = new StreamReader(ms);
                string json = reader.ReadToEnd();

                var pt = TryGetPlaytime(json);

                if (pt.HasValue)
                {
                    switchPlaytime = pt.Value;
                    lblPlaytimeSwitch.Text = $"Playtime Switch: {FormatSecondsAsHMS(switchPlaytime.Value)}";
                }
                else
                {
                    switchPlaytime = null;
                    lblPlaytimeSwitch.Text = "Playtime Switch: not found";
                }
            }
            catch
            {
                switchPlaytime = null;
                lblPlaytimeSwitch.Text = "Playtime Switch: error";
            }

            ComparePlaytimes();
        }

        private static string FormatSecondsAsHMS(double seconds)
        {
            var ts = TimeSpan.FromSeconds(seconds);
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
        }

        private double? TryGetPlaytime(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Ruta conocida actual: playerData.playTime
            if (root.TryGetProperty("playerData", out var playerData) &&
                playerData.TryGetProperty("playTime", out var ptKnown) &&
                ptKnown.TryGetDouble(out double dKnown))
            {
                return dKnown;
            }

            // Respaldo: búsqueda recursiva por si cambia la estructura
            return FindDoublePropertyRecursive(root, "playTime");
        }

        private double? FindDoublePropertyRecursive(JsonElement el, string name)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase) &&
                            prop.Value.ValueKind == JsonValueKind.Number &&
                            prop.Value.TryGetDouble(out double d))
                        {
                            return d;
                        }
                        var nested = FindDoublePropertyRecursive(prop.Value, name);
                        if (nested.HasValue) return nested;
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                    {
                        var nested = FindDoublePropertyRecursive(item, name);
                        if (nested.HasValue) return nested;
                    }
                    break;
            }
            return null;
        }
        private void ComparePlaytimes()
        {
            if (!pcPlaytime.HasValue || !switchPlaytime.HasValue)
            {
                lblPlaytimePc.ForeColor = System.Drawing.Color.Black;
                lblPlaytimeSwitch.ForeColor = System.Drawing.Color.Black;
                return;
            }

            if (Math.Abs(pcPlaytime.Value - switchPlaytime.Value) < 0.001) // casi iguales
            {
                lblPlaytimePc.ForeColor = System.Drawing.Color.Blue;
                lblPlaytimeSwitch.ForeColor = System.Drawing.Color.Blue;

                lblPlaytimePc.Text += " (Synchronized)";
                lblPlaytimeSwitch.Text += " (Synchronized)";
            }
            else if (pcPlaytime > switchPlaytime)
            {
                lblPlaytimePc.ForeColor = System.Drawing.Color.Green;
                lblPlaytimeSwitch.ForeColor = System.Drawing.Color.Red;
            }
            else
            {
                lblPlaytimePc.ForeColor = System.Drawing.Color.Red;
                lblPlaytimeSwitch.ForeColor = System.Drawing.Color.Green;
            }
        }



        //backup
        private string CreateBackupRoot()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups", timestamp);
            Directory.CreateDirectory(root);
            return root;
        }

        private void BackupFromPc(string pcPath, string backupRoot)
        {
            string dest = Path.Combine(backupRoot, "pc");
            CopyDirectory(pcPath, dest);
        }

        private void CopyDirectory(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);

            // Copiar archivos
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string target = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, target, true);
            }

            // Recursividad en subcarpetas
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string subDest = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, subDest);
            }
        }

        private void BackupFromSwitch(string switchPath, string backupRoot)
        {
            string dest = Path.Combine(backupRoot, "switch");
            CopyFromSwitchRecursiveBack(switchPath, dest);
        }
        
        
        private void CopyFromSwitchRecursiveBack(string switchPath, string pcPath)
        {
            Directory.CreateDirectory(pcPath);

            // Copiar archivos de la carpeta actual
            var files = switchDevice.GetFiles(switchPath);
            foreach (var file in files.Where(f => f.EndsWith(".dat")))
            {
                string localFile = Path.Combine(pcPath, Path.GetFileName(file));

                using (var fs = File.Create(localFile))
                {
                    switchDevice.DownloadFile(file, fs);
                }

                // (opcional) encriptar aquí si corresponde
                string json = File.ReadAllText(localFile);
                SaveFileEncoder.EncodeDatFile(json, localFile);
            }

            // Procesar subcarpetas
            var dirs = switchDevice.GetDirectories(switchPath);
            foreach (var dir in dirs)
            {
                string localSubDir = Path.Combine(pcPath, Path.GetFileName(dir));
                CopyFromSwitchRecursiveBack(dir, localSubDir);
            }
        }
        

    }
}
