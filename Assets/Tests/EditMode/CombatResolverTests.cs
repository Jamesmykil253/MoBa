using MOBA.Services;
using NUnit.Framework;

namespace MOBA.Tests.EditMode
{
    public class CombatResolverTests
    {
        private RiskSkillCombatResolver resolver;
        private CharacterStats attacker;
        private CharacterStats defender;

        [SetUp]
        public void SetUp()
        {
            resolver = new RiskSkillCombatResolver();
            attacker = CharacterStats.DefaultStats;
            attacker.Attack = 150f;
            attacker.CritChanceBonus = 0.5f;
            attacker.CritDamageBonus = 0.75f;
            defender = CharacterStats.DefaultStats;
            defender.PhysicalDefense = 50f;
            defender.PhysicalResistance = 0.2f;
        }

        [Test]
        public void ResolveCombat_ProducesDeterministicDamage()
        {
            var request = new CombatRequest(attacker, defender, 100f, DamageType.Physical, true, 0.6f, 1.5f);
            var result = resolver.ResolveCombat(request);

            Assert.Greater(result.FinalDamage, 0f);
            var second = resolver.ResolveCombat(request);
            Assert.AreEqual(result.FinalDamage, second.FinalDamage, 0.0001f);
        }

        [Test]
        public void ResolveCombat_RespectsResistance()
        {
            var highResDefender = defender;
            highResDefender.PhysicalResistance = 0.9f;
            var requestLow = new CombatRequest(attacker, defender, 80f, DamageType.Physical, false, 0f, 0f);
            var requestHigh = new CombatRequest(attacker, highResDefender, 80f, DamageType.Physical, false, 0f, 0f);

            var baseResult = resolver.ResolveCombat(requestLow);
            var resistantResult = resolver.ResolveCombat(requestHigh);

            Assert.Less(resistantResult.FinalDamage, baseResult.FinalDamage);
        }
    }
}
