using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Specialized;

namespace Statki
{
    /// <summary>
    /// Ekran wyszukiwania przeciwnika. Próbuje połączyć się z serwerem,
    /// odlicza czas i reaguje na znalezienie gry lub błędy.
    /// </summary>
    public partial class GameSearch : UserControl
    {
        private MainWindow main;

        /// <summary>
        /// Obiekt obsługujący połączenie z serwerem.
        /// </summary>
        private ClientConnection connection;

        /// <summary>
        /// Timer aktualizujący czas oczekiwania na przeciwnika.
        /// </summary>
        private DispatcherTimer timer;

        /// <summary>
        /// Liczba sekund, które minęły od rozpoczęcia wyszukiwania.
        /// </summary>
        private int secondsPassed;

        /// <summary>
        /// Konstruktor inicjalizuje komponent, timer, połączenie z serwerem i zaczyna wyszukiwanie gry.
        /// </summary>
        /// <param name="_main">Referencja do okna głównego aplikacji.</param>
        public GameSearch(MainWindow _main)
        {
            InitializeComponent();

            main = _main;
            SearchTimer.Text = "00:00";

            // Inicjalizacja i uruchomienie timera, który aktualizuje czas oczekiwania
            secondsPassed = 0;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            // Tworzenie nowego połączenia sieciowego
            connection = new();
            ((App)Application.Current).Connection = connection;

            // Subskrypcja zdarzenia rozłączenia serwera
            connection.ServerDisconnected += main.ServerDisconnected;

            // Rozpoczęcie procesu połączenia
            StartConnection(connection);
        }

        /// <summary>
        /// Zdarzenie wywoływane co sekundę, aktualizuje licznik czasu.
        /// </summary>
        private void Timer_Tick(object? sender, EventArgs e)
        {
            secondsPassed++;
            SearchTimer.Text = string.Format("{0:mm\\:ss}", TimeSpan.FromSeconds(secondsPassed).Duration());
        }

        /// <summary>
        /// Obsługa kliknięcia przycisku anulowania, który zatrzymuje timer, rozłącza i wraca do menu.
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;
                connection.Disconnect();

                main.ShowMenu();
            });
        }

        /// <summary>
        /// Inicjuje połączenie z serwerem oraz obsługuje komunikaty zwrotne.
        /// </summary>
        /// <param name="connection">Obiekt ClientConnection do połączenia z serwerem.</param>
        private async void StartConnection(ClientConnection connection)
        {
            // Pobranie portu z pliku App.config
            string? string_port = ConfigurationManager.AppSettings["Port"];
            if (string_port == null)
            {
                MessageBox.Show("Błąd: Port nie jest ustawiony. (App.config)");
                return;
            }
            int port = int.Parse(string_port);

            // Pobranie IP z pliku App.config
            string? ip = ConfigurationManager.AppSettings["IP"];
            if (ip == null)
            {
                MessageBox.Show("Błąd: IP nie jest ustawione. (App.config)");
                return;
            }

            // Pobranie nazwy użytkownika z wartości globalnej
            string nickname = ((App)Application.Current).Username;

            try
            {
                // Próba nawiązania połączenia z serwerem
                await connection.ConnectAsync(ip, port, nickname);
            }
            catch
            {
                Error("Nie udało się połączyć z serwerem. Spróbuj ponownie.");
                return;
            }

            // Obsługa wiadomości od serwera
            connection.MessageReceived += msg =>
            {
                if (msg.StartsWith("ZNALEZIONO:"))
                {
                    string opponent_nickname = msg.Substring(11);

                    Dispatcher.Invoke(() =>
                    {
                        timer.Stop();
                        timer.Tick -= Timer_Tick;
                        main.ShowPrep(opponent_nickname);
                    });
                }
                else if (msg.StartsWith("ZAJETE"))
                {
                    connection.Disconnect();
                    Error("Serwery są obecnie pełne. Spróbuj ponownie.");
                    return;
                }
            };
        }

        /// <summary>
        /// Wyświetla błąd w interfejsie użytkownika oraz zatrzymuje zegar.
        /// </summary>
        /// <param name="errmsg">Treść błędu do wyświetlenia.</param>
        private void Error(string errmsg)
        {
            Dispatcher.Invoke(() =>
            {
                timer.Stop();
                timer.Tick -= Timer_Tick;

                connection.Disconnect();

                SearchTimer.Visibility = Visibility.Collapsed;

                StatusBlock.FontSize = 60;
                StatusBlock.Text = errmsg;
            });
        }
    }
}