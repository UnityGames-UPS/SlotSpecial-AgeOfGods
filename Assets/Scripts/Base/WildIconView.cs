using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WildIconView : MonoBehaviour
{
  [SerializeField] private Image Image;
  private SlotIconView target;

  internal bool isGold { get; private set; }
  private bool isTracking;

  [Header("Wild Feature (wheel-triggered wild)")]
  [SerializeField] private Sprite goldWildWhiteSprite;
  [SerializeField] private Vector3 wildFeatureLocalPos = new Vector3(4.77f, 2.35f, 0f);
  [SerializeField] private Vector2 wildFeatureSizeDelta = new Vector2(345.3f, 363.7f);
  private ImageAnimation wildAnimation;

  private Vector3 initialRootLocalPosition;
  private Vector3 initialImageLocalPos;
  private Vector2 initialSizeDelta;
  private bool cached;

  private void Awake()
  {
    if (Image != null)
    {
      wildAnimation = Image.GetComponent<ImageAnimation>();
      initialImageLocalPos = Image.rectTransform.localPosition;
      initialSizeDelta = Image.rectTransform.sizeDelta;
    }
  }

  internal void Init(SlotIconView slotIconView)
  {
    target = slotIconView;
    if (!cached)
    {
      initialRootLocalPosition = transform.localPosition;
      cached = true;
      gameObject.SetActive(false);
    }
  }

  private void LateUpdate()
  {
    if (isTracking && target != null)
      transform.position = target.transform.position;
  }

  internal void SetGoldIcon(Sprite sprite)
  {
    isGold = true;
    Image.sprite = sprite;
    Image.color = Color.white;
    Image.sprite = goldWildWhiteSprite;
    Image.rectTransform.localPosition = initialImageLocalPos;
    Image.rectTransform.sizeDelta = initialSizeDelta;
    gameObject.SetActive(true);
    isTracking = true;
  }

  internal void AnimateGoldIcon(bool show = false)
  {
    Image.DOFade(show ? 1f : 0f, 0.5f);
  }

  internal void Reset()
  {
    if (isGold)
    {
      AnimateGoldIcon(true);
      DOVirtual.DelayedCall(0.7f, () =>
      {
        isTracking = false;
        gameObject.SetActive(false);
      });
      isGold = false;
    }
    else
    {
      isTracking = false;
      ResetWildFeatureAnimation();
    }
  }

  internal IEnumerator PlayWildFeatureAnimation()
  {
    isTracking = false;
    transform.localPosition = initialRootLocalPosition;

    Image.rectTransform.localPosition = wildFeatureLocalPos;
    Image.rectTransform.sizeDelta = wildFeatureSizeDelta;

    gameObject.SetActive(true);
    Image.color = Color.white;

    if (wildAnimation != null)
    {
      wildAnimation.StartAnimation();
      yield return new WaitUntil(() => !wildAnimation.isplaying);
    }
  }

  internal void ResetWildFeatureAnimation()
  {
    if (wildAnimation != null)
    {
      wildAnimation.StopAnimation();
      wildAnimation.ResetToFirstFrame();
    }
    if (cached)
    {
      Image.rectTransform.localPosition = initialImageLocalPos;
      Image.rectTransform.sizeDelta = initialSizeDelta;
      transform.localPosition = initialRootLocalPosition;
      gameObject.SetActive(false);
    }
  }
}
