# 第一章劇情已加入

這次採用不覆蓋同學場景內容的方式處理：沒有改 `.unity` 場景、沒有刪物件、沒有搬模型或地形，只強化既有的第一章腳本。

## 已加入的劇情流程

1. 開場淡入：1930.10.7 霧社婚禮，玩家以第一人稱醒來。
2. 自由探索：可與族人交談，聽到婚禮祝福與生活對話。
3. 送酒任務：玩家幫新郎把酒送給賓客，會顯示進度。
4. 分享食物：可選互動，增加婚禮生活感。
5. 加入舞蹈：靠近 `DanceTrigger` 按 E 後，玩家視角會跟著鼓聲繞舞圈移動。
6. 日警闖入：婚禮停止，日警嘲諷並推倒酒杯，氣氛轉為壓迫。
7. 玩家選擇：按 1 上前阻止，按 2 沉默觀望。
8. 分支合流：兩個選擇會有不同旁白與結果數值，最後都回到日警離開、族人沉默憤怒的結尾。

## 修改過的檔案

- `Assets/腳本/第一章演出/Chapter1PerformanceController.cs`
  - 補完整中文劇情流程。
  - 補任務目標、備用字幕 HUD、備用選項 UI。
  - 沒接 `Dialogue UI` 或 `Choice UI` 時，也能用 OnGUI 顯示字幕與選項。
  - 自動尋找 `XR Origin (VR)`、`DanceTrigger`、`PoliceIntrusionSequence` 等既有物件。
  - 章節結尾會把選擇結果存到 `PlayerPrefs`，方便第二章讀取。

- `Assets/腳本/第一章演出/Chapter1Interactable.cs`
  - 自動尋找第一章控制器。
  - NPC 對話可輪播多句。
  - 新增 `BeginChapter` 和 `MissionHint` 互動類型。
  - 如果 prompt 沒填，會依互動類型自動顯示中文提示。

## Unity 裡怎麼測

1. 打開有內容的第一章場景，例如 `Assets/Scenes/第一章新版 1.unity`。
2. 按 Play。
3. 進場後會自動出現第一章開場字幕與目前目標。
4. 靠近 `DanceTrigger` 按 E，會進入舞蹈，舞蹈後接日警闖入。
5. 選擇畫面出現時，按 1 或 2 測兩個分支。

## 接同學後續章節用的結果

第一章結尾會保存：

- `Chapter1_ConflictChoice`：`Intervene` 或 `Watch`
- `Chapter1_PeopleInjured`：第一章分支造成的傷亡壓力數值
- `Chapter1_Morale`：士氣/凝聚感數值

第二章腳本可以用下面方式讀取：

```csharp
string choice = PlayerPrefs.GetString("Chapter1_ConflictChoice", "");
int peopleInjured = PlayerPrefs.GetInt("Chapter1_PeopleInjured", 0);
int morale = PlayerPrefs.GetInt("Chapter1_Morale", 0);
```

## 注意

目前沒有直接把新 UI 物件寫進場景，所以不會覆蓋同學做好的 Canvas 或場景配置。若之後要做正式 UI，只要把 `Chapter1DialogueUI` 和 `Chapter1ChoiceUI` 接到 `Chapter1_PerformanceController`，備用 HUD 仍可保留作為測試用。
