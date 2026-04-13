using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Statki
{
    /// <summary>
    /// Klasa odpowiedzialna za generowanie plansz gracza i przeciwnika.
    /// Wykorzystywana do tworzenia siatek przy rozmieszczaniu statków, podglądzie i grze.
    /// </summary>
    internal class BoardGenerator
    {
        /// <summary>
        /// Generuje planszę gracza w postaci siatki 10x10 z przyciskami.
        /// Każdy przycisk reprezentuje jedno pole i ma przypisany identyfikator w postaci Point(row, col).
        /// </summary>
        /// <param name="Grid">Kontener typu UniformGrid, do którego dodawane są przyciski.</param>
        /// <param name="Click">Delegat obsługujący kliknięcie w pole.</param>
        public void GeneratePlayerBoard(UniformGrid Grid, RoutedEventHandler Click)
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    Button button = new()
                    {
                        Background = Brushes.LightBlue, // Domyślny kolor tła planszy
                        Tag = new Point(row, col)       // Zapisywanie pozycji pola
                    };

                    // Dodanie obsługi kliknięcia do przycisku
                    button.Click += Click;

                    // Dodanie przycisku do siatki
                    Grid.Children.Add(button);
                }
            }
        }

        /// <summary>
        /// Generuje planszę gracza jako nieklikalną siatkę 10x10.
        /// Każde pole ma kolor zależny od wartości w macierzy playerBoardState.
        /// </summary>
        /// <param name="Grid">Kontener typu UniformGrid, do którego dodawane są pola jako ramki.</param>
        /// <param name="playerBoardState">Dwuwymiarowa tablica reprezentująca stan planszy: 0 - puste, 1 - statek, 2 - trafiony statek, 3 - zatopiony statek.</param>
        public void GenerateUnclickableBoard(UniformGrid Grid, int[,] playerBoardState)
        {
            for (int row = 0; row < 10; row++)
            {
                for (int col = 0; col < 10; col++)
                {
                    Border border = new()
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = Brushes.Gray,
                        Margin = new Thickness(0),
                        Tag = new Point(row, col)
                    };

                    // Ustawianie koloru tła w zależności od stanu planszy
                    if (playerBoardState[row, col] == 0)
                    {
                        border.Background = Brushes.LightBlue; // Pole puste
                    }
                    else if (playerBoardState[row, col] == 1)
                    {
                        border.Background = Brushes.SpringGreen; // Pole ze statkiem
                    }

                    // Dodanie ramki do siatki
                    Grid.Children.Add(border);
                }
            }
        }
    }
}
