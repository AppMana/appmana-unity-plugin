namespace AppMana.ComponentModel
{
    /// <summary>
    /// The interface of the object that will ultimately implement the lobby closing functionality for multiplayer games
    /// </summary>
    internal interface ILobby
    {
        /// <summary>
        /// Closes the lobby from further players.
        /// </summary>
        /// <para>
        /// Once called, all the available player slots will be closed, and no more players will be able to join
        /// this Unity instance.</para>
        void CloseLobby();
    }
}