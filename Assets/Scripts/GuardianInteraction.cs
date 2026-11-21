
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
    public TMP_Text guardianMessageText;
    public TMP_Text dialogueText;
    public TMP_InputField playerMessageInput;
    public Button sendButton;
    public ScrollRect dialogueScrollRect;

    [Header("Player Control")]
    public GameObject player;

    [Header("Game State")]
    public CampfireManager campfireManager;
    public WolfWander wolf;
    public WolfHealth wolfHealth;

    private bool playerNearby;
    private bool isRequesting;
    private bool dialogueOpen;

    private enum GuardianState
    {
        Idle,
        Warning,
        WolfConcern,
        CampfireGuidance,
        Progress,
        Victory
    }

    private GuardianState currentState = GuardianState.Idle;

    [Header("Guardian AI")]
    public float detectionRadius = 15f;
    public float eventCheckInterval = 2f;

    public float automaticMessageCooldown = 15f;
    private float nextAutomaticMessageTime;
    public float automaticMessageDisplayTime = 6f;

    private float eventCheckTimer;
    private bool victoryDialogueTriggered;
    private int lastKnownLitCampfires;

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

        if (campfireManager != null)
            lastKnownLitCampfires = campfireManager.LitCampfires;
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(SendPlayerMessage);

        RestorePlayerMovement();
    }

    private void Update()
    {
        // Check the guardian's AI at regular intervals.
        eventCheckTimer += Time.deltaTime;

        if (eventCheckTimer >= eventCheckInterval)
        {
            eventCheckTimer = 0f;

            GuardianState newState = EvaluateGuardianState();

            if (newState != currentState)
            {
                currentState = newState;

                if (newState != GuardianState.Idle)
                {
                    Debug.Log("Eldrin FSM entered state: " + newState);

                    if (!dialogueOpen &&
                        !isRequesting &&
                        Time.time >= nextAutomaticMessageTime)
                    {
                        nextAutomaticMessageTime =
                            Time.time + automaticMessageCooldown;

                        StartCoroutine(GetAutomaticGuardianResponse(newState));
                    }
                }
            }
        }
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

    private string GetGameState()
    {
        StringBuilder state = new StringBuilder();

        // Campfire progress
        if (campfireManager != null)
        {
            state.AppendLine(
                $"Campfires lit: {campfireManager.LitCampfires}/{campfireManager.totalCampfires}");

            state.AppendLine(
                $"Campfires remaining: {CampfireInteraction.UnlitCampfireCount}");
        }

        // Living ghosts
        EnemyHealth[] ghosts =
            FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        int livingGhosts = 0;

        foreach (EnemyHealth ghost in ghosts)
        {
            if (ghost != null && ghost.currentHealth > 0)
                livingGhosts++;
        }

        state.AppendLine($"Living ghosts: {livingGhosts}");

        // Wolf status
        if (wolf != null)
        {
            state.AppendLine($"Wolf is companion: {wolf.IsCompanion}");
            state.AppendLine($"Wolf behaviour: {wolf.currentState}");
            state.AppendLine($"Wolf hunger: {wolf.Hunger:F0}/100");
        }

        if (wolfHealth != null)
        {
            state.AppendLine(
                $"Wolf health: {wolfHealth.currentHealth:F0}/{wolfHealth.maxHealth:F0}");

            state.AppendLine($"Wolf is wounded: {wolfHealth.IsWounded}");
        }

        return state.ToString();
    }

    private GuardianState EvaluateGuardianState()
    {
        if (player == null)
            return GuardianState.Idle;

        // 1. All campfires restored
        if (campfireManager != null &&
            campfireManager.LitCampfires >= campfireManager.totalCampfires)
        {
            return GuardianState.Victory;
        }

        // 2. Ghosts near the player
        EnemyHealth[] ghosts =
            FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);

        foreach (EnemyHealth ghost in ghosts)
        {
            if (ghost != null &&
                ghost.currentHealth > 0 &&
                Vector3.Distance(player.transform.position,
                                 ghost.transform.position) <= detectionRadius)
            {
                return GuardianState.Warning;
            }
        }

        // 3. Wolf needs protection
        if (wolfHealth != null && wolfHealth.IsWounded)
            return GuardianState.WolfConcern;

        // 4. A new campfire has been restored
        if (campfireManager != null &&
            campfireManager.LitCampfires > lastKnownLitCampfires)
        {
            lastKnownLitCampfires = campfireManager.LitCampfires;
            return GuardianState.Progress;
        }

        // 5. Player is near an unlit campfire
        CampfireInteraction[] campfires =
            FindObjectsByType<CampfireInteraction>(FindObjectsSortMode.None);

        foreach (CampfireInteraction campfire in campfires)
        {
            if (campfire != null &&
                !campfire.IsLit &&
                Vector3.Distance(player.transform.position,
                                 campfire.transform.position) <= detectionRadius)
            {
                return GuardianState.CampfireGuidance;
            }
        }

        return GuardianState.Idle;
    }

    private string GetStatePrompt(GuardianState state)
    {
        switch (state)
        {
            case GuardianState.Warning:
                return "Warn the player that ghosts are nearby. Give a brief survival warning.";

            case GuardianState.WolfConcern:
                return "The wolf companion is wounded. Advise the player to protect it.";

            case GuardianState.CampfireGuidance:
                return "The player is near an unlit campfire. Encourage them to restore it.";

            case GuardianState.Progress:
                return "The player has restored another campfire. Acknowledge their progress.";

            case GuardianState.Victory:
                return "All campfires have been restored. Deliver a meaningful victory message.";

            default:
                return "";
        }
    }

    private IEnumerator GetAutomaticGuardianResponse(GuardianState state)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            yield break;

        isRequesting = true;

        string prompt =
            GetStatePrompt(state) +
            "\n\nCURRENT GAME STATE:\n" +
            GetGameState() +
            "\nSpeak as Eldrin. Use only the supplied game information. " +
            "Keep the message under 35 words. Do not ask the player a question.";

        RequestData requestData = new RequestData
        {
            systemInstruction = new SystemInstruction
            {
                parts = new Part[]
                {
                new Part { text = guardianInstructions }
                }
            },
            contents = new Content[]
            {
            new Content
            {
                role = "user",
                parts = new Part[]
                {
                    new Part { text = prompt }
                }
            }
            }
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

                    if (guardianMessageText != null)
                    {
                        guardianMessageText.text = "Eldrin: " + reply;

                        CancelInvoke(nameof(ClearGuardianMessage));
                        Invoke(nameof(ClearGuardianMessage), automaticMessageDisplayTime);
                    }

                    Debug.Log("Eldrin: " + reply);
                }
                else
                {
                    Debug.LogError("Eldrin received an empty automatic response.");
                }
            }
            else
            {
                Debug.LogError(
                    "Automatic Gemini error: " +
                    request.responseCode + " " +
                    request.downloadHandler.text);
            }
        }

        isRequesting = false;
    }

    private void ClearGuardianMessage()
    {
        if (guardianMessageText != null)
            guardianMessageText.text = "";
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
                  new Part
                  {
                   text = guardianInstructions +
                           "\n\nCURRENT GAME STATE:\n" +
                           GetGameState() +
                           "\nUse this game state when answering. " +
                           "Treat it as the source of truth. " +
                           "Never invent game events or claim something happened " +
                           "unless the game state or player confirms it."
                  }
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
