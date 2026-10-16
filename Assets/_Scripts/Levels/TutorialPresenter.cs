using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialPresenter : MonoBehaviour
{
    private LevelDefinition _level;
    private int _playerMoves;
    private TMP_Text _messageText;
    private GameObject _banner;

    public void Begin(LevelDefinition level)
    {
        _level = level;
        _playerMoves = 0;
        EnsureBanner();
        UpdateMessage();
    }

    public void OnPlayerMovePlaced()
    {
        _playerMoves++;
        UpdateMessage();
    }

    public void Hide()
    {
        if (_banner != null)
            _banner.SetActive(false);
    }

    private void EnsureBanner()
    {
        if (_banner != null)
            return;

        GameObject canvasObject = new GameObject("TutorialCanvas");
        canvasObject.transform.SetParent(null, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        _banner = new GameObject("TutorialBanner", typeof(RectTransform), typeof(Image));
        _banner.transform.SetParent(canvasObject.transform, false);
        RectTransform bannerRect = _banner.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -24f);
        bannerRect.sizeDelta = new Vector2(560f, 106f);
        Image bannerImage = _banner.GetComponent<Image>();
        bannerImage.color = new Color(0.11f, 0.16f, 0.24f, 0.94f);
        bannerImage.raycastTarget = false;

        GameObject label = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(_banner.transform, false);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 16f);
        labelRect.offsetMax = new Vector2(-24f, -16f);
        _messageText = label.GetComponent<TextMeshProUGUI>();
        _messageText.fontSize = 26f;
        _messageText.color = Color.white;
        _messageText.alignment = TextAlignmentOptions.Center;
        _messageText.enableWordWrapping = true;
        _messageText.raycastTarget = false;
    }

    private void UpdateMessage()
    {
        if (_messageText == null)
            return;
        if (_level == null || _level.tutorialSteps == null)
        {
            _banner.SetActive(false);
            return;
        }

        TutorialStep activeStep = null;
        foreach (TutorialStep step in _level.tutorialSteps)
        {
            if (step.afterPlayerMoves <= _playerMoves &&
                (activeStep == null || step.afterPlayerMoves >= activeStep.afterPlayerMoves))
                activeStep = step;
        }

        if (activeStep == null)
        {
            _banner.SetActive(false);
            return;
        }

        _messageText.text = activeStep.message;
        _banner.SetActive(!string.IsNullOrWhiteSpace(activeStep.message));
    }
}
