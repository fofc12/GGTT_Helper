using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ZeroWubiLens;

public partial class VocabularyWorkshopWindow : Window
{
    private readonly Settings _settings;
    private readonly VocabularyStore _store;
    private readonly ObservableCollection<VocabularyEntry> _entries = new();

    internal VocabularyWorkshopWindow(Settings settings)
    {
        _settings = settings;
        _store = new VocabularyStore(settings.ResolvedZeroXiRepoPath);
        InitializeComponent();
        CandidateGrid.ItemsSource = _entries;
        Loaded += (_, _) =>
        {
            LevelBox.SelectedIndex = 2;
            QueryBox.Text = "ghdm";
            RefreshResults();
        };
    }

    private void RefreshResults()
    {
        try
        {
            _entries.Clear();
            foreach (var entry in _store.Lookup(QueryBox.Text))
                _entries.Add(entry);
            Log($"Loaded {_entries.Count} entries");
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void CandidateGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CandidateGrid.SelectedItem is not VocabularyEntry entry)
            return;
        EditTextBox.Text = entry.Text;
        EditCodeBox.Text = entry.Code;
        WeightBox.Text = string.IsNullOrWhiteSpace(entry.Weight) ? "100000" : entry.Weight;
    }

    private void Lookup_Click(object sender, RoutedEventArgs e) => RefreshResults();
    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshResults();

    private void QueryBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            RefreshResults();
        }
    }

    private void Pin_Click(object sender, RoutedEventArgs e) => SetState("pinned");
    private void Promote_Click(object sender, RoutedEventArgs e) => SetState("promoted");
    private void Pending_Click(object sender, RoutedEventArgs e) => SetState("pending");
    private void Reject_Click(object sender, RoutedEventArgs e) => SetState("rejected");
    private void ClearState_Click(object sender, RoutedEventArgs e) => SetState("normal");

    private void SetState(string state)
    {
        try
        {
            _store.SetState(EditTextBox.Text.Trim(), EditCodeBox.Text.Trim(), state);
            Log($"{EditCodeBox.Text}\t{EditTextBox.Text}\t{state}");
            RefreshResults();
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void AddEntry_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var level = ((ComboBoxItem)LevelBox.SelectedItem).Tag?.ToString() ?? "personal";
            var weight = int.TryParse(WeightBox.Text.Trim(), out var parsed) ? parsed : 100000;
            _store.AddEntry(EditTextBox.Text.Trim(), EditCodeBox.Text.Trim(), level, weight);
            Log($"added {EditCodeBox.Text}\t{EditTextBox.Text}\t{level}");
            RefreshResults();
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private async void Deploy_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Log("install + deploy started...");
            var output = await Task.Run(() => _store.InstallAndDeploy());
            Log(output.Trim());
        }
        catch (Exception ex)
        {
            Log(ex.Message);
        }
    }

    private void Log(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;
        LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        LogBox.ScrollToEnd();
    }
}
