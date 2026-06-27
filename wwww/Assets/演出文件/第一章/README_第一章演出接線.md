# 第一章演出接線說明

這份是給演出組和程式組接 Unity 場景用的簡版說明。

## 1. 建立演出控制器

在第一章場景新增一個空物件，命名：

`Chapter1_PerformanceController`

掛上腳本：

`Chapter1PerformanceController`

Inspector 需要拖入：

- `Player Controller`：玩家物件上的 `PCPlayerController`
- `Character Controller`：玩家物件上的 `CharacterController`
- `Dialogue UI`：字幕 UI 物件上的 `Chapter1DialogueUI`
- `Choice UI`：選項 UI 物件上的 `Chapter1ChoiceUI`
- `Fade Canvas`：黑幕淡入淡出的 CanvasGroup
- `Police Group`：兩名日警的父物件，開場先關閉
- `Wedding Ambience`：婚禮環境音
- `Tension Ambience`：壓迫段低沉音效

## 2. Timeline 建議

先做三段就能跑主要流程：

- `Dance Timeline`：玩家加入舞蹈
- `Police Enter Timeline`：日警進場、婚禮停止、推倒酒杯
- `Ending Timeline`：日警離開、族人沉默、淡出

分支可以晚一點補：

- `Intervene Timeline`：玩家上前阻止
- `Watch Timeline`：玩家沉默觀望

## 3. NPC 互動

要讓 NPC 或區域可以互動，就掛：

`Chapter1Interactable`

常用類型：

- `Talk`：族人一句生活對話
- `DeliverWine`：送酒任務
- `ShareFood`：分享食物
- `JoinDance`：加入舞蹈並進入日警進場
- `StartPoliceSequence`：直接觸發日警進場，方便測試

玩家需要有 Tag：

`Player`

互動區的 Collider 要打開：

`Is Trigger`

## 4. UI 接法

字幕 UI：

- 建一個 Canvas
- 放 Speaker 文字和 Body 文字
- 掛 `Chapter1DialogueUI`
- 拖入 `CanvasGroup`、`speakerText`、`bodyText`

選項 UI：

- 放問題文字
- 放兩個 Button
- 掛 `Chapter1ChoiceUI`
- option A 是「上前阻止」
- option B 是「沉默觀望」

## 5. 建議先測的流程

1. 在場景放玩家、火堆、族人、日警父物件。
2. 日警父物件先關閉。
3. 新增一個舞圈 Trigger，掛 `Chapter1Interactable`，類型選 `JoinDance`。
4. 玩家走進舞圈按 F。
5. 確認玩家控制被鎖住、舞蹈 Timeline 播放、日警出現、選項 UI 出現。

## 6. 敏感橋段演出提醒

不要直接拍露骨侵犯畫面。建議用：

- 酒杯落地
- 鼓聲停止
- 族人表情
- 手部後退
- 火光晃動
- 黑幕與聲音

這樣比較適合畢業專題，也比較尊重歷史題材。
