using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace StatkiSerwer
{
    /// <summary>
    /// Klasa statyczna do zapisywania wyników gry oraz aktualizacji statystyk graczy w bazie danych.
    /// </summary>
    public static class GameSave
    {
        /// <summary>
        /// Ustawienia połączenia do bazy danych odczytane z pliku App.config.
        /// </summary>
        private static ConnectionStringSettings connectionSettings =
                ConfigurationManager.ConnectionStrings["ConnectionString"];

        /// <summary>
        /// Połączenie do bazy danych PostgreSQL.
        /// </summary>
        private static NpgsqlConnection conn = 
            new NpgsqlConnection(connectionSettings.ConnectionString);

        /// <summary>
        /// Zapisuje wynik gry i aktualizuje statystyki zwycięzcy i przegranego w bazie danych.
        /// </summary>
        /// <param name="player1">Nick pierwszego gracza.</param>
        /// <param name="player2">Nick drugiego gracza.</param>
        /// <param name="zwyciezca">Nick zwycięzcy.</param>
        /// <param name="przegrany">Nick przegranego.</param>
        public static void Save(string player1, string player2, string zwyciezca, string przegrany)
        {
            // Otwarcie połączenia z bazą danych
            if (conn.ConnectionString != "")
            {
                conn.Open();

                GameSaving(player1, player2, zwyciezca);

                PlayerStatisticsSave(zwyciezca, "UPDATE \"Uzytkownicy\" SET rozegrane_gry=rozegrane_gry+1 WHERE nick=@nick;");
                PlayerStatisticsSave(przegrany, "UPDATE \"Uzytkownicy\" SET rozegrane_gry=rozegrane_gry+1 WHERE nick=@nick;");
                PlayerStatisticsSave(zwyciezca, "UPDATE \"Uzytkownicy\" SET zwyciestwa=zwyciestwa+1 WHERE nick=@nick;");
                PlayerStatisticsSave(przegrany, "UPDATE \"Uzytkownicy\" SET porazki=porazki+1 WHERE nick=@nick;");

                // Zamknięcie połączenia z bazą danych
                conn.Close();
            }
        }

        /// <summary>
        /// Wstawia rekord nowej gry do tabeli "Gry".
        /// </summary>
        /// <param name="gracz1">Nick pierwszego gracza.</param>
        /// <param name="gracz2">Nick drugiego gracza.</param>
        /// <param name="zwyciezca">Nick zwycięzcy.</param>
        private static void GameSaving(string gracz1, string gracz2, string zwyciezca)
        {
            // Zapytanie SQL wstawiające nową grę z aktualnym znacznikiem czasu
            string query = "INSERT INTO \"Gry\" (gracz1, gracz2, zwyciezca, data) VALUES (@g1, @g2, @z, CURRENT_TIMESTAMP);";

            NpgsqlCommand com = new NpgsqlCommand(query, conn);
            using (com)
            {
                // Parametryzacja zapytania, która zapobiega przed SQL injection
                com.Parameters.AddWithValue("g1", gracz1);
                com.Parameters.AddWithValue("g2", gracz2);
                com.Parameters.AddWithValue("z", zwyciezca);

                // Wykonanie zapytania
                com.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Aktualizuje statystyki pojedynczego gracza w tabeli "Uzytkownicy".
        /// </summary>
        /// <param name="nickname">Nick gracza, którego statystyki mają być zaktualizowane.</param>
        /// <param name="query">Zapytanie SQL aktualizujące statystyki.</param>
        private static void PlayerStatisticsSave(string nickname, string query)
        {
            NpgsqlCommand com = new NpgsqlCommand(query, conn);
            using (com)
            {
                com.Parameters.AddWithValue("nick", nickname);

                com.ExecuteNonQuery();
            }
        }
    }
}
