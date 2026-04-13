using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Statki
{
    /// <summary>
    /// Logika ekranu rozgrywki w Statkach.
    /// Odpowiada za strzelanie, koloryzację planszy, komunikację z serwerem i obsługę tur.
    /// </summary>
    public partial class GameScreen : UserControl
    {
        private MainWindow main;
        private ClientConnection connection;

        /// <summary>
        /// Timer tury, wskazujący że gracz ma 45 sekund na ruch.
        /// </summary>
        private DispatcherTimer turnTimer;

        private int timeLeft;

        /// <summary>
        /// Globalny przycisk, przechowujący kliknięty przycisk z pola przeciwnika.
        /// </summary>
        private Button? button;

        /// <summary>
        /// Flaga wskazująca, czy jest obecnie tura gracza.
        /// </summary>
        private bool playerTurn;

        /// <summary>
        /// Liczba niezatopionych statków gracza.
        /// </summary>
        private int[] ShipAmount;

        /// <summary>
        /// Globalne przechowywanie koordynatów najświeżej klikniętego pola.
        /// </summary>
        int currentRow, currentCol;

        /// <summary>
        /// Macierz przechowująca informacje o stanie pól na planszy przeciwnika.
        /// </summary>
        private int[,] opponentBoardState;

        /// <summary>
        /// Macierz przechowująca informacje o stanie pól na planszy gracza.
        /// </summary>
        private int[,] playerBoardState;
        /* 
         * 4 typy pól
         * Tag = 0 -> Puste pole nieodkryte
         * Tag = 1 -> Pole ze statkiem nieodkryte
         * Tag = 2 -> Puste pole odkryte
         * Tag = 3 -> Pole ze statkiem odkryte
        */

        /// <summary>
        /// Konstruktor uruchamiający ekran rozgrywki.
        /// </summary>
        /// <param name="_main">Referencja do głównego okna.</param>
        /// <param name="_playerBoardState">Stan planszy gracza, przekazany z ekranu przygotowania.</param>
        public GameScreen(MainWindow _main, int[,] _playerBoardState)
        {
            InitializeComponent();
            main = _main;

            ShipAmount = [4, 3, 2, 1];
            playerTurn = false;

            // Ustawianie timera
            turnTimer = new DispatcherTimer();
            turnTimer.Interval = TimeSpan.FromSeconds(1);
            turnTimer.Tick += TurnTimer_Tick;
            StartTurnTimer();

            TurnTimer.Text = "00:45";
            TurnInfo.Text = "Tura przeciwnika.";

            playerBoardState = _playerBoardState;
            opponentBoardState = new int[10, 10];

            button = null;

            // Generowanie plansz
            BoardGenerator board_generator = new();

            board_generator.GenerateUnclickableBoard(PlayerGrid, playerBoardState);
            board_generator.GeneratePlayerBoard(OpponentGrid, Board_Click);

            // Pobranie obiektu do połączenia
            connection = ((App)Application.Current).Connection
                ?? throw new InvalidOperationException("Brak aktywnego połączenia z serwerem.");

            // Przekierowanie do metody GameLogic() przy komunikatach
            connection.MessageReceived += msg =>
            {
                Application.Current.Dispatcher.Invoke(() => GameLogic(msg));
            };
        }

        /// <summary>
        /// Główna logika gry reagująca na wiadomości od serwera.
        /// </summary>
        /// <param name="msg">Odebrana wiadomość.</param>
        private void GameLogic(string msg)
        {
            // Komunikat o strzale
            if (msg.Substring(0, 4) == "POZ:")
            {
                int x = int.Parse(msg[4].ToString());
                int y = int.Parse(msg[5].ToString());

                if (playerBoardState[x, y] == 0)
                {
                    playerBoardState[x, y] = 2;
                    ColorField(PlayerGrid, x, y, 0);

                    connection.Send("PUDLO");
                }
                else if (playerBoardState[x, y] == 1)
                {
                    int ship_count;
                    playerBoardState[x, y] = 3;

                    // Sprawdzenie, czy statek jest zatopiony za pomocą DFS
                    if (GameDepthSearch(x, y, out ship_count, false, null, playerBoardState))
                    {
                        ColorField(PlayerGrid, x, y, 1);
                        connection.Send("TRAFIONY");
                    }
                    else
                    {
                        ColorField(PlayerGrid, x, y, 2);

                        // Szukanie pól do pokolorowania na czerwono na planszy gracza, bo statek się zatopił
                        GameDepthSearch(x, y, out ship_count, true, PlayerGrid, playerBoardState);
                        connection.Send("ZATOPIONY");

                        ShipAmount[ship_count - 1]--;

                        Result(false);
                    }
                }

                StopTurnTimer();
                StartTurnTimer();

                TurnInfo.FontSize = 32;
                TurnInfo.Text = "Twoja tura.";
                TurnInfo.Foreground = Brushes.White;
                playerTurn = true;
            }

            // Zwrotna informacja o strzale
            if (msg == "PUDLO" || msg == "TRAFIONY" || msg == "ZATOPIONY")
            {
                // Kolorowanie pola klikniętego, ponieważ gracz posiada do niego referencję w wartości globalnej
                if (button != null)
                {
                    if (msg == "PUDLO")
                    {
                        opponentBoardState[currentRow, currentCol] = 2;
                        button.Background = Brushes.DarkBlue;
                    }

                    if (msg == "TRAFIONY")
                    {
                        opponentBoardState[currentRow, currentCol] = 3;
                        button.Background = Brushes.LightPink;
                    }

                    if (msg == "ZATOPIONY")
                    {
                        int ship_count;
                        button.Background = Brushes.Red;

                        // Kolorujemy cały zatopiony statek na czerwono na planszy przeciwnika
                        GameDepthSearch(currentRow, currentCol, out ship_count, true, OpponentGrid, opponentBoardState);
                    }
                }
            }

            // Informacja, czy koniec gry
            if (msg == "KONIEC")
            {
                Result(true);
            }

            // Jeśli gracz jako pierwszy dołączył się do serwera - on zaczyna
            // Jeśli czas się skończył - tura przeciwnika
            if (msg == "ZACZNIJ" || msg == "_KONIECCZASU")
            {
                TurnInfo.FontSize = 32;
                TurnInfo.Foreground = Brushes.White;
                TurnInfo.Text = "Twoja tura.";

                playerTurn = true;
            }

            // Informacja o rozłączeniu
            if (msg == "ROZLACZENIE")
            {
                Dispatcher.Invoke(() =>
                {
                    connection?.Disconnect();
                    main.OpponentDisconnected();
                });
            }
        }

        /// <summary>
        /// Obsługuje kliknięcie w planszę przeciwnika.
        /// </summary>
        private void Board_Click(object sender, RoutedEventArgs e)
        {
            // Jeśli nie jest tura gracza, cofamy
            if (!playerTurn)
            {
                TurnInfo.Foreground = Brushes.OrangeRed;
                return;
            }

            // Pobranie klikniętego przycisku i jego koordynatów
            button = (Button)sender;
            var p = (Point)button.Tag;
            currentRow = (int)p.X;
            currentCol = (int)p.Y;

            // Jeśli strzelone pole już było sprawdzane
            if (opponentBoardState[currentRow, currentCol] != 0)
            {
                TurnInfo.Text = "Strzel w inne pole.";
                return;
            }

            connection.Send($"POZ:{currentRow}{currentCol}");

            StopTurnTimer();

            TurnInfo.FontSize = 32;
            TurnInfo.Text = "Tura przeciwnika.";
            playerTurn = false;

            StartTurnTimer();
        }

        /// <summary>
        /// Przeszukuje zatopiony statek (DFS).
        /// </summary>
        /// <param name="x">Rząd startowy.</param>
        /// <param name="y">Kolumna startowa.</param>
        /// <param name="shipCount">Zwracana liczba pól statku.</param>
        /// <param name="sinking">Czy aktualnie zatapiamy statek?</param>
        /// <param name="grid">Siatka, dla kolorowania (opcjonalna).</param>
        /// <param name="BoardState">Stan planszy do przeszukiwania.</param>
        /// <returns>True, jeśli znaleziono nieodkryte fragmenty statku.</returns>
        private bool GameDepthSearch(int x, int y, out int shipCount, bool sinking, UniformGrid? grid, int[,] BoardState)
        {
            shipCount = 1;
            bool[,] visited = new bool[10, 10];

            // Stos
            Stack<(int row, int col)> stack = new();
            stack.Push((x, y));

            visited[x, y] = true;

            int[,] directions = new int[4, 2]
            {
                { -1, 0 }, // gora
                { 1, 0 },  // dol
                { 0, -1 }, // lewo
                { 0, 1 }   // prawo
            };

            while (stack.Count > 0)
            {
                var (row, col) = stack.Pop();

                // Sprawdzamy wszystkie cztery strony zanim przejdziemy do nowego pola
                for (int i = 0; i < 4; i++)
                {
                    // Bierzemy obecna pozycje, po czym dodajemy odpowiednio koordynaty
                    int newRow = row + directions[i, 0];
                    int newCol = col + directions[i, 1];

                    if (newRow >= 0 && newRow < 10 && newCol >= 0 && newCol < 10)
                    {
                        // Jeżeli istnieje już trafiony statek, szukamy dalej
                        if (BoardState[newRow, newCol] == 3)
                        {
                            if (!visited[newRow, newCol])
                            {
                                visited[newRow, newCol] = true;
                                stack.Push((newRow, newCol));
                                shipCount++;
                            }

                            if (sinking && grid != null)
                            {
                                ColorField(grid, newRow, newCol, 2);
                            }
                        }

                        // Jeżeli istnieje sąsiadujący statek nieodkryty, zwracamy prawdę
                        if (BoardState[newRow, newCol] == 1)
                        {
                            return true;
                        }
                    }
                }
            }

            // Nieznaleziono nietrafionych sąsiednich części statku
            return false;
        }

        /// <summary>
        /// Koloruje dane pole na planszy.
        /// </summary>
        /// <param name="grid">Plansza (siatka) do kolorowania.</param>
        /// <param name="row">Rząd pola.</param>
        /// <param name="col">Kolumna pola.</param>
        /// <param name="color">Liczba koloru: 0 - pudło, 1 - trafiony, 2 - zatopiony.</param>
        private void ColorField(UniformGrid grid, int row, int col, int color)
        {
            foreach (FrameworkElement element in grid.Children)
            {
                if (element.Tag is Point p && (int)p.X == row && (int)p.Y == col)
                {
                    Brush? brush;

                    switch (color)
                    {
                        case 0:
                            brush = Brushes.DarkBlue;
                            break;

                        case 1:
                            brush = Brushes.LightPink;
                            break;
                        case 2:
                            brush = Brushes.Red;
                            break;

                        default:
                            brush = null;
                            break;
                    }

                    if (brush == null) return;

                    // Rozróżnienie, ponieważ plansza przeciwnika składa się z przycisków, a własna z ramek
                    if (element is Button button)
                    {
                        button.Background = brush;
                    }
                    else if (element is Border border)
                    {
                        border.Background = brush;
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Sprawdza, czy gracz przegrał/zakończył grę i pokazuje wynik.
        /// </summary>
        /// <param name="win">Flaga informująca czy gracz wygrał.</param>
        private void Result(bool win)
        {
            // Jeśli nie ma jeszcze wygranej, sprawdzamy czy jeszcze są jakieś statki pływające
            if (!win)
            {
                bool loss = true;

                for (int i = 0; i < 4; i++)
                {
                    if (ShipAmount[i] != 0)
                    {
                        loss = false;
                        break;
                    }
                }

                // Jeśli przegrana, kończymy grę
                if (loss)
                {
                    connection.Send($"KONIEC{((App)Application.Current).Username}");

                    ResultText.Text = "PRZEGRANA!";

                    End();
                }
            }
            else
            {
                End();
            }
        }

        /// <summary>
        /// Kończy grę — blokuje planszę i pokazuje ekran końcowy.
        /// </summary>
        private void End()
        {
            foreach (Button b in OpponentGrid.Children)
            {
                b.IsHitTestVisible = false;
            }

            // Kasujemy i chowamy timer
            turnTimer.Tick -= TurnTimer_Tick;
            TurnTimer.Visibility = Visibility.Collapsed;

            // Chowamy info o turze
            TurnInfo.Visibility = Visibility.Collapsed;

            // Pokazujemy wynik gry
            ResultText.Visibility = Visibility.Visible;

            // Chowamy legendę plansz
            Legenda.Visibility = Visibility.Hidden;
            Button_legenda.Visibility = Visibility.Hidden;

            // Uruchamiamy przycisk do powrotu do głównego menu
            BackButton.Visibility = Visibility.Visible;
            BackButton.IsEnabled = true;
        }

        /// <summary>
        /// Rozłączenie z serwerem i powrót do menu.
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            connection.Disconnect();
            main.ShowMenu();
        }

        /// <summary>
        /// Uruchamia timer tury, trwający 45 sekund.
        /// </summary>
        private void StartTurnTimer()
        {
            timeLeft = 45;
            TurnTimer.Text = "00:45";

            turnTimer.Start();
        }

        /// <summary>
        /// Zatrzymuje timer tury.
        /// </summary>
        private void StopTurnTimer()
        {
            turnTimer.Stop();
        }

        /// <summary>
        /// Odliczanie sekund do końca tury i reakcja na przekroczenie limitu czasu.
        /// </summary>
        private void TurnTimer_Tick(object? sender, EventArgs e)
        {
            timeLeft--;
            TurnTimer.Text = string.Format("{0:mm\\:ss}", TimeSpan.FromSeconds(timeLeft).Duration());

            // Przekroczenie limitu czasu na ruch
            if (timeLeft <= 0)
            {
                StopTurnTimer();

                if (playerTurn)
                {
                    TurnInfo.FontSize = 24;
                    TurnInfo.Text = "Czas minął. Tura przeciwnika.";
                    playerTurn = false;
                    connection.Send("_KONIECCZASU");
                }

                StartTurnTimer();
            }
        }

        /// <summary>
        /// Przycisk pokazujący lub ukrywający legendę kolorów.
        /// </summary>
        private void Legend_Click(object sender, RoutedEventArgs e)
        {
            if (Legenda.Visibility == Visibility.Collapsed)
            {
                Button_legenda.Content = "Ukryj legendę";
                Legenda.Visibility = Visibility.Visible;
            }
            else
            {
                Button_legenda.Content = "Legenda";
                Legenda.Visibility = Visibility.Collapsed;
            }
        }
    }
}
