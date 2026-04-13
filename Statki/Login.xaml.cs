using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Npgsql;

namespace Statki
{
    /// <summary>
    /// Klasa odpowiedzialna za ekran logowania użytkownika.
    /// </summary>
    public partial class Login : UserControl
    {
        private MainWindow main;

        /// <summary>
        /// Ustawienia połączenia do bazy danych odczytane z pliku App.config.
        /// </summary>
        private ConnectionStringSettings connectionSettings;

        /// <summary>
        /// Konstruktor klasy Login. Inicjalizuje komponenty i tworzy połączenie z bazą danych.
        /// </summary>
        /// <param name="_main">Referencja do głównego okna.</param>
        public Login(MainWindow _main)
        {
            InitializeComponent();
            main = _main;

            // Pobranie informacji o połączeniu z pliku App.config
            connectionSettings = ConfigurationManager.ConnectionStrings["ConnectionString"];
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Zatwierdź" i próbuje zalogować użytkownika.
        /// Sprawdza poprawność danych i weryfikuje je z bazą danych.
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string nickname = NicknameBox.Text.Trim();
            string password = PasswordBox.Password;

            // Sprawdzenie czy pola nie są puste
            if (string.IsNullOrWhiteSpace(nickname) || string.IsNullOrWhiteSpace(password))
            {
                ErrorBlock.Text = "Nie podano nazwy lub hasła.";
                ErrorBlock.Visibility = Visibility.Visible;

                return;
            }

            // Hashowanie hasła do porównania
            string hash = Register.HashPassword(password);

            // Połączenie do bazy danych
            NpgsqlConnection conn = new NpgsqlConnection(connectionSettings.ConnectionString);

            using (conn)
            {
                try
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM \"Uzytkownicy\" WHERE nick = @nick AND haslo = @haslo";

                    using (NpgsqlCommand com = new NpgsqlCommand(query, conn))
                    {
                        // Parametryzacja zapytania, która zapobiega przed SQL injection
                        com.Parameters.AddWithValue("nick", nickname);
                        com.Parameters.AddWithValue("haslo", hash);

                        // ExecuteScalar zwróci pojedynczą wartość: liczbę użytkowników spełniających warunek
                        int userCount = Convert.ToInt32(com.ExecuteScalar());

                        // Jeśli użytkownik istnieje
                        if (userCount > 0)
                        {
                            // Zapisanie zalogowanego użytkownika do obiektu aplikacji
                            ((App)Application.Current).Username = nickname;

                            // Przejście do głównego menu
                            main.ShowMenu();
                        }
                        else
                        {
                            ErrorBlock.Text = "Nie istnieje użytkownik o podanych danych.";
                            ErrorBlock.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd połączenia z bazą danych: " + ex.Message);
                    Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Gość" – logowanie bez konta.
        /// </summary>
        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            ((App)Application.Current).Username = "Gosc";
            main.ShowMenu();
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Zarejestruj", przełącza widok do ekranu rejestracji.
        /// </summary>
        private void Register_Click(object sender, RoutedEventArgs e)
        {
            main.ShowRegistering();
        }
    }
}
