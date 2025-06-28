using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIGIFPlayer : MonoBehaviour
{
    [Header("GIF Settings")]
    [SerializeField] private Sprite frame1;  // 第一帧
    [SerializeField] private Sprite frame2;  // 第二帧
    [SerializeField] private float frameInterval = 0.1f; // 帧间隔（秒）

    [Header("UI Reference")]
    [SerializeField] private Image targetImage; // 目标 Image 组件

    private Coroutine playRoutine;
    private bool isPlaying = false;

    private void Awake()
    {
        // 如果没有手动指定 Image，尝试从当前 GameObject 获取
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        // 初始显示第一帧
        if (targetImage != null && frame1 != null)
        {
            targetImage.sprite = frame1;
        }
    }

  private void Start()
  {
    Play();
  }

  // 开始播放动画
  public void Play()
  {
    if (isPlaying || frame1 == null || frame2 == null || targetImage == null)
      return;

    isPlaying = true;
    playRoutine = StartCoroutine(PlayAnimation());
  }

    // 停止播放
    public void Stop()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }
    }

    // 切换播放/停止
    public void Toggle()
    {
        if (isPlaying) Stop();
        else Play();
    }

    // 动画循环协程
    private IEnumerator PlayAnimation()
    {
        while (true)
        {
            targetImage.sprite = frame1;
            yield return new WaitForSeconds(frameInterval);

            targetImage.sprite = frame2;
            yield return new WaitForSeconds(frameInterval);
        }
    }

    // 动态修改帧
    public void SetFrames(Sprite newFrame1, Sprite newFrame2)
    {
        frame1 = newFrame1;
        frame2 = newFrame2;

        // 如果正在播放，重启动画
        if (isPlaying)
        {
            Stop();
            Play();
        }
        else if (targetImage != null && frame1 != null)
        {
            targetImage.sprite = frame1;
        }
    }

    // 修改播放速度
    public void SetFrameInterval(float interval)
    {
        frameInterval = Mathf.Max(0.01f, interval); // 最小间隔 0.01 秒

        // 如果正在播放，重启动画以应用新间隔
        if (isPlaying)
        {
            Stop();
            Play();
        }
    }

    private void OnDestroy()
    {
        Stop(); // 销毁时停止协程
    }
}