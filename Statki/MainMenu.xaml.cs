using System;
using System.Collections.Generic;
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

namespace Statki
{
    /// <summary>
    /// Klasa odpowiedzialna za główne menu aplikacji po zalogowaniu.
    /// </summary>
    public partial class MainMenu : UserControl
    {
        private MainWindow main;

        /// <summary>
        /// Konstruktor głównego menu. Inicjalizuje komponenty i ustawia referencję do MainWindow.
        /// </summary>
        /// <param name="_main">Referencja do głównego okna aplikacji.</param>
        public MainMenu(MainWindow _main)
        {
            InitializeComponent();
            main = _main;
        }

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Znajdź grę".
        /// Przełącza widok do ekranu wyszukiwania przeciwnika.
        /// </summary>
        private void FindGame_Click(object sender, RoutedEventArgs e)
        {
            main.ShowSearch();
        }

        /*
        /// <summary>
        /// Obsługuje kliknięcie przycisku "Wyniki".
        /// Przełącza widok do ekranu z wynikami graczy.
        /// (Obecnie wyłączone)
        /// </summary>
        private void Scores_Click(object sender, RoutedEventArgs e)
        {
            main.ShowScores();
        }
        */

        /// <summary>
        /// Obsługuje kliknięcie przycisku "Wyjdź".
        /// Zamyka aplikację.
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}

