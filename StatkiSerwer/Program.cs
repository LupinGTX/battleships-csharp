using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Configuration;

namespace StatkiSerwer
{
    /// <summary>
    /// Główna klasa serwera obsługująca połączenia TCP oraz logikę gry.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Gracz oczekujący na przeciwnika.
        /// </summary>
        static Player? waitingPlayer = null;

        /// <summary>
        /// Obiekt do synchronizacji dostępu do współdzielonych zasobów.
        /// </summary>
        static object locker = new();

        /// <summary>
        /// Określa, czy gra została zakończona.
        /// </summary>
        static bool gameOver = false;

        /// <summary>
        /// Określa, czy serwer jest dostępny do przyjmowania nowych graczy.
        /// </summary>
        static bool ServerFree = true;

        /// <summary>
        /// Liczba aktualnie połączonych graczy.
        /// </summary>
        static int connectedPlayers = 0;

        /// <summary>
        /// Metoda główna, uruchamia serwer TCP.
        /// </summary>
        static void Main()
        {
            // Pobranie portu z App.config
            string? string_port = ConfigurationManager.AppSettings["ServerPort"];
            if (string_port == null)
            {
                WriteInConsole("Błąd: Port nie jest ustawiony. (App.config)");
                return;
            }
            int port = int.Parse(string_port);

            // Utworzenie obiektu nasłuchującego (listener)
            // na dowolnym interfejsie IP (IPAddress.Any) i podanym porcie
            TcpListener listener = new TcpListener(IPAddress.Any, port);

            try
            {
                // Uruchomienie nasłuchiwania
                listener.Start();
                WriteInConsole($"Serwer nasłuchuje na porcie {port}...");

                // Pętla oczekująca na połączenia od klientów
                while (true)
                {
                    TcpClient client = listener.AcceptTcpClient();

                    // Utworzenie nowego wątku do obsługi klienta
                    new Thread(() => HandleClient(client)).Start();
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                WriteInConsole($"Port {port} jest już zajęty. Inny serwer może już działać.");
            }
            catch (Exception ex)
            {
                WriteInConsole($"Nieoczekiwany błąd: {ex.Message}");
            }
        }

        /// <summary>
        /// Wypisuje wiadomość na konsoli serwera z aktualnym czasem.
        /// </summary>
        /// <param name="msg">Wiadomość do wyświetlenia.</param>
        static void WriteInConsole(string msg)
        {
            Console.WriteLine("[Serwer {0}]: {1}", DateTime.Now.ToString("G"), msg);
        }

        /// <summary>
        /// Obsługuje pojedyncze połączenie klienta - logika kojarzenia, gra i komunikacja.
        /// </summary>
        /// <param name="client">Połączenie TCP z klientem.</param>
        static void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            Player? player = null; // Obiekt gracza
            Player opponent;       // Pomocniczy obiekt, aby przypisać przeciwnika gracza

            try
            {
                // Bufor do odebrania pierwszej wiadomości (nicku gracza)
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    client.Close();
                    return;
                }

                // Sekcja krytyczna, tylko jeden wątek może tu wejść naraz
                lock (locker)
                {
                    string nickMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    player = new Player(client, nickMessage);

                    // Zwiększanie licznika połączonych graczy
                    connectedPlayers++;

                    // Sprawdzenie, czy serwer jest pełny
                    // oraz zabezpieczenie aby ten sam gracz nie mógł grać przeciwko sobie
                    if (!ServerFree               ||
                        (player.Nickname != "Gosc" &&
                        waitingPlayer != null      &&
                        player.Nickname == waitingPlayer.Nickname))
                    {
                        Send(client, "ZAJETE");
                        Thread.Sleep(500);
                        client.Close();
                        return;
                    }

                    WriteInConsole($"{player.Nickname} połączył się.");

                    if (waitingPlayer == null)
                    {
                        // Nie ma czekającego gracza, czyli obecny zostaje zapisany jako pierwszy
                        player.First = true;
                        waitingPlayer = player;
                    }
                    else
                    {
                        // Jest już ktoś w kolejce
                        opponent = waitingPlayer;
                        waitingPlayer = null;

                        // Serwer zajęty, gra trwa
                        ServerFree = false;
                        gameOver = false;

                        // Wysyłamy do obu graczy informację o znalezieniu przeciwnika
                        Send(player.Client, "ZNALEZIONO:" + opponent.Nickname);
                        Send(opponent.Client, "ZNALEZIONO:" + player.Nickname);

                        // Ustawiamy sobie wzajemnie przeciwników
                        player.Opponent = opponent;
                        opponent.Opponent = player;
                    }
                }

                // Pętla nasłuchująca wiadomości od gracza przez cały czas trwania połączenia
                while (true)
                {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    if (message == "ROZLACZENIE") break;

                    // Wysyłanie ruchów i odpowiedzi przeciwnikowi w grze
                    else if (message.StartsWith("POZ:") || 
                        message == "PUDLO" || 
                        message == "TRAFIONY" || 
                        message == "ZATOPIONY" || 
                        message == "_KONIECCZASU")
                    {
                        lock (locker)
                        {
                            if (player.Opponent != null)
                            {
                                // Przesyłamy ruch przeciwnikowi
                                Send(player.Opponent.Client, message);
                            }
                        }
                    }

                    // Obsługa gotowości do gry
                    else if (message == "READY")
                    {
                        lock (locker)
                        {
                            player.IsReady = true;
                            WriteInConsole($"{player.Nickname} jest gotowy.");

                            // Jeśli obaj gracze gotowi, start gry
                            if (player.Opponent != null && player.Opponent.IsReady)
                            {
                                Send(player.Client, "START");
                                Send(player.Opponent.Client, "START");
                                WriteInConsole("Obaj gracze są gotowi. START.");

                                // Zaczyna gracz, który pierwszy doszedł na serwer
                                if (player.First)
                                {
                                    WriteInConsole($"{player.Nickname} zaczyna.");
                                    Send(player.Client, "ZACZNIJ");
                                }
                                else
                                {
                                    WriteInConsole($"{player.Opponent.Nickname} zaczyna.");
                                    Send(player.Opponent.Client, "ZACZNIJ");
                                }
                            }
                        }
                    }

                    // Anulowanie gotowości do gry
                    else if (message == "NOTREADY")
                    {
                        lock (locker)
                        {
                            player.IsReady = false;
                            WriteInConsole($"{player.Nickname} anulowal gotowosc.");
                        }
                    }

                    // Komunikat o zakończeniu gry
                    else if (message.StartsWith("KONIEC"))
                    {
                        lock (locker)
                        {
                            gameOver = true;

                            // Wyciągamy nick przegranego
                            string przegrany = message.Substring(6);
                            string zwyciezca;

                            if (player.Opponent != null)
                            {
                                // Ustalanie zwycięzcy
                                if (player.Nickname == przegrany)
                                {
                                    zwyciezca = player.Opponent.Nickname;
                                }
                                else
                                {
                                    zwyciezca = player.Nickname;
                                }
                                WriteInConsole($"Koniec gry. {zwyciezca} wygrał!");

                                // Powiadomienie drugiego gracza o końcu
                                Send(player.Opponent.Client, "KONIEC");

                                // Zapis wyniku gry do bazy danych
                                if (player.First)
                                {
                                    GameSave.Save(player.Nickname, player.Opponent.Nickname, zwyciezca, przegrany);
                                }
                                else // Aktualizacja statystyk gracza w bazie danych
                                {
                                    GameSave.Save(player.Opponent.Nickname, player.Nickname, zwyciezca, przegrany);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) // Łapanie wyjątków
            {
                WriteInConsole(e.Message);
            }
            finally
            {
                lock (locker)
                {
                    // Rozłączanie gracza
                    if (player != null)
                    {
                        WriteInConsole($"{player.Nickname} rozłączony.");
                        connectedPlayers--;

                        // Wyczyszczenie gracza czekającego, jeśli nim był
                        if (waitingPlayer == player) waitingPlayer = null;

                        // Wysłanie komunikatu o rozłączeniu gracza, jeśli gra się nie skończyła
                        if (player.Opponent != null && !gameOver)
                        {
                            try { Send(player.Opponent.Client, "ROZLACZENIE"); } catch { }
                        }

                        // "Znullowanie" przeciwnika
                        if (player.Opponent != null)
                        {
                            player.Opponent = null;
                        }
                    }

                    // Otwarcie serwera po grze
                    if (connectedPlayers == 0)
                    {
                        ServerFree = true;
                    }

                    try { client.Close(); } catch { }
                }
            }
        }

        /// <summary>
        /// Wysyła wiadomość do klienta za pomocą strumienia TCP.
        /// </summary>
        /// <param name="client">Obiekt klienta TCP.</param>
        /// <param name="message">Wiadomość do wysłania.</param>
        static void Send(TcpClient client, string message)
        {
            try
            {
                if (client?.Connected != true) return;

                NetworkStream stream = client.GetStream();
                if (!stream.CanWrite) return;

                // Konwersja wiadomości do tablicy bajtów w kodowaniu UTF-8 i dodanie znaku nowej linii
                byte[] bytes = Encoding.UTF8.GetBytes(message + "\n");
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd przy wysyłaniu do klienta: {ex.Message}");
            }
        }
    }
}
