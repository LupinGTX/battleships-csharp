using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
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

namespace Statki
{
    /// <summary>
    /// Ekran rejestracji nowego użytkownika.
    /// </summary>
    public partial class Register : UserControl
    {
        /// <summary>
        /// Referencja do głównego okna aplikacji, prowadząca system nawigacji między ekranami.
        /// </summary>
        private MainWindow main;

        /// <summary>
        /// Ustawienia połączenia z bazą danych, pobierane z pliku App.config.
        /// </summary>
        private ConnectionStringSettings connectionSettings;

        /// <summary>
        /// Konstruktor ekranu rejestracji.
        /// </summary>
        /// <param name="_main">Referencja do głównego okna aplikacji.</param>
        public Register(MainWindow _main)
        {
            InitializeComponent();
            main = _main;
            connectionSettings = ConfigurationManager.ConnectionStrings["ConnectionString"];
        }

        /// <summary>
        /// Obsługa kliknięcia przycisku "Zarejestruj".
        /// Waliduje dane użytkownika, sprawdza ich unikalność w bazie i zapisuje nowego użytkownika.
        /// </summary>
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            string nickname = NicknameBox.Text.Trim();
            string password1 = PasswordBox1.Password;
            string password2 = PasswordBox2.Password;

            // Walidacja, czy nie rejestruje ktoś gościa
            if (nickname == "Gosc")
            {
                ErrorBlock.Text = "Nieprawidłowa nazwa użytkownika.";
                ErrorBlock.Visibility = Visibility.Visible;

                return;
            }

            // Walidacja, czy w ogóle wpisano dane
            if (string.IsNullOrWhiteSpace(nickname) ||
                string.IsNullOrWhiteSpace(password1) ||
                string.IsNullOrWhiteSpace(password2))
            {
                ErrorBlock.Text = "Nie podano nazwy lub hasła.";
                ErrorBlock.Visibility = Visibility.Visible;

                return;
            }

            // Walidacja długości nicku
            if (nickname.Length < 1 || nickname.Length > 15)
            {
                ErrorBlock.Text = "Nazwa powinna zawierać od 1 do 15 znaków.";
                ErrorBlock.Visibility = Visibility.Visible;

                return;
            }

            // Walidacja, czy wpisane hasła są identyczne
            if (password1 != password2)
            {
                ErrorBlock.Text = "Hasła się różnią.";
                ErrorBlock.Visibility = Visibility.Visible;

                return;
            }

            // Hashowanie hasła
            string hash = HashPassword(password1);

            // Połączenie do bazy danych
            NpgsqlConnection conn = new NpgsqlConnection(connectionSettings.ConnectionString);

            using (conn)
            {
                try
                {
                    conn.Open();
                    string query1 = "SELECT COUNT(*) FROM \"Uzytkownicy\" WHERE nick = @nick";

                    // Sprawdzamy, czy taki nick już istnieje
                    using (NpgsqlCommand checkCom = new NpgsqlCommand(query1, conn))
                    {
                        checkCom.Parameters.AddWithValue("nick", nickname);
                        int userCount = Convert.ToInt32(checkCom.ExecuteScalar());

                        if (userCount > 0)
                        {
                            ErrorBlock.Text = "Podana nazwa użytkownika już istnieje.";
                            ErrorBlock.Visibility = Visibility.Visible;

                            return;
                        }
                    }

                    string query2 = "INSERT INTO \"Uzytkownicy\" (nick, haslo, zalozenie) VALUES (@nick, @pass, CURRENT_TIMESTAMP)";

                    // Zapisywanie nowego użytkownika
                    using (NpgsqlCommand insertCom = new NpgsqlCommand(query2, conn))
                    {
                        insertCom.Parameters.AddWithValue("nick", nickname);
                        insertCom.Parameters.AddWithValue("pass", hash);

                        insertCom.ExecuteNonQuery();
                        MessageBox.Show("Rejestracja zakończona sukcesem!");

                        // Przejście do ekranu logowania
                        main.ShowLogin();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Błąd połączenia z bazą: " + ex.Message);
                    Application.Current.Shutdown();
                }
            }
        }

        /// <summary>
        /// Obsługa kliknięcia przycisku "Logowanie," który przenosi do ekranu logowania.
        /// </summary>
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            main.ShowLogin();
        }

        /// <summary>
        /// Haszuje hasło użytkownika za pomocą SHA256 i koduje je w Base64.
        /// </summary>
        /// <param name="password">Hasło w postaci tekstowej.</param>
        /// <returns>Hasz hasła w postaci ciągu Base64.</returns>
        public static string HashPassword(string password)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(password);
                var hash = sha.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }
    }
}
