using System;

namespace MOBA.Services
{
    public interface IScoringService
    {
        event Action<int, int> ScoreChanged;
        int TeamCount { get; }
        void ResetScores(bool notify = true);
        bool AddScore(int team, int points, bool notify = true);
        void SetScore(int team, int value, bool notify = true);
        int GetScore(int team);
    }

    public sealed class ScoringService : IScoringService
    {
        private readonly int[] teamScores;

        public ScoringService(int teamCount)
        {
            if (teamCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(teamCount));
            }

            teamScores = new int[teamCount];
        }

        public ScoringService(int[] backingArray)
        {
            teamScores = backingArray ?? throw new ArgumentNullException(nameof(backingArray));
        }

        public event Action<int, int> ScoreChanged;

        public int TeamCount => teamScores.Length;

        public void ResetScores(bool notify = true)
        {
            for (int i = 0; i < teamScores.Length; i++)
            {
                SetScore(i, 0, notify);
            }
        }

        public bool AddScore(int team, int points, bool notify = true)
        {
            if (!IsValidTeam(team))
            {
                return false;
            }

            teamScores[team] += points;
            if (notify)
            {
                ScoreChanged?.Invoke(team, teamScores[team]);
            }

            return true;
        }

        public void SetScore(int team, int value, bool notify = true)
        {
            if (!IsValidTeam(team))
            {
                return;
            }

            teamScores[team] = value;
            if (notify)
            {
                ScoreChanged?.Invoke(team, value);
            }
        }

        public int GetScore(int team)
        {
            if (!IsValidTeam(team))
            {
                return 0;
            }

            return teamScores[team];
        }

        private bool IsValidTeam(int team)
        {
            return team >= 0 && team < teamScores.Length;
        }
    }
}
