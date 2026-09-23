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
