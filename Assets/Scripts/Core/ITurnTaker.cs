using System.Threading.Tasks; // Required for Task

namespace MyGame.Core
{
    /// <summary>
    /// Interface for any entity that can take a turn in a turn-based system.
    /// </summary>
    public interface ITurnTaker
    {
        /// <summary>
        /// Initiates the entity's turn. Returns a Task that completes when the turn is over.
        /// </summary>
        Task TakeTurn();
    }
}
