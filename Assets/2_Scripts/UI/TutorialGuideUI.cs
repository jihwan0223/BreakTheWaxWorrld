using UnityEngine;
using UnityEngine.UIElements;

// 처음 플레이 퀘스트 안내: 보스 도전 버튼 자리 아래 카드에 두루마리 아이콘 + 다음 할 일을 보여줌 (책상 부수고 맵2 가면 끝)
// 단계는 매 프레임 상태를 보고 넘어감 (이벤트 구독 없음), 저장은 Step 하나
public class TutorialGuideUI : MonoBehaviour
{
    private static TutorialGuideUI _instance; // 하나만

    public static int Step { get; set; } // 진행 단계 (저장됨). 0 = 시작, Finished = 끝
    public static long PendingReward { get; set; } // 끝냈지만 아직 안 준 퀘스트 보상 조각 (저장됨, 완료 연출 때 지급)

    private const float Left = 23f; // 조각 상자 / 보스 도전 버튼과 같은 왼쪽 (둘 다 left 20 + unity-button 기본 여백 3px)
    private const float Top = 172f; // 조각 표시 아래 (보스 도전 버튼 자리 142보다 조금 아래)
    private const float BelowButton = 142f + 80f + 12f; // 보스 도전 버튼(142, 높이 80)이 떠 있을 때 그 아래
    private const int FontSize = 28; // px - 안내 글 (UiColored는 굵기 없음)
    private const float FinalSeconds = 5f; // 마지막 안내가 떠 있는 시간 (초, 실제 시간)
    private const float PanelAlpha = 0.7f; // 카드 투명도 (보스전 패널과 같음)

    private const int StepBreakEgg = 0; // 달걀 부수기
    private const int StepPick = 1; // 조각 줍기
    private const int StepUpgrade = 2; // 첫 업그레이드
    private const int StepCrystal = 3; // 결정으로 커서 줍기 해금
    private const int StepApple = 4; // 사과 해금
    private const int StepButter = 5; // 버터 해금
    private const int StepBread = 6; // 식빵 해금
    private const int StepDesk = 7; // 책상 부수기 (맵2 해금)
    private const int StepNextMap = 8; // 맵2로 이동
    private const int StepFinal = 9; // 새 맵 보상 안내
    public const int Finished = 10; // 끝

    private static readonly string[] Texts = // 단계별 안내 글 (한국어 키, Localization.csv에 번역)
    {
        "달걀을 클릭해 부수세요",
        "떨어진 조각을 주우세요",
        "업그레이드 창에서 조각 {0}개로 강화하세요",
        "업그레이드 창에서 커서 줍기를 해금하세요",
        "업그레이드 창에서 사과를 해금하세요",
        "업그레이드 창에서 버터를 해금하세요",
        "업그레이드 창에서 식빵을 해금하세요",
        "보스 도전으로 책상을 부수세요",
        "오른쪽 화살표로 다음 맵에 가세요",
        "새 지역일수록 보상이 커져요.",
    };

    // 단계를 끝내면 주는 조각 = 다음에 살 오브젝트 해금 가격 x RewardRatio (-1 = 보상 없음)
    // 달걀/줍기는 보상을 주면 줍기 단계가 저절로 끝나서 없음
    private static readonly int[] RewardObject = { -1, -1, 1, 1, 2, 3, 4, 6, 6 }; // 단계별 기준 오브젝트 (사과 1 / 버터 2 / 식빵 3 / 책상 4 / 자전거 6)
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
    private float _doneElapsed = -1f; // 완료 연출 진행 시간 (-1 = 연출 중 아님)
    private Texture2D _scrollTexture; // 두루마리 그림
    private int _shownStep = -1; // 지금 글자로 보여주는 단계
    private float _finalElapsed; // 마지막 안내가 떠 있던 시간
    private float _popElapsed = PopSeconds; // 새 글 연출 진행 시간

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
        _panel.style.left = Left;
        _panel.style.top = Top;
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
        texts.pickingMode = PickingMode.Ignore;
        _panel.Add(texts);

        _label = new Label();
        _label.style.fontSize = FontSize;
        _label.style.color = PixelStyle.Text.linear;
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
            }
            _doneElapsed += Time.unscaledDeltaTime;
            float fadeStart = StrikeSeconds + HoldSeconds; // 줄 긋고 보상 보여준 뒤 사라지기 시작
            if (_doneElapsed < fadeStart + FadeSeconds + NextDelaySeconds)
            {
                _strike.style.width = Length.Percent(100f * Mathf.Clamp01(_doneElapsed / StrikeSeconds));
                _panel.style.opacity = 1f - Mathf.Clamp01((_doneElapsed - fadeStart) / FadeSeconds);
                bool showDone = _doneElapsed < fadeStart + FadeSeconds; // 사라진 뒤 쉬는 동안은 숨김
                _panel.style.display = showDone ? DisplayStyle.Flex : DisplayStyle.None;
                if (showDone) _panel.style.top = BossChallengeUI.ChallengeShown ? BelowButton : Top;
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

        _panel.style.top = BossChallengeUI.ChallengeShown ? BelowButton : Top; // 보스 도전 버튼이 있으면 그 아래로
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

    // 이미 한 일은 건너뜀 (불러온 세이브 포함). 뒤 단계를 먼저 해도(예: 커서 줍기 안 사고 사과 해금) 그 다음 단계로 넘어감
    private static void Advance()
    {
        ZoneManager zones = ZoneManager.Instance; // 맵 상태
        ObjectManager objects = ObjectManager.Instance; // 해금 상태
        if (zones == null || objects == null) return;
        if (Step == 0 && zones.UnlockedMaxZone >= 1) { Step = Finished; return; } // 튜토리얼 생기기 전에 맵2를 연 옛 세이브

        for (int s = StepFinal - 1; s >= Step; s--) // 끝낸 단계 중 가장 뒤 단계의 다음으로
            if (StepDone(s, zones, objects))
            {
                for (int done = Step; done <= s; done++) // 건너뛴 단계 보상도 같이
                    PendingReward += Reward(done); // 완료 연출 때 지급
                Step = s + 1;
                break;
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
            case StepApple: return objects.IsUnlocked(1);
            case StepButter: return objects.IsUnlocked(2);
            case StepBread: return objects.IsUnlocked(3);
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
