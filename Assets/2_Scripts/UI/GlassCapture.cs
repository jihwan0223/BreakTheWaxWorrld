using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 게임 화면을 작게 그려서 흐린 텍스처로 만듦 (유리 창 배경용 - 업그레이드 창 / 통계 창)
public class GlassCapture
{
    private readonly int _downsample; // 처음 렌더 해상도 = 화면 / 이 값
    private readonly int _blurSteps; // 절반씩 줄이는 횟수 (많을수록 더 흐림)
    private RenderTexture _source; // 카메라가 그리는 작은 화면
    private RenderTexture _blurred; // 줄여서 흐려진 최종 화면
    private readonly UniversalRenderPipeline.SingleCameraRequest _request = new UniversalRenderPipeline.SingleCameraRequest(); // 카메라 한 번 그리기 요청

    public GlassCapture(int downsample, int blurSteps)
    {
        _downsample = downsample;
        _blurSteps = blurSteps;
    }

    // 이번 프레임 화면을 다시 그림. 못 그리면 null
    public RenderTexture Capture()
    {
        Camera cam = Camera.main; // 게임 화면 카메라
        if (cam == null) return null;

        int w = Mathf.Max(1, Screen.width / _downsample); // 작은 화면 너비
        int h = Mathf.Max(1, Screen.height / _downsample); // 작은 화면 높이
        if (_source == null || _source.width != w || _source.height != h)
        {
            Release();
            _source = new RenderTexture(w, h, 16) { filterMode = FilterMode.Bilinear };
            // 알파 없는 포맷: 카메라 알파가 0이면 UI에서 투명해져 뒤가 그대로 비침
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RGB111110Float) ? RenderTextureFormat.RGB111110Float : RenderTextureFormat.RGB565; // 최종 화면 포맷
            _blurred = new RenderTexture(Mathf.Max(1, w >> _blurSteps), Mathf.Max(1, h >> _blurSteps), 0, format) { filterMode = FilterMode.Bilinear };
        }

        _request.destination = _source;
        if (!RenderPipeline.SupportsRenderRequest(cam, _request)) return null;
        RenderPipeline.SubmitRenderRequest(cam, _request);

        // 절반씩 줄여 가며 복사하면 싸게 흐려짐
        RenderTexture current = _source; // 지금 단계 화면
        for (int i = 1; i < _blurSteps; i++)
        {
            var half = RenderTexture.GetTemporary(Mathf.Max(1, current.width / 2), Mathf.Max(1, current.height / 2)); // 다음 단계 화면
            half.filterMode = FilterMode.Bilinear;
            Graphics.Blit(current, half);
            if (current != _source) RenderTexture.ReleaseTemporary(current);
            current = half;
        }
        Graphics.Blit(current, _blurred);
        if (current != _source) RenderTexture.ReleaseTemporary(current);
        return _blurred;
    }

    public void Release()
    {
        if (_source != null) { _source.Release(); Object.Destroy(_source); }
        if (_blurred != null) { _blurred.Release(); Object.Destroy(_blurred); }
        _source = _blurred = null;
    }
}
