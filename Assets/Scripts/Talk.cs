using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class DialogueManager : MonoBehaviour {
    public GameObject dialogueUI;
    public Image hintUI;
    public Text dialogueText;
    public Text choiceText; // 選択肢テキスト表示用
    public float letterDelay = 0.05f;
    public string dialogueFilePath;
    
    // キー設定
    public KeyCode interactKey = KeyCode.E;
    public KeyCode optionYesKey = KeyCode.Y;
    public KeyCode optionNoKey = KeyCode.N;
    
    // 表示終了インジケータのカスタマイズ用
    public string indicatorSymbol = "▽";
    public float indicatorBlinkSpeed = 0.5f;
    
    // テキスト表示に関する追加設定
    public AudioSource typingSound;
    public bool playTypingSound = false;
    
    private DialogueData dialogueData;
    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private bool skipTyping = false;
    private bool isPlayerInside = false;
    private bool isTalking = false;
    private bool isWaitingForChoice = false;
    private Coroutine blinkCoroutine = null;
    private PlayerControl playerControl;
    private DialogueOption[] currentOptions = null;

    private void Start() {
        LoadDialogueData();
        dialogueUI.SetActive(false);
        hintUI.gameObject.SetActive(false);
        if (choiceText != null) {
            choiceText.gameObject.SetActive(false);
        }
    }

    private void LoadDialogueData() {
        string filePath = Path.Combine(Application.streamingAssetsPath, dialogueFilePath);
        if (File.Exists(filePath)) {
            string jsonData = File.ReadAllText(filePath);
            dialogueData = JsonUtility.FromJson<DialogueData>(jsonData);
            Debug.Log($"Loaded {dialogueData.dialogues.Length} dialogue entries from {filePath}");
        } else {
            Debug.LogError("Dialogue file not found: " + filePath);
        }
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            ShowInteractionHint(true);
            Debug.Log("Player entered the trigger area.");
            playerControl = other.gameObject.GetComponent<PlayerControl>();
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player")) {
            isPlayerInside = false;
            EndDialogue();
            ShowInteractionHint(false);
        }
    }

    private void ShowInteractionHint(bool show) {
        if (hintUI != null) {
            hintUI.gameObject.SetActive(show);
        }
    }

    void Update() {
        if (!isPlayerInside) return;

        // 会話開始
        if (Input.GetKeyDown(interactKey) && !isTalking) {
            StartDialogue();
            return;
        }

        if (isTalking) {
            // テキスト表示中のスキップ
            if (isTyping && Input.GetKeyDown(interactKey)) {
                skipTyping = true;
                return;
            }

            // 選択肢の処理
            if (isWaitingForChoice) {
                HandleChoiceInput();
                return;
            }

            // 次のテキストへ進む（選択肢待ちでない場合のみ）
            if (!isTyping && !isWaitingForChoice && Input.GetKeyDown(interactKey)) {
                DisplayNextSentence();
            }
        }
    }

    private void HandleChoiceInput() {
        if (Input.GetKeyDown(optionYesKey)) {
            // 「はい」を選択
            SelectOption(0); // 最初の選択肢（「はい」）
        } else if (Input.GetKeyDown(optionNoKey)) {
            // 「いいえ」を選択
            SelectOption(1); // 2番目の選択肢（「いいえ」）
        }
    }

    private void SelectOption(int optionIndex) {
        if (currentOptions == null || optionIndex >= currentOptions.Length) {
            return;
        }

        // 選択肢表示を非表示に
        isWaitingForChoice = false;
        if (choiceText != null) {
            choiceText.gameObject.SetActive(false);
        }

        DialogueOption selectedOption = currentOptions[optionIndex];
        
        // 選択肢のインデックスに基づいて次のダイアログへジャンプ
        currentDialogueIndex = selectedOption.nextDialogueIndex;
        
        // 選択肢固有のアクションがあれば実行
        if (!string.IsNullOrEmpty(selectedOption.action)) {
            PerformAction(selectedOption.action);
        }
        
        // 次のセリフへ進む
        DisplayNextSentence();
    }

    private void StartDialogue() {
        if (dialogueData == null || dialogueData.dialogues.Length == 0) {
            Debug.LogError("No dialogue data loaded!");
            return;
        }
        
        ShowInteractionHint(false);
        dialogueUI.SetActive(true);
        isTalking = true;
        isWaitingForChoice = false;
        
        if (playerControl != null) {
            playerControl.DisableMovement();
        }
        
        currentDialogueIndex = 0;
        DisplayNextSentence();
    }

    private void EndDialogue() {
        dialogueUI.SetActive(false);
        isTalking = false;
        isWaitingForChoice = false;
        
        if (choiceText != null) {
            choiceText.gameObject.SetActive(false);
        }
        
        if (playerControl != null) {
            playerControl.EnableMovement();
        }
        
        ResetDialogue();

        if (isPlayerInside) {
            ShowInteractionHint(true);
        }
    }

    private void ResetDialogue() {
        isTyping = false;
        currentDialogueIndex = 0;
        currentOptions = null;
        
        if (blinkCoroutine != null) {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    private void DisplayNextSentence() {
        if (dialogueData != null && currentDialogueIndex < dialogueData.dialogues.Length) {
            DialogueEntry entry = dialogueData.dialogues[currentDialogueIndex];
            StartCoroutine(TypeSentence(entry));
            currentDialogueIndex++;
        } else {
            EndDialogue();
        }
    }

    private IEnumerator TypeSentence(DialogueEntry entry) {
        isTyping = true;
        skipTyping = false;
        dialogueText.text = "";
        
        string sentence = entry.sentence;
        string processedText = sentence != null ? sentence.Replace("\\n", "\n") : "";

        foreach (char letter in processedText) {
            if (skipTyping) {
                dialogueText.text = processedText;
                break;
            }
            
            dialogueText.text += letter;
            
            if (playTypingSound && typingSound != null && letter != ' ' && letter != '\n') {
                typingSound.Play();
            }
            
            yield return new WaitForSeconds(letterDelay);
        }
        
        dialogueText.text = processedText;
        isTyping = false;

        // 選択肢の表示
        if (entry.options != null && entry.options.Length > 0) {
            DisplayChoices(entry.options);
        } else {
            // 選択肢がない場合は次に進むインジケータを表示
            blinkCoroutine = StartCoroutine(BlinkIndicator());
            
            // アクションがあれば実行（特定のセリフの直後）、action=OpenDoorのとき
            if (!string.IsNullOrEmpty(entry.action)) {
                PerformAction(entry.action);
            }
        }
    }

    private void DisplayChoices(DialogueOption[] options) {
        currentOptions = options;
        isWaitingForChoice = true;
        
        if (choiceText != null) {
            // 選択肢テキストの作成（例: [Y] はい / [N] いいえ）
            string choicesStr = "";
            for (int i = 0; i < options.Length; i++) {
                string keyName = i == 0 ? optionYesKey.ToString() : optionNoKey.ToString();
                choicesStr += $"[{keyName}] {options[i].optionText}";
                
                if (i < options.Length - 1) {
                    choicesStr += " / ";
                }
            }
            
            choiceText.text = choicesStr;
            choiceText.gameObject.SetActive(true);
        } else {
            // 選択肢テキストUIがない場合はダイアログテキストに追加
            string currentText = dialogueText.text;
            string choicesStr = "\n\n";
            
            for (int i = 0; i < options.Length; i++) {
                string keyName = i == 0 ? optionYesKey.ToString() : optionNoKey.ToString();
                choicesStr += $"[{keyName}] {options[i].optionText}";
                
                if (i < options.Length - 1) {
                    choicesStr += " / ";
                }
            }
            
            dialogueText.text = currentText + choicesStr;
        }
    }

    private void PerformAction(string action) {
        if (string.IsNullOrEmpty(action))
            return;
            
        switch (action) {
            case "OpenDoor":
                OpenDoor();
                break;
            case "CloseDoor":
                CloseDoor();
                break;
            case "GiveItem":
                GiveItem();
                break;
            default:
                SendCustomActionEvent(action);
                break;
        }
    }

    private void OpenDoor() {
        Debug.Log("OpenDoor action executed");
        GameObject door = GameObject.Find("PF Props Wooden Gate");
        if (door != null) {
            door.SetActive(false);
            GameObject openedDoor = GameObject.Find("PF Props Wooden Gate Opened");
            if (openedDoor != null) {
                openedDoor.SetActive(true);
            } else {
                Debug.LogWarning("Opened door object not found.");
            }
        } else {
            Debug.LogWarning("Door object not found.");
        }
    }
    
    private void CloseDoor() {
        GameObject door = GameObject.Find("PF Props Wooden Gate");
        if (door != null) {
            door.SetActive(true);
            GameObject openedDoor = GameObject.Find("PF Props Wooden Gate Opened");
            if (openedDoor != null) {
                openedDoor.SetActive(false);
            }
        }
    }
    
    private void GiveItem() {
        Debug.Log("Item given to player");
    }
    
    private void SendCustomActionEvent(string action) {
        Debug.Log($"Custom action triggered: {action}");
    }

    private IEnumerator BlinkIndicator() {
        if (dialogueText == null) {
            yield break;
        }
        
        bool showIndicator = true;
        while (!isTyping && !isWaitingForChoice) {
            string baseText = Regex.Replace(dialogueText.text, $" {indicatorSymbol}$", "");
            dialogueText.text = baseText + (showIndicator ? " " + indicatorSymbol : "");
            showIndicator = !showIndicator;
            yield return new WaitForSeconds(indicatorBlinkSpeed);
        }
    }
}

[System.Serializable]
public class DialogueData {
    public DialogueEntry[] dialogues;
}

[System.Serializable]
public class DialogueEntry {
    public string sentence;
    public DialogueOption[] options;
    public string action;
}

[System.Serializable]
public class DialogueOption {
    public string optionText;
    public int nextDialogueIndex;
    public string action;
}