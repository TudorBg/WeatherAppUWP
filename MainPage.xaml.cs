using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace WeatherApp
{
    public sealed partial class MainPage : Page
    {
        private const string WeatherApiKey = "2344bda1766d48c8b73105117230904";
        private const string WeatherApiBaseUrl = "http://api.weatherapi.com/v1/";
        private const string PexelsApiKey = "qrnhqFXiChTmceNBhEa2J7jQvl4PJIf9157Lyt4vIwamFwfpo2goUXsq";
        private bool isMetric = true;
        private bool isInternalUpdate = false;
        private bool newInit = true;
        private string location = "";

        public MainPage()
        {
            this.InitializeComponent();
            this.MinHeight = 500;
            this.MinWidth = 900;

            System.Diagnostics.Debug.WriteLine("Constructor MainPage() called.");
            SuggestionsListView = (ListView)FindName("SuggestionsListView");
        }

        private void OnUnitChecked(object sender, RoutedEventArgs e)
        {
            if (newInit)
            {
                newInit = false;
                return;
            }
            else
            {
                RadioButton rb = sender as RadioButton;
                isMetric = rb.Content.ToString() == "Metric";

                if (!string.IsNullOrEmpty(CityInput.Text))
                {
                    var city = CityInput.Text.Split(',')[0];
                    _ = UpdateWeather(city);
                }
            }
            

        }

        private void OnSearchFieldFocus(object sender, RoutedEventArgs e)
        {
            SuggestionsListView.Visibility = Visibility.Visible;
        }

        private async void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInternalUpdate)
            {
                isInternalUpdate = false;
                return;
            }
            var query = (sender as TextBox).Text;
            if (query.Length > 2)
            {
                try
                {
                    var suggestions = await GetAutocompleteSuggestions(query);
                    SuggestionsListView.ItemsSource = suggestions;
                    SuggestionsListView.Visibility = Visibility.Visible;
                }
                catch (Exception ex) {
                    Debug.WriteLine($"Error fetching suggestions: {ex.Message}");
                    SuggestionsListView.ItemsSource = null;
                    SuggestionsListView.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                SuggestionsListView.ItemsSource = null;
                SuggestionsListView.Visibility = Visibility.Collapsed;
            }
        }

        private async Task<List<string>> GetAutocompleteSuggestions(string query)
        {
            try
            {
                var client = new HttpClient();
                var response = await client.GetStringAsync($"{WeatherApiBaseUrl}search.json?key={WeatherApiKey}&q={query}");
                var locations = JArray.Parse(response);
                var suggestions = new List<string>();
                foreach (var location in locations)
                {
                    suggestions.Add($"{location["name"]}, {location["country"]}");
                }
                return suggestions;
            }
            catch (Exception ex) {
                Debug.WriteLine($"Error fetching autocomplete suggestions: {ex.Message}");
                return new List<string>();
            }
        }

        private async void OnSearchSuggestionTapped(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var listView = sender as ListView;
                location = listView.SelectedItem.ToString();
                isInternalUpdate = true;
                CityInput.Text = location;
                SuggestionsListView.Visibility = Visibility.Collapsed;
                await UpdateWeather(location.Split(',')[0]);
                isInternalUpdate = false;
            }
        }

        private async Task<string> GetImageUrl(string city)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", PexelsApiKey);

            var response = await client.GetAsync($"https://api.pexels.com/v1/search?query={city}&orientation=landscape");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var imageDataJson = JObject.Parse(content);
                if (imageDataJson["photos"].Count() > 0)
                {
                    var randomPhoto = new Random().Next(0, imageDataJson["photos"].Count());
                    return imageDataJson["photos"][randomPhoto]["src"]["original"].ToString();
                }
            }

            return "https://images.pexels.com/photos/531756/pexels-photo-531756.jpeg?auto=compress&cs=tinysrgb&w=1260&h=750&dpr=1";
        }

        private async Task UpdateWeather(string city)
        {
            var client = new HttpClient();
            var response = await client.GetStringAsync($"{WeatherApiBaseUrl}current.json?key={WeatherApiKey}&q={city}");
            var weatherData = JObject.Parse(response);

            var currentWeather = weatherData["current"];
            Location.Text = location;
            TemperatureTextBlock.Text = isMetric ? $"{currentWeather["temp_c"]}°C" : $"{currentWeather["temp_f"]}°F";
            ConditionTextBlock.Text = currentWeather["condition"]["text"].ToString();
            WindTextBlock.Text = isMetric ? $"{currentWeather["wind_kph"]} km/h, {currentWeather["wind_dir"]}" : $"{currentWeather["wind_mph"]} mi/h, {currentWeather["wind_dir"]}";
            UVTextBlock.Text = $"UV Index: {currentWeather["uv"]}";
            RealFeelTextBlock.Text = isMetric ? $"Feels like: {currentWeather["feelslike_c"]}°C" : $"Feels like: {currentWeather["feelslike_f"]}°F";
            WeatherIcon.Source = new BitmapImage(new Uri($"http:{currentWeather["condition"]["icon"]}"));
            WeatherInfoPanel.Visibility = Visibility.Visible;
            
            
            CityImage.ImageSource = new BitmapImage(new Uri(await GetImageUrl(city)));
            
        }
    }
}
