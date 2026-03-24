using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using UnityEngine.EventSystems;




public class AiTextReciever : MonoBehaviour
{

    public TextMeshProUGUI AiResponse;
    public TMP_InputField responseField;
    public AudioSource TextToSpeech;
    public AudioClip AudioClip;
    public string questioncompare;
    public string AircraftType;
    public PlaneSO plane;
    
    private string workerUrl = "https://spring-poetry-3bc5.alexli010972.workers.dev/";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string path = Path.Combine(Application.persistentDataPath, "topic.txt");
        TextToSpeech = GameObject.FindWithTag("AudioSource").GetComponent<AudioSource>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnEnable()
    {
        StartCoroutine(GetAudioFile("Hi, what can I help you with?"));
    }

    public void PlayerInput(string NewInput)
    {
        EventSystem.current.SetSelectedGameObject(null);

        if (NewInput == ""){
            return;
        }

        Debug.Log(NewInput);
        AiResponse.text = NewInput;
        responseField.text = NewInput;

        StartCoroutine(UserQuestionChecker(NewInput));
        // StartCoroutine(AskChatGPT(NewInput));
        
        
    }
    


    

    public IEnumerator AskChatGPT(string prompt)
    {
        string AdjustedPrompt = $"{prompt} Keep it simple and under 100 words";
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
                Choices response = JsonUtility.FromJson<Choices>(req.downloadHandler.text);
                AiResponse.text = response.choices[0].message.content;
                responseField.text = response.choices[0].message.content;
                StartCoroutine(GetAudioFile(AiResponse.text));
                
            } 

            else
                Debug.LogError(req.error);
        }
    }
    

    IEnumerator GetAudioFile(string inputtext)
    {
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
                string path = Path.Combine(Application.persistentDataPath, "TextToSpeech.mp3");
                File.WriteAllBytes(path, req.downloadHandler.data);
                using(UnityWebRequest AudioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG)){
                    yield return AudioRequest.SendWebRequest();
                    AudioClip ReturnedAudioClip = DownloadHandlerAudioClip.GetContent(AudioRequest);
                    TextToSpeech.clip = ReturnedAudioClip;
                    TextToSpeech.Play();
                }
            }
        }    
    }

    IEnumerator UserQuestionChecker(string playertextinput)
    {
        string loweredinput = playertextinput.ToLower();

        for (int i = 0; i < plane.FAQS.Count; i++) {
            if (loweredinput.Contains(plane.FAQS[i].keyword)) {
                AiResponse.text = plane.FAQS[i].answer;
                responseField.text = plane.FAQS[i].answer;
                yield break;
            }
        }

        var jsonBody = JsonUtility.ToJson(new TopicRequestBody(playertextinput, questioncompare));
        using (UnityWebRequest req = new UnityWebRequest(workerUrl + "playerquestionchecker", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("x-app-secret", "test_secret");
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success){
                Debug.Log(req.downloadHandler.text);
                if (req.downloadHandler.text == "true"){
                    StartCoroutine(AskChatGPT(playertextinput + AircraftType));
                }
                else {
                    AiResponse.text = "Please stay on topic.";
                    responseField.text = "Please stay on topic.";
                }

                // File.WriteAllText(path, req.downloadHandler.text);

            } 
            else
                Debug.LogError(req.error);
        }
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
