using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System;

[System.Serializable]
public enum PanelOutcome
{
    NextScene,
    RestartLevel,
    Nothing
}

[System.Serializable]
public enum ButtonActionType
{
    NextPanel,    // Переход на следующую панель
    SkipPanel,    // Пропуск одной панели
    TriggerOutcome // Завершение диалога с указанным исходом
}

[System.Serializable]
public struct DialogueButton
{
    public Button button;
    public ButtonActionType actionType;
    public PanelOutcome buttonOutcome; // Исход, вызываемый кнопкой
}

[System.Serializable]
public struct DialogueNode
{
    public GameObject panel;
    public TextMeshProUGUI textComponent;
    public DialogueButton[] buttons;
    public bool disableScreenTap;
    public PanelOutcome outcome;
    public bool isEndNode;
}

public class DialogueSystem : MonoBehaviour
{
    [Header("Dialogue Nodes")]
    public DialogueNode[] dialogueNodes;

    [Header("Gameplay Settings")]
    public GameObject player;
    public string nextSceneName;
    public float minLoadingTime = 2f;

    [Header("Animation Settings")]
    public string animationTriggerName = "Play";
    public GameObject endDialoguePanel;
    public GameObject loadingAnimationPanel;

    private int currentNodeIndex = 0;
    private bool isDialogueActive = false;
    private bool isAnimationPlaying = false;
    private bool isTransitioning = false;

    void Start()
    {
        InitializeDialogueSystem();
        SetInitialState();
    }

    void InitializeDialogueSystem()
    {
        foreach (var node in dialogueNodes)
        {
            if (node.panel != null)
            {
                node.panel.SetActive(false);
                InitializeNodeComponents(node);
            }
            else
            {
                Debug.LogWarning("Panel is missing in DialogueNode!");
            }
        }
    }

    void InitializeNodeComponents(DialogueNode node)
    {
        if (node.buttons != null)
        {
            foreach (var btn in node.buttons)
            {
                if (btn.button != null)
                {
                    Debug.Log($"Initializing button on panel {node.panel.name}A with actionType {btn.actionType}");
                    btn.button.onClick.RemoveAllListeners();
                    btn.button.onClick.AddListener(() => HandleButtonClick(btn));
                    btn.button.gameObject.SetActive(false);
                }
            }
        }

        if (node.buttons == null || node.buttons.Length == 0)
        {
            Button panelButton = node.panel.GetComponent<Button>() ?? node.panel.AddComponent<Button>();
            panelButton.transition = Selectable.Transition.None;
            panelButton.onClick.RemoveAllListeners();
            if (node.isEndNode)
            {
                panelButton.onClick.AddListener(() => StartCoroutine(HandleDialogueOutcomeWithOutcome(node.outcome)));
            }
            else
            {
                panelButton.onClick.AddListener(() => HandleButtonClick(new DialogueButton { actionType = ButtonActionType.NextPanel }));
            }
        }
    }

    void SetInitialState()
    {
        if (endDialoguePanel) endDialoguePanel.SetActive(false);
        if (loadingAnimationPanel) loadingAnimationPanel.SetActive(false);
    }

    void HandleButtonClick(DialogueButton btn)
    {
        if (!isDialogueActive || isTransitioning)
        {
            Debug.Log($"Button click ignored: isDialogueActive={isDialogueActive}, isTransitioning={isTransitioning}");
            return;
        }

        if (btn.actionType == ButtonActionType.TriggerOutcome)
        {
            Debug.Log($"Triggering outcome: {btn.buttonOutcome} from panel {dialogueNodes[currentNodeIndex].panel.name}");
            StartCoroutine(HandleDialogueOutcomeWithOutcome(btn.buttonOutcome));
        }
        else
        {
            int skipValue = btn.actionType == ButtonActionType.SkipPanel ? 2 : 1;
            Debug.Log($"Button clicked on panel {dialogueNodes[currentNodeIndex].panel.name} with actionType {btn.actionType}. Skipping {skipValue} panels.");
            StartCoroutine(ProcessNodeTransition(skipValue));
        }
    }

    IEnumerator ProcessNodeTransition(int skipCount)
    {
        isTransitioning = true;

        DeactivateCurrentNode();
        yield return null;

        int newIndex = currentNodeIndex + skipCount;
        Debug.Log($"Transitioning from {currentNodeIndex} to {newIndex}");

        if (newIndex >= dialogueNodes.Length)
        {
            Debug.Log("Reached end of dialogue nodes, ending dialogue.");
            EndDialogue();
            yield break;
        }

        currentNodeIndex = newIndex;
        DialogueNode node = dialogueNodes[currentNodeIndex];
        node.panel.SetActive(true);

        if (node.textComponent != null)
        {
            Debug.Log($"Text set for panel {node.panel.name}: {node.textComponent.text}");
        }

        yield return null;
        ActivateButtons(node);
        ForceUIUpdate();

        isTransitioning = false;
    }

    void DeactivateCurrentNode()
    {
        DialogueNode node = dialogueNodes[currentNodeIndex];
        node.panel.SetActive(false);
    }

    void ActivateButtons(DialogueNode node)
    {
        if (node.buttons != null)
        {
            foreach (var btn in node.buttons)
            {
                if (btn.button != null)
                {
                    btn.button.gameObject.SetActive(true);
                    btn.button.interactable = true;
                    Debug.Log($"Activated button on panel {node.panel.name}, interactable: {btn.button.interactable}");
                }
            }
        }
    }

    void ForceUIUpdate()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
    }

    void Update()
    {
        if (!isDialogueActive || isAnimationPlaying || isTransitioning) return;
        if (dialogueNodes[currentNodeIndex].disableScreenTap) return;

        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            if (!IsPointerOverUIObject())
            {
                HandleButtonClick(new DialogueButton { actionType = ButtonActionType.NextPanel });
            }
        }
    }

    bool IsPointerOverUIObject()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject() || 
               (Input.touchCount > 0 && 
                EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            StartDialogue();
        }
    }

    void StartDialogue()
    {
        if (dialogueNodes.Length == 0 || dialogueNodes[0].panel == null)
        {
            Debug.LogError("Dialogue configuration error!");
            return;
        }

        if (isDialogueActive) return;

        currentNodeIndex = 0;
        isDialogueActive = true;
        DialogueNode node = dialogueNodes[currentNodeIndex];
        node.panel.SetActive(true);
        StartCoroutine(ActivateNodeAfterDelay(node));
        TogglePlayer(false);
    }

    IEnumerator ActivateNodeAfterDelay(DialogueNode node)
    {
        yield return null;
        ActivateButtons(node);
        ForceUIUpdate();
    }

    void EndDialogue()
    {
        StartCoroutine(HandleDialogueOutcomeWithOutcome(dialogueNodes[currentNodeIndex].outcome));
    }

    IEnumerator HandleDialogueOutcomeWithOutcome(PanelOutcome outcome)
    {
        isAnimationPlaying = true;
        isDialogueActive = false;

        if (endDialoguePanel != null)
        {
            endDialoguePanel.SetActive(true);
            yield return PlayPanelAnimation(endDialoguePanel);
        }

        yield return ProcessOutcome(outcome);

        CleanupAfterDialogue();
        isAnimationPlaying = false;
    }

    IEnumerator PlayPanelAnimation(GameObject panel)
    {
        Animator animator = panel.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(animationTriggerName);
            yield return new WaitForSeconds(
                animator.GetCurrentAnimatorStateInfo(0).length / 
                Mathf.Max(1f, animator.GetCurrentAnimatorStateInfo(0).speed)
            );
        }
        else
        {
            yield return new WaitForSeconds(minLoadingTime);
        }
    }

    IEnumerator ProcessOutcome(PanelOutcome outcome)
    {
        switch (outcome)
        {
            case PanelOutcome.NextScene:
                yield return LoadNewScene(nextSceneName);
                break;
            
            case PanelOutcome.RestartLevel:
                yield return LoadNewScene(SceneManager.GetActiveScene().name);
                break;

            case PanelOutcome.Nothing:
                yield return new WaitForSeconds(minLoadingTime);
                break;
        }
    }

    IEnumerator LoadNewScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            if (loadingAnimationPanel != null)
            {
                endDialoguePanel.SetActive(false);
                loadingAnimationPanel.SetActive(true);
                yield return PlayPanelAnimation(loadingAnimationPanel);
            }

            asyncLoad.allowSceneActivation = true;
            yield return asyncLoad;
        }
    }

    void CleanupAfterDialogue()
    {
        SetInitialState();
        TogglePlayer(true);
    }

    void TogglePlayer(bool state)
    {
        if (player != null)
        {
            player.SetActive(state);
        }
    }
}