using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace MinimalMenu
{
    public class MainMenuBuilder : MonoBehaviour
    {
        [Header("Modern Platformer Palette")]
        [SerializeField] private Sprite backgroundImage; // Added: Assign UI Sprite here
        [SerializeField] private Color backgroundColor   = new Color(0.15f, 0.16f, 0.25f);
        [SerializeField] private Color panelColor        = new Color(0.20f, 0.22f, 0.32f); 
        [SerializeField] private Color textColor         = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] private Color accentColor       = new Color(1.00f, 0.85f, 0.30f);
        [SerializeField] private Color buttonIdleColor   = new Color(0.25f, 0.65f, 0.85f);
        [SerializeField] private Color buttonHoverColor  = new Color(0.35f, 0.75f, 0.95f);
        [SerializeField] private Color shadowColor       = new Color(0.05f, 0.05f, 0.10f, 1f);

        [Header("Content & Levels")]
        [SerializeField] private string gameTitle = "JUMP & RUN";
        [SerializeField] private string level1SceneName = "Level_01";
        [SerializeField] private string level2SceneName = "Level_02";
        [SerializeField] private string level3SceneName = "Level_03";
        [SerializeField] private string level4SceneName = "Level_04";

        [Header("Fonts")]
        [SerializeField] private TMP_FontAsset titleFont;
        [SerializeField] private TMP_FontAsset bodyFont;

        private RectTransform _aboutPanel;
        private RectTransform _levelPanel;

        private void Awake()
        {
            EnsureEventSystem();
            var canvas = BuildCanvas();
            BuildBackground(canvas);

            var controller = gameObject.AddComponent<MainMenuController>();
            controller.FirstLevelSceneName = level1SceneName;
            controller.Fader = BuildFader(canvas);

            BuildTitle(canvas);
            var buttonColumn = BuildButtonColumn(canvas);

            // Main Menu Buttons
            var playButton   = BuildButton(buttonColumn, "START");
            var levelsButton = BuildButton(buttonColumn, "LEVELS");
            var aboutButton  = BuildButton(buttonColumn, "ABOUT");
            var quitButton   = BuildButton(buttonColumn, "QUIT");

            // Build Sub-Panels
            _aboutPanel = BuildAboutPanel(canvas, out Button aboutBackButton);
            _levelPanel = BuildLevelPanel(canvas, controller, out Button levelBackButton);

            // Event Wiring
            playButton.onClick.AddListener(controller.OnPlayPressed);
            levelsButton.onClick.AddListener(() => _levelPanel.gameObject.SetActive(true));
            aboutButton.onClick.AddListener(() => _aboutPanel.gameObject.SetActive(true));
            quitButton.onClick.AddListener(controller.OnQuitPressed);

            aboutBackButton.onClick.AddListener(() => _aboutPanel.gameObject.SetActive(false));
            levelBackButton.onClick.AddListener(() => _levelPanel.gameObject.SetActive(false));
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private Canvas BuildCanvas()
        {
            var canvasGO = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private void BuildBackground(Canvas canvas)
        {
            var bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(canvas.transform, false);
            StretchFull(bg.GetComponent<RectTransform>());

            var img = bg.GetComponent<Image>();
            if (backgroundImage != null)
            {
                img.sprite = backgroundImage;
                img.color = Color.white;
            }
            else
            {
                img.color = backgroundColor;
            }
        }

        private ScreenFader BuildFader(Canvas canvas)
        {
            var faderGO = new GameObject("ScreenFader", typeof(Image), typeof(CanvasGroup));
            faderGO.transform.SetParent(canvas.transform, false);
            StretchFull(faderGO.GetComponent<RectTransform>());

            var img = faderGO.GetComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;

            var group = faderGO.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            faderGO.transform.SetAsLastSibling();
            return faderGO.AddComponent<ScreenFader>();
        }

        private void BuildTitle(Canvas canvas)
        {
            var titleGO = new GameObject("Title", typeof(TextMeshProUGUI), typeof(Shadow));
            titleGO.transform.SetParent(canvas.transform, false);
            var rt = titleGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -120);
            rt.sizeDelta = new Vector2(1000, 200);

            var tmp = titleGO.GetComponent<TextMeshProUGUI>();
            tmp.text = gameTitle;
            tmp.fontSize = 110;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = accentColor;
            tmp.fontStyle = FontStyles.Bold;
            if (titleFont != null) tmp.font = titleFont;

            var shadow = titleGO.GetComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(8, -8);
        }

        private RectTransform BuildButtonColumn(Canvas canvas)
        {
            var columnGO = new GameObject("ButtonColumn", typeof(RectTransform), typeof(VerticalLayoutGroup));
            columnGO.transform.SetParent(canvas.transform, false);
            var rt = columnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -60);
            rt.sizeDelta = new Vector2(360, 360);

            var layout = columnGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = true;
            layout.childControlHeight = false;

            return rt;
        }

        private Button BuildButton(RectTransform parent, string label)
        {
            var buttonGO = new GameObject(label + "Button", typeof(Image), typeof(Button), typeof(LayoutElement), typeof(Shadow));
            buttonGO.transform.SetParent(parent, false);

            buttonGO.GetComponent<LayoutElement>().preferredHeight = 65;

            var image = buttonGO.GetComponent<Image>();
            image.color = buttonIdleColor;

            var shadow = buttonGO.GetComponent<Shadow>();
            shadow.effectColor = shadowColor;
            shadow.effectDistance = new Vector2(6, -6);

            var button = buttonGO.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            var hover = buttonGO.AddComponent<ButtonHoverEffect>();
            hover.Setup(image, buttonIdleColor, buttonHoverColor, accentColor);

            var textGO = new GameObject("Label", typeof(TextMeshProUGUI), typeof(Shadow));
            textGO.transform.SetParent(buttonGO.transform, false);
            StretchFull(textGO.GetComponent<RectTransform>());

            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            if (bodyFont != null) tmp.font = bodyFont;

            var textShadow = textGO.GetComponent<Shadow>();
            textShadow.effectColor = new Color(0, 0, 0, 0.3f);
            textShadow.effectDistance = new Vector2(2, -2);

            return button;
        }

        private RectTransform BuildLevelPanel(Canvas canvas, MainMenuController controller, out Button backButton)
        {
            var panelGO = new GameObject("LevelPanel", typeof(Image), typeof(Shadow));
            panelGO.transform.SetParent(canvas.transform, false);
            var rt = panelGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640, 520);
            panelGO.GetComponent<Image>().color = panelColor;

            var panelShadow = panelGO.GetComponent<Shadow>();
            panelShadow.effectColor = shadowColor;
            panelShadow.effectDistance = new Vector2(10, -10);

            panelGO.SetActive(false);

            // Title
            var titleGO = new GameObject("LevelTitle", typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -30);
            titleRT.sizeDelta = new Vector2(500, 50);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "SELECT LEVEL";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = accentColor;
            titleTMP.fontSize = 40;
            titleTMP.fontStyle = FontStyles.Bold;

            // 2x2 Grid Container for Levels
            var gridGO = new GameObject("LevelGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGO.transform.SetParent(panelGO.transform, false);
            var gridRT = gridGO.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridRT.anchorMax = new Vector2(0.5f, 0.5f);
            gridRT.pivot = new Vector2(0.5f, 0.5f);
            gridRT.anchoredPosition = new Vector2(0, 10);
            gridRT.sizeDelta = new Vector2(480, 220);

            var grid = gridGO.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(225, 80);
            grid.spacing = new Vector2(25, 25);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            string[] levelNames = { level1SceneName, level2SceneName, level3SceneName, level4SceneName };

            for (int i = 0; i < 4; i++)
            {
                int levelNum = i + 1;
                string sceneName = levelNames[i];
                var levelBtn = BuildButton(gridRT, $"LEVEL {levelNum}");
                levelBtn.onClick.AddListener(() => controller.OnLevelPressed(sceneName));
            }

            // Back Button
            var backGO = new GameObject("BackButton", typeof(Image), typeof(Button), typeof(Shadow));
            backGO.transform.SetParent(panelGO.transform, false);
            var backRT = backGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.pivot = new Vector2(0.5f, 0f);
            backRT.anchoredPosition = new Vector2(0, 35);
            backRT.sizeDelta = new Vector2(200, 55);

            var backImg = backGO.GetComponent<Image>();
            backImg.color = new Color(0.85f, 0.25f, 0.35f);

            var backShadow = backGO.GetComponent<Shadow>();
            backShadow.effectColor = shadowColor;
            backShadow.effectDistance = new Vector2(5, -5);

            backButton = backGO.GetComponent<Button>();
            backGO.AddComponent<ButtonHoverEffect>().Setup(backImg, new Color(0.85f, 0.25f, 0.35f), new Color(0.95f, 0.35f, 0.45f), accentColor);

            var backLabelGO = new GameObject("Label", typeof(TextMeshProUGUI));
            backLabelGO.transform.SetParent(backGO.transform, false);
            StretchFull(backLabelGO.GetComponent<RectTransform>());
            var backLabelTMP = backLabelGO.GetComponent<TextMeshProUGUI>();
            backLabelTMP.text = "RETURN";
            backLabelTMP.alignment = TextAlignmentOptions.Center;
            backLabelTMP.color = textColor;
            backLabelTMP.fontSize = 24;
            backLabelTMP.fontStyle = FontStyles.Bold;

            return rt;
        }

        private RectTransform BuildAboutPanel(Canvas canvas, out Button backButton)
        {
            var panelGO = new GameObject("AboutPanel", typeof(Image), typeof(Shadow));
            panelGO.transform.SetParent(canvas.transform, false);
            var rt = panelGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(640, 520);
            panelGO.GetComponent<Image>().color = panelColor;
            
            var panelShadow = panelGO.GetComponent<Shadow>();
            panelShadow.effectColor = shadowColor;
            panelShadow.effectDistance = new Vector2(10, -10);

            panelGO.SetActive(false);

            var titleGO = new GameObject("AboutTitle", typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(panelGO.transform, false);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 1f);
            titleRT.anchorMax = new Vector2(0.5f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0, -25);
            titleRT.sizeDelta = new Vector2(500, 50);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = "ABOUT";
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.color = accentColor;
            titleTMP.fontSize = 40;
            titleTMP.fontStyle = FontStyles.Bold;

            var descGO = new GameObject("DescriptionText", typeof(TextMeshProUGUI));
            descGO.transform.SetParent(panelGO.transform, false);
            var descRT = descGO.GetComponent<RectTransform>();
            descRT.anchorMin = new Vector2(0.5f, 0.5f);
            descRT.anchorMax = new Vector2(0.5f, 0.5f);
            descRT.pivot = new Vector2(0.5f, 0.5f);
            descRT.anchoredPosition = new Vector2(0, 15);
            descRT.sizeDelta = new Vector2(560, 330);
            var descTMP = descGO.GetComponent<TextMeshProUGUI>();
            
            descTMP.text = "A thrilling 2D platformer adventure.\nDeveloped by Minesweeper.\n\n<color=#FFD700>CONTROLS</color>\n[A] & [D] - Move\n[SPACE] - Jump\n[K] - Active Ball Throw Mode\nHold [SPACE] - Boost Throw Power\n[MOUSE CLICK] - In-Game Action";
            
            descTMP.alignment = TextAlignmentOptions.Center;
            descTMP.color = textColor;
            descTMP.fontSize = 21;
            descTMP.lineSpacing = 10;

            var backGO = new GameObject("BackButton", typeof(Image), typeof(Button), typeof(Shadow));
            backGO.transform.SetParent(panelGO.transform, false);
            var backRT = backGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.5f, 0f);
            backRT.anchorMax = new Vector2(0.5f, 0f);
            backRT.pivot = new Vector2(0.5f, 0f);
            backRT.anchoredPosition = new Vector2(0, 35);
            backRT.sizeDelta = new Vector2(200, 55);
            
            var backImg = backGO.GetComponent<Image>();
            backImg.color = new Color(0.85f, 0.25f, 0.35f);
            
            var backShadow = backGO.GetComponent<Shadow>();
            backShadow.effectColor = shadowColor;
            backShadow.effectDistance = new Vector2(5, -5);

            backButton = backGO.GetComponent<Button>();
            backGO.AddComponent<ButtonHoverEffect>().Setup(backImg, new Color(0.85f, 0.25f, 0.35f), new Color(0.95f, 0.35f, 0.45f), accentColor);

            var backLabelGO = new GameObject("Label", typeof(TextMeshProUGUI));
            backLabelGO.transform.SetParent(backGO.transform, false);
            StretchFull(backLabelGO.GetComponent<RectTransform>());
            var backLabelTMP = backLabelGO.GetComponent<TextMeshProUGUI>();
            backLabelTMP.text = "RETURN";
            backLabelTMP.alignment = TextAlignmentOptions.Center;
            backLabelTMP.color = textColor;
            backLabelTMP.fontSize = 24;
            backLabelTMP.fontStyle = FontStyles.Bold;

            return rt;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}