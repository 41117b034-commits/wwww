using UnityEngine;
using TMPro;

public class TaskTrigger : MonoBehaviour
{
    [Header("介面")]
    public GameObject taskPanel;
    public TMP_Text taskText;

    [Header("任務設定")]
    public GameObject targetItem;
    public string missionMessage = "請拿起指定物品";
    public string completeMessage = "任務完成！";

    private bool taskStarted;
    private bool taskCompleted;

    void OnEnable()
    {
        PickupItem.OnItemPickedUp += CheckPickedItem;
    }

    void OnDisable()
    {
        PickupItem.OnItemPickedUp -= CheckPickedItem;
    }

    void Start()
    {
        if (taskPanel != null)
        {
            taskPanel.SetActive(false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || taskCompleted)
        {
            return;
        }

        taskStarted = true;

        if (taskText != null)
        {
            taskText.text = missionMessage;
        }

        if (taskPanel != null)
        {
            taskPanel.SetActive(true);
        }

        Debug.Log("任務開始");
    }

    void CheckPickedItem(GameObject pickedItem)
    {
        if (!taskStarted || taskCompleted)
        {
            return;
        }

        if (pickedItem != targetItem)
        {
            return;
        }

        taskCompleted = true;

        if (taskText != null)
        {
            taskText.text = completeMessage;
        }

        Debug.Log("任務完成：" + pickedItem.name);

        Invoke(nameof(HideTaskPanel), 2f);
    }

    void HideTaskPanel()
    {
        if (taskPanel != null)
        {
            taskPanel.SetActive(false);
        }
    }
}