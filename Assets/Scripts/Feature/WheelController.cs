using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WheelController : MonoBehaviour
{
  [SerializeField] private AudioController audioController;
  [SerializeField] internal WheelView UISmallWheel;
  [SerializeField] internal WheelView UIMediumWheel;
  [SerializeField] internal WheelView UILargeWheel;
  [SerializeField] internal WheelView SmallWheel;
  [SerializeField] internal WheelView MediumWheel;
  [SerializeField] internal WheelView LargeWheel;
  [SerializeField] internal CanvasGroup WheelParentCG;
  [SerializeField] private Sprite[] InARowSprites;
  [SerializeField] private Image InARowImage;
  [SerializeField] private float WheelRotationDuration = 2f;
  [SerializeField] private ImageAnimation centerShine;
  [SerializeField] internal bool canPlayAudio = false;
  private WheelView UIActiveWheel;

  void Awake()
  {
    if (centerShine != null)
    {
      centerShine.doLoopAnimation = false;
      centerShine.StartOnAwake = false;
      WheelView.SetGraphicAlpha(centerShine, 0f);
      centerShine.gameObject.SetActive(false);
    }
    StartCoroutine(ResetWheelsPanel());
  }

  void PlayCenterShine()
  {
    if (centerShine == null) return;
    centerShine.gameObject.SetActive(true);
    WheelView.SetGraphicAlpha(centerShine, 1f);
    centerShine.StartAnimation();
  }

  void StopCenterShine()
  {
    if (centerShine == null) return;
    centerShine.StopAnimation();
    WheelView.SetGraphicAlpha(centerShine, 0f);
    centerShine.gameObject.SetActive(false);
  }

  IEnumerator WaitForWinShineComplete(WheelView wheel)
  {
    // Wait for the non-looping shines to actually start (first Invoke fires inside ImageAnimation),
    // bounded by a grace period in case they never run (missing references, etc).
    float grace = 0.6f;
    float t = 0f;
    while (t < grace && !AnyNonLoopShinePlaying(wheel))
    {
      t += Time.deltaTime;
      yield return null;
    }
    while (AnyNonLoopShinePlaying(wheel))
    {
      yield return null;
    }
  }

  bool AnyNonLoopShinePlaying(WheelView wheel)
  {
    if (centerShine != null && centerShine.isplaying) return true;
    if (wheel != null && wheel.IsTriangleShinePlaying()) return true;
    return false;
  }

  internal void PopulateWheelValues(Bonus bonus)
  {
    if (SmallWheel != null)
      SmallWheel.PopulateValues(bonus.smallWheelFeature.featureValues);
    if (MediumWheel != null)
      MediumWheel.PopulateValues(bonus.mediumWheelFeature.featureValues);
    if (LargeWheel != null)
      LargeWheel.PopulateValues(bonus.largeWheelFeature.featureValues);
  }

  internal IEnumerator PlayWheel(WheelBonus wheelBonus)
  {
    var chain = (wheelBonus.wheelTypeChain != null && wheelBonus.wheelTypeChain.Count > 0)
      ? wheelBonus.wheelTypeChain
      : new List<string> { wheelBonus.wheelType };

    string terminalType = chain[chain.Count - 1];
    if (!string.IsNullOrEmpty(wheelBonus.wheelType) &&
        !terminalType.Equals(wheelBonus.wheelType, StringComparison.OrdinalIgnoreCase))
    {
      Debug.LogWarning($"[WheelController] wheelType='{wheelBonus.wheelType}' disagrees with last chain entry '{terminalType}'; trusting chain.");
    }

    for (int i = 0; i < chain.Count; i++)
    {
      bool isFirst = (i == 0);
      bool isLast = (i == chain.Count - 1);
      WheelView wheel = ResolveWheel(chain[i], out WheelView uiWheel, out Sprite inARowSprite);
      if (wheel == null)
      {
        Debug.LogError("Invalid wheel type in chain: " + chain[i]);
        yield break;
      }

      if (!isFirst) wheel.canvasGroup.alpha = 0f;
      else wheel.canvasGroup.alpha = 1f;
      wheel.gameObject.SetActive(true);

      canPlayAudio=true;
      wheel.SpinTheWheel();
      // audioController.Play("wheel");

      if (isFirst)
      {
        UIActiveWheel = uiWheel;
        InARowImage.sprite = inARowSprite;
        UIActiveWheel.canvasGroup.DOFade(0, 0.5f);
        WheelParentCG.DOFade(1, 0.5f).OnComplete(() =>
        {
          WheelParentCG.interactable = true;
          WheelParentCG.blocksRaycasts = true;
        });
      }
      else
      {
        WheelView prevWheel = ResolveWheel(chain[i - 1], out WheelView prevUIWheel, out _);
        wheel.canvasGroup.DOFade(1, 0.5f);
        if (prevUIWheel != null) prevUIWheel.canvasGroup.DOFade(1, 0.5f);
        if (uiWheel != null) uiWheel.canvasGroup.DOFade(0, 0.5f);
        yield return new WaitForSeconds(0.5f);
        if (prevWheel != null)
        {
          prevWheel.StopWinHighlight();
          prevWheel.ResetRotation();
          prevWheel.gameObject.SetActive(false);
        }
        StopCenterShine();
        InARowImage.sprite = inARowSprite;
        UIActiveWheel = uiWheel;
      }

      wheel.targetIndex = isLast
        ? FindTargetIndex(wheel, wheelBonus)
        : FindLevelUpIndex(wheel);

      yield return StartCoroutine(wheel.StopWheel());
      canPlayAudio = false;
      audioController.Play("wheel_win");
      wheel.PlayWinHighlight();
      PlayCenterShine();
      yield return WaitForWinShineComplete(wheel);

      if (!isLast)
      {
        yield return new WaitForSeconds(0.5f);
      }
    }

    yield return new WaitForSeconds(2f);
    yield return ResetWheelsPanel();
  }

  WheelView ResolveWheel(string type, out WheelView uiWheel, out Sprite inARowSprite)
  {
    switch ((type ?? "").ToLower())
    {
      case "small":
        uiWheel = UISmallWheel;
        inARowSprite = InARowSprites[0];
        return SmallWheel;
      case "medium":
        uiWheel = UIMediumWheel;
        inARowSprite = InARowSprites[1];
        return MediumWheel;
      case "large":
        uiWheel = UILargeWheel;
        inARowSprite = InARowSprites[2];
        return LargeWheel;
    }
    uiWheel = null;
    inARowSprite = null;
    return null;
  }

  int FindLevelUpIndex(WheelView wheel)
  {
    foreach (var item in wheel.wheelItems)
    {
      if (item != null && item.isLevelUp) return item.index;
    }
    Debug.LogError($"[WheelController] No level-up segment found on wheel '{wheel.name}'. Mark at least one WheelItem.isLevelUp = true.");
    return -1;
  }

  int FindTargetIndex(WheelView wheel, WheelBonus bonus)
  {
    foreach (var item in wheel.wheelItems)
    {
      if (item == null) continue;

      if (item.type.Equals(bonus.featureType, StringComparison.OrdinalIgnoreCase)
          && item.value == bonus.featureValue)
      {
        return item.index;
      }
    }

    var dump = new System.Text.StringBuilder();
    dump.Append($"[WheelController] No wheelItem matched bonus: wheelType='{bonus.wheelType}', featureType='{bonus.featureType}', featureValue={bonus.featureValue}. Available items on '{wheel.name}':");
    for (int i = 0; i < wheel.wheelItems.Length; i++)
    {
      var it = wheel.wheelItems[i];
      if (it == null) { dump.Append($"\n  [{i}] <null>"); continue; }
      dump.Append($"\n  [{i}] index={it.index} type='{it.type}' value={it.value}");
    }
    Debug.LogError(dump.ToString());
    return -1;
  }

  IEnumerator ResetWheelsPanel()
  {
    if (UIActiveWheel != null)
    {
      UIActiveWheel.canvasGroup.DOFade(1, 0.5f);
      UIActiveWheel = null;
    }

    yield return WheelParentCG.DOFade(0, 0.5f).OnComplete(() =>
    {
      WheelParentCG.interactable = false;
      WheelParentCG.blocksRaycasts = false;
      StopCenterShine();
      SmallWheel.StopWinHighlight();
      MediumWheel.StopWinHighlight();
      LargeWheel.StopWinHighlight();
      SmallWheel.gameObject.SetActive(false);
      MediumWheel.gameObject.SetActive(false);
      LargeWheel.gameObject.SetActive(false);
      SmallWheel.ResetRotation();
      MediumWheel.ResetRotation();
      LargeWheel.ResetRotation();
    }).WaitForCompletion();
  }
}
