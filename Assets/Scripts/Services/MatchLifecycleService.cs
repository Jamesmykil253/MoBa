using System;
using UnityEngine;

namespace MOBA.Services
{
    public interface IMatchLifecycleService
    {
        event Action<int> MatchEnded;
        bool IsActive { get; }
        float TimeRemaining { get; }
        void Configure(float durationSeconds, int targetScore);
        bool StartMatch();
        void StopMatch(int winningTeam);
        void Tick(float deltaTime);
    }

    /// <summary>
    /// Coordinates match state transitions and victory checks independently of MonoBehaviours.
    /// </summary>
    public sealed class MatchLifecycleService : IMatchLifecycleService
    {
        private readonly IScoringService scoringService;
        private float matchDuration = 1800f;
        private int scoreToWin = 100;
        private float timeRemaining;
        private bool isActive;

        public MatchLifecycleService(IScoringService scoringService)
        {
            this.scoringService = scoringService ?? throw new ArgumentNullException(nameof(scoringService));
        }

        public event Action<int> MatchEnded;

        public bool IsActive => isActive;
        public float TimeRemaining => timeRemaining;

        public void Configure(float durationSeconds, int targetScore)
        {
            matchDuration = Mathf.Max(1f, durationSeconds);
            scoreToWin = Mathf.Max(1, targetScore);
            if (!isActive)
            {
                timeRemaining = matchDuration;
            }
        }

        public bool StartMatch()
        {
            if (isActive)
            {
                return false;
            }

            scoringService.ResetScores();
            timeRemaining = matchDuration;
            isActive = true;
            return true;
        }

        public void StopMatch(int winningTeam)
        {
            if (!isActive)
            {
                return;
            }

            isActive = false;
            MatchEnded?.Invoke(winningTeam);
        }

        public void Tick(float deltaTime)
        {
            if (!isActive)
            {
                return;
            }

            timeRemaining = Mathf.Max(0f, timeRemaining - Mathf.Max(0f, deltaTime));

            if (timeRemaining <= 0f)
            {
                StopMatch(-1);
                return;
            }

            if (CheckScoreVictory(out var winningTeam))
            {
                StopMatch(winningTeam);
            }
        }

        private bool CheckScoreVictory(out int winningTeam)
        {
            for (int team = 0; team < scoringService.TeamCount; team++)
            {
                if (scoringService.GetScore(team) >= scoreToWin)
                {
                    winningTeam = team;
                    return true;
                }
            }

            winningTeam = -1;
            return false;
        }
    }
}
