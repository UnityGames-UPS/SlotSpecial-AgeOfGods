using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AutoSpinPanelController : MonoBehaviour
{
  [Header("Refs")]
  [SerializeField] private RectTransform panelRoot;
  [SerializeField] private CanvasGroup buttonsCanvasGroup;
  [SerializeField] private Button autoSpinButton;
  [SerializeField] private RectTransform autoSpinButtonRect;
  [SerializeField] private Button[] spinCountButtons;
  [SerializeField] private TMP_Text[] spinCountButtonTexts;
  [SerializeField] private GameObject autoSpinStopParent;
  [SerializeField] private TMP_Text stopCountText;
  [SerializeField] private GameObject stopCountTextGO;
  [SerializeField] private GameObject stopInfinityTextGO;
  [SerializeField] private GameManager gameManager;

  [Header("Layout")]
  [SerializeField] private float visibleLocalY = -15.05f;
  [SerializeField] private float hiddenLocalY = -315.8f;
  [SerializeField] private float animDuration = 0.2f;

  [Header("Text Colors")]
  [SerializeField] private Color textNormalPressed = Color.white;
  [SerializeField] private Color textHover = Color.yellow;
  [SerializeField] private Color textDisabled = Color.gray;

  [Header("Spin counts")]
  [SerializeField] private int[] spinCounts = { 5, 10, 20, 50, 100, -1 };

  private bool _isOpen;
  private bool _isAnimating;
  private bool _hoverOnButton;
  private bool _hoverOnPanel;
  private bool _isMobileOrTablet;
  private Coroutine _deferredCloseRoutine;
  private Tween _slideTween;
  private TMP_Text[][] _perButtonTexts;

  void Awake()
  {
    if (panelRoot != null)
    {
      var p = panelRoot.localPosition;
      p.y = hiddenLocalY;
      panelRoot.localPosition = p;
    }
    SetButtonsInteractable(false);
    if (autoSpinStopParent != null) autoSpinStopParent.SetActive(false);
  }

  void Start()
  {
    _isMobileOrTablet = JSFunctCalls.IsMobileOrTablet();

    _perButtonTexts = new TMP_Text[spinCountButtons.Length][];
    for (int i = 0; i < spinCountButtons.Length; i++)
    {
      int idx = i;
      var btn = spinCountButtons[i];
      if (btn == null) continue;
      _perButtonTexts[i] = btn.GetComponentsInChildren<TMP_Text>(true);
      btn.onClick.RemoveAllListeners();
      btn.onClick.AddListener(() => StartCoroutine(OnCountSelected(idx)));
      AttachButtonHoverHandlers(btn, idx);
    }

    ApplyTextColors(textNormalPressed);
    if (autoSpinButton != null) AttachAutoSpinButtonHandlers();
    if (panelRoot != null) AttachPanelHoverHandlers();
  }

  void AttachAutoSpinButtonHandlers()
  {
    var trigger = autoSpinButton.gameObject.GetComponent<EventTrigger>();
    if (trigger == null) trigger = autoSpinButton.gameObject.AddComponent<EventTrigger>();
    trigger.triggers.Clear();

    AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
    {
      if (_isMobileOrTablet) return;
      if (!autoSpinButton.interactable) return;
      _hoverOnButton = true;
      TryOpen();
    });
    AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
    {
      if (_isMobileOrTablet) return;
      _hoverOnButton = false;
      ScheduleDeferredClose();
    });
    AddTrigger(trigger, EventTriggerType.PointerClick, _ =>
    {
      if (!_isMobileOrTablet) return;
      if (!autoSpinButton.interactable) return;
      if (_isAnimating) return;
      if (_isOpen) Close();
      else Open();
    });
  }

  void AttachPanelHoverHandlers()
  {
    var trigger = panelRoot.gameObject.GetComponent<EventTrigger>();
    if (trigger == null) trigger = panelRoot.gameObject.AddComponent<EventTrigger>();
    trigger.triggers.Clear();

    AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
    {
      if (_isMobileOrTablet) return;
      if (autoSpinButton != null && !autoSpinButton.interactable) return;
      _hoverOnPanel = true;
      TryOpen();
    });
    AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
    {
      if (_isMobileOrTablet) return;
      _hoverOnPanel = false;
      ScheduleDeferredClose();
    });
  }

  void AttachButtonHoverHandlers(Button btn, int idx)
  {
    var trigger = btn.gameObject.GetComponent<EventTrigger>();
    if (trigger == null) trigger = btn.gameObject.AddComponent<EventTrigger>();
    trigger.triggers.Clear();

    AddTrigger(trigger, EventTriggerType.PointerEnter, _ =>
    {
      if (btn.interactable) ColorButtonTexts(idx, textHover);
    });
    AddTrigger(trigger, EventTriggerType.PointerExit, _ =>
    {
      if (btn.interactable) ColorButtonTexts(idx, textNormalPressed);
    });
    AddTrigger(trigger, EventTriggerType.PointerDown, _ =>
    {
      if (btn.interactable) ColorButtonTexts(idx, textNormalPressed);
    });
  }

  static void AddTrigger(EventTrigger trigger, EventTriggerType type, System.Action<BaseEventData> cb)
  {
    var entry = new EventTrigger.Entry { eventID = type };
    entry.callback.AddListener(d => cb(d));
    trigger.triggers.Add(entry);
  }

  void TryOpen()
  {
    if (_isOpen || _isAnimating) return;
    Open();
  }

  void ScheduleDeferredClose()
  {
    if (_deferredCloseRoutine != null) StopCoroutine(_deferredCloseRoutine);
    _deferredCloseRoutine = StartCoroutine(DeferredCloseCheck());
  }

  IEnumerator DeferredCloseCheck()
  {
    yield return null;
    yield return null;
    if (!_hoverOnButton && !_hoverOnPanel && _isOpen && !_isAnimating)
      Close();
    _deferredCloseRoutine = null;
  }

  void Open()
  {
    if (_isAnimating || _isOpen) return;
    _isAnimating = true;
    SetButtonsInteractable(false);
    KillSlideTween();
    _slideTween = panelRoot.DOLocalMoveY(visibleLocalY, animDuration).SetEase(Ease.OutCubic).OnComplete(() =>
    {
      _isOpen = true;
      _isAnimating = false;
      SetButtonsInteractable(true);
      if (!_isMobileOrTablet && !_hoverOnButton && !_hoverOnPanel) Close();
    });
  }

  void Close()
  {
    if (_isAnimating || !_isOpen) return;
    _isAnimating = true;
    SetButtonsInteractable(false);
    KillSlideTween();
    _slideTween = panelRoot.DOLocalMoveY(hiddenLocalY, animDuration).SetEase(Ease.InCubic).OnComplete(() =>
    {
      _isOpen = false;
      _isAnimating = false;
    });
  }

  IEnumerator CloseAndWait()
  {
    if (!_isOpen && !_isAnimating) yield break;
    if (_isAnimating)
    {
      KillSlideTween();
      _isAnimating = false;
    }
    _isAnimating = true;
    SetButtonsInteractable(false);
    yield return panelRoot.DOLocalMoveY(hiddenLocalY, animDuration).SetEase(Ease.InCubic).WaitForCompletion();
    _isOpen = false;
    _isAnimating = false;
  }

  void KillSlideTween()
  {
    if (_slideTween != null && _slideTween.IsActive())
    {
      _slideTween.Kill();
      _slideTween = null;
    }
  }

  IEnumerator OnCountSelected(int idx)
  {
    if (_isAnimating) yield break;
    SetButtonsInteractable(false);
    if (gameManager != null) gameManager.ToggleButtonGrp(false);
    int count = spinCounts[idx];
    yield return CloseAndWait();
    if (gameManager != null) gameManager.StartAutoSpin(count);
  }

  void SetButtonsInteractable(bool on)
  {
    if (buttonsCanvasGroup != null)
    {
      buttonsCanvasGroup.interactable = on;
      buttonsCanvasGroup.blocksRaycasts = on;
    }
    for (int i = 0; i < spinCountButtons.Length; i++)
    {
      if (spinCountButtons[i] != null) spinCountButtons[i].interactable = on;
    }
    ApplyTextColors(on ? textNormalPressed : textDisabled);
  }

  void ApplyTextColors(Color c)
  {
    for (int i = 0; i < spinCountButtonTexts.Length; i++)
    {
      if (spinCountButtonTexts[i] != null) spinCountButtonTexts[i].color = c;
    }
  }

  void ColorButtonTexts(int idx, Color c)
  {
    if (_perButtonTexts == null || idx < 0 || idx >= _perButtonTexts.Length) return;
    var arr = _perButtonTexts[idx];
    if (arr == null) return;
    for (int i = 0; i < arr.Length; i++)
    {
      if (arr[i] != null) arr[i].color = c;
    }
  }

  internal void CloseIfOpen()
  {
    if (!_isOpen || _isAnimating) return;
    Close();
  }

  internal void ShowStopButton(int countOrMinusOne)
  {
    if (autoSpinStopParent != null) autoSpinStopParent.SetActive(true);
    bool infinity = countOrMinusOne < 0;
    if (stopCountTextGO != null) stopCountTextGO.SetActive(!infinity);
    if (stopInfinityTextGO != null) stopInfinityTextGO.SetActive(infinity);
    if (!infinity && stopCountText != null) stopCountText.text = countOrMinusOne.ToString();
  }

  internal void UpdateStopCount(int remaining)
  {
    if (remaining < 0) return;
    if (stopCountText != null && stopCountTextGO != null && stopCountTextGO.activeSelf)
      stopCountText.text = remaining.ToString();
  }

  internal void HideStopButton()
  {
    if (autoSpinStopParent != null) autoSpinStopParent.SetActive(false);
  }
}
