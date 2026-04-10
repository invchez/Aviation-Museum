using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.EventSystems;
using UnityEngine.UI;




public class AiTextReciever : MonoBehaviour
{
    const string GreetingMessage = "Hi, what can I help you with?";

    public TextMeshProUGUI AiResponse;
    public TMP_InputField responseField;
    public TMP_InputField userQuestionInputField;
    public AudioSource TextToSpeech;
    public AudioClip AudioClip;
    public PlaneSO plane;
    
    private string workerUrl = "https://spring-poetry-3bc5.alexli010972.workers.dev/";
    int contextVersion;
    Coroutine scrollRoutine;
    Coroutine audioRoutine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TextToSpeech = GameObject.FindWithTag("AudioSource").GetComponent<AudioSource>();

        // On first panel activation, OnEnable runs before Start. Do not clear chat here,
        // otherwise the initial greeting appended in OnEnable disappears.
        SetPlaneContext(plane, clearHistory: false);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        ShowGreetingForCurrentContext();
    }

    void ShowGreetingForCurrentContext()
    {
        if (AiResponse != null)
        {
            AiResponse.text = GreetingMessage;
        }

        AppendConversationLine("AI", GreetingMessage);
        PlayMessageAudio(GreetingMessage, contextVersion);
    }

    public void SetPlaneContext(PlaneSO activePlane, bool clearHistory = true)
    {
        plane = activePlane;

        if (clearHistory)
        {
            // New context means old pending requests are stale and should not append into this transcript.
            contextVersion++;
            StopAllCoroutines();
            scrollRoutine = null;
            audioRoutine = null;

            // Clear history on active plot/layout changes so each context starts fresh.
            if (responseField != null)
            {
                responseField.text = string.Empty;
            }

            if (AiResponse != null)
            {
                AiResponse.text = string.Empty;
            }

            if (TextToSpeech != null)
            {
                TextToSpeech.Stop();
            }

            if (isActiveAndEnabled)
            {
                ShowGreetingForCurrentContext();
                return;
            }
        }

        ScrollConversationToBottom();
    }

    void ScrollConversationToBottom()
    {
        if (responseField == null)
        {
            return;
        }

        // Avoid forcing scroll updates while this panel is inactive.
        if (!isActiveAndEnabled || !responseField.gameObject.activeInHierarchy)
        {
            return;
        }

        if (scrollRoutine != null)
        {
            StopCoroutine(scrollRoutine);
        }

        scrollRoutine = StartCoroutine(ScrollConversationToBottomNextFrame());
    }

    IEnumerator ScrollConversationToBottomNextFrame()
    {
        yield return null;

        if (responseField == null)
        {
            scrollRoutine = null;
            yield break;
        }

        // Use TMP_InputField's own scrollbar reference so scrolling behavior stays tied to the field setup.
        Canvas.ForceUpdateCanvases();
        responseField.ForceLabelUpdate();

        Scrollbar verticalScrollbar = responseField.verticalScrollbar;
        if (verticalScrollbar != null)
        {
            bool topToBottom = verticalScrollbar.direction == Scrollbar.Direction.TopToBottom;
            verticalScrollbar.value = topToBottom ? 1f : 0f;
        }
        else
        {
            // Fallback if no scrollbar is assigned: move caret to end so latest text remains visible.
            responseField.caretPosition = responseField.text.Length;
            responseField.MoveTextEnd(false);
        }

        scrollRoutine = null;
    }

    void AppendConversationLine(string speakerPrefix, string message)
    {
        if (responseField == null || string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string formattedMessage = $"{speakerPrefix}: {message.Trim()}";

        if (string.IsNullOrWhiteSpace(responseField.text))
        {
            responseField.text = formattedMessage;
            ScrollConversationToBottom();
            return;
        }

        // Keep clear chat readability between turns as requested.
        responseField.text += $"\n\n{formattedMessage}";

        ScrollConversationToBottom();
    }

    string BuildContextAwarePrompt(string playerQuestion)
    {
        if (plane == null)
        {
            // No active plane context is available, so fall back to a short generic reply.
            return $"{playerQuestion} Keep it simple and under 100 words";
        }

        string planeName = string.IsNullOrWhiteSpace(plane.Name) ? "Unknown Aircraft" : plane.Name;
        string manufacturer = string.IsNullOrWhiteSpace(plane.Manufacturer) ? "Unknown" : plane.Manufacturer;
        string topSpeed = string.IsNullOrWhiteSpace(plane.TSpeed) ? "Unknown" : plane.TSpeed;
        string wingSpan = string.IsNullOrWhiteSpace(plane.WingSpan) ? "Unknown" : plane.WingSpan;
        string details = string.IsNullOrWhiteSpace(plane.Information) ? "No additional details provided." : plane.Information;

        // This keeps topic handling flexible and semantic instead of relying on rigid keyword combinations.
        return
            "You are an aviation museum assistant helping a visitor.\n" +
            "Active aircraft context:\n" +
            $"- Name: {planeName}\n" +
            $"- Manufacturer: {manufacturer}\n" +
            $"- Top Speed: {topSpeed}\n" +
            $"- Wing Span: {wingSpan}\n" +
            $"- Details: {details}\n\n" +
            $"Visitor question: {playerQuestion}\n\n" +
            "Instructions:\n" +
            "- Answer in under 100 words.\n" +
            "- If the question is clearly related to this active aircraft or aviation museum context, answer normally.\n" +
            "- If it is unrelated, respond exactly with: Please stay on topic.";
    }

    void ClearUserInputField(TMP_InputField activeInputField)
    {
        // Prefer an explicitly assigned question input field from the inspector.
        TMP_InputField targetInputField = userQuestionInputField;

        // Fallback: use the currently selected TMP input field, but never clear the response history field.
        if (targetInputField == null && activeInputField != null && activeInputField != responseField)
        {
            targetInputField = activeInputField;
        }

        if (targetInputField == null || targetInputField == responseField)
        {
            return;
        }

        // Avoid triggering extra value-change callbacks while clearing submitted text.
        targetInputField.SetTextWithoutNotify(string.Empty);
        targetInputField.caretPosition = 0;
    }

    public void PlayerInput(string NewInput)
    {
        TMP_InputField activeInputField = null;
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
        {
            activeInputField = EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>();
            EventSystem.current.SetSelectedGameObject(null);
        }

        string trimmedInput = NewInput?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedInput)){
            return;
        }

        Debug.Log(trimmedInput);
        AiResponse.text = trimmedInput;
        AppendConversationLine("User", trimmedInput);

        int expectedContextVersion = contextVersion;
        StartCoroutine(UserQuestionChecker(trimmedInput, expectedContextVersion));
        ClearUserInputField(activeInputField);
        // StartCoroutine(AskChatGPT(NewInput));
        
        
    }
    


    

    void PlayMessageAudio(string message, int expectedContextVersion)
    {
        if (audioRoutine != null)
        {
            StopCoroutine(audioRoutine);
        }

        audioRoutine = StartCoroutine(GetAudioFile(message, expectedContextVersion));
    }

    public IEnumerator AskChatGPT(string prompt, int expectedContextVersion = -1)
    {
        if (expectedContextVersion < 0)
        {
            expectedContextVersion = contextVersion;
        }

        string AdjustedPrompt = prompt;
        var jsonBody = JsonUtility.ToJson(new RequestBody(AdjustedPrompt));
        using (UnityWebRequest req = new UnityWebRequest(workerUrl + "playeraiquestion", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("x-app-secret", "test_secret");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success){
                if (expectedContextVersion != contextVersion)
                {
                    yield break;
                }

                Choices response = JsonUtility.FromJson<Choices>(req.downloadHandler.text);
                string aiMessage = response.choices[0].message.content;
                AiResponse.text = aiMessage;
                AppendConversationLine("AI", aiMessage);
                PlayMessageAudio(aiMessage, expectedContextVersion);
                
            } 

            else
                Debug.LogError(req.error);
        }
    }
    

    IEnumerator GetAudioFile(string inputtext, int expectedContextVersion = -1)
    {
        if (expectedContextVersion < 0)
        {
            expectedContextVersion = contextVersion;
        }

        TextToSpeechRequest SpeechRequest = new TextToSpeechRequest{
            text = inputtext
        };
        string json = JsonUtility.ToJson(SpeechRequest);
        byte[] JsonByte = System.Text.Encoding.UTF8.GetBytes(json);
        using (UnityWebRequest req = new UnityWebRequest(workerUrl + "playerquestionaudio", "POST")){
            req.uploadHandler = new UploadHandlerRaw(JsonByte);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("x-app-secret", "test_secret");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success){
                if (expectedContextVersion != contextVersion)
                {
                    yield break;
                }

                string path = Path.Combine(Application.persistentDataPath, "TextToSpeech.mp3");
                File.WriteAllBytes(path, req.downloadHandler.data);
                using(UnityWebRequest AudioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG)){
                    yield return AudioRequest.SendWebRequest();
                    if (expectedContextVersion != contextVersion)
                    {
                        yield break;
                    }

                    AudioClip ReturnedAudioClip = DownloadHandlerAudioClip.GetContent(AudioRequest);
                    if (TextToSpeech == null)
                    {
                        GameObject sourceObject = GameObject.FindWithTag("AudioSource");
                        if (sourceObject != null)
                        {
                            TextToSpeech = sourceObject.GetComponent<AudioSource>();
                        }
                    }

                    if (TextToSpeech == null)
                    {
                        yield break;
                    }

                    TextToSpeech.clip = ReturnedAudioClip;
                    TextToSpeech.Play();
                }
            }
        }    
    }

    IEnumerator UserQuestionChecker(string playertextinput, int expectedContextVersion = -1)
    {
        if (expectedContextVersion < 0)
        {
            expectedContextVersion = contextVersion;
        }

        string loweredinput = playertextinput.ToLower();

        if (plane != null && plane.FAQS != null)
        {
            for (int i = 0; i < plane.FAQS.Count; i++) {
                if (loweredinput.Contains(plane.FAQS[i].keyword)) {
                    if (expectedContextVersion != contextVersion)
                    {
                        yield break;
                    }

                    AiResponse.text = plane.FAQS[i].answer;
                    AppendConversationLine("AI", plane.FAQS[i].answer);
                    yield break;
                }
            }
        }

        string contextAwarePrompt = BuildContextAwarePrompt(playertextinput);
        StartCoroutine(AskChatGPT(contextAwarePrompt, expectedContextVersion));
        yield break;
    }


    [System.Serializable]
    class TextToSpeechRequest
    {
        public string model = "gpt-4o-mini-tts";
        public string text;
        public string voice = "coral";
    }

    [System.Serializable]
    public class RequestBody
    {
        public string message;
        public RequestBody(string msg) { message = msg; }
    }

    [System.Serializable]
    public class TopicRequestBody
    {
        public string message;
        public string topic;
        public TopicRequestBody(string msg, string t){ message = msg; topic = t; }
    }


    [System.Serializable]
    public class Choices{
        public Message[] choices;
        
    }

    [System.Serializable]
    public class Message{
        public Content message;

    }

    [System.Serializable]
    public class Content{
        public string content;

    }

}
