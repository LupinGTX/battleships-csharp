using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;

namespace Statki
{
    /// <summary>
    /// Reprezentuje połączenie klienta z serwerem w grze.
    /// </summary>
    public class ClientConnection
    {
        private bool isDisconnecting = false;

        /// <summary>
        /// Obiekt TCP reprezentujący połączenie klienta z serwerem.
        /// </summary>
        private TcpClient? client;

        /// <summary>
        /// Strumień danych służący do komunikacji z serwerem.
        /// </summary>
        private NetworkStream? stream;

        /// <summary>
        /// Zdarzenie wywoływane, gdy klient odbierze wiadomość tekstową od serwera.
        /// </summary>
        public event Action<string>? MessageReceived;

        /// <summary>
        /// Zdarzenie wywoływane, gdy połączenie z serwerem zostanie nieoczekiwanie przerwane.
        /// </summary>
        public event Action? ServerDisconnected;

        /// <summary>
        /// Nawiązuje asynchroniczne połączenie z serwerem i wysyła nick użytkownika.
        /// </summary>
        /// <param name="ip">Adres IP serwera.</param>
        /// <param name="port">Port serwera.</param>
        /// <param name="nickname">Nick gracza do wysłania przy połączeniu.</param>
        public async Task ConnectAsync(string ip, int port, string nickname)
        {
            // Asynchroniczne połączenie z serwerem
            client = new();
            await client.ConnectAsync(ip, port);
            stream = client.GetStream();

            // Wysyłanie nick'u
            Send(nickname);

            _ = Task.Run(ReceiveLoop);
        }

        /// <summary>
        /// Wysyła wiadomość do serwera.
        /// </summary>
        /// <param name="message">Wiadomość do wysłania.</param>
        public void Send(string message)
        {
            if (stream == null) return;

            // Zamienia string na tablicę bajtów, żeby można ją było przesłać
            byte[] data = Encoding.UTF8.GetBytes(message + "\n");
            stream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Asynchroniczna pętla odbierająca dane od serwera.
        /// </summary>
        private async Task ReceiveLoop()
        {
            byte[] buffer = new byte[1024];
            StringBuilder sb = new();

            try
            {
                // Pętla nasłuchująca
                while (true)
                {
                    if (stream == null) break;
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        if (!isDisconnecting)
                        {
                            ServerDisconnected?.Invoke();
                        }

                        break;
                    }

                    // Danie wiadomości do stringa z bufora
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    MessageReceived?.Invoke(message);
                }
            }
            catch
            {
                if (!isDisconnecting)
                {
                    ServerDisconnected?.Invoke();
                }
            }
        }

        /// <summary>
        /// Rozłącza klienta z serwerem i zamyka strumienie.
        /// </summary>
        public void Disconnect()
        {
            isDisconnecting = true;

            try
            {
                Send("ROZLACZENIE");
                if (stream != null) stream.Close();
                if (client != null) client.Close();
            }
            catch { }
        }
    }
}
