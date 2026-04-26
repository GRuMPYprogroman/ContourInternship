using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using WpfApp.Attributes;

namespace WpfApp.ViewModels;

public partial class MainWindowViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
    [FileExtension(".dat")]
    private string? _datFilePath;
    
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(StartProcessCommand))]
    [FileExtension(".csv")]
    private string? _csvFilePath;
    
    [RelayCommand]
    private void OpenFile(string extension)
    {
        var dialog = new OpenFileDialog()
        {
            Title = "Open File",
            DefaultExt = ".csv",
            Filter = "CSV Files (.csv)|*.csv| DAT Files (*.dat)|*.dat",
            Multiselect = false,
        };
        
        bool? result = dialog.ShowDialog();
        
        if (result != true)
            return;

        switch (extension)
        {
            case ".csv":
                CsvFilePath = dialog.FileName;
                break;
            case ".dat":
                DatFilePath = dialog.FileName;
                break;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartProcess))]
    private void StartProcess()
    {
        
    }

    private bool CanStartProcess => !HasErrors && !string.IsNullOrWhiteSpace(DatFilePath) 
                                               && !string.IsNullOrWhiteSpace(CsvFilePath);
}