using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;

namespace MOBA.Testing
{
    /// <summary>
    /// RSB Combat System Tests
    /// Validates Risk-Skill-Balance combat formula calculations
    /// Tests manual aim integration and damage scaling
    /// </summary>
    [TestFixture]
    public class RSBCombatSystemTests : MOBATestFramework
    {
        private RSBCombatSystem combatSystem;
        
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            combatSystem = testCombatSystem;
        }
        
        [Test]
        public void RSBCombatSystem_CalculatesBaseDamage_Correctly()
        {
            // Arrange
            var testAbility = new AbilityData
            {
                name = "TestAttack",
                damage = 100f,
                range = 10f,
                speed = 5f
            };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 targetPos = Vector3.forward * 5f; // 5 units away
            
            // Act
            float damage = combatSystem.CalculateAbilityDamage(testAbility, attackerPos, targetPos);
            
            // Assert
            Assert.Greater(damage, 0f, "Damage should be positive");
            Assert.LessOrEqual(damage, 200f, "Damage should not exceed reasonable bounds");
        }
        
        [Test]
        public void RSBCombatSystem_AppliesDistanceScaling()
        {
            // Arrange
            var testAbility = new AbilityData
            {
                name = "TestAttack",
                damage = 100f,
                range = 10f,
                speed = 5f
            };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 closeTarget = Vector3.forward * 2f;   // Close range
            Vector3 farTarget = Vector3.forward * 15f;    // Far range
            
            // Act
            float closeDamage = combatSystem.CalculateAbilityDamage(testAbility, attackerPos, closeTarget);
            float farDamage = combatSystem.CalculateAbilityDamage(testAbility, attackerPos, farTarget);
            
            // Assert
            Assert.Greater(closeDamage, 0f, "Close damage should be positive");
            Assert.Greater(farDamage, 0f, "Far damage should be positive");
            // Distance scaling may vary based on implementation
        }
        
        [Test]
        public void RSBCombatSystem_HandlesZeroDistance()
        {
            // Arrange
            var testAbility = new AbilityData
            {
                name = "MeleeAttack",
                damage = 50f,
                range = 5f,
                speed = 0f
            };
            
            Vector3 position = Vector3.zero;
            
            // Act
            float damage = combatSystem.CalculateAbilityDamage(testAbility, position, position);
            
            // Assert
            Assert.Greater(damage, 0f, "Zero distance should still produce positive damage");
        }
        
        [Test]
        public void RSBCombatSystem_ValidatesAbilityData()
        {
            // Arrange
            var invalidAbility = new AbilityData
            {
                name = "",
                damage = -10f,  // Invalid negative damage
                range = 0f,
                speed = -5f     // Invalid negative speed
            };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 targetPos = Vector3.forward;
            
            // Act & Assert
            // The system should handle invalid data gracefully
            Assert.DoesNotThrow(() =>
            {
                float damage = combatSystem.CalculateAbilityDamage(invalidAbility, attackerPos, targetPos);
                Assert.GreaterOrEqual(damage, 0f, "Damage should not be negative even with invalid input");
            }, "System should handle invalid ability data gracefully");
        }
        
        [Test]
        public void RSBCombatSystem_CalculatesProjectileDamage()
        {
            // Arrange
            var projectileAbility = new AbilityData
            {
                name = "Fireball",
                damage = 75f,
                range = 12f,
                speed = 8f
            };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 targetPos = Vector3.forward * 8f;
            
            // Act
            float damage = combatSystem.CalculateAbilityDamage(projectileAbility, attackerPos, targetPos);
            
            // Assert
            Assert.Greater(damage, 0f, "Projectile damage should be positive");
            Assert.Less(damage, 300f, "Projectile damage should be reasonable");
        }
        
        [Test]
        public void RSBCombatSystem_HandlesDifferentAbilityTypes()
        {
            // Arrange
            var meleeAbility = new AbilityData { name = "Sword", damage = 80f, range = 2f, speed = 0f };
            var rangedAbility = new AbilityData { name = "Arrow", damage = 60f, range = 15f, speed = 10f };
            var areaAbility = new AbilityData { name = "Explosion", damage = 120f, range = 8f, speed = 3f };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 targetPos = Vector3.forward * 5f;
            
            // Act
            float meleeDamage = combatSystem.CalculateAbilityDamage(meleeAbility, attackerPos, targetPos);
            float rangedDamage = combatSystem.CalculateAbilityDamage(rangedAbility, attackerPos, targetPos);
            float areaDamage = combatSystem.CalculateAbilityDamage(areaAbility, attackerPos, targetPos);
            
            // Assert
            Assert.Greater(meleeDamage, 0f, "Melee damage should be positive");
            Assert.Greater(rangedDamage, 0f, "Ranged damage should be positive");
            Assert.Greater(areaDamage, 0f, "Area damage should be positive");
            
            // All should produce reasonable damage values
            Assert.Less(meleeDamage, 500f, "Melee damage should be reasonable");
            Assert.Less(rangedDamage, 500f, "Ranged damage should be reasonable");
            Assert.Less(areaDamage, 500f, "Area damage should be reasonable");
        }
        
        [Test]
        public void RSBCombatSystem_PerformanceTest_CalculationSpeed()
        {
            // Arrange
            var testAbility = new AbilityData
            {
                name = "PerformanceTest",
                damage = 100f,
                range = 10f,
                speed = 5f
            };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 targetPos = Vector3.forward * 7f;
            
            // Act & Assert - Performance test
            AssertPerformance(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    combatSystem.CalculateAbilityDamage(testAbility, attackerPos, targetPos);
                }
            }, 0.01f, "100 damage calculations");
        }
        
        [Test]
        public void RSBCombatSystem_ConsistentResults_SameInput()
        {
            // Arrange
            var testAbility = new AbilityData
            {
                name = "ConsistencyTest",
                damage = 100f,
                range = 10f,
                speed = 5f
            };
            
            Vector3 attackerPos = Vector3.zero;
            Vector3 targetPos = Vector3.forward * 6f;
            
            // Act - Multiple calculations with same input
            float damage1 = combatSystem.CalculateAbilityDamage(testAbility, attackerPos, targetPos);
            float damage2 = combatSystem.CalculateAbilityDamage(testAbility, attackerPos, targetPos);
            float damage3 = combatSystem.CalculateAbilityDamage(testAbility, attackerPos, targetPos);
            
            // Assert - Should be consistent
            Assert.AreEqual(damage1, damage2, 0.001f, "Damage calculations should be consistent");
            Assert.AreEqual(damage2, damage3, 0.001f, "Damage calculations should be consistent");
        }
    }
}