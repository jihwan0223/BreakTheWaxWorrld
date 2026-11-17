using UnityEngine;
using UnityEngine.UI;

// 창 배경을 반투명 유리처럼: 뒤의 게임 화면을 흐리게 실시간으로 보여줌
[RequireComponent(typeof(Image))]
public class GlassBackdrop : MonoBehaviour
{
    [SerializeField] private Color tint = new Color(0.32f, 0.36f, 0.48f, 1f); // 흐린 화면에 곱하는 색 (어둡게 할수록 덜 투명해 보임)
    [SerializeField, Range(2, 16)] private int downsample = 4; // 처음 렌더 해상도 = 화면 / 이 값
    [SerializeField, Range(0, 4)] private int blurSteps = 2; // 절반씩 줄이는 횟수 (많을수록 더 흐림)
    [SerializeField, Range(0f, 0.3f)] private float sheenAlpha = 0.08f; // 위쪽 유리 광택 세기

    private RawImage _view; // 흐린 화면을 띄우는 이미지
    private GlassCapture _capture; // 흐린 게임 화면 만드는 곳

    void Awake()
    {
        var panel = GetComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0f); // 안 보이게만, 레이캐스트는 유지 (스크롤 드래그용)

        _view = CreateChild("GlassView", 0).AddComponent<RawImage>();
        _view.raycastTarget = false;

        var sheen = CreateChild("GlassSheen", 1).AddComponent<RawImage>();
        sheen.raycastTarget = false;
        sheen.texture = SheenTexture();
        sheen.color = new Color(1f, 1f, 1f, sheenAlpha);
    }

    private GameObject CreateChild(string childName, int siblingIndex)
    {
        var go = new GameObject(childName, typeof(RectTransform)); // 패널 전체를 덮는 자식
        var rt = (RectTransform)go.transform; // 꽉 채우기용
        rt.SetParent(transform, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        rt.SetSiblingIndex(siblingIndex);
        return go;
    }

    // 위는 밝고 아래로 사라지는 세로 그라데이션
    private static Texture2D SheenTexture()
    {
        const int height = 64; // 그라데이션 단계 수
        var tex = new Texture2D(1, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp }; // 1px 너비 세로 텍스처
        for (int y = 0; y < height; y++)
        {
            float t = (float)y / (height - 1); // 0 = 아래, 1 = 위
            tex.SetPixel(0, y, new Color(1f, 1f, 1f, Mathf.Pow(t, 3f)));
        }
        tex.Apply();
        return tex;
    }

    void LateUpdate()
    {
        _capture ??= new GlassCapture(downsample, blurSteps);
        RenderTexture blurred = _capture.Capture(); // 흐린 게임 화면
        if (blurred == null) return;
        _view.texture = blurred;
        _view.color = tint;
    }

    void OnDisable() => _capture?.Release();
}
