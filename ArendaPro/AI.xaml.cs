using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace ArendaPro
{
    /// <summary>
    /// Логика взаимодействия для AI.xaml
    /// </summary>
    ///         private readonly HttpClient _httpClient = new HttpClient();
   
    public partial class AI : Window
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private const string OllamaUrl = "http://localhost:11434/api/generate";
        public AI()
        {
            InitializeComponent();

            // Пример кода для анализа
            CodeTextBox.Text = @"public class Calculator 
{
    public int Add(int a, int b) => a + b;
}";
        }

        private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string prompt = $"{PromptTextBox.Text}:\n{CodeTextBox.Text}";
                string response = await GetAnalysisFromOllama(prompt);

                ResultTextBox.Text = response;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка анализа");
            }
        }

        private async Task<string> GetAnalysisFromOllama(string prompt)
        {
            var request = new
            {
                model = "codellama",
                prompt,
                stream = false
            };

            var json = JsonConvert.SerializeObject(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OllamaUrl, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            dynamic result = JsonConvert.DeserializeObject(responseJson);
            return result.response;
        }
    }
}

