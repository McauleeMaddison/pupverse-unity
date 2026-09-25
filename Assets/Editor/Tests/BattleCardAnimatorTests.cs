using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

        BattleController MakeBattle()
        {
            var go = new GameObject("Round controller");
            go.transform.SetParent(fixture.transform);
            go.SetActive(false);
            var battle = go.AddComponent<BattleController>();
            var p = new GameObject("Player data"); p.transform.SetParent(go.transform); p.SetActive(false);
            var r = new GameObject("Rival data"); r.transform.SetParent(go.transform); r.SetActive(false);
            battle.playerCard = p.AddComponent<CardView>();
            battle.opponentCard = r.AddComponent<CardView>();
            battle.playerCard.data = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Brooklyn.asset");
            battle.opponentCard.data = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Cards/Raven.asset");
            battle.resultTitle = MakeText(go.transform);
            battle.resultDetail = MakeText(go.transform);
            battle.scoreLabel = MakeText(go.transform);
            battle.statButtons = new Button[5];
            battle.statBindings = new BattleController.StatButtonBinding[5];
            for (int i = 0; i < 5; i++)
            {
                var button = new GameObject("Stat " + i, typeof(RectTransform), typeof(Image), typeof(Button));
                button.transform.SetParent(go.transform);
                MakeText(button.transform);
                battle.statButtons[i] = button.GetComponent<Button>();
                // Deliberately reverse Inspector bindings to verify explicit stat mapping.
                battle.statBindings[4-i] = new BattleController.StatButtonBinding { stat = (CardStat)i, button = battle.statButtons[i] };
            }
            battle.cardAnimator = animator;
            battle.comparisonDuration = .06f;
            battle.winnerReadDuration = .06f;
            battle.resultHoldDuration = .06f;
            go.SetActive(true);
            return battle;
        }

        static Text MakeText(Transform parent)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent);
            return go.GetComponent<Text>();
        }

        [UnityTest]
        public IEnumerator AllStatsResolveBeforeOnlyWinnerMoves()
        {
            fixture.AddComponent<BattleCardEffects>();
            var battle = MakeBattle();
            for (int i = 0; i < 5; i++)
            {
                CardStat stat = (CardStat)i;
                Assert.That(battle.statButtons[i].GetComponentInChildren<Text>().text, Does.Contain(battle.playerCard.data.EffectiveValue(stat).ToString()));
                Assert.That(battle.resultDetail.text, Does.Not.Contain("Raven:"), "Hide the rival total before choosing.");
                battle.statButtons[i].onClick.Invoke();
                var expected = BattleRules.Resolve(battle.playerCard.data, battle.opponentCard.data, stat);
                Assert.That(battle.IsResolved, Is.True);
                Assert.That(battle.Result.Stat, Is.EqualTo(stat));
                Assert.That(battle.Result.PlayerTotal, Is.EqualTo(expected.PlayerTotal));
                Assert.That(battle.Result.OpponentTotal, Is.EqualTo(expected.OpponentTotal));
                Assert.That(animator.IsAttacking, Is.False, "The result must be shown before movement.");
                battle.Choose((CardStat)((i + 1) % 5));
                Assert.That(battle.Result.Stat, Is.EqualTo(stat), "Repeated taps cannot replace a result.");
                bool moved = false;
                float deadline = Time.realtimeSinceStartup + 4f;
                while (battle.IsResolving && Time.realtimeSinceStartup < deadline)
                {
                    if (expected.Winner == BattleWinner.Player)
                    {
                        Assert.That(rival.localPosition.Equals(rivalStart), Is.True);
                        moved |= !player.localPosition.Equals(playerStart);
                    }
                    else
                    {
                        Assert.That(player.localPosition.Equals(playerStart), Is.True);
                        moved |= !rival.localPosition.Equals(rivalStart);
                    }
                    if (animator.IsAttacking) Assert.That(battle.resultTitle.text, Does.Contain("WINS"));
                    yield return null;
                }
                Assert.That(battle.IsResolving, Is.False);
                Assert.That(moved, Is.True);
                Assert.That(battle.resultTitle.text, Is.EqualTo((expected.Winner == BattleWinner.Player ? "BROOKLYN" : "RAVEN") + " WINS"));
                Assert.That(player.localPosition.Equals(playerStart), Is.True);
                Assert.That(rival.localPosition.Equals(rivalStart), Is.True);
                battle.Replay();
            }
            Assert.That(battle.Wins, Is.EqualTo(2));
            Assert.That(battle.Losses, Is.EqualTo(3));
            Assert.That(battle.Draws, Is.Zero);
        }

        [UnityTest]
        public IEnumerator DrawDoesNotAnimateAndAutomaticNextRoundUnlocksButtons()
        {
            var battle = MakeBattle();
            battle.autoNextRound = true;
            // Reuse the same real card data for both sides to exercise a draw without editing assets.
            battle.opponentCard.data = battle.playerCard.data;
            battle.Choose(CardStat.Speed);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (battle.IsResolving && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(animator.IsAttacking, Is.False);
                yield return null;
            }
            Assert.That(battle.IsResolving, Is.False);
            Assert.That(battle.Draws, Is.EqualTo(1));
            Assert.That(battle.IsResolved, Is.False);
            Assert.That(animator.CompletedAttacks, Is.Zero);
            foreach (var button in battle.statButtons) Assert.That(button.interactable, Is.True);
        }

        [UnityTest]
        public IEnumerator DisablingBattleRestoresWinningCardAndClearsLock()
        {
            var battle = MakeBattle();
            battle.Choose(CardStat.Power);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!animator.IsAttacking && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(animator.IsAttacking, Is.True);
            battle.enabled = false;
            Assert.That(battle.IsResolving, Is.False);
            Assert.That(battle.IsResolved, Is.False);
            Assert.That(animator.IsAttacking, Is.False);
            Assert.That(player.localPosition.Equals(playerStart), Is.True);
            Assert.That(rival.localPosition.Equals(rivalStart), Is.True);
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
