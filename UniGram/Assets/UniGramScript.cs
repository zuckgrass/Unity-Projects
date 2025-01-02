using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class UniGram : MonoBehaviour
{
    [Header("Telegram Bot Configuration")]
    public string telegramToken;
    public string chatID;

    /// <summary>
    /// Sends a message to a Telegram chat.
    /// </summary>
    /// <param name="message">Text message to send.</param>
    public void SendMessageToTelegram(string message)
    {
        string url = $"https://api.telegram.org/bot{telegramToken}/sendMessage";
        StartCoroutine(SendRequest(url, $"chat_id={chatID}&text={message}"));
    }

    /// <summary>
    /// Sends an image or sticker to a Telegram chat.
    /// </summary>
    /// <param name="filePath">Path to the image or sticker file.</param>
    /// <param name="isSticker">Set true if sending a sticker; otherwise, false for an image.</param>
    public void SendFileToTelegram(string filePath, bool isSticker = false)
    {
        string url = isSticker
            ? $"https://api.telegram.org/bot{telegramToken}/sendSticker"
            : $"https://api.telegram.org/bot{telegramToken}/sendPhoto";

        StartCoroutine(UploadFile(url, filePath));
    }

    private IEnumerator SendRequest(string url, string parameters)
    {
        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(url, parameters))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error sending message: {request.error}");
            }
            else
            {
                Debug.Log("Message sent successfully.");
            }
        }
    }

    private IEnumerator UploadFile(string url, string filePath)
    {
        WWWForm form = new WWWForm();
        form.AddField("chat_id", chatID);

        // Determine the correct field name based on the URL
        string fileFieldName = url.Contains("sendSticker") ? "sticker" : "photo";
        form.AddBinaryData(fileFieldName, System.IO.File.ReadAllBytes(filePath), System.IO.Path.GetFileName(filePath));

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error uploading file: {request.error}");
            }
            else
            {
                Debug.Log("File sent successfully.");
            }
        }
    }

}
