using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SemC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private StaticHashFile _hashFile;

        public MainWindow()
        {
            InitializeComponent();
            _hashFile = new StaticHashFile(2500, 5);
        }

        private void UpdateStats()
        {
            txtStats.Text = $"Čtení bloků: {_hashFile.ReadCount}\nZápis bloků: {_hashFile.WriteCount}";
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) return;

            var record = new Record
            {
                Name = txtName.Text.Trim(),
                Population = int.TryParse(txtPop.Text, out int pop) ? pop : 0,
                Area = double.TryParse(txtArea.Text, out double area) ? area : 0
            };

            _hashFile.ResetIOStats();
            bool success = _hashFile.Insert(record);

            lstOutput.Items.Insert(0, success ? $"Vloženo: {record}" : $"Chyba: Obec {record.Name} již existuje.");
            UpdateStats();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string key = txtName.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            _hashFile.ResetIOStats();
            var record = _hashFile.Search(key);

            if (record != null)
                lstOutput.Items.Insert(0, $"Nalezeno: {record}");
            else
                lstOutput.Items.Insert(0, $"Nenalezeno: Obec '{key}' neexistuje.");

            UpdateStats();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            string key = txtName.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            _hashFile.ResetIOStats();
            bool success = _hashFile.Delete(key);

            lstOutput.Items.Insert(0, success ? $"Smazáno: {key}" : $"Chyba při mazání: Obec '{key}' nenalezena.");
            UpdateStats();
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            lstOutput.Items.Insert(0, "Generuji 10 000 unikátních záznamů...");
            _hashFile.ResetIOStats();

            Random rnd = new Random();
            int count = 0;

            while (count < 10000)
            {
                string name = $"Obec_{Guid.NewGuid().ToString().Substring(0, 8)}";
                var record = new Record
                {
                    Name = name,
                    Population = rnd.Next(100, 50000),
                    Area = Math.Round(rnd.NextDouble() * 100 + 1, 2)
                };

                if (_hashFile.Insert(record))
                {
                    count++;
                }
            }

            lstOutput.Items.Insert(0, "Generování dokončeno.");
            UpdateStats();
        }

        private void BtnPrintAll_Click(object sender, RoutedEventArgs e)
        {
            lstOutput.Items.Clear();
            _hashFile.ResetIOStats();

            Stopwatch sw = Stopwatch.StartNew();
            var allRecords = _hashFile.GetAllRecords();
            sw.Stop();

            foreach (var r in allRecords)
            {
                lstOutput.Items.Add(r.ToString());
            }

            lstOutput.Items.Insert(0, $"--- VÝPIS {allRecords.Count} ZÁZNAMŮ (Doba: {sw.ElapsedMilliseconds} ms) ---");
            UpdateStats();
        }
    }
}