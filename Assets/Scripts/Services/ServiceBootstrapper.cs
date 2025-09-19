using UnityEngine;

namespace MOBA.Services
{
    /// <summary>
    /// Registers default gameplay services at scene bootstrap.
    /// </summary>
    public class ServiceBootstrapper : MonoBehaviour
    {
        [Header("Scoring")]
        [SerializeField] private bool registerScoringService = true;
        [SerializeField, Min(1)] private int defaultTeamCount = 2;

        [Header("Match Lifecycle")]
        [SerializeField] private bool registerMatchService = true;
        [SerializeField] private float defaultMatchDuration = 1800f;
        [SerializeField] private int defaultScoreToWin = 100;

        private void Awake()
        {
            if (registerScoringService && !ServiceRegistry.TryResolve<IScoringService>(out _))
            {
                var scoring = new ScoringService(defaultTeamCount);
                ServiceRegistry.Register<IScoringService>(scoring);
            }

            if (registerMatchService && !ServiceRegistry.TryResolve<IMatchLifecycleService>(out _))
            {
                if (!ServiceRegistry.TryResolve<IScoringService>(out var scoring))
                {
                    scoring = new ScoringService(defaultTeamCount);
                    ServiceRegistry.Register<IScoringService>(scoring);
                }

                var match = new MatchLifecycleService(scoring);
                match.Configure(defaultMatchDuration, defaultScoreToWin);
                ServiceRegistry.Register<IMatchLifecycleService>(match);
            }
        }
    }
}
