using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Pupverse.Tests
{
    public class BattleHudPresentationTests
    {
        bool originalReducedMotion;
        BattleController battle;
        BattleHudPresentation hud;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Battle3D.unity");
            originalReducedMotion=GameSettings.ReducedMotion;
            yield return new EnterPlayMode();
            battle=Object.FindAnyObjectByType<BattleController>();
            hud=Object.FindAnyObjectByType<BattleHudPresentation>();
            yield return null;
            yield return null;
            Assert.That(hud,Is.Not.Null);
            Assert.That(hud.IsBuilt,Is.True);
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameSettings.ReducedMotion=originalReducedMotion;
            yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator PhoneHudRevealsOnlyAfterSelectionAndResetsAfterBothWinners()
        {
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(hud.ShowingComparison,Is.False);
            var seen=new Rect[5];
            for(int i=0;i<5;i++)
            {
                var rect=(RectTransform)battle.statButtons[i].transform;
                Assert.That(rect.rect.width,Is.GreaterThanOrEqualTo(44));
                Assert.That(rect.rect.height,Is.GreaterThanOrEqualTo(44));
                seen[i]=new Rect(rect.anchoredPosition,rect.rect.size);
                for(int j=0;j<i;j++) Assert.That(seen[i].Overlaps(seen[j]),Is.False,"Touch targets must not overlap");
                Assert.That(battle.statButtons[i].GetComponent<BattleStatTile>(),Is.Not.Null);
                foreach(var label in battle.statButtons[i].GetComponentsInChildren<Text>())
                    if(label.enabled && label.name=="Stat value")
                        Assert.That(label.cachedTextGenerator.vertexCount,Is.GreaterThan(0),"Stat numbers must actually render");
            }
            foreach(var graphic in Object.FindObjectsByType<BattleHudGraphic>(FindObjectsSortMode.None))
            {
                Assert.That(graphic.raycastTarget,Is.False);
                Assert.That(graphic.GetComponent<CanvasRenderer>(),Is.Not.Null,"Custom HUD meshes require a renderer");
            }
            Assert.That(battle.cardAnimator.IsAttacking,Is.False);
            var surface=battle.statButtons[0].GetComponent<BattleStatTile>().panel;
            Assert.That(surface.openFrame,Is.True);
            Assert.That(surface.surfaceOpacity,Is.InRange(.15f,.5f));
            Capture("ready");
            yield return new WaitForSecondsRealtime(.15f);
            foreach(CardStat stat in new[]{CardStat.Speed,CardStat.Power})
            {
                battle.statButtons[(int)stat].onClick.Invoke();
                Assert.That(hud.ShowingComparison,Is.True);
                Assert.That(battle.cardAnimator.IsAttacking,Is.False);
                foreach(var button in battle.statButtons) Assert.That(button.interactable,Is.False);
                yield return new WaitForSecondsRealtime(battle.comparisonDuration*.65f);
                Capture(stat+"-comparison");
                yield return new WaitForSecondsRealtime(battle.comparisonDuration*.4f);
                Capture(stat+"-winner");
                Assert.That(battle.resultTitle.text,Does.Contain("WINS"));
                float deadline=Time.realtimeSinceStartup+8;
                while(battle.IsResolving && Time.realtimeSinceStartup<deadline) yield return null;
                Assert.That(battle.IsResolving,Is.False);
                Assert.That(hud.ShowingComparison,Is.False);
                foreach(var button in battle.statButtons) Assert.That(button.interactable,Is.True);
            }
            Assert.That(battle.Wins,Is.EqualTo(1)); Assert.That(battle.Losses,Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator ReducedMotionShowsFinalTotalsWithoutTileMotion()
        {
            GameSettings.ReducedMotion=true;
            battle.Choose(CardStat.Speed);
            yield return null; yield return null;
            var selected=battle.statButtons[(int)CardStat.Speed].GetComponent<BattleStatTile>();
            Assert.That(selected.visual.localScale,Is.EqualTo(Vector3.one));
            Assert.That(selected.visual.anchoredPosition,Is.EqualTo(Vector2.zero));
            foreach(var text in hud.layout.controlsRoot.GetComponentsInChildren<Text>())
                if(text.name=="Player total") Assert.That(text.text,Is.EqualTo("102"));
            Assert.That(battle.cardAnimator.IsAttacking,Is.False);
        }
        static void Capture(string name)
        {
            // Screenshots are test artifacts, captured by Unity's renderer rather than the desktop.
            string folder=Path.Combine(Application.temporaryCachePath,"BattleHudChecks");
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));
            Debug.Log("Battle HUD visual check: "+Path.Combine(folder,name+".png"));
        }
    }
}

namespace Pupverse.Tests
{
    public class BattleHudPresentationTests_Layout
    {
        [TestCase(320,548)]
        [TestCase(375,627)]
        [TestCase(390,759)]
        [TestCase(430,839)]
        [TestCase(360,752)]
        [TestCase(412,867)]
        [TestCase(844,340)]
        public void PhoneSizesKeepControlsInsideSafeAreaAndTouchTargetsSeparate(float width,float height)
        {
            float scale=height>=width?width/390:height/390;
            Vector2 safe=new Vector2(width,height)/scale;
            var layout=new Battle3DController.PhoneLayout(safe);
            Assert.That(layout.Height+72,Is.LessThan(safe.y),"Keep the matchup header above the controls");
            var targets=new Rect[5];
            for(int i=0;i<5;i++)
            {
                Rect r=layout.StatRect(i); targets[i]=r;
                Assert.That(r.width*scale,Is.GreaterThanOrEqualTo(44),"44-point minimum touch width");
                Assert.That(r.height*scale,Is.GreaterThanOrEqualTo(44),"44-point minimum touch height");
                Assert.That(r.xMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(r.xMax,Is.LessThanOrEqualTo(safe.x));
                Assert.That(r.yMin,Is.GreaterThanOrEqualTo(0));
                Assert.That(r.yMax,Is.LessThan(layout.ComparisonRect.yMin));
                for(int j=0;j<i;j++) Assert.That(r.Overlaps(targets[j]),Is.False);
            }
        }
        [Test]
        public void ExtendingFloorBehindHudPreservesCardScreenFraming()
        {
            Vector2 size=new Vector2(1284,2600);
            float reserved=720;
            var before=Matrix4x4.Perspective(60,size.x/(size.y-reserved),.3f,1000);
            var after=Battle3DController.ExtendArenaProjection(60,size,reserved,.3f,1000);
            foreach(var point in new[]{new Vector4(0,0,-8,1),new Vector4(-1,2,-12,1),new Vector4(2,-2,-10,1)})
            {
                var a=before*point; var b=after*point;
                float oldY=reserved+(a.y/a.w+1)*.5f*(size.y-reserved);
                float newY=(b.y/b.w+1)*.5f*size.y;
                Assert.That(newY,Is.EqualTo(oldY).Within(.001f));
                Assert.That(b.x/b.w,Is.EqualTo(a.x/a.w).Within(.00001f));
            }
        }
    }
}
