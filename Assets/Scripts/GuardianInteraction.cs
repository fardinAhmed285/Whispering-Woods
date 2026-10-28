
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class GuardianInteraction : MonoBehaviour
{
    [Header("Gemini API")]
    public string apiKey;
    public string model = "gemini-3.8-flash";

    [Header("Dialogue UI")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_InputField playerMessageInput;
    public Button sendButton;
    public ScrollRect dialogueScrollRect;

    [Header("Player Control")]
    public GameObject player;

    private bool playerNearby;
    private bool isRequesting;
    private bool dialogueOpen;

    private string conversationDisplay = "";

    private readonly List<Content> conversationHistory =
        new List<Content>();

    private readonly List<Behaviour> disabledMovementScripts =
        new List<Behaviour>();

    [Serializable]
    public class Part
    {
        public string text;
    }

    [Serializable]
    public class Content
    {
        public string role;
        public Part[] parts;
    }

    [Serializable]
    public class SystemInstruction
    {
        public Part[] parts;
    }

    [Serializable]
    public class RequestData
    {
        public SystemInstruction systemInstruction;
        public Content[] contents;
    }

    [Serializable]
    public class Candidate
    {
        public Content content;
    }

    [Serializable]
    public class ResponseData
    {
        public Candidate[] candidates;
    }

    private readonly string guardianInstructions =
        "You are Eldrin, the Forest Guardian in a cozy magical forest game. " +
        "You are a wise, friendly, mysterious spirit who protects the forest. " +
        "The forest contains magical trees, rabbits, wolves, fruit and spirits. " +
        "Help the player explore the forest, explain its mysteries, and suggest simple quests. " +
        "Speak naturally and stay in character. " +
        "Never claim the player completed an action unless they tell you so. " +
        "Keep replies under 60 words.";

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (sendButton != null)
            sendButton.onClick.AddListener(SendPlayerMessage);
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(SendPlayerMessage);

        RestorePlayerMovement();
    }

    private void Update()
    {
        if (!playerNearby || Keyboard.current == null)
            return;

        // E only opens the dialogue. It never closes it while typing.
        if (!dialogueOpen && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenDialogue();
        }

        // Escape closes the dialogue.
        if (dialogueOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseDialogue();
        }
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
            CloseDialogue();
        }
    }

    private void OpenDialogue()
    {
        dialogueOpen = true;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StopPlayerMovement();

        if (dialogueText != null && string.IsNullOrEmpty(conversationDisplay))
        {
            conversationDisplay =
                "Eldrin: Welcome, traveller. What would you like to know about the forest?";

            dialogueText.text = conversationDisplay;
        }
        else if (dialogueText != null)
        {
            dialogueText.text = conversationDisplay;
        }

        ScrollToBottom();

        if (playerMessageInput != null)
            playerMessageInput.ActivateInputField();
    }

    private void CloseDialogue()
    {
        if (!dialogueOpen)
            return;

        dialogueOpen = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        RestorePlayerMovement();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void StopPlayerMovement()
    {
        if (player == null)
            return;

        MonoBehaviour[] scripts = player.GetComponents<MonoBehaviour>();

        foreach (MonoBehaviour script in scripts)
        {
            string scriptName = script.GetType().Name;

            if (script != this &&
                script.enabled &&
                (scriptName.Contains("Movement") ||
                 scriptName.Contains("Controller") ||
                 scriptName.Contains("ThirdPerson")))
            {
                script.enabled = false;
                disabledMovementScripts.Add(script);
            }
        }
    }

    private void RestorePlayerMovement()
    {
        foreach (Behaviour script in disabledMovementScripts)
        {
            if (script != null)
                script.enabled = true;
        }

        disabledMovementScripts.Clear();
    }

    public void SendPlayerMessage()
    {
        if (!dialogueOpen || isRequesting || playerMessageInput == null)
            return;

        string message = playerMessageInput.text.Trim();

        if (string.IsNullOrEmpty(message))
            return;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            dialogueText.text = "Eldrin: My magic is not connected.";
            return;
        }

        playerMessageInput.text = "";

        conversationDisplay += "\n\nYou: " + message;
        dialogueText.text = conversationDisplay + "\n\nEldrin is thinking...";

        ScrollToBottom();

        StartCoroutine(GetGuardianResponse(message));
    }

    private IEnumerator GetGuardianResponse(string playerMessage)
    {
        isRequesting = true;

        conversationHistory.Add(new Content
        {
            role = "user",
            parts = new Part[]
            {
                new Part { text = playerMessage }
            }
        });

        RequestData requestData = new RequestData
        {
            systemInstruction = new SystemInstruction
            {
                parts = new Part[]
                {
                    new Part { text = guardianInstructions }
                }
            },
            contents = conversationHistory.ToArray()
        };

        string json = JsonUtility.ToJson(requestData);

        string url =
            "https://generativelanguage.googleapis.com/v1beta/models/"
            + model + ":generateContent";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
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
                    string reply =
                        response.candidates[0].content.parts[0].text;

                    conversationHistory.Add(new Content
                    {
                        role = "model",
                        parts = new Part[]
                        {
                            new Part { text = reply }
                        }
                    });

                    conversationDisplay += "\n\nEldrin: " + reply;
                    dialogueText.text = conversationDisplay;
                }
                else
                {
                    conversationHistory.RemoveAt(
                        conversationHistory.Count - 1);

                    conversationDisplay +=
                        "\n\nEldrin: I cannot find the words right now.";

                    dialogueText.text = conversationDisplay;

                    Debug.LogError("Gemini returned no text.");
                }
            }
            else
            {
                conversationHistory.RemoveAt(
                    conversationHistory.Count - 1);

                conversationDisplay +=
                    "\n\nEldrin: My connection is having trouble. Please try again.";

                dialogueText.text = conversationDisplay;

                Debug.LogError(
                    "Gemini API error: " +
                    request.responseCode + " " +
                    request.downloadHandler.text);
            }
        }

        ScrollToBottom();
        isRequesting = false;
    }

    private void ScrollToBottom()
    {
        if (dialogueScrollRect == null)
            return;

        Canvas.ForceUpdateCanvases();

        if (dialogueText != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                dialogueText.rectTransform);

        Canvas.ForceUpdateCanvases();

        dialogueScrollRect.verticalNormalizedPosition = 0f;
    }
}
