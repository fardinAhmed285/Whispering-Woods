
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using TMPro;

public class GuardianInteraction : MonoBehaviour
{
    [Header("Gemini API")]
    public string apiKey;
    public string model = "gemini-3.8-flash";

    [Header("Dialogue")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    private bool playerNearby;
    private bool isRequesting;

    [Serializable]
    private class RequestData
    {
        public ContentData[] contents;
    }

    [Serializable]
    private class ContentData
    {
        public PartData[] parts;
    }

    [Serializable]
    private class PartData
    {
        public string text;
    }

    [Serializable]
    private class ResponseData
    {
        public CandidateData[] candidates;
    }

    [Serializable]
    private class CandidateData
    {
        public ContentData content;
    }

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            playerNearby = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;

            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerNearby &&
            !isRequesting &&
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(true);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                dialogueText.text = "Guardian: My magic is not connected yet.";
                return;
            }

            StartCoroutine(GetGuardianGreeting());
        }
    }

    private IEnumerator GetGuardianGreeting()
    {
        isRequesting = true;

        dialogueText.text = "The Guardian is thinking...";

        string prompt =
            "You are the Forest Guardian in a cozy magical forest game. " +
            "Speak as a mysterious, friendly forest spirit. " +
            "Welcome the player and offer one short clue about the forest. " +
            "Keep your response under 50 words.";

        RequestData requestData = new RequestData
        {
            contents = new ContentData[]
            {
                new ContentData
                {
                    parts = new PartData[]
                    {
                        new PartData { text = prompt }
                    }
                }
            }
        };

        string json = JsonUtility.ToJson(requestData);

        string url =
            "https://generativelanguage.googleapis.com/v1beta/models/"
            + model + ":generateContent";

        using (UnityWebRequest request =
               new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler =
                new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));

            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("x-goog-api-key", apiKey);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ResponseData response =
                    JsonUtility.FromJson<ResponseData>(
                        request.downloadHandler.text);

                if (response != null &&
                    response.candidates != null &&
                    response.candidates.Length > 0 &&
                    response.candidates[0].content != null &&
                    response.candidates[0].content.parts != null &&
                    response.candidates[0].content.parts.Length > 0)
                {
                    dialogueText.text =
                        "Guardian: " +
                        response.candidates[0].content.parts[0].text;
                }
                else
                {
                    dialogueText.text =
                        "Guardian: I cannot find the words right now.";
                    Debug.LogError("Gemini returned no text.");
                }
            }
            else
            {
                dialogueText.text =
                    "Guardian: My connection to the forest has failed.";

                Debug.LogError(
                    "Gemini API error: " +
                    request.responseCode + " " +
                    request.downloadHandler.text);
            }
        }

        isRequesting = false;
    }
}
