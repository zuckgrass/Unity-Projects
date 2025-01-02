using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UniGramUI : MonoBehaviour
{
    [Header("Telegram Bot Configuration")]
    public string telegramToken;
    public string chatID;

    [Header("UI Elements")]
    public TMP_InputField textInputField;
    public Button sendStickerButton;
    public Button sendImageButton;
    public Button setTextColorButton;
    public Button setBackgroundColorButton;
    public TMP_Dropdown fontDropdown; // Dropdown for font selection
    public TextMeshProUGUI statusText;

    [Header("Color Pickers")]
    public FlexibleColorPicker textColorPicker;
    public FlexibleColorPicker backgroundColorPicker;

    private Color textColor = Color.black; // Default text color
    private Color backgroundColor = Color.white; // Default background color
    private UniGram uniGram;
    private GameObject colorPickerCanvas;

    // Array of fonts available for selection
    public TMP_FontAsset[] availableFonts;
    private TMP_FontAsset selectedFont;

    private void Start()
    {
        // Initialize UniGram
        uniGram = FindObjectOfType<UniGram>();
        if (uniGram == null)
        {
            Debug.LogError("UniGram component not found!");
            return;
        }
        uniGram.telegramToken = telegramToken;
        uniGram.chatID = chatID;

        // Assign button click listeners
        sendStickerButton.onClick.AddListener(SendAsSticker);
        sendImageButton.onClick.AddListener(SendAsImage);

        colorPickerCanvas = GameObject.FindGameObjectWithTag("colorpicker");
        if (colorPickerCanvas != null)
        {
            colorPickerCanvas.GetComponent<Canvas>().enabled = true;
        }
        else
        {
            Debug.LogError("Canvas with tag 'colorpicker' not found!");
        }

        setTextColorButton.onClick.AddListener(() =>
        {
            textColor = textColorPicker.color; // Update text color
            statusText.text = $"Text color set to {ColorUtility.ToHtmlStringRGB(textColor)}";
        });

        setBackgroundColorButton.onClick.AddListener(() =>
        {
            backgroundColor = backgroundColorPicker.color; // Update background color
            statusText.text = $"Background color set to {ColorUtility.ToHtmlStringRGB(backgroundColor)}";
        });

        // Populate font dropdown and set listener
        PopulateFontDropdown();
        fontDropdown.onValueChanged.AddListener(OnFontSelected);
    }

    private void PopulateFontDropdown()
    {
        fontDropdown.ClearOptions();

        if (availableFonts == null || availableFonts.Length == 0)
        {
            Debug.LogError("Available fonts array is empty! Please assign fonts in the Inspector.");
            return;
        }

        foreach (var font in availableFonts)
        {
            fontDropdown.options.Add(new TMP_Dropdown.OptionData(font.name));
        }

        fontDropdown.RefreshShownValue();
        selectedFont = availableFonts[0]; // Default to the first font
    }


    private void OnFontSelected(int index)
    {
        selectedFont = availableFonts[index];
        statusText.text = $"Font set to {selectedFont.name}";
    }

    private void SendAsSticker()
    {
        if (textColor == backgroundColor)
        {
            statusText.text = "Error: Text and background colors must be different.";
            return;
        }

        string text = textInputField.text;
        if (ValidateInput(text))
        {
            string filePath = ConvertTextToSticker(text);
            uniGram.SendFileToTelegram(filePath, true);
            statusText.text = "Sticker sent successfully!";
        }
        else
        {
            statusText.text = "Error: Text input is empty!";
        }
    }

    private void SendAsImage()
    {
        if (textColor == backgroundColor)
        {
            statusText.text = "Error: Text and background colors must be different.";
            return;
        }

        string text = textInputField.text;
        if (ValidateInput(text))
        {
            string filePath = ConvertTextToImage(text);
            uniGram.SendFileToTelegram(filePath, false);
            statusText.text = "Image sent successfully!";
        }
        else
        {
            statusText.text = "Error: Text input is empty!";
        }
    }

    private bool ValidateInput(string input)
    {
        return !string.IsNullOrWhiteSpace(input);
    }

    private string ConvertTextToImage(string text)
    {
        string filePath = Application.persistentDataPath + "/image.png";
        int width = 256;
        int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Fill background
        Color[] fillColor = new Color[width * height];
        for (int i = 0; i < fillColor.Length; i++) fillColor[i] = backgroundColor;
        texture.SetPixels(fillColor);

        // Create UI for rendering
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(width, height);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.font = selectedFont;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(width, height);
        textRect.localPosition = Vector3.zero;

        Camera camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = height / 2;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor;

        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        camera.targetTexture = renderTexture;

        camera.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        GameObject.DestroyImmediate(canvasGO);
        GameObject.DestroyImmediate(camera.gameObject);

        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);

        Debug.Log($"Image created at {filePath}");
        return filePath;
    }

    private string ConvertTextToSticker(string text)
    {
        string filePath = Application.persistentDataPath + "/sticker.png";
        int width = 512; // Telegram stickers are typically 512x512 pixels
        int height = 512;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Fill background with the selected background color (ensure transparency if needed)
        Color[] fillColor = new Color[width * height];
        for (int i = 0; i < fillColor.Length; i++) fillColor[i] = backgroundColor; // Use selected background color
        texture.SetPixels(fillColor);

        // Create UI for rendering
        GameObject canvasGO = new GameObject("Canvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(width, height);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(canvasGO.transform);
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 48; // Adjusted font size for better sticker visibility
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.font = selectedFont;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(width, height);
        textRect.localPosition = Vector3.zero;

        Camera camera = new GameObject("Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -10);
        camera.orthographic = true;
        camera.orthographicSize = height / 2;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = backgroundColor; // Use selected background color

        RenderTexture renderTexture = new RenderTexture(width, height, 24);
        camera.targetTexture = renderTexture;

        camera.Render();

        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        GameObject.DestroyImmediate(canvasGO);
        GameObject.DestroyImmediate(camera.gameObject);

        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);

        Debug.Log($"Sticker created at {filePath}");
        return filePath;
    }

}