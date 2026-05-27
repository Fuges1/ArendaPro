using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace ArendaPro
{
    /// <summary>
    /// Логика взаимодействия для AI.xaml
    /// </summary>
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
    // Логика: метод Add добавляет новую запись/элемент, подготавливая данные перед сохранением.
    // Метод Add: добавляет новую сущность в доменную коллекцию и синхронизирует зависимые визуальные и вычислительные представления (комментарий #1).
    public int Add(int a, int b) => a + b;
}";
        }

        // Метод AnalyzeButton_Click: обрабатывает нажатие в интерфейсе: считывает ввод, проверяет ограничения и запускает следующий пользовательский шаг (комментарий #2).
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

        // Метод GetAnalysisFromOllama: реализует отдельный этап внутренней логики модуля: трансформирует вход, применяет правила и формирует следующий шаг исполнения (комментарий #3).
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
