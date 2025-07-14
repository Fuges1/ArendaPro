using Microsoft.Office.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Windows;

namespace ArendaPro
{

    public partial class ReturnReportWindow : Window
    {

        private readonly BD database;
        public string ReportFilePath { get; private set; }
        private readonly int _contractId;
        private readonly bool _isEarly;
        private readonly List<string> _photoPaths = new();
        private readonly string _employeeName;

        public ReturnReportWindow(int contractId, bool isEarly, string employeeName)
        {
            InitializeComponent();
            database = new BD(ConfigurationManager.ConnectionStrings["DbConnection"].ConnectionString);
            _contractId = contractId;
            _isEarly = isEarly;
            _employeeName = employeeName;

            EmployeeTextBox.Text = _employeeName;

            if (_isEarly)
            {
                ReasonLabel.Visibility = Visibility.Visible;
                ReasonTextBox.Visibility = Visibility.Visible;
            }
        }
        private void PhotosBorder_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effects = DragDropEffects.Copy;
            else e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void PhotosBorder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                AddPhotos(files);
            }
        }
        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Выберите фото",
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
                AddPhotos(dlg.FileNames);
        }

        private void AddPhotos(string[] files)
        {
            foreach (var f in files)
            {
                if (!_photoPaths.Contains(f))
                {
                    _photoPaths.Add(f);
                    PhotosList.Items.Add(new
                    {
                        FileName = System.IO.Path.GetFileName(f),
                        FullPath = f
                    });
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (_isEarly && string.IsNullOrWhiteSpace(ReasonTextBox.Text))
            {
                MessageBox.Show("Пожалуйста, укажите причину досрочного возврата.",
                                "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsDir);
            var fileName = $"report_{_contractId}_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            var fullPath = Path.Combine(reportsDir, fileName);

            var wordApp = new Microsoft.Office.Interop.Word.Application();
            var doc = wordApp.Documents.Add();
            var rng = doc.Content;
            rng.Font.Name = "Times New Roman";
            rng.Font.Size = 12;

            rng.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphCenter;
            rng.Text = $"ОТЧЁТ ПО ДОГОВОРУ №{_contractId}\r\n" +
                       $"Дата составления: {DateTime.Now:dd.MM.yyyy HH:mm}\r\n\r\n";
            rng.InsertParagraphAfter();

            var tableStart = doc.Content.End - 1;
            var tblRange = doc.Range(tableStart, tableStart);
            int rowsCount = 2 + (_isEarly ? 1 : 0);
            var table = doc.Tables.Add(tblRange, rowsCount, 2);
            table.Borders.Enable = 1;
            table.Range.Cells.VerticalAlignment = Microsoft.Office.Interop.Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            table.Cell(1, 1).Range.Text = "Параметр";
            table.Cell(1, 2).Range.Text = "Значение";
            table.Cell(2, 1).Range.Text = "Состояние автомобиля";
            table.Cell(2, 2).Range.Text = StateTextBox.Text.Trim();
            table.Cell(3, 1).Range.Text = "Сотрудник";
            table.Cell(3, 2).Range.Text = EmployeeTextBox.Text.Trim();
            if (_isEarly)
            {
                table.Cell(4, 1).Range.Text = "Причина досрочного возврата";
                table.Cell(4, 2).Range.Text = ReasonTextBox.Text.Trim();
            }

            rng = doc.Content;
            rng.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
            rng.InsertParagraphAfter();
            rng.InsertParagraphAfter();

            rng = doc.Content;
            rng.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
            rng.ParagraphFormat.Alignment = Microsoft.Office.Interop.Word.WdParagraphAlignment.wdAlignParagraphLeft;
            rng.Text = "Фотографии:\r\n";
            rng.InsertParagraphAfter();

            foreach (var photo in _photoPaths)
            {
                rng = doc.Content;
                rng.Collapse(Microsoft.Office.Interop.Word.WdCollapseDirection.wdCollapseEnd);
                try
                {
                    var pic = doc.InlineShapes.AddPicture(photo, LinkToFile: false, SaveWithDocument: true, Range: rng);
                    pic.Width = wordApp.CentimetersToPoints(10);    

                    pic.LockAspectRatio = MsoTriState.msoTrue;
                }
                catch
                {
                    rng.InsertAfter(Path.GetFileName(photo) + "\r\n");
                }
                rng.InsertParagraphAfter();
            }

             
            doc.SaveAs2(fullPath);
            doc.Close();
            wordApp.Quit();

        
            ReportFilePath = fullPath;
            DialogResult = true;
            Close();
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
