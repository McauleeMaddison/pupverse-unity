using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pupverse.Tests
{
    public class BattleCardAnimatorTests
    {
        GameObject fixture;
        BattleCardAnimator animator;
        Transform player, rival, playerBody, rivalBody;
        Vector3 playerStart, rivalStart;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return new EnterPlayMode();
            fixture = new GameObject("Battle animation test fixture");
            player = new GameObject("Player root").transform;
            rival = new GameObject("Rival root").transform;
            player.SetParent(fixture.transform);
            rival.SetParent(fixture.transform);
            playerBody = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            rivalBody = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            playerBody.SetParent(player);
            rivalBody.SetParent(rival);
            playerBody.localPosition = new Vector3(0.35f, 1.25f, 0f);
            rivalBody.localPosition = new Vector3(0f, 1.25f, 5f);
            var core = new GameObject("Core").transform;
            core.SetParent(fixture.transform);
            core.position = new Vector3(0f, 0.45f, 2.5f);
            animator = fixture.AddComponent<BattleCardAnimator>();
            var settings = new SerializedObject(animator);
            settings.FindProperty("playerCardRoot").objectReferenceValue = player;
            settings.FindProperty("rivalCardRoot").objectReferenceValue = rival;
            settings.FindProperty("battleCore").objectReferenceValue = core;
            settings.FindProperty("attackDuration").floatValue = 0.1f;
            settings.FindProperty("centrePauseDuration").floatValue = 0.05f;
            settings.FindProperty("returnDuration").floatValue = 0.1f;
            settings.ApplyModifiedPropertiesWithoutUndo();
            playerStart = player.localPosition;
            rivalStart = rival.localPosition;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(fixture);
            yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator TurnsMoveTowardCoreAndReturnExactly()
        {
            animator.AttackRival();
            Assert.That(animator.IsAttacking, Is.False, "Raven must wait for Brooklyn.");
            animator.AttackPlayer();
            animator.AttackPlayer();
            animator.ResetTurns();
            Assert.That(animator.IsAttacking, Is.True);
            bool movedForward = false;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (animator.IsAttacking && Time.realtimeSinceStartup < deadline)
            {
                movedForward |= player.position.z > playerStart.z;
                Assert.That(rival.localPosition.Equals(rivalStart), Is.True);
                yield return null;
            }
            Assert.That(animator.IsAttacking, Is.False);
            Assert.That(movedForward, Is.True);
            Assert.That(player.localPosition.Equals(playerStart), Is.True);
            Assert.That(animator.IsPlayerTurn, Is.False);
            animator.AttackPlayer();
            Assert.That(animator.IsAttacking, Is.False, "Brooklyn must wait for Raven.");
            animator.AttackRival();
            bool movedBack = false;
            deadline = Time.realtimeSinceStartup + 3f;
            while (animator.IsAttacking && Time.realtimeSinceStartup < deadline)
            {
                movedBack |= rival.localPosition.z < rivalStart.z;
                Assert.That(player.localPosition.Equals(playerStart), Is.True);
                yield return null;
            }
            Assert.That(animator.IsAttacking, Is.False);
            Assert.That(movedBack, Is.True, "Offset rival card must move toward the core, not away.");
            Assert.That(rival.localPosition.Equals(rivalStart), Is.True);
            Assert.That(playerBody.localPosition.Equals(new Vector3(0.35f, 1.25f, 0f)), Is.True);
            Assert.That(rivalBody.localPosition.Equals(new Vector3(0f, 1.25f, 5f)), Is.True);
            Assert.That(animator.IsPlayerTurn, Is.True);
        }

        [UnityTest]
        public IEnumerator AllStatChoicesUseExistingRulesAndRestoreCards()
        {
            var brooklyn = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Brooklyn.asset");
            var raven = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Raven.asset");
            fixture.AddComponent<BattleCardEffects>();
            var battle = fixture.AddComponent<Battle3DController>();
            var settings = new SerializedObject(battle);
            settings.FindProperty("animator").objectReferenceValue = animator;
            settings.FindProperty("playerCard").objectReferenceValue = brooklyn;
            settings.FindProperty("rivalCard").objectReferenceValue = raven;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Vector3 rotation = player.localEulerAngles;
            Vector3 scale = player.localScale;
            Assert.That(battle.Choose((CardStat)99), Is.False);
            for (int i = 0; i < 5; i++)
            {
                CardStat stat = (CardStat)i;
                Assert.That(battle.Choose(stat), Is.True);
                Assert.That(battle.Choose(stat), Is.False, "Cannot select a second attack during resolution.");
                battle.NextRound();
                Assert.That(battle.IsResolving, Is.True, "Cannot reset during an attack.");
                bool effectVisible = false;
                float deadline = Time.realtimeSinceStartup + 4f;
                while (battle.IsResolving && Time.realtimeSinceStartup < deadline)
                {
                    foreach (var line in fixture.GetComponentsInChildren<LineRenderer>()) effectVisible |= line.enabled;
                    Assert.That(rival.localPosition.Equals(rivalStart), Is.True);
                    yield return null;
                }
                Assert.That(battle.IsResolving, Is.False);
                Assert.That(effectVisible, Is.True, "Each stat must have a visible effect.");
                Assert.That(battle.IsResolved, Is.True);
                BattleResult expected = BattleRules.Resolve(brooklyn, raven, stat);
                Assert.That(battle.Result.PlayerTotal, Is.EqualTo(expected.PlayerTotal));
                Assert.That(battle.Result.OpponentTotal, Is.EqualTo(expected.OpponentTotal));
                Assert.That(battle.Result.Winner, Is.EqualTo(expected.Winner));
                Assert.That(battle.Result.Stat, Is.EqualTo(stat));
                Assert.That(player.localPosition.Equals(playerStart), Is.True);
                Assert.That(player.localEulerAngles.Equals(rotation), Is.True);
                Assert.That(player.localScale.Equals(scale), Is.True);
                foreach (var line in fixture.GetComponentsInChildren<LineRenderer>()) Assert.That(line.enabled, Is.False);
                Assert.That(battle.Choose(stat), Is.False, "A resolved round must wait for Next round.");
                battle.NextRound();
                Assert.That(battle.IsResolved, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator CancelledStatAttackDoesNotResolveRound()
        {
            var battle = fixture.AddComponent<Battle3DController>();
            var settings = new SerializedObject(battle);
            settings.FindProperty("animator").objectReferenceValue = animator;
            settings.FindProperty("playerCard").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Brooklyn.asset");
            settings.FindProperty("rivalCard").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Raven.asset");
            settings.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(battle.Choose(CardStat.Intelligence), Is.True);
            yield return null;
            battle.enabled = false;
            Assert.That(battle.IsResolved, Is.False);
            Assert.That(battle.IsResolving, Is.False);
            Assert.That(player.localPosition.Equals(playerStart), Is.True);
            Assert.That(animator.CompletedAttacks, Is.Zero);
            battle.enabled = true;
            Assert.That(battle.Choose(CardStat.Luck), Is.True);
        }

        [UnityTest]
        public IEnumerator DisableRestoresPositionWithoutConsumingTurn()
        {
            animator.AttackPlayer();
            yield return null;
            animator.enabled = false;
            Assert.That(player.localPosition.Equals(playerStart), Is.True);
            Assert.That(animator.IsAttacking, Is.False);
            Assert.That(animator.IsPlayerTurn, Is.True);
            animator.enabled = true;
            animator.AttackPlayer();
            Assert.That(animator.IsAttacking, Is.True);
        }
    }
}
