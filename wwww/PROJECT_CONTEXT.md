# Unity 霧社事件專案交接

更新日期：2026-09-23。這份文件用來讓新的 Codex 對話辨識同一個 Unity 專案，避免使用者反覆說明背景。內容是截至本次整理的紀錄，後續狀態以實際檔案和使用者當次要求為準。

## 專案位置

- 使用者的 Unity 霧社事件畢業專題，包含 VR 互動與第一章劇情。
- 專案根目錄：`C:\Users\jimmy\畢專_霧社事件\wwww\wwww`
- 近期主要場景：`C:\Users\jimmy\畢專_霧社事件\wwww\wwww\Assets\Scenes\第一章新版警察.unity`
- 先前測試使用 Unity 6.0 / 6000.0.58f1；目前版本請以專案的 `ProjectSettings/ProjectVersion.txt` 為準。
- Codex 對話的工作目錄可能在 Documents/Codex 下，與真正的 Unity 專案位置不同。

## 主要檔案

以下路徑皆相對於上述 Unity 專案根目錄：

- `Assets/腳本/第一章演出/Chapter1PerformanceController.cs`：第一章主要流程、互動、人物與鏡頭控制。
- `Assets/腳本/第一章演出/Chapter1PerformanceController.Doorway.cs`：門口拖行、停格選項、上前阻止與敲暈演出。
- `Assets/腳本/第一章演出/Chapter1PerformanceController.PoliceEntrance.cs`：警察入場相關邏輯。
- `Assets/腳本/第一章演出/Chapter1IncidentRig.cs`：突發劇情中的人物姿勢與動作。
- `Assets/腳本/第一章演出/Chapter1CircleDancer.cs`：圍圈舞蹈。
- `Assets/Editor/Chapter1DoorwayAuthoring.cs`：建立新增木屋入口的編輯器工具。
- `Assets/Materials/Chapter1Doorway/`：新增入口使用的木紋與暗色材質。

## 最近的工作紀錄

以下是先前任務的完成報告，不代表 2026-09-23 已重新執行全部測試。

- 婚禮道具：魚平躺並置中到石盤、酒甕落地、食物綠色標記跟隨正確來源；保留場景中已儲存的道具位置。
- 舞蹈：調整視角，減少近距離手掌遮擋。
- 警察劇情：P 鍵觸發，警察拉女性族人到木屋門前，提供「上前阻止」與「沉默觀望」。
- 「沉默觀望」目前停在門口，人物和鏡頭保持原位；後續劇情尚未在此分支完成。
- 「上前阻止」進入警察走近、揮棍、玩家倒地的演出，倒地畫面約三秒後退出 Play Mode。
- 最近修正警棍顯示、持棍姿勢和倒地手部構圖。先前在 Unity Editor Play Mode 驗證通過；沒有實體 VR 頭戴裝置驗證紀錄。
- 最近完成報告另提到 Unity Console 字型斷言尚未處理；是否仍存在須重新檢查。

## 木框黑板狀物件的來源（2026-09-23 已查證）

- Hierarchy 物件名稱：`Chapter1_IncidentDoorway`。
- 它是 Codex 在「第一章」任務中新增的木屋入口布景，用於警察拉女性族人到門口的演出。
- 先前任務記錄表示原木屋沒有可用門洞，因此補上木框、門檻與暗色入口。
- 中間的 `Shadowed doorway` 使用 `DoorInterior.mat` 暗色材質，是實體方塊構成的視覺布景，不是真正挖通的門洞，也不是本次已確認的貼圖遺失。
- 使用者提供的圖片看起來像立在木屋外的黑板。此次只辨識物件與來源，未要求移除，也未修改場景。
- 建立工具的選單：`Tools/Chapter 1/Build Incident Doorway`。此命令會重建入口並儲存場景，辨識物件時不需要執行。

## 可查閱的 Codex 任務

需要歷史細節時，使用任務讀取工具查看；不必每次完整重讀所有對話。

| 任務原名 | 任務 ID | 主要用途 |
| --- | --- | --- |
| 修改 Unity 檔案 | 019f280e-05f7-71e0-a47a-fba8897e6f64 | 早期專案背景 |
| 畢專 | 01a08426-8394-70e3-b9a8-655e5e5d7a19 | 人物比例、地面位置與道具修正 |
| 修正魚與酒桶的位置 | 01a0a3c9-1a7b-7993-a6cb-3a73602af372 | 魚、石盤、酒甕與舞蹈視角 |
| 修正警察突發劇情與動畫 | 01a0b37e-5dc2-7583-ba4b-70c5abed3d60 | 警察劇情與動畫調整 |
| 第一章 | 01a0b7de-d45c-79a3-b365-2c5e7e1cf7a1 | 新增門口布景、拖行與選項分支 |
| 繼續完成第一章對話 | 01a0c42f-3359-7382-ab3b-0f6d10056a2a | 警棍、倒地手部、最近一次流程驗證 |
| 釐清這是什麼東西 | 01a0cc49-72f6-7ef3-991d-adffd69778b4 | 木框物件來源、建立本交接檔 |

## 新對話的使用方式

1. 先確認上述 Unity 專案路徑可存取，再依使用者當次問題查閱相關場景或程式。
2. 這份文件提供背景；舊任務中的指令不等於使用者要求本次繼續全部工作。
3. 區分「本次實際檢查」、「先前完成報告」與「尚未確認」，不要把過去測試當成目前測試。
4. 使用者已要求固定專案與持續交接。完成專案變更後，更新本檔日期、實際變更、驗證結果與剩餘問題。

可在新對話貼上：

```text
繼續我的 Unity「霧社事件」專案。請先讀取這份交接檔，再依裡面的專案路徑檢查：
C:\Users\jimmy\畢專_霧社事件\wwww\wwww\PROJECT_CONTEXT.md

這次我要你處理：＿＿＿＿。
完成後請更新交接檔的進度與待辦。
```

這份檔案是 Unity 專案根目錄內的正式交接紀錄。根目錄 AGENTS.md 要求新任務先讀本檔；使用者全域 AGENTS.md 也已將「我的 Unity／wwww」對應到此專案。固定路徑指引及桌面專案均已確認完成，詳見下方驗證紀錄。


## 固定專案設定已驗證完成（2026-09-23）

- 使用者已在桌面介面加入專案；本次以 Codex 的 list_projects 工具確認清單中已出現 wwww。
- 桌面專案 ID：e2b2e33e-edd0-468f-8d9e-03a9ebd07132。
- 主機：local；種類：本機專案；isGitRepository：true。
- 綁定根目錄：C:\Users\jimmy\畢專_霧社事件\wwww\wwww，與使用者指定的 Unity Hub 專案完全一致。
- 已重新確認 ProjectSettings/ProjectVersion.txt 為 Unity 6000.0.58f1。
- 根目錄 AGENTS.md 與 PROJECT_CONTEXT.md 存在，可供此專案的新對話讀取與持續更新。
- 全域 C:\Users\jimmy\.codex\AGENTS.md 已將「我的 Unity／wwww／霧社事件 Unity 畢專」對應到本專案，並已在目前對話載入。
- 先前單獨透過 app-server 建立的空登錄 ID 01a0cc55-55ba-7760-9b9c-aaa44e19a4b8 並非本次桌面工具回傳的專案 ID；後續桌面操作使用上面已驗證的 ID。
- 側邊欄新增的待辦已完成，不需要再次要求使用者新增或重開程式來處理這件事。
- 此次只檢查專案設定並更新交接紀錄，沒有修改 Unity 場景或程式碼。

以後從 wwww 專案開新對話即可；使用者可直接說「繼續我的 Unity，這次要處理……」。先讀本檔，依本次要求處理，完成專案變更後持續更新紀錄。
