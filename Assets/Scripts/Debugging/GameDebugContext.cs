using System;

namespace MOBA.Debugging
{
    /// <summary>
    /// Represents the debug context for a log entry, allowing filtering by system and mechanic.
    /// </summary>
    public readonly struct GameDebugContext
    {
        public GameDebugCategory Category { get; }
        public GameDebugSystemTag System { get; }
        public GameDebugMechanicTag Mechanic { get; }
        public string Subsystem { get; }
        public string Actor { get; }

        public GameDebugContext(
            GameDebugCategory category = GameDebugCategory.General,
            GameDebugSystemTag system = GameDebugSystemTag.Core,
            GameDebugMechanicTag mechanic = GameDebugMechanicTag.General,
            string subsystem = null,
            string actor = null)
        {
            Category = category;
            System = system;
            Mechanic = mechanic;
            Subsystem = subsystem;
            Actor = actor;
        }

        public GameDebugContext WithSubsystem(string subsystem)
        {
            return new GameDebugContext(Category, System, Mechanic, subsystem, Actor);
        }

        public GameDebugContext WithActor(string actor)
        {
            return new GameDebugContext(Category, System, Mechanic, Subsystem, actor);
        }

        public override string ToString()
        {
            return $"Category={Category}, System={System}, Mechanic={Mechanic}, Subsystem={Subsystem}, Actor={Actor}";
        }
    }
}
