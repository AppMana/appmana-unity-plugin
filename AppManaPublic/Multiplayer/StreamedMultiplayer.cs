using System;

namespace AppMana.Multiplayer
{
    /// <summary>
    /// Compatibility API methods for controlling the multiplayer experience in a stream.
    /// </summary>
    [Obsolete]
    public class StreamedMultiplayer
    {
        private static StreamedMultiplayer m_Instance;

        public static StreamedMultiplayer instance => m_Instance ??= new StreamedMultiplayer();


        /// <summary>
        /// Closes the lobby. No more players can join the game.
        /// </summary>
        public void CloseLobby()
        {
            StreamedInputs.instance.CloseLobby();
        }
    }
}