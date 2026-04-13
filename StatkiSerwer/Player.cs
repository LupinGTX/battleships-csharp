using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace StatkiSerwer
{
    /// <summary>
    /// Reprezentuje gracza połączonego z serwerem.
    /// </summary>
    public class Player
    {
        /// <summary>
        /// Połączenie TCP z klientem reprezentującym tego gracza.
        /// </summary>
        public TcpClient Client { get; }

        /// <summary>
        /// Nazwa gracza przesłana przez klienta.
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// Przeciwnik gracza.
        /// </summary>
        public Player? Opponent { get; set; }

        /// <summary>
        /// Czy gracz jest gotowy do rozpoczęcia gry.
        /// </summary>
        public bool IsReady { get; set; }

        /// <summary>
        /// Czy gracz rozpoczyna grę jako pierwszy.
        /// </summary>
        public bool First { get; set; }

        /// <summary>
        /// Inicjalizuje nową instancję klasy <see cref="Player"/> z podanym połączeniem i nickiem.
        /// </summary>
        /// <param name="client">Obiekt TcpClient reprezentujący połączenie z graczem.</param>
        /// <param name="nickname">Nick gracza przesłany przez klienta.</param>
        public Player(TcpClient client, string nickname)
        {
            Client = client;
            Nickname = nickname;
            Opponent = null;
            IsReady = false;
            First = false;
        }
    }
}
