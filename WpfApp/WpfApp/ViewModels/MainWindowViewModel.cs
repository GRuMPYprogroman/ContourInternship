using System.IO;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WpfApp.Attributes;
using WpfApp.Models;

namespace WpfApp.ViewModels;

public partial class MainWindowViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartProcessModule1Command))]
    [NotifyCanExecuteChangedFor(nameof(StartProcessModule2Command))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(StartProcessModule1Command))]
    [FileExtension(".dat")]
    private string? _datFilePathModule1;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(StartProcessModule1Command))]
    [FileExtension(".csv")]
    private string? _csvFilePathModule1;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(StartProcessModule2Command))]
    [FileExtension(".dat")]
    private string? _datFilePathModule2;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(StartProcessModule2Command))]
    [FileExtension(".csv")]
    private string? _csvFilePathModule2;

    [ObservableProperty] private double _gaugePercent;

    [RelayCommand]
    private void OpenFileModule1(string extension)
    {
        var dialog = new OpenFileDialog()
        {
            Title = "Open File",
            Filter = "CSV/DAT Files (*.csv;*.dat)|*.csv;*.dat|CSV Files (.csv)|*.csv| DAT Files (*.dat)|*.dat",
            FilterIndex = 1,
            Multiselect = false,
        };

        bool? result = dialog.ShowDialog();

        if (result != true)
            return;

        switch (extension)
        {
            case ".csv":
                CsvFilePathModule1 = dialog.FileName;
                break;
            case ".dat":
                DatFilePathModule1 = dialog.FileName;
                break;
        }
    }
    
    [RelayCommand]
    private void OpenFileModule2(string extension)
    {
        var dialog = new OpenFileDialog()
        {
            Title = "Open File",
            Filter = "CSV/DAT Files (*.csv;*.dat)|*.csv;*.dat|CSV Files (.csv)|*.csv| DAT Files (*.dat)|*.dat",
            FilterIndex = 1,
            Multiselect = false,
        };

        bool? result = dialog.ShowDialog();

        if (result != true)
            return;

        switch (extension)
        {
            case ".csv":
                CsvFilePathModule2 = dialog.FileName;
                break;
            case ".dat":
                DatFilePathModule2 = dialog.FileName;
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartProcessModule1))]
    private async Task StartProcessModule1()
    {
        IsBusy = true;
        GaugePercent = 0;
        
        try
        {
            await Task.Run(async () =>
            {
                int blockSize = 60;
                int packageSize = 1456;

                List<PackageModule1> packages = new();
                
                await using (FileStream datFile = File.OpenRead(DatFilePathModule1!))
                using (var binReader = new BinaryReader(datFile))
                {
                    long fileSize = datFile.Length;

                    if (fileSize % packageSize != 0)
                        throw new InvalidDataException($"Incorrect file size: {fileSize} bytes - data corrupted");

                    long packageCount = fileSize / packageSize;

                    for (long i = 0; i < packageCount; i++)
                    {
                        uint size = binReader.ReadUInt32();
                        uint type = binReader.ReadUInt32();
                        uint number = binReader.ReadUInt32();
                        uint timeMs = binReader.ReadUInt32();

                        var package = new PackageModule1(size, type, number, timeMs, new int[blockSize, 6]);

                        for (int j = 0; j < blockSize; j++)
                        {
                            package.Channels[j, 0] = binReader.ReadInt32();
                            package.Channels[j, 1] = binReader.ReadInt32();
                            package.Channels[j, 2] = binReader.ReadInt32();
                            package.Channels[j, 3] = binReader.ReadInt32();
                            package.Channels[j, 4] = binReader.ReadInt32();
                            package.Channels[j, 5] = binReader.ReadInt32();
                        }

                        packages.Add(package);

                        Application.Current.Dispatcher.Invoke(() => GaugePercent += (i + 1) * 100.0 / packageCount);
                    }
                }

                await using (FileStream csvFile = File.OpenWrite(CsvFilePathModule1!))
                await using (var streamWriter = new StreamWriter(csvFile))
                {
                    var sb = new StringBuilder();

                    sb.AppendLine("Packet;Channel 1;Channel 2;Channel 3;Channel 4;Channel 5;Channel 6");

                    foreach (var package in packages)
                    {
                        int rows = package.Channels.GetLength(0);
                        int cols = package.Channels.GetLength(1);

                        double[] averages = new double[cols];

                        for (int col = 0; col < cols; col++)
                        {
                            long sum = 0;

                            for (int row = 0; row < rows; row++)
                            {
                                sum += package.Channels[row, col];
                            }

                            averages[col] = (double)sum / rows;
                        }

                        string result = $"{package.Number};{string.Join(";", averages)}";;

                        sb.AppendLine(result);
                    }

                    await streamWriter.WriteLineAsync(sb.ToString());
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            GaugePercent = 0;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartProcessModule2))]
    private async Task StartProcessModule2()
    {
        IsBusy = true;
        GaugePercent = 0;

        try
        {
            await Task.Run(async () =>
            {
                int blockSize = 32;

                List<PackageModule2> packages = new();

                await using (FileStream datFile = File.OpenRead(DatFilePathModule2!))
                using (var binReader = new BinaryReader(datFile))
                {
                    long fileSize = datFile.Length;

                    if (fileSize % blockSize != 0)
                        throw new InvalidDataException($"Incorrect file size: {fileSize} bytes - data corrupted");

                    long packageCount = fileSize / blockSize;

                    for (long i = 0; i < packageCount; i++)
                    {
                        uint rawData = binReader.ReadUInt32();

                        bool isEnabled1 = (rawData & 0b1) != 0;

                        uint value11 = (rawData >> 1) & 0b111;
                        uint value12 = (rawData >> 4) & 0b111;
                        uint value13 = (rawData >> 7) & 0b1_1111_1111;

                        bool isEnabled2 = ((rawData >> 16) & 0b1) != 0;

                        uint value21 = (rawData >> 17) & 0b111_1111_1111;
                        uint value22 = (rawData >> 28) & 0b1111;

                        packages.Add(new PackageModule2(
                            isEnabled1,
                            value11,
                            value12,
                            value13,
                            isEnabled2,
                            value21,
                            value22));

                        Application.Current.Dispatcher.Invoke(() => GaugePercent += (i + 1) * 100.0 / packageCount);
                    }
                }

                await using (FileStream csvFile = File.OpenWrite(CsvFilePathModule2!))
                await using (var streamWriter = new StreamWriter(csvFile))
                {
                    var sb = new StringBuilder();

                    sb.AppendLine("IsEnabled1;Value11;Value12;Value13;IsEnabled2;Value21;Value22");

                    foreach (var package in packages)
                    {
                        sb.AppendLine(string.Join(';',
                            package.IsEnabled1,
                            package.Value11,
                            package.Value12,
                            package.Value13,
                            package.IsEnabled2,
                            package.Value21,
                            package.Value22));
                    }

                    await streamWriter.WriteLineAsync(sb.ToString());
                }
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            GaugePercent = 0;
        }
    }
    
    public void Load()
    {
        string filePath = $"{GetDataPath()}/cache.bin";
        
        if (File.Exists(filePath))
        {
            using var file = File.OpenRead(filePath);
            using var binaryWriter = new BinaryReader(file);

            DatFilePathModule1 = binaryWriter.ReadString();
            CsvFilePathModule1 = binaryWriter.ReadString();
            DatFilePathModule2 = binaryWriter.ReadString();
            CsvFilePathModule2 = binaryWriter.ReadString();
        }
    }

    public void Save()
    {
        using var file = File.Create($"{GetDataPath()}/cache.bin");
        using var binaryWriter = new BinaryWriter(file);
        
        binaryWriter.Write(DatFilePathModule1 ?? "");
        binaryWriter.Write(CsvFilePathModule1 ?? "");
        binaryWriter.Write(DatFilePathModule2 ?? "");
        binaryWriter.Write(CsvFilePathModule2 ?? "");
    }

    private string GetDataPath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string directory = Path.Combine(appData, "WpfContourApp", "PathData");

        Directory.CreateDirectory(directory);
        
        return directory;
    }

    private bool CanStartProcessModule1 =>
        !IsBusy
        && !GetErrors(nameof(DatFilePathModule1)).Any()
        && !GetErrors(nameof(CsvFilePathModule1)).Any()
        && !string.IsNullOrWhiteSpace(DatFilePathModule1)
        && !string.IsNullOrWhiteSpace(CsvFilePathModule1);

    private bool CanStartProcessModule2 =>
        !IsBusy
        && !GetErrors(nameof(DatFilePathModule2)).Any()
        && !GetErrors(nameof(CsvFilePathModule2)).Any()
        && !string.IsNullOrWhiteSpace(DatFilePathModule2)
        && !string.IsNullOrWhiteSpace(CsvFilePathModule2);
}