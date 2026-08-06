using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class WheelView : MonoBehaviour
{
  Tween rotationTween;
  [SerializeField] internal CanvasGroup canvasGroup;
  [SerializeField] internal int targetIndex;
  [SerializeField] internal WheelItem[] wheelItems;
  [SerializeField] private bool isStatic = true;
  [SerializeField] private float staticRotationDuration;
  [SerializeField] private float spinStartSpeed = 360f;
  [SerializeField] private float minStopDuration = 3f;
  [SerializeField] private float winHighlightFadeDuration = 0.5f;

  private WheelItem activeWinItem;

  void Awake()
  {
    InitializeWinHighlights();
  }

  void InitializeWinHighlights()
  {
    if (wheelItems == null) return;
    foreach (var item in wheelItems)
    {
      if (item == null) continue;
      if (item.winningTriangleShine != null)
      {
        item.winningTriangleShine.doLoopAnimation = false;
        item.winningTriangleShine.StartOnAwake = false;
        SetGraphicAlpha(item.winningTriangleShine, 0f);
        item.winningTriangleShine.gameObject.SetActive(false);
      }
      if (item.winningBorderLoop != null)
      {
        item.winningBorderLoop.doLoopAnimation = true;
        item.winningBorderLoop.StartOnAwake = false;
        SetGraphicAlpha(item.winningBorderLoop, 0f);
        item.winningBorderLoop.gameObject.SetActive(false);
      }
      if (item.winningHighlightRoot != null)
      {
        item.winningHighlightRoot.gameObject.SetActive(false);
      }
    }
  }

  internal static void SetGraphicAlpha(ImageAnimation anim, float a)
  {
    if (anim == null || anim.rendererDelegate == null) return;
    var c = anim.rendererDelegate.color;
    c.a = a;
    anim.rendererDelegate.color = c;
  }

  void Start()
  {
    if (isStatic)
    {
      rotationTween = transform.DOLocalRotate(new Vector3(0, 0, -360), staticRotationDuration, RotateMode.FastBeyond360)
        .SetLoops(-1, LoopType.Incremental)
        .SetEase(Ease.Linear);
    }
    ValidateWheelItems();
  }

  internal void PlayWinHighlight()
  {
    WheelItem target = null;
    foreach (var it in wheelItems)
    {
      if (it != null && it.index == targetIndex) { target = it; break; }
    }
    if (target == null)
    {
      Debug.LogError($"[WheelView:{name}] PlayWinHighlight: no wheelItem with index={targetIndex}");
      return;
    }

    activeWinItem = target;

    if (target.winningHighlightRoot != null)
      target.winningHighlightRoot.gameObject.SetActive(true);

    PlayShineWithFade(target.winningTriangleShine);
    PlayShineWithFade(target.winningBorderLoop);
  }

  internal bool IsTriangleShinePlaying()
  {
    return activeWinItem != null
        && activeWinItem.winningTriangleShine != null
        && activeWinItem.winningTriangleShine.isplaying;
  }

  void PlayShineWithFade(ImageAnimation anim)
  {
    if (anim == null) return;
    anim.gameObject.SetActive(true);
    SetGraphicAlpha(anim, 0f);
    anim.rendererDelegate.DOFade(1f, winHighlightFadeDuration);
    anim.StartAnimation();
  }

  internal void StopWinHighlight()
  {
    if (activeWinItem != null)
    {
      if (activeWinItem.winningTriangleShine != null)
      {
        activeWinItem.winningTriangleShine.StopAnimation();
        SetGraphicAlpha(activeWinItem.winningTriangleShine, 0f);
        activeWinItem.winningTriangleShine.gameObject.SetActive(false);
      }
      if (activeWinItem.winningBorderLoop != null)
      {
        activeWinItem.winningBorderLoop.StopAnimation();
        SetGraphicAlpha(activeWinItem.winningBorderLoop, 0f);
        activeWinItem.winningBorderLoop.gameObject.SetActive(false);
      }
      if (activeWinItem.winningHighlightRoot != null)
        activeWinItem.winningHighlightRoot.gameObject.SetActive(false);
      activeWinItem = null;
    }
  }

  void ValidateWheelItems()
  {
    if (wheelItems == null || wheelItems.Length == 0) return;
    var seenIndices = new HashSet<int>();
    var duplicates = new List<int>();
    for (int i = 0; i < wheelItems.Length; i++)
    {
      var it = wheelItems[i];
      if (it == null) { Debug.LogError($"[WheelView:{name}] wheelItems[{i}] is null"); continue; }
      if (string.IsNullOrWhiteSpace(it.type))
        Debug.LogError($"[WheelView:{name}] wheelItems[{i}] has empty type (index={it.index})");
      if (!seenIndices.Add(it.index)) duplicates.Add(it.index);
    }
    if (duplicates.Count > 0)
    {
      Debug.LogError($"[WheelView:{name}] duplicate WheelItem.index values: {string.Join(", ", duplicates)} — every segment must have a unique index.");
    }
  }

  internal void PopulateValues(List<FeatureValue> values)
  {
    if (wheelItems == null || values == null) return;

    int count = values.Count;
    for (int i = 0; i < count; i++)
    {
      string type = values[i].type.ToUpper();
      bool placed = false;
      for (int j = 0; j < wheelItems.Length; j++)
      {
        if (wheelItems[j] == null || wheelItems[j].type == null) continue;
        if (type == wheelItems[j].type.ToUpper() && wheelItems[j].value == 0)
        {
          wheelItems[j].value = values[i].value;
          placed = true;
          break;
        }
      }
      if (!placed)
      {
        Debug.LogWarning($"[WheelView:{name}] PopulateValues could not place server entry type='{values[i].type}', value={values[i].value} — no wheelItem with matching type and value==0.");
      }
    }
  }

  internal void ResetRotation()
  {
    rotationTween?.Kill(false);
    transform.localEulerAngles = Vector3.zero;
  }

  internal void SpinTheWheel()
  {
    rotationTween?.Kill(false);
    float loopDuration = 360f / spinStartSpeed;
    rotationTween = transform.DOLocalRotate(new Vector3(0, 0, -360), loopDuration, RotateMode.FastBeyond360)
      .SetLoops(-1, LoopType.Incremental)
      .SetEase(Ease.Linear);
  }

  internal IEnumerator StopWheel()
  {
    WheelItem target = null;
    foreach (var it in wheelItems)
    {
      if (it != null && it.index == targetIndex) { target = it; break; }
    }
    if (target == null)
    {
      Debug.LogError($"[WheelView:{name}] StopWheel: no wheelItem with index={targetIndex}; aborting stop.");
      yield break;
    }

    rotationTween?.Kill(false);

    float currentZ = transform.localEulerAngles.z;
    float rawStopAngle = UnityEngine.Random.Range(target.minStopAngle, target.maxStopAngle);
    float stopAngle = Mathf.Repeat(rawStopAngle, 360f);
    float alignDelta = (currentZ - stopAngle + 360f) % 360f;
    if (alignDelta < 1f) alignDelta = 360f;

    // Linear deceleration (Ease.OutQuad). v(0) = 2Δ/T; with T fixed by minStopDuration,
    // Δ = v0·T/2. We snap Δ up to the next full-rotation boundary that still lands on
    // stopAngle, then recompute T so v(0) matches spinStartSpeed (smooth handoff).
    float minTotalDelta = spinStartSpeed * minStopDuration / 2f;
    float fullRotations = Mathf.Ceil((minTotalDelta - alignDelta) / 360f);
    if (fullRotations < 1f) fullRotations = 1f;
    float totalDelta = fullRotations * 360f + alignDelta;
    float duration = 2f * totalDelta / spinStartSpeed;
    float endZ = currentZ - totalDelta;

    yield return transform.DOLocalRotate(
        new Vector3(0, 0, endZ),
        duration,
        RotateMode.FastBeyond360
    ).SetEase(Ease.OutQuad).WaitForCompletion();

    Debug.Log($"[WheelView:{name}] Stopped at z={transform.localEulerAngles.z:F2} (target stopAngle={stopAngle:F2}, index={targetIndex}, type='{target.type}', value={target.value})");
  }
}

[Serializable]
public class WheelItem
{
  public string type = "";
  public int index = 0;
  public double value;
  public float minStopAngle;
  public float maxStopAngle;
  public bool isLevelUp;
  public Transform winningHighlightRoot;
  public ImageAnimation winningTriangleShine;
  public ImageAnimation winningBorderLoop;
}
