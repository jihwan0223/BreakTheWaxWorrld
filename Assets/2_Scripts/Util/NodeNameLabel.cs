using TMPro;
using UnityEngine;

// 업그레이드 트리 노드 아래에 이름 글자를 붙임 (그림만으로는 뭔지 몰라서). Enabled를 끄면 예전처럼 그림만
public static class NodeNameLabel
{
    public static readonly bool Enabled = false; // 노드 이름 표시 (true = 켬)

    private const float FontSize = 100f; // 트리 판 기준 px (기본 줌에서도 읽히는 크기)
    private const float WidthRatio = 2.2f; // 노드 한 변 대비 글자 칸 너비
    private const float Gap = 8f; // 노드와 글자 사이 px

    public static void Set(ref TextMeshProUGUI label, RectTransform node, string text)
    {
        if (!Enabled) return;
        if (label == null)
        {
            var labelObject = new GameObject("NodeNameLabel", typeof(RectTransform)); // 글자 오브젝트
            labelObject.transform.SetParent(node, false);
            label = labelObject.AddComponent<TextMeshProUGUI>();
            if (GameFonts.Tmp != null) label.font = GameFonts.Tmp;
            if (GameFonts.TmpOutline != null) label.fontSharedMaterial = GameFonts.TmpOutline; // 어느 배경에서도 읽히게 테두리
            label.fontSize = FontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Top;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false; // 노드 클릭을 가로채지 않게

            RectTransform rect = label.rectTransform; // 노드 아래 가운데
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(node.rect.width * WidthRatio, FontSize * 2.4f);
            rect.anchoredPosition = new Vector2(0f, -Gap);
        }
        if (label.text != text) label.text = text;
    }
}
