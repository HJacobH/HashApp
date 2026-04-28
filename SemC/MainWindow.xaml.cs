using System;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace SemC
{

    public partial class MainWindow : Window
    {
        private StaticHashFile<string, ObecData> _hashFile;

        public MainWindow()
        {
            InitializeComponent();

            _hashFile = new StaticHashFile<string, ObecData>(
                primaryBlocksCount: 2500,
                blockingFactor: 5,
                hashFunction: (key, blockCount) =>
                {
                    int hash = 0;
                    foreach (char c in key)
                    {
                        hash = (hash * 31 + c) % blockCount;
                    }
                    return Math.Abs(hash);
                },

                serializeRecord: (writer, record) =>
                {
                    writer.Write(record.Key); 
                    writer.Write(record.Value.Population); 
                    writer.Write(record.Value.Area);       
                },

                deserializeRecord: (reader) =>
                {
                    string key = reader.ReadString();
                    int pop = reader.ReadInt32();
                    double area = reader.ReadDouble();

                    return new HashRecord<string, ObecData>(key, new ObecData { Population = pop, Area = area });
                }
            );
        }

        protected override void OnClosed(EventArgs e)
        {
            _hashFile?.Dispose();
            base.OnClosed(e);
        }

        private void UpdateStats()
        {
            txtStats.Text = $"Čtení bloků: {_hashFile.ReadCount}\nZápis bloků: {_hashFile.WriteCount}";
        }

        private void BtnInsert_Click(object sender, RoutedEventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            var data = new ObecData
            {
                Population = int.TryParse(txtPop.Text, out int pop) ? pop : 0,
                Area = double.TryParse(txtArea.Text, out double area) ? area : 0
            };

            _hashFile.ResetIOStats();
            bool success = _hashFile.Insert(name, data);

            lstOutput.Items.Insert(0, success ? $"Vloženo: {name} ({data})" : $"Chyba: Obec {name} již existuje.");
            UpdateStats();
            
            if (success)
            {
                txtName.Clear(); txtPop.Clear(); txtArea.Clear();
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string key = txtName.Text.Trim();
            if (string.IsNullOrEmpty(key)) return;

            _hashFile.ResetIOStats();
            var record = _hashFile.Search(key);

            if (record != null)
                lstOutput.Items.Insert(0, $"Nalezeno: {record.Key} - {record.Value}");
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
            lstOutput.Items.Insert(0, "Generuji 10 000 unikátních záznamů a zapisuji na disk...");
            _hashFile.ResetIOStats();

            Random rnd = new Random();
            int count = 0;
            Stopwatch sw = Stopwatch.StartNew();

            while (count < 10000)
            {
                string name = $"Obec_{Guid.NewGuid().ToString().Substring(0, 8)}";
                var data = new ObecData
                {
                    Population = rnd.Next(100, 50000),
                    Area = Math.Round(rnd.NextDouble() * 100 + 1, 2)
                };

                if (_hashFile.Insert(name, data))
                {
                    count++;
                }
            }

            sw.Stop();
            lstOutput.Items.Insert(0, $"Generování dokončeno za {sw.ElapsedMilliseconds} ms.");
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
                lstOutput.Items.Add($"{r.Key} ({r.Value})");
            }

            lstOutput.Items.Insert(0, $"--- VÝPIS {allRecords.Count} ZÁZNAMŮ (Doba načtení z disku: {sw.ElapsedMilliseconds} ms) ---");
            UpdateStats();
        }
    }
}