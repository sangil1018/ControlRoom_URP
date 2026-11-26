using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ControlRoom.UI.Tests
{
    /// <summary>
    /// 관제실 전면 벽면 TV(중앙 시계·06호기 상/하단 경보/주의/상승 배지) 동기화 컨트롤러의
    /// 시스템 시계 포맷, 정상/비상 아이콘·점멸·상승 화살표 연동 및 활성 코디네이터 자동 감지를 검증하는 단위 테스트입니다.
    /// </summary>
    public class WallDisplaySyncTests
    {
        private GameObject mRootGo;
        private WallDisplaySyncController mWallCtrl;
        private ControlRoomSpritePack mSprites;
        private TMP_FontAsset mFallbackFont;

        [SetUp]
        public void setUp()
        {
            try
            {
                mRootGo = new GameObject("TestWallDisplayRoot");
                Canvas ctrCanvas = createTestCanvas("Canvas_Wall_CTR_Upper");
                Canvas u6UpCanvas = createTestCanvas("Canvas_Wall_Unit06_Upper");
                Canvas u6LowCanvas = createTestCanvas("Canvas_Wall_Unit06_Lower");

                mFallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                ControlRoomUIFactory.DefaultFont = mFallbackFont;

                mSprites = new ControlRoomSpritePack
                {
                    alarmOff = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    alarmOn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    cautionOff = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    cautionOn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    noticeOff = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    noticeOn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    circleDot = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f)
                };

                mWallCtrl = mRootGo.AddComponent<WallDisplaySyncController>();
                mWallCtrl.configureReferences(ctrCanvas, u6UpCanvas, u6LowCanvas, mFallbackFont, mSprites);
                mWallCtrl.initializeWallDisplays();
            }
            catch (Exception ex)
            {
                Assert.Fail($"WallDisplaySyncTests SetUp 오류: {ex}");
            }
        }

        [TearDown]
        public void tearDown()
        {
            if (mRootGo != null)
            {
                UnityEngine.Object.DestroyImmediate(mRootGo);
                mRootGo = null;
            }
        }

        private Canvas createTestCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(mRootGo.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            return canvas;
        }

        [Test]
        public void testWallCenterClockSyncsWithSystemDateTimeFormat()
        {
            try
            {
                DateTime sampleTime = new DateTime(2026, 11, 26, 14, 5, 9);
                mWallCtrl.updateSystemClock(sampleTime);

                Assert.AreEqual("14:05:09", mWallCtrl.CenterClockTimeText);
                Assert.AreEqual("Thursday, Nov. 26, 2026", mWallCtrl.CenterClockDateText);

                // refreshSystemClock 호출 시 현재 PC 로컬 날짜/시각과 동기화됨 검증
                mWallCtrl.refreshSystemClock();
                DateTime now = DateTime.Now;
                StringAssert.Contains(now.Year.ToString(), mWallCtrl.CenterClockDateText);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testWallCenterClockSyncsWithSystemDateTimeFormat 실패: {ex}");
            }
        }

        [Test]
        public void testWallUnit06AlarmCautionBlinkAndUpArrowsSyncWithScenario1And2()
        {
            try
            {
                // 시나리오 1 정상 운전: Alarm 0, Caution 0, LOCA false -> 경보 아이콘 꺼짐 & UNIT 06 상승 화살표 숨김
                mWallCtrl.applyDisplayState(0, 0, 1, false, true, 0);
                Assert.IsFalse(mWallCtrl.IsUnit06AlarmIconOn);
                Assert.IsFalse(mWallCtrl.IsUnit06CautionIconOn);
                Assert.IsFalse(mWallCtrl.AreUnit06UpArrowsVisible);
                Assert.AreEqual("0", mWallCtrl.Unit06AlarmCountText);
                Assert.AreEqual("0", mWallCtrl.Unit06CautionCountText);

                // 시나리오 2 LOCA 비상 운전: Alarm 2, Caution 1, blinkOn=true -> 빨간 X / 노란 ! 점등 & 상승 화살표 3개 표시
                mWallCtrl.applyDisplayState(2, 1, 1, true, true, 2);
                Assert.IsTrue(mWallCtrl.IsUnit06AlarmIconOn);
                Assert.IsTrue(mWallCtrl.IsUnit06CautionIconOn);
                Assert.IsTrue(mWallCtrl.AreUnit06UpArrowsVisible);
                Assert.AreEqual("2", mWallCtrl.Unit06AlarmCountText);
                Assert.AreEqual("1", mWallCtrl.Unit06CautionCountText);
                Assert.AreEqual(2, mWallCtrl.ActiveHeartbeatIndex);

                // 점멸 OFF 위상에서는 아이콘이 어두워지고 카운트와 상승 화살표는 유지됨
                mWallCtrl.applyDisplayState(2, 1, 1, true, false, 0);
                Assert.IsFalse(mWallCtrl.IsUnit06AlarmIconOn);
                Assert.IsFalse(mWallCtrl.IsUnit06CautionIconOn);
                Assert.IsTrue(mWallCtrl.AreUnit06UpArrowsVisible);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testWallUnit06AlarmCautionBlinkAndUpArrowsSyncWithScenario1And2 실패: {ex}");
            }
        }

        [Test]
        public void testWallDisplayAutoSyncsWithActiveScenario2Coordinator()
        {
            try
            {
                var coordGo = new GameObject("TempS2Coord");
                coordGo.transform.SetParent(mRootGo.transform, false);

                var cL = createTestCanvas("S2L");
                var cC = createTestCanvas("S2C");
                var cR = createTestCanvas("S2R");

                var coord = coordGo.AddComponent<Scenario2StationCoordinator>();
                coord.configureReferences(cL, cC, cR, mFallbackFont, mSprites, null, null);
                coord.initializeStation();

                // 초기 LOCA 상태(Alarm=1, Caution=1, IsLocaActive=true) 자동 동기화 확인
                mWallCtrl.syncFromActiveCoordinator();
                Assert.AreEqual("1", mWallCtrl.Unit06AlarmCountText);
                Assert.AreEqual("1", mWallCtrl.Unit06CautionCountText);
                Assert.IsTrue(mWallCtrl.AreUnit06UpArrowsVisible);

                // 시나리오 2 진행 중 MOV-0318 차단으로 2차 경보 추가 시(Alarm=2, Caution=2) 벽면 TV 자동 반영 확인
                coord.Scenario2Model.advanceStep(Scenario2SequenceStep.S2_19_Mov0318Closed);
                mWallCtrl.syncFromActiveCoordinator();
                Assert.AreEqual("2", mWallCtrl.Unit06AlarmCountText);
                Assert.AreEqual("2", mWallCtrl.Unit06CautionCountText);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testWallDisplayAutoSyncsWithActiveScenario2Coordinator 실패: {ex}");
            }
        }

        [Test]
        public void testAllTwelveUnitWallTVsAndCenterTVSyncHeartbeatTogether()
        {
            try
            {
                // 06호기를 제외한 나머지 11개 호기(01~05, 07~12) 상단·하단 TV 캔버스를 모두 추가 등록
                var extraUpperCanvases = new System.Collections.Generic.List<Canvas>();
                var extraLowerCanvases = new System.Collections.Generic.List<Canvas>();
                for (int unit = 1; unit <= 12; unit++)
                {
                    if (unit == 6) continue;
                    extraUpperCanvases.Add(createTestCanvas($"Canvas_Wall_Unit{unit:00}_Upper"));
                    extraLowerCanvases.Add(createTestCanvas($"Canvas_Wall_Unit{unit:00}_Lower"));
                }

                mWallCtrl.configureAdditionalUnitCanvases(extraUpperCanvases.ToArray());
                mWallCtrl.configureAdditionalUnitLowerCanvases(extraLowerCanvases.ToArray());
                mWallCtrl.initializeWallDisplays();

                // 중앙 1대 + 1~12호기 상단 TV 12대 + 1~12호기 하단 TV 12대 = 총 25개 하트비트 그룹 생성 검증
                Assert.AreEqual(25, mWallCtrl.TotalHeartbeatGroupCount);

                // 하트비트 인덱스 0 -> 1 -> 2 순환 시 25개 TV가 모두 동일 위상으로 동기화되는지 검증
                for (int idx = 0; idx < 3; idx++)
                {
                    mWallCtrl.applyDisplayState(0, 0, 1, false, false, idx);
                    Assert.AreEqual(idx, mWallCtrl.ActiveHeartbeatIndex);
                    Assert.IsTrue(mWallCtrl.areAllHeartbeatGroupsSyncedTo(idx), $"하트비트 인덱스 {idx} 동기화 실패");
                    Assert.IsFalse(mWallCtrl.areAllHeartbeatGroupsSyncedTo((idx + 1) % 3));
                }
            }
            catch (Exception ex)
            {
                Assert.Fail($"testAllTwelveUnitWallTVsAndCenterTVSyncHeartbeatTogether 실패: {ex}");
            }
        }

        [Test]
        public void testGreenBoxFillsCoverInteriorWithoutClippingBorder()
        {
            try
            {
                // 중앙 상단 TV 녹색 박스 전체 채움 패널(602x120, 위치 661, -4) 검증
                Transform ctrFill = mRootGo.transform.Find("Canvas_Wall_CTR_Upper/CenterWallRoot/CenterGreenBoxFill");
                Assert.IsNotNull(ctrFill, "CenterGreenBoxFill 패널이 생성되지 않았습니다.");
                var ctrRt = ctrFill.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(661f, -4f), ctrRt.anchoredPosition);
                Assert.AreEqual(new Vector2(602f, 120f), ctrRt.sizeDelta);

                // 06호기 상단 TV 녹색 박스 전체 채움 패널(652x140, 위치 594, -4) 및 UNIT 06 라벨 검증
                Transform u6Fill = mRootGo.transform.Find("Canvas_Wall_Unit06_Upper/U6UpperRoot/U6GreenBoxFill");
                Assert.IsNotNull(u6Fill, "U6GreenBoxFill 패널이 생성되지 않았습니다.");
                var u6Rt = u6Fill.GetComponent<RectTransform>();
                Assert.AreEqual(new Vector2(594f, -4f), u6Rt.anchoredPosition);
                Assert.AreEqual(new Vector2(652f, 140f), u6Rt.sizeDelta);

                var unitTitle = u6Fill.Find("UnitTitle")?.GetComponent<TextMeshProUGUI>();
                var unitNum = u6Fill.Find("UnitNumber")?.GetComponent<TextMeshProUGUI>();
                Assert.IsNotNull(unitTitle);
                Assert.IsNotNull(unitNum);
                Assert.AreEqual("UNIT", unitTitle.text);
                Assert.AreEqual("06", unitNum.text);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testGreenBoxFillsCoverInteriorWithoutClippingBorder 실패: {ex}");
            }
        }

        [Test]
        public void testLowerSmallClocksAreRemovedWhileMainClockRemains()
        {
            try
            {
                Transform u6Lower = mRootGo.transform.Find("Canvas_Wall_Unit06_Lower");
                Assert.IsNotNull(u6Lower);

                // 하단 작은 시계(SubClockMask) 오브젝트가 0개인지 전수 검사
                int subClockCount = 0;
                foreach (Transform t in u6Lower.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.StartsWith("SubClockMask", StringComparison.Ordinal))
                    {
                        subClockCount++;
                    }
                }
                Assert.AreEqual(0, subClockCount, "하단 작은 시계(SubClockMask)가 남아 있습니다.");

                // 06호기 하단 좌측 상단 하트비트 마스크는 존재해야 함
                Transform u6LowerHb = u6Lower.Find("U6LowerRoot/U6LowerHbMask");
                Assert.IsNotNull(u6LowerHb, "06호기 하단 하트비트 마스크가 생성되지 않았습니다.");

                // 중앙 메인 시계는 정상 유지됨을 검증
                DateTime sampleTime = new DateTime(2026, 11, 26, 11, 24, 49);
                mWallCtrl.updateSystemClock(sampleTime);
                Assert.AreEqual("11:24:49", mWallCtrl.CenterClockTimeText);
                Assert.AreEqual("Thursday, Nov. 26, 2026", mWallCtrl.CenterClockDateText);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testLowerSmallClocksAreRemovedWhileMainClockRemains 실패: {ex}");
            }
        }
    }
}
