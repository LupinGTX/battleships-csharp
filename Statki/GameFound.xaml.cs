using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Printing;
using System.Runtime.InteropServices.Marshalling;
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
using System.Xml.Schema;

namespace Statki
{
    /// <summary>
    /// Klasa odpowiadająca za ekran ustawiania statków oraz gotowości gracza do rozpoczęcia gry.
    /// </summary>
    public partial class GameFound : UserControl
    {
        private MainWindow main;
        private ClientConnection connection;

        /// <summary>
        /// Stan planszy gracza. 0 - puste pole, 1 - statek, 2 - trafiony statek, 3 - zatopiony statek.
        /// </summary>
        private int[,] playerBoardState;

        private bool ready;

        /// <summary>
        /// Liczba zliczonych statków każdego typu.
        /// </summary>
        private int[] counted_ships;

        /// <summary>
        /// Limity dozwolonej liczby statków każdego typu.
        /// 4 jednomasztowce, 3 dwumasztowce, 2 trójmasztowce, 1 czteromasztowiec.
        /// </summary>
        private int[] limits;

        /// <summary>
        /// Konstruktor ekranu z informacją o przeciwniku i generowaniem planszy.
        /// </summary>
        /// <param name="_main">Referencja do okna głównego aplikacji.</param>
        /// <param name="opponent_nick">Nazwa użytkownika przeciwnika, aby wyświetlić ją graczowi.</param>
        public GameFound(MainWindow _main, string opponent_nick)
        {
            InitializeComponent();
            main = _main;

            playerBoardState = new int[10, 10];

            BoardGenerator board_generator = new();
            board_generator.GeneratePlayerBoard(PlayersGrid, Board_Click);

            ready = false;
            limits = [4, 3, 2, 1];
            counted_ships = new int[4];
            /*
             * 4 typy statków
             * 0 - jednomasztowiec
             * 1 - dwumasztowiec
             * 2 - trójmasztowiec
             * 3 - czteromasztowiec
            */

            Opponent.Text = $"Znaleziono grę. Przeciwnik: {opponent_nick}";

            // Pobranie połączenia z obiektu globalnego
            connection = ((App)Application.Current).Connection
                ?? throw new InvalidOperationException("Brak aktywnego połączenia z serwerem.");
            connection.MessageReceived += Message_Received;
        }

        /// <summary>
        /// Obsługuje wiadomości przychodzące od serwera.
        /// </summary>
        /// <param name="msg">Treść przesłanego komunikatu.</param>
        private void Message_Received(string msg)
        {
            if (msg == "ROZLACZENIE")
            {
                Dispatcher.Invoke(() =>
                {
                    connection?.Disconnect();
                    main.OpponentDisconnected();
                });
            }

            if (msg == "START")
            {
                Dispatcher.Invoke(() =>
                {
                    if (connection != null) connection.MessageReceived -= Message_Received;
                    main.ShowGame(playerBoardState);
                });
            }
        }

        /// <summary>
        /// Obsługa kliknięcia w przycisk siatki — dodaje lub usuwa statek.
        /// </summary>
        private void Board_Click(object sender, RoutedEventArgs e)
        {
            // Pozyskanie klikniętego przycisku i jego koordynatów
            Button button = (Button)sender;
            var p = (Point)button.Tag;
            int row = (int)p.X;
            int col = (int)p.Y;

            // false - dodawanie, true - usuwanie statku
            bool deleting;

            if (playerBoardState[row, col] == 0) deleting = false;
            else deleting = true;

            // Sprawdzenie, czy dozwolone jest postawienie statku w danym miejscu
            // (nie więcej niż 4 części są dozwolone)
            if (UpdateShipInfo(row, col, deleting))
            {
                if (playerBoardState[row, col] == 0)
                {
                    playerBoardState[row, col] = 1;
                    button.Background = Brushes.SpringGreen;
                }
                else
                {
                    playerBoardState[row, col] = 0;
                    button.Background = Brushes.LightBlue;
                }
            }

            // Sprawdzenie, czy gracz już może kliknąć przycisk gotowości do gry
            IsReady();
        }

        /// <summary>
        /// Aktualizuje informacje o statkach w zależności od dodawania lub usuwania pola.
        /// </summary>
        /// <param name="row">Numer rzędu klikniętego przycisku.</param>
        /// <param name="col">Numer kolumny klikniętego przycisku.</param>
        /// <param name="deleting">Informacja, czy kliknięto w przycisk, aby usunąć statek czy go dodać.</param>
        /// <returns>True, jeśli można ustawić statek na danym polu.</returns>
        private bool UpdateShipInfo(int row, int col, bool deleting)
        {
            int ship_count; // Ilość części statków obok siebie, które są ustawione i stawiane
            int neighbours; // Ilość statków bezpośrednio sąsiadujących z klikniętym polem

            // Wyszukiwanie w głąb
            PrepDepthSearch(row, col, out ship_count, out neighbours);

            // Dodawanie statków
            if (!deleting)
            {
                // Jeśli byłby 5-masztowiec, nie ma możliwości postawienia go
                if (ship_count > 4)
                {
                    return false;
                }

                // Tymczasowo dodany statek, aby sprawdzić czy powstałby statek kwadratowy 2x2
                playerBoardState[row, col] = 1;

                if (IsSquare(row, col))
                {
                    playerBoardState[row, col] = 0;
                    return false;
                }

                // Usuniecie tymczasowego statku
                playerBoardState[row, col] = 0;

                // Dodanie ustalonego statku do ich liczebności
                counted_ships[ship_count - 1]++;

                // System aktualizowania pozostałych statków do ustawienia
                switch (ship_count)
                {
                    case 1:
                        break;

                    case 2:
                        counted_ships[0]--;
                        break;

                    case 3:
                        if (neighbours == 1)      { counted_ships[1]--; }
                        else if (neighbours == 2) { counted_ships[0] -= 2; }
                        break;

                    case 4:
                        if (neighbours == 1)      { counted_ships[2]--; }
                        else if (neighbours == 2) { counted_ships[0]--; counted_ships[1]--; }
                        else if (neighbours == 3) { counted_ships[0] -= 3; }
                        break;

                    default:
                        break;
                }
            }
            else
            {
                // Usuwanie wskazanego statku
                counted_ships[ship_count - 1]--;

                switch (ship_count)
                {
                    case 1:
                        break;

                    case 2:
                        counted_ships[0]++;
                        break;

                    case 3:
                        if (neighbours == 1)      { counted_ships[1]++; }
                        else if (neighbours == 2) { counted_ships[0] += 2; }
                        break;

                    case 4:
                        if (neighbours == 1)      { counted_ships[2]++; }
                        else if (neighbours == 2) { counted_ships[0]++; counted_ships[1]++; }
                        else if (neighbours == 3) { counted_ships[0] += 3; }
                        break;

                    default:
                        break;
                }
            }

            // Aktualizowanie informacji o statkach na samym ekranie
            UpdatingGUI();

            return true;
        }

        /// <summary>
        /// Aktualizuje wyświetlaną informację o pozostałych statkach i przycisku "Gotowy".
        /// </summary>
        private void UpdatingGUI()
        {
            // Flaga sprawdzająca, czy jest za dużo ustawionych statków (dla wyświetlania komunikatu)
            bool TooManyShips = false;

            // Używanie Inlines, aby różne części tekstu miały oddzielne kolory na podstawie tego, czy przekroczono limit statków
            ShipRemains.Inlines.Clear();

            ShipRemains.Inlines.Add(new Run("Ustaw swoje statki. Pozostały: ")
            {
                Foreground = Brushes.White
            });

            for (int i = 0; i < 4; i++)
            {
                // Sprawdzenie, czy za dużo statków jest postawionych
                if (counted_ships[i] > limits[i])
                {
                    ShipRemains.Inlines.Add(new Run($"{-(limits[i] - counted_ships[i])}x{i + 1}")
                    {
                        Foreground = Brushes.Red
                    });

                    TooManyShips = true;

                    ReadyButton.Visibility = Visibility.Collapsed;
                    TooManyShipsBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    ShipRemains.Inlines.Add(new Run($"{limits[i] - counted_ships[i]}x{i + 1}")
                    {
                        Foreground = Brushes.White
                    });
                }

                if (!TooManyShips)
                {
                    ReadyButton.Visibility = Visibility.Visible;
                    TooManyShipsBlock.Visibility = Visibility.Collapsed;
                }

                // Rozróżnienie 4-masztowca, aby zamiast przecinka była kropka
                if (i == 3)
                {
                    ShipRemains.Inlines.Add(new Run(".")
                    {
                        Foreground = Brushes.White
                    });
                }
                else
                {
                    ShipRemains.Inlines.Add(new Run(", ")
                    {
                        Foreground = Brushes.White
                    });
                }
            }
        }

        /// <summary>
        /// DFS liczący liczbę bezpośrednich sąsiednich części statków pola startowego i rozmiar całego statku klikniętego pola.
        /// </summary>
        /// <param name="startRow">Startowy numer rzędu klikniętego pola.</param>
        /// <param name="startCol">Startowy numer kolumny klikniętego pola.</param>
        /// <param name="shipCount">Obliczana liczba części statków.</param>
        /// <param name="neighbours">Obliczana liczba bezpośrednich sąsiednich części statków.</param>
        private void PrepDepthSearch(int startRow, int startCol, out int shipCount, out int neighbours)
        {
            shipCount = 1;
            neighbours = 0;
            bool[,] visited = new bool[10, 10];

            Stack<(int row, int col)> stack = new();
            stack.Push((startRow, startCol));

            visited[startRow, startCol] = true;

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
                        if (playerBoardState[newRow, newCol] == 1)
                        {
                            if (!visited[newRow, newCol])
                            {
                                visited[newRow, newCol] = true;
                                stack.Push((newRow, newCol));
                                shipCount++;
                            }

                            // Jesli to sasiad faktycznego kliknietego pola, zwiekszamy sasiadow
                            if (row == startRow && col == startCol)
                            {
                                neighbours++;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sprawdza, czy gracz może kliknąć przycisk "Gotowy" oraz anuluje gotowość jeśli edytuje planszę.
        /// </summary>
        private void IsReady()
        {
            bool ready_clickable = true;

            for (int i = 0; i < 4; i++)
            {
                if (counted_ships[i] != limits[i]) ready_clickable = false;
            }

            if (ready_clickable)
            {
                ReadyButton.Opacity = 1;
                ReadyButton.IsEnabled = true;
            }
            else
            {
                ReadyButton.Opacity = 0.7;
                ReadyButton.IsEnabled = false;
            }

            // Jesli gracz jest gotowy i edytuje statki, gotowosc sie anuluje
            if (ready)
            {
                Cancel();
            }
        }

        /// <summary>
        /// Obsługa kliknięcia przycisku gotowości. Wysyła komunikat i aktualizuje UI.
        /// </summary>
        private void Ready_Click(object sender, RoutedEventArgs e)
        {
            ShipRemains.Inlines.Clear();
            ShipRemains.Text = "Oczekiwanie na przeciwnika...";

            ready = true;
            connection.Send("READY");

            // Aktualizowanie metody, której używa przycisk przy kliknięciu (gotowość lub anulowanie)
            ReadyButton.Click -= Ready_Click;
            ReadyButton.Click += Cancel_Click;
            ReadyButton.Content = "Anuluj";
            ReadyButton.Background = Brushes.DarkGray;
        }

        /// <summary>
        /// Obsługa kliknięcia przycisku anulowania gotowości.
        /// </summary>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ShipRemains.Text = "Ustaw swoje statki. Pozostały: 0x1, 0x2, 0x3, 0x4.";

            Cancel();
        }

        /// <summary>
        /// Wewnętrzna logika anulowania gotowości — przywraca interfejs.
        /// </summary>
        private void Cancel()
        {
            ready = false;
            connection.Send($"NOTREADY");

            ReadyButton.Click -= Cancel_Click;
            ReadyButton.Click += Ready_Click;
            ReadyButton.Content = "Gotowy";
            ReadyButton.Background = Brushes.Goldenrod;
        }

        /// <summary>
        /// Sprawdza, czy pole należy do kwadratu 2x2, czyli nieprawidłowej formacji czteromasztowca.
        /// </summary>
        /// <param name="row">Numer rzędu sprawdzanego pola.</param>
        /// <param name="col">Numer kolumny sprawdzanego pola.</param>
        /// <returns>True, jeśli statek byłby kwadratem 2x2.</returns>
        private bool IsSquare(int row, int col)
        {
            // 4 możliwe pozycje początkowe kwadratu 2x2
            int[,] offsets = new int[4, 2] {
                { 0, 0 },
                { -1, 0 },
                { 0, -1 },
                { -1, -1 }
            };

            for (int i = 0; i < 4; i++)
            {
                // Ponieważ startowy blok może być częścią kwadratu w 4 miejscach, te też odwiedzamy
                int startRow = row + offsets[i, 0];
                int startCol = col + offsets[i, 1];

                if (startRow >= 0 && startRow < 9 && startCol >= 0 && startCol < 9)
                {
                    if (playerBoardState[startRow, startCol] == 1 &&
                        playerBoardState[startRow + 1, startCol] == 1 &&
                        playerBoardState[startRow, startCol + 1] == 1 &&
                        playerBoardState[startRow + 1, startCol + 1] == 1)
                    {
                        return true; // Znaleziono kwadrat 2x2, do którego należy to pole
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Destruktor — odsubskrybowuje nasłuchiwanie, jeśli kontrolka jest usuwana.
        /// </summary>
        ~GameFound()
        {
            if (connection != null)
            {
                connection.MessageReceived -= Message_Received;
            }
        }
    }
}
