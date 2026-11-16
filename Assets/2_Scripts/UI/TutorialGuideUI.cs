using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// 처음 플레이 퀘스트 안내: 위쪽 줄 카드에 두루마리 아이콘 + 다음 할 일을 보여주고, 눌러야 할 버튼/노드를 화살표로 가리킴 (책상 부수고 맵2 가면 끝)
// 단계는 매 프레임 상태를 보고 넘어감 (이벤트 구독 없음), 저장은 Step 하나
public class TutorialGuideUI : MonoBehaviour
{
    private static TutorialGuideUI _instance; // 하나만

    public static int Step { get; set; } // 진행 단계 (저장됨). 0 = 시작, Finished = 끝
    public static long PendingReward { get; set; } // 끝냈지만 아직 안 준 퀘스트 보상 조각 (저장됨, 완료 연출 때 지급)
    private static bool _skipRewardsOnce; // 불러온 직후 첫 확인은 보상 없이 (이미 해 둔 일에 보상을 몰아 주지 않게)

    // 세이브를 불러왔을 때. 단계 구성이 바뀐 옛 세이브는 처음부터 다시 훑음 (끝낸 세이브는 끝난 채로)
    public static void Loaded(int step, int version, long pendingReward)
    {
        const int oldFinished = 10; // 버전 1의 끝 번호
        Step = version == Version ? step : step >= oldFinished ? Finished : 0;
        PendingReward = version == Version ? pendingReward : 0;
        _skipRewardsOnce = true;
    }

    // 튜토리얼 첫 단계(첫 강화 / 커서 줍기)에선 그 노드 말고는 못 삼
    public static bool IsBlocked(string nodeId) =>
        ForceFirstSteps && ((Step == StepUpgrade && nodeId != UpgradeManager.FirstNodeId) || (Step == StepCrystal && nodeId != CursorNodeId));

    public const int Version = 2; // 단계 구성 번호 (세이브에 같이 저장, 다르면 Loaded에서 단계를 다시 맞춤)

    // --- 되돌리기 스위치: false로 바꾸면 그 기능만 예전처럼 ---
    public static readonly bool ForceFirstSteps = true; // 첫 강화 / 커서 줍기 단계에선 그 노드만 살 수 있음
    public static readonly bool GuideInTree = true; // 업그레이드 창에서 할 노드 위에 화살표 (살 수 있을 때만)
    public static readonly bool PointAtButtons = true; // 눌러야 할 버튼(업그레이드 / 무기 / 보스 도전 / 다음 맵)에 화살표 (살 수 있을 때만)
    public static readonly bool CardInTopBar = true; // 카드를 위쪽 줄(조각 상자와 버튼 사이)에 (false = 왼쪽 위 조각 상자 아래, 오브젝트를 가릴 수 있음)

    public static readonly Dictionary<string, VisualElement> Targets = new Dictionary<string, VisualElement>(); // 화살표가 가리킬 버튼들 (각 UI가 등록)
    public static readonly Color BlockedTint = new Color(0.35f, 0.35f, 0.35f, 1f); // 잠긴 노드 색 (어둡게)

    private const float Left = 23f; // (CardInTopBar 끌 때) 조각 상자 / 보스 도전 버튼과 같은 왼쪽
    private const float Top = 172f; // (CardInTopBar 끌 때) 조각 표시 아래
    private const float BelowButton = 142f + 80f + 12f; // (CardInTopBar 끌 때) 보스 도전 버튼이 떠 있으면 그 아래
    private const float BarLeft = 380f; // 위쪽 줄: 조각 상자 오른쪽 옆
    private const float BarTop = 20f; // 위쪽 줄: 조각 상자 / 버튼과 같은 높이
    private const float BarGap = 16f; // 위쪽 줄: 카드와 업그레이드 버튼 사이 틈
    private const float BarMaxWidth = 455f; // 위쪽 줄: 버튼 자리를 아직 모를 때 카드 최대 너비
    private const int FontSize = 24; // px - 안내 글 (12px 폰트의 2배라 또렷함, 길면 두 줄)
    private const float FinalSeconds = 5f; // 마지막 안내가 떠 있는 시간 (초, 실제 시간)
    private const float PanelAlpha = 0.7f; // 카드 투명도 (보스전 패널과 같음)

    private const int StepBreakEgg = 0; // 달걀 부수기
    private const int StepPick = 1; // 조각 줍기
    private const int StepUpgrade = 2; // 달걀 드랍량 강화 (첫 업그레이드)
    private const int StepCrystal = 3; // 결정으로 커서 줍기 해금
    private const int StepFist = 4; // 맨손 데미지 강화
    private const int StepApple = 5; // 사과 해금
    private const int StepBroom = 6; // 결정으로 빗자루(자동 줍기) 해금
    private const int StepButter = 7; // 버터 해금
    private const int StepBread = 8; // 식빵 해금
    private const int StepWeapon = 9; // 무기 창에서 프라이팬 사기
    private const int StepDeskUnlock = 10; // 책상 해금
    private const int StepDesk = 11; // 보스 도전으로 책상 부수기 (맵2 해금)
    private const int StepNextMap = 12; // 맵2로 이동
    private const int StepFinal = 13; // 새 맵 보상 안내
    public const int Finished = 14; // 끝

    private const string CursorNodeId = "164_커서_줍기_해금"; // 커서 줍기 노드
    private const string FistNodeId = "1_맨손_데미지_강화"; // 맨손 데미지 노드
    private const string BroomNodeId = "161_자동_줍기_해금"; // 빗자루 노드

    private static readonly string[] Texts = // 단계별 안내 글 (한국어 키, Localization.csv에 번역)
    {
        "달걀을 클릭해 부수세요",
        "떨어진 조각을 주우세요",
        "조각 {0}개로 달걀 드랍량을 강화하세요",
        "결정으로 커서 줍기를 해금하세요",
        "맨손 데미지를 강화하세요",
        "사과를 해금하세요",
        "결정으로 자동 줍기를 해금하세요",
        "버터를 해금하세요",
        "식빵을 해금하세요",
        "무기 창에서 프라이팬을 사세요",
        "책상을 해금하세요",
        "보스 도전으로 책상을 부수세요",
        "오른쪽 화살표로 다음 맵에 가세요",
        "새 지역일수록 보상이 커져요.",
    };

    // 단계별 업그레이드 트리 노드 (없으면 null) - 노드 이름 또는 "unlock:오브젝트 번호"
    private static readonly string[] TreeTargets =
    {
        null, null, UpgradeManager.FirstNodeId, CursorNodeId, FistNodeId, "unlock:1", BroomNodeId, "unlock:2", "unlock:3", null, "unlock:4", null, null, null,
    };

    // 단계별 눌러야 할 버튼 (Targets 키, 없으면 null). 트리 노드 단계는 "upgrade"
    private static readonly string[] ButtonTargets =
    {
        null, null, "upgrade", "upgrade", "upgrade", "upgrade", "upgrade", "upgrade", "upgrade", "weapon", "upgrade", "boss", "next", null,
    };

    // 단계를 끝내면 주는 조각 = 다음에 살 것(오브젝트 해금 가격) x RewardRatio (-1 = 보상 없음)
    // 달걀/줍기는 보상을 주면 줍기 단계가 저절로 끝나서 없음. 책상 해금 뒤엔 바로 보스라 없음
    private static readonly int[] RewardObject = { -1, -1, 1, 1, 1, 2, 2, 3, 3, 4, -1, 6, 6 }; // 사과 1 / 버터 2 / 식빵 3 (프라이팬과 같은 가격) / 책상 4 / 자전거 6
    private const double RewardRatio = 0.3; // 해금 가격의 30%
    private const int RewardFontSize = 22; // px - 보상 줄

    private const float PopSeconds = 0.25f; // 새 글이 뜰 때 커졌다 돌아오는 시간
    private const float PopScale = 1.12f; // 새 글 커지는 배율
    private const int IconDot = 2; // 아이콘 한 도트 = 2px (16도트 = 32px)
    private const float StrikeSeconds = 0.35f; // 완료한 퀘스트에 줄 긋는 시간
    private static readonly Color ClearColor = new Color(0.16f, 0.66f, 0.28f); // 클리어 초록 (PixelStyle.Good은 어두워서 갈색 글과 구분이 안 됨)
    private const float HoldSeconds = 0.8f; // 줄 긋고 보상(+N)을 보여주는 시간
    private const float FadeSeconds = 0.4f; // 그 뒤 사라지는 시간
    private const float NextDelaySeconds = 1.5f; // 사라진 뒤 다음 퀘스트가 뜨기까지

    private VisualElement _panel; // 카드 전체
    private VisualElement _icon; // 두루마리
    private Label _label; // 안내 글
    private Label _reward; // 보상 줄 ("+N조각")
    private VisualElement _strike; // 완료 줄 (안내 글 위를 가로지름)
    private bool _multiLine; // 완료한 퀘스트 글이 두 줄 이상 (취소선을 글자에 직접)
    private float _doneElapsed = -1f; // 완료 연출 진행 시간 (-1 = 연출 중 아님)
    private Texture2D _scrollTexture; // 두루마리 그림
    private int _shownStep = -1; // 지금 글자로 보여주는 단계
    private float _finalElapsed; // 마지막 안내가 떠 있던 시간
    private float _popElapsed = PopSeconds; // 새 글 연출 진행 시간
    private VisualElement _pointer; // 버튼을 가리키는 화살표
    private Texture2D _arrowTexture; // 화살표 그림 (아래를 가리킴)
    private RectTransform _marker; // 업그레이드 트리 안에서 할 노드 위에 뜨는 화살표
    private int _refreshedStep = -1; // 잠금 표시를 마지막으로 갱신한 단계
    private const float PointerSize = 64f; // px - 버튼 화살표
    private const float MarkerSize = 200f; // 트리 판 기준 px - 노드 화살표
    private const float BobSpeed = 6f; // 화살표 위아래 흔들림 빠르기

    public static void Ensure()
    {
        if (_instance != null) return;
        var uiObject = new GameObject("TutorialGuideUI");
        uiObject.SetActive(false); // 설정을 마친 뒤 켜야 처음부터 제대로 그려짐
        var document = uiObject.AddComponent<UIDocument>();
        PieceUI pieceUI = FindFirstObjectByType<PieceUI>();
        PanelSettings settings = pieceUI != null ? pieceUI.GetComponent<UIDocument>().panelSettings : null; // 조각 표시와 같은 판
        if (settings == null)
        {
            settings = PixelStyle.NewPanelSettings();
            settings.sortingOrder = 100;
        }
        document.panelSettings = settings;
        document.sortingOrder = 9; // 보스 도전 버튼(10) 바로 아래
        _instance = uiObject.AddComponent<TutorialGuideUI>();
        uiObject.SetActive(true);
    }

    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;
        root.Clear();
        GameFonts.Apply(root);
        if (GameFonts.UiColored != null)
            root.style.unityFontDefinition = FontDefinition.FromSDFFont(GameFonts.UiColored);
        root.pickingMode = PickingMode.Ignore; // 클릭은 게임으로

        if (_scrollTexture == null) _scrollTexture = PixelTexture(Scroll);

        _panel = new VisualElement(); // 카드: 아이콘 + 글 한 줄
        _panel.style.position = Position.Absolute;
        if (CardInTopBar)
        {
            _panel.style.left = BarLeft;
            _panel.style.top = BarTop;
            _panel.style.maxWidth = BarMaxWidth;
        }
        else
        {
            _panel.style.left = Left;
            _panel.style.top = Top;
        }
        _panel.style.flexDirection = FlexDirection.Row;
        _panel.style.alignItems = Align.Center;
        _panel.style.paddingLeft = 12;
        _panel.style.paddingRight = 16;
        _panel.style.paddingTop = 8;
        _panel.style.paddingBottom = 9;
        _panel.style.transformOrigin = new TransformOrigin(0, 0); // 왼쪽 위 기준으로 커짐
        PixelStyle.Frame(_panel, PixelStyle.Box);
        _panel.style.unityBackgroundImageTintColor = new Color(1f, 1f, 1f, PanelAlpha);
        _panel.pickingMode = PickingMode.Ignore;

        _icon = new VisualElement();
        _icon.style.width = 16 * IconDot;
        _icon.style.height = 16 * IconDot;
        _icon.style.marginRight = 8;
        _icon.style.backgroundImage = new StyleBackground(_scrollTexture);
        _icon.pickingMode = PickingMode.Ignore;
        _panel.Add(_icon);

        var texts = new VisualElement(); // 안내 글 + 보상 줄 (세로)
        texts.style.flexShrink = 1; // 카드가 좁으면 글이 두 줄로
        texts.pickingMode = PickingMode.Ignore;
        _panel.Add(texts);

        _label = new Label();
        _label.style.fontSize = FontSize;
        _label.style.color = PixelStyle.Text.linear;
        _label.style.whiteSpace = WhiteSpace.Normal; // 길면 줄바꿈
        _label.pickingMode = PickingMode.Ignore;
        // 글자 칸(Label)에 자식을 넣으면 글자 크기를 못 재서 카드가 잘림 - 감싸는 칸에 글과 줄을 같이 넣음
        var labelLine = new VisualElement(); // 안내 글 + 완료 줄
        labelLine.pickingMode = PickingMode.Ignore;
        texts.Add(labelLine);
        labelLine.Add(_label);

        _strike = new VisualElement();
        _strike.style.position = Position.Absolute;
        _strike.style.left = 0;
        _strike.style.top = Length.Percent(50);
        _strike.style.height = IconDot; // 도트 한 칸 굵기
        _strike.style.width = 0;
        _strike.style.backgroundColor = PixelStyle.Text.linear;
        _strike.pickingMode = PickingMode.Ignore;
        labelLine.Add(_strike);

        _reward = new Label();
        _reward.style.fontSize = RewardFontSize;
        _reward.style.color = PixelStyle.Text.linear; // 안내 글과 같은 색
        _reward.pickingMode = PickingMode.Ignore;
        texts.Add(_reward);

        root.Add(_panel);

        if (_arrowTexture == null) _arrowTexture = ArrowTexture();
        _pointer = new VisualElement();
        _pointer.style.position = Position.Absolute;
        _pointer.style.width = PointerSize;
        _pointer.style.height = PointerSize;
        _pointer.style.backgroundImage = new StyleBackground(_arrowTexture);
        _pointer.style.display = DisplayStyle.None;
        _pointer.pickingMode = PickingMode.Ignore;
        root.Add(_pointer);

        _shownStep = -1;
        Loc.Changed += Redraw;
    }

    void OnDisable()
    {
        Loc.Changed -= Redraw;
    }

    private void Redraw() => _shownStep = -1; // 언어 바뀌면 글자 다시 넣음

    void Update()
    {
        if (_panel == null) return;
        Advance();
        if (Step == StepFinal && _shownStep == StepFinal) // 마지막 안내가 실제로 뜬 뒤부터 셈
        {
            _finalElapsed += Time.unscaledDeltaTime;
            if (_finalElapsed >= FinalSeconds) Step = Finished;
        }

        bool windowOpen = UIPointerGuard.PopupOpen || Time.timeScale == 0f; // 업그레이드/설정/도감 등 창이 열려 있음 (보스 도전 버튼과 같은 기준)
        UpdateGuide(windowOpen);

        // 완료 연출: 보이던 퀘스트에 줄을 긋고 보상 조각을 넣은 뒤 사라지고, 잠깐 쉬고 다음 퀘스트
        // 창이 열려 있으면(업그레이드 창에서 산 경우 등) 멈춰 있다가 창을 닫으면 시작
        if (_doneElapsed < 0f && _shownStep >= 0 && _shownStep < StepFinal && Step != _shownStep) _doneElapsed = 0f;
        if (_doneElapsed >= 0f)
        {
            if (windowOpen || ZoneBreaker.ActiveBoss != null) { _panel.style.display = DisplayStyle.None; return; } // 보스전 중엔 이 화면이 숨겨져 있음
            if (_doneElapsed == 0f && PendingReward > 0) // 줄 긋기 시작할 때 보상 지급 - 조각 표시가 올라가는 게 보임
            {
                CurrencyManager.Instance?.AddPieces(PendingReward);
                _reward.text = Loc.T("+{0}조각", NumberFormatUtil.Format(PendingReward));
                _reward.style.display = DisplayStyle.Flex; // 보상 없던 퀘스트를 건너뛰며 끝낸 경우에도 보이게
                PendingReward = 0;
            }
            if (_doneElapsed == 0f) // 클리어 표시: 글과 줄을 초록색으로
            {
                _label.style.color = ClearColor.linear;
                _reward.style.color = ClearColor.linear;
                _strike.style.backgroundColor = ClearColor.linear;
                _multiLine = _label.resolvedStyle.height > FontSize * 1.6f; // 두 줄 이상이면 줄 하나로는 못 그음
                if (_multiLine) _label.text = "<s>" + _label.text + "</s>"; // 줄마다 글자 취소선
            }
            _doneElapsed += Time.unscaledDeltaTime;
            float fadeStart = StrikeSeconds + HoldSeconds; // 줄 긋고 보상 보여준 뒤 사라지기 시작
            if (_doneElapsed < fadeStart + FadeSeconds + NextDelaySeconds)
            {
                _strike.style.width = Length.Percent(_multiLine ? 0f : 100f * Mathf.Clamp01(_doneElapsed / StrikeSeconds));
                _panel.style.opacity = 1f - Mathf.Clamp01((_doneElapsed - fadeStart) / FadeSeconds);
                bool showDone = _doneElapsed < fadeStart + FadeSeconds; // 사라진 뒤 쉬는 동안은 숨김
                _panel.style.display = showDone ? DisplayStyle.Flex : DisplayStyle.None;
                if (showDone) PlaceCard();
                return;
            }
            _doneElapsed = -1f; // 끝: 줄 지우고 다음 퀘스트 표시
            _strike.style.width = 0;
            _panel.style.opacity = 1f;
            _reward.style.color = PixelStyle.Text.linear;
            _label.style.color = PixelStyle.Text.linear; // 다음 퀘스트는 원래 색
            _strike.style.backgroundColor = PixelStyle.Text.linear;
        }
        if (PendingReward > 0 && !windowOpen) // 연출 없이 남은 보상 (불러온 세이브 등)
        {
            CurrencyManager.Instance?.AddPieces(PendingReward);
            PendingReward = 0;
        }

        bool show = Step >= 0 && Step < Finished && !windowOpen;
        _panel.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (!show) return;

        PlaceCard();
        _popElapsed += Time.unscaledDeltaTime;
        float pop = Mathf.Lerp(PopScale, 1f, Mathf.Clamp01(_popElapsed / PopSeconds)); // 새 글이 뜰 때 살짝 커졌다 돌아옴
        _panel.style.scale = new Scale(new Vector3(pop, pop, 1f));

        if (_shownStep == Step) return;
        if (_shownStep >= 0) _popElapsed = 0f; // 처음 뜰 때(불러오기 / 언어 바꿈)는 연출 없음
        _shownStep = Step;
        _icon.style.display = Step == StepFinal ? DisplayStyle.None : DisplayStyle.Flex; // 마지막 안내는 할 일이 아니라 아이콘 없음
        _label.text = Step == StepUpgrade ? Loc.T(Texts[Step], NumberFormatUtil.Format(NextCost(UpgradeManager.FirstNodeId))) : Loc.T(Texts[Step]);
        long reward = Reward(Step); // 이 단계 보상
        _reward.text = reward > 0 ? Loc.T("+{0}조각", NumberFormatUtil.Format(reward)) : "";
        _reward.style.display = reward > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void PlaceCard()
    {
        if (!CardInTopBar) { _panel.style.top = BossChallengeUI.ChallengeShown ? BelowButton : Top; return; } // 왼쪽 위일 땐 보스 도전 버튼 아래로 비킴
        if (Targets.TryGetValue("upgrade", out VisualElement upgrade) && upgrade.panel != null && upgrade.worldBound.width > 0f)
            _panel.style.maxWidth = Mathf.Max(200f, upgrade.worldBound.xMin - BarLeft - BarGap); // 업그레이드 버튼을 넘지 않게
    }

    // 지금 할 일을 화면에서 가리킴: 창 밖에선 눌러야 할 버튼, 업그레이드 창 안에선 할 노드. 둘 다 살 수 있을 때만 (돈이 모이면 뜸)
    private void UpdateGuide(bool windowOpen)
    {
        float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * BobSpeed)); // 0~1 위아래 흔들림
        UpgradeTreeUI tree = UpgradeTreeUI.Instance; // 업그레이드 트리 (환생 창 아님)
        bool treeOpen = tree != null && UpgradeTreeUI.Current == tree;

        if (treeOpen && _refreshedStep != Step) tree.RefreshAll(); // 단계가 바뀌면 잠금(어둡게) 표시 갱신
        if (treeOpen) _refreshedStep = Step;

        bool affordable = TargetAffordable(); // 지금 단계의 것을 살 돈이 있음

        // 업그레이드 창 안: 할 노드 위에 화살표 (카메라는 안 움직임)
        RectTransform node = null; // 지금 할 노드
        string target = Step >= 0 && Step < TreeTargets.Length ? TreeTargets[Step] : null;
        if (GuideInTree && treeOpen && target != null && affordable)
        {
            node = target.StartsWith("unlock:") ? tree.FindUnlockNode(int.Parse(target.Substring(7))) : tree.FindNode(target);
            if (node != null && !node.gameObject.activeInHierarchy) node = null; // 아직 안 보이는 노드
        }
        if (node != null)
        {
            if (_marker == null)
            {
                var markerObject = new GameObject("TutorialMarker", typeof(RectTransform), typeof(UnityEngine.UI.Image)); // 노드 화살표
                var image = markerObject.GetComponent<UnityEngine.UI.Image>(); // 화살표 그림
                image.sprite = Sprite.Create(_arrowTexture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
                image.raycastTarget = false; // 노드 클릭을 가로채지 않게
                _marker = (RectTransform)markerObject.transform;
                _marker.sizeDelta = new Vector2(MarkerSize, MarkerSize);
                _marker.anchorMin = _marker.anchorMax = new Vector2(0.5f, 1f);
                _marker.pivot = new Vector2(0.5f, 0f);
            }
            if (_marker.parent != node) _marker.SetParent(node, false);
            _marker.gameObject.SetActive(true);
            _marker.anchoredPosition = new Vector2(0f, 10f + bob * 40f);
        }
        else if (_marker != null) _marker.gameObject.SetActive(false);

        // 창 밖: 눌러야 할 버튼 아래(화면 아래쪽 버튼이면 위)에 화살표
        string key = Step >= 0 && Step < ButtonTargets.Length ? ButtonTargets[Step] : null;
        VisualElement button = null; // 가리킬 버튼
        bool busy = windowOpen || ZoneBreaker.ActiveBoss != null || _doneElapsed >= 0f; // 창 / 보스전 / 완료 연출 중엔 숨김
        if (PointAtButtons && !busy && key != null && affordable) Targets.TryGetValue(key, out button);
        if (button == null || button.panel == null || button.resolvedStyle.display == DisplayStyle.None || button.worldBound.width <= 0f)
        {
            _pointer.style.display = DisplayStyle.None;
            return;
        }
        Rect bound = button.worldBound; // 버튼 자리 (화면 px)
        bool below = bound.center.y < _pointer.parent.worldBound.height * 0.5f; // 화면 위쪽 버튼이면 그 아래에서 위를 가리킴
        _pointer.style.display = DisplayStyle.Flex;
        _pointer.style.left = bound.center.x - PointerSize * 0.5f;
        _pointer.style.top = below ? bound.yMax + 4f + bob * 14f : bound.yMin - PointerSize - 4f - bob * 14f;
        _pointer.style.rotate = new Rotate(below ? 180f : 0f);
    }

    // 지금 단계에서 사야 할 것을 살 수 있는지 (사는 단계가 아니면 true)
    private static bool TargetAffordable()
    {
        if (Step == StepWeapon) return WeaponManager.Instance != null && WeaponManager.Instance.AnyUnlockAffordable();
        string target = Step >= 0 && Step < TreeTargets.Length ? TreeTargets[Step] : null; // 이 단계 노드
        if (target == null) return true;
        if (!target.StartsWith("unlock:")) return UpgradeManager.Instance != null && UpgradeManager.Instance.CanUpgrade(target);
        int index = int.Parse(target.Substring(7)); // 해금할 오브젝트 번호
        return ObjectManager.Instance != null && CurrencyManager.Instance != null && ObjectManager.Instance.IsUnlockOrderMet(index)
            && CurrencyManager.Instance.GetPieces() >= ObjectManager.Instance.GetUnlockCost(index);
    }

    // 지금 단계를 끝내면 다음 단계로. 뒤 단계를 먼저 해 둬도 지금 단계는 그대로 남고,
    // 지금 단계를 끝내면 미리 해 둔 다음 단계가 화면에 뜨자마자 바로 완료 연출 (한 단계씩)
    private void Advance()
    {
        ZoneManager zones = ZoneManager.Instance; // 맵 상태
        ObjectManager objects = ObjectManager.Instance; // 해금 상태
        if (zones == null || objects == null) return;
        if (Step == 0 && zones.UnlockedMaxZone >= 1) { Step = Finished; _skipRewardsOnce = false; return; } // 튜토리얼 생기기 전에 맵2를 연 옛 세이브

        if (_skipRewardsOnce) // 불러온 직후: 이미 해 둔 일은 연출 / 보상 없이 건너뜀
        {
            _skipRewardsOnce = false;
            while (Step < StepFinal && StepDone(Step, zones, objects)) Step++;
            return;
        }
        if (_shownStep != Step || _doneElapsed >= 0f) return; // 지금 단계가 화면에 뜬 뒤에만 (완료 연출이 하나씩 보이게)
        if (Step < StepFinal && StepDone(Step, zones, objects))
        {
            PendingReward += Reward(Step); // 완료 연출 때 지급
            Step++;
        }
    }

    private static long Reward(int step)
    {
        if (step < 0 || step >= RewardObject.Length || RewardObject[step] < 0 || ObjectManager.Instance == null) return 0;
        return NumberFormatUtil.RoundPrice((long)(ObjectManager.Instance.GetUnlockCost(RewardObject[step]) * RewardRatio));
    }

    private static bool StepDone(int step, ZoneManager zones, ObjectManager objects)
    {
        switch (step)
        {
            case StepBreakEgg: return GameStats.ObjectsBroken > 0;
            case StepPick: return CurrencyManager.Instance != null && CurrencyManager.Instance.TotalPieces > 0;
            case StepUpgrade: return UpgradeManager.Instance != null && UpgradeManager.Instance.FirstUpgradeBought;
            case StepCrystal: return UpgradeManager.Instance != null && UpgradeManager.Instance.HoverPickupIsUnlocked;
            case StepFist: return UpgradeManager.Instance != null && UpgradeManager.Instance.GetLevel(FistNodeId) >= 1;
            case StepApple: return objects.IsUnlocked(1);
            case StepBroom: return UpgradeManager.Instance != null && UpgradeManager.Instance.AutoSweepIsUnlocked;
            case StepButter: return objects.IsUnlocked(2);
            case StepBread: return objects.IsUnlocked(3);
            case StepWeapon: return WeaponManager.Instance != null && WeaponManager.Instance.IsUnlocked(1);
            case StepDeskUnlock: return objects.IsUnlocked(4);
            case StepDesk: return zones.UnlockedMaxZone >= 1;
            case StepNextMap: return zones.CurrentZone >= 1;
            default: return false;
        }
    }

    private static long NextCost(string nodeId)
    {
        PieceCost[] costs = UpgradeManager.Instance != null ? UpgradeManager.Instance.GetNextCost(nodeId) : null; // 다음 레벨 가격
        return costs != null && costs.Length > 0 ? costs[0].amount : 0;
    }

    // --- 16x16 도트 아이콘 (문자 = 색, . = 투명) ---
    private static readonly string[] Scroll = // 두루마리
    {
        "................", "..OOOOOOOOOOO...", ".OLPPPPPPPPPpO..", ".OPPPPPPPPPPpO..", "..OPPOOOOOPpO...", "..OPPPPPPPPpO...",
        "..OPPOOOOPPpO...", "..OPPPPPPPPpO...", "..OPPOOOOOPpO...", "..OPPPPPPPPpO...", "..OPPPPPPPPpO...", ".OLPPPPPPPPPpO..",
        ".OPPPPPPPPPPpO..", "..OOOOOOOOOOO...", "................", "................",
    };

    // 아래를 가리키는 16x16 도트 화살표 (노란색 + 어두운 테두리)
    private static Texture2D ArrowTexture()
    {
        const int size = 16; // 도트 수
        var fill = new bool[size, size]; // 화살표 안쪽 (y = 0이 위)
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                fill[x, y] = y < 8 ? x >= 5 && x <= 10 && y >= 1 : Mathf.Abs(x - 7.5f) <= 14.5f - y; // 위는 기둥, 아래는 세모
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edge = fill[x, y] && (x == 0 || y == 0 || x == size - 1 || y == size - 1 || !fill[x - 1, y] || !fill[x + 1, y] || !fill[x, y - 1] || !fill[x, y + 1]); // 테두리 칸
                texture.SetPixel(x, size - 1 - y, !fill[x, y] ? Color.clear : edge ? new Color32(58, 42, 32, 255) : new Color32(255, 205, 60, 255));
            }
        texture.Apply();
        return texture;
    }

    private static Texture2D PixelTexture(string[] rows)
    {
        const int size = 16; // 도트 수
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, size - 1 - y, PixelColor(rows[y][x])); // 문자열 첫 줄이 위
        texture.Apply();
        return texture;
    }

    private static Color PixelColor(char c)
    {
        switch (c)
        {
            case 'O': return new Color32(58, 42, 32, 255); // 외곽선
            case 'P': return new Color32(245, 230, 200, 255); // 종이
            case 'p': return new Color32(214, 190, 150, 255); // 종이 그늘
            case 'L': return new Color32(255, 250, 235, 255); // 종이 밝은 곳
            default: return Color.clear;
        }
    }
}
