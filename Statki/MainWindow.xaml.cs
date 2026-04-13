using System.Net;
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

namespace Statki
{
    /// <summary>
    /// Główne okno aplikacji. Zarządza ekranami aplikacji oraz reaguje na rozłączenia z serwerem i przeciwnikiem.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Konstruktor MainWindow. Inicjalizuje xaml i pokazuje ekran logowania.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ShowLogin();
        }

        /// <summary>
        /// Wyświetla ekran logowania.
        /// </summary>
        public void ShowLogin()
        {
            MainContent.Content = new Login(this);
        }

        /// <summary>
        /// Wyświetla ekran rejestracji.
        /// </summary>
        public void ShowRegistering()
        {
            MainContent.Content = new Register(this);
        }

        /// <summary>
        /// Wyświetla menu główne.
        /// </summary>
        public void ShowMenu()
        {
            MainContent.Content = new MainMenu(this);
        }

        /// <summary>
        /// Wyświetla ekran wyszukiwania przeciwnika.
        /// </summary>
        public void ShowSearch()
        {
            MainContent.Content = new GameSearch(this);
        }

        /// <summary>
        /// Wyświetla ekran z informacją o znalezieniu przeciwnika i przygotowanie do gry.
        /// </summary>
        /// <param name="opponent_nickname">Nazwa przeciwnika.</param>
        public void ShowPrep(string opponent_nickname)
        {
            MainContent.Content = new GameFound(this, opponent_nickname);
        }

        /// <summary>
        /// Wyświetla ekran gry z aktualnym stanem planszy gracza.
        /// </summary>
        /// <param name="playerBoardState">Dwuwymiarowa tablica reprezentująca planszę gracza.</param>
        public void ShowGame(int[,] playerBoardState)
        {
            MainContent.Content = new GameScreen(this, playerBoardState);
        }

        /// <summary>
        /// Obsługuje utratę połączenia z serwerem. Informuje użytkownika i przekierowuje do menu.
        /// </summary>
        public void ServerDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Utracono połączenie z serwerem. Powróć do menu głównego.", "Utrata połączenia", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShowMenu();
            });
        }

        /// <summary>
        /// Obsługuje rozłączenie przeciwnika. Informuje użytkownika i przekierowuje do menu jak poprzednia metoda.
        /// </summary>
        public void OpponentDisconnected()
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Utracono połączenie z przeciwnikiem. Powróć do menu głównego.", "Utrata połączenia", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShowMenu();
            });
        }
    }
};