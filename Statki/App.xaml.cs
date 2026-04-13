using System.Configuration;
using System.Data;
using System.Windows;

namespace Statki
{
    /// <summary>
    /// Klasa główna aplikacji WPF. Przechowuje globalne dane aplikacji, jak nazwa użytkownika i połączenie z serwerem.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Nazwa użytkownika aktualnie zalogowanego w aplikacji. Domyślnie ustawiona jako "Gosc".
        /// </summary>
        public string Username { get; set; } = "Gosc";

        /// <summary>
        /// Obiekt zarządzający połączeniem z serwerem gry.
        /// </summary>
        public ClientConnection? Connection { get; set; }
    }
}
