using System;
using UnityEngine;
using UnityEngine.UI;

namespace SoundAds.UI
{
    /// <summary>
    /// Non-intrusive ad banner pinned to the top safe area: "AD" badge, advertiser and title,
    /// countdown, progress bar and (Standard ads) a Mute button that unlocks after the mute delay.
    /// Built entirely in code so the SDK has no prefab/TMP dependency on the host project.
    /// Only the banner itself receives raycasts; the rest of the game stays interactive.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class AdOverlayView : MonoBehaviour
    {
        static readonly Color PanelColor = new(0.07f, 0.07f, 0.10f, 0.94f);
        static readonly Color AccentColor = new(1.00f, 0.78f, 0.18f, 1f);
        static readonly Color TrackColor = new(1f, 1f, 1f, 0.15f);
        static readonly Color MutedButtonColor = new(1f, 1f, 1f, 0.12f);

        public event Action MuteClicked;

        RectTransform _safeArea;
        Text _title;
        Text _subtitle;
        RectTransform _progressFill;
        Button _muteButton;
        Text _muteLabel;
        Image _muteImage;
        AdFormat _format;
        bool _lowVolumeHint;
        Rect _appliedSafeArea;

        /// <summary>True while the banner is on screen (tests use this).</summary>
        public bool IsVisible => gameObject.activeSelf;

        /// <summary>The mute button, exposed for tests and automation.</summary>
        public Button MuteButton => _muteButton;

        public static AdOverlayView Create(Transform parent)
        {
            var root = new GameObject("SoundAdsOverlay", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            root.AddComponent<GraphicRaycaster>();

            var view = root.AddComponent<AdOverlayView>();
            view.Build((RectTransform)root.transform);
            root.SetActive(false);
            return view;
        }

        void Build(RectTransform root)
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _safeArea = NewRect("SafeArea", root);
            Stretch(_safeArea);

            // Banner: top of the safe area, full width, fixed height.
            var panel = NewRect("Banner", _safeArea);
            panel.anchorMin = new Vector2(0f, 1f);
            panel.anchorMax = new Vector2(1f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.sizeDelta = new Vector2(-40f, 190f);
            panel.anchoredPosition = new Vector2(0f, -20f);
            panel.gameObject.AddComponent<Image>().color = PanelColor;

            // "AD" badge
            var badge = NewRect("Badge", panel);
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 0.5f);
            badge.pivot = new Vector2(0f, 0.5f);
            badge.sizeDelta = new Vector2(90f, 60f);
            badge.anchoredPosition = new Vector2(28f, 8f);
            badge.gameObject.AddComponent<Image>().color = AccentColor;
            var badgeText = NewText("Label", badge, font, 34, FontStyle.Bold, TextAnchor.MiddleCenter);
            badgeText.text = "AD";
            badgeText.color = Color.black;
            Stretch(badgeText.rectTransform);

            // Title + subtitle
            _title = NewText("Title", panel, font, 38, FontStyle.Bold, TextAnchor.LowerLeft);
            SetHorizontalBand(_title.rectTransform, 140f, 260f, 0.52f, 0.92f);

            _subtitle = NewText("Subtitle", panel, font, 30, FontStyle.Normal, TextAnchor.UpperLeft);
            _subtitle.color = new Color(1f, 1f, 1f, 0.75f);
            SetHorizontalBand(_subtitle.rectTransform, 140f, 260f, 0.14f, 0.50f);

            // Mute button (right)
            _muteButton = NewRect("MuteButton", panel).gameObject.AddComponent<Button>();
            var muteRect = (RectTransform)_muteButton.transform;
            muteRect.anchorMin = muteRect.anchorMax = new Vector2(1f, 0.5f);
            muteRect.pivot = new Vector2(1f, 0.5f);
            muteRect.sizeDelta = new Vector2(210f, 90f);
            muteRect.anchoredPosition = new Vector2(-28f, 8f);
            _muteImage = _muteButton.gameObject.AddComponent<Image>();
            _muteButton.targetGraphic = _muteImage;
            _muteLabel = NewText("Label", muteRect, font, 30, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(_muteLabel.rectTransform);
            _muteButton.onClick.AddListener(() => MuteClicked?.Invoke());

            // Progress bar (bottom edge of the banner)
            var track = NewRect("ProgressTrack", panel);
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.sizeDelta = new Vector2(0f, 10f);
            track.gameObject.AddComponent<Image>().color = TrackColor;

            _progressFill = NewRect("Fill", track);
            _progressFill.anchorMin = Vector2.zero;
            _progressFill.anchorMax = new Vector2(0f, 1f);
            _progressFill.offsetMin = _progressFill.offsetMax = Vector2.zero;
            _progressFill.gameObject.AddComponent<Image>().color = AccentColor;
        }

        public void Show(AdResponse ad, AdFormat format, bool lowVolumeHint)
        {
            _format = format;
            _lowVolumeHint = lowVolumeHint;
            _title.text = string.IsNullOrEmpty(ad.advertiser) ? ad.title : $"{ad.advertiser} · {ad.title}";
            _muteButton.gameObject.SetActive(format == AdFormat.Standard);
            ApplySafeArea();
            UpdateProgress(0f, ad.durationSec, format == AdFormat.Standard ? float.MaxValue : -1f);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <param name="progress">0-1.</param>
        /// <param name="remainingSec">Seconds left in the ad.</param>
        /// <param name="muteInSec">Seconds until mute unlocks; 0 = available; negative = not applicable.</param>
        public void UpdateProgress(float progress, float remainingSec, float muteInSec)
        {
            _progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);

            string countdown = $"0:{Mathf.CeilToInt(remainingSec):00}";
            if (_format == AdFormat.Rewarded)
            {
                _subtitle.text = _lowVolumeHint
                    ? $"Turn up your volume to earn the reward · {countdown}"
                    : $"Listen to the end to earn your reward · {countdown}";
            }
            else
            {
                _subtitle.text = $"Audio ad · {countdown}";
            }

            if (muteInSec < 0f) return;

            bool canMute = muteInSec <= 0f;
            _muteButton.interactable = canMute;
            _muteImage.color = canMute ? AccentColor : MutedButtonColor;
            _muteLabel.color = canMute ? Color.black : new Color(1f, 1f, 1f, 0.7f);
            _muteLabel.text = canMute ? "MUTE" : $"Mute in {Mathf.CeilToInt(Mathf.Min(muteInSec, 99f))}";
        }

        void Update()
        {
            if (Screen.safeArea != _appliedSafeArea) ApplySafeArea(); // rotation / notch changes
        }

        void ApplySafeArea()
        {
            var area = Screen.safeArea;
            _appliedSafeArea = area;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            _safeArea.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            _safeArea.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            _safeArea.offsetMin = _safeArea.offsetMax = Vector2.zero;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Text NewText(string name, Transform parent, Font font, int size, FontStyle style, TextAnchor anchor)
        {
            var text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void SetHorizontalBand(RectTransform rect, float left, float right, float yMin, float yMax)
        {
            rect.anchorMin = new Vector2(0f, yMin);
            rect.anchorMax = new Vector2(1f, yMax);
            rect.offsetMin = new Vector2(left, 0f);
            rect.offsetMax = new Vector2(-right, 0f);
        }
    }
}
