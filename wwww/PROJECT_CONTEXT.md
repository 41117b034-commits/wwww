# Unity 霧社事件專案交接

更新日期：2026-10-04。這份文件用來讓新的 Codex 對話辨識同一個 Unity 專案，避免使用者反覆說明背景。內容是截至本次整理的紀錄，後續狀態以實際檔案和使用者當次要求為準。

最新狀態：2026-10-04 接續踢酒角度、酒桶貼地及遠離火堆、離場右手與黑幕退出要求，完成目前版本的實際 P → 2、P → 1 驗證。兩條分支都在全黑後自動退出 Editor Play Mode，未載入第二章；臨時驗證工具已移出 Assets。詳見文末「踢酒、酒桶與黑幕退出接續收尾」。

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

## 婚禮轉入警察劇情的銜接與滿版畫面（2026-09-24）

- 使用者採用「警察入場短鏡頭＋族人退開」方案，並要求移除劇情上下黑邊；授權直接修改原專案及操作 Unity。
- 修改 `Chapter1PerformanceController.PoliceEntrance.cs`：移除群眾直接重排位置的 `StageStoppedWeddingCrowd`，改成保留當下位置、約 0.9 秒轉頭反應、1.8 秒警察入場短鏡頭，然後接全景看族人走向站位。
- 族人依目前位置選附近的空站位，新郎保留後續對峙所需位置；錯開起步時間，路徑遇火堆時繞行，走到位置後轉向警察。移動時長依路程調整，本次兩輪正式驗證約 6.3 秒及 7 秒（包含警察短鏡頭）。
- 修改 `Chapter1IncidentRig.cs`：讓群眾沿用腳步動作與地面校正，補上 Generic 骨架解析與 0.6 秒起始姿勢混合，涵蓋場景中的 Humanoid 與 Generic 族人。
- 修改 `Chapter1PerformanceController.cs`：新版警察場景不再事先把女性族人瞬移到舞圈外；劇情黑邊的預設值改為關閉。
- 已在 Unity 中將 `Assets/Scenes/第一章新版警察.unity` 的 `showCinematicLetterbox` 設為 false 並儲存。入場鏡頭也明確使用完整 Camera viewport，字幕保留。

### 本次實際驗證

- 使用 Unity 6000.0.58f1 重新編譯，進入 Editor Play Mode，實際按 P 觸發；也測過開場文字尚在顯示時按 P。
- 第二輪 P 鍵驗證：14 名轉場族人皆成功建立動作骨架，包含 2 名 Generic；入場、族人退開、後續對話、推擠及門口選項皆有執行到，沒有測試期間的 Error／Exception。
- 記錄每幀水平位置；轉場最大水平速度約 17 場景單位／秒（成人身高約 13 場景單位），沒有觀察到原本的群眾瞬移尖峰。
- 額外在 Play Mode 用臨時 Editor 測試工具設定送酒、食物與舞蹈進度完成，呼叫原本的任務完成檢查，確認自動觸發路徑也能完成相同轉場且没有 Error／Exception。這是完成狀態模擬，未逐項重玩送酒、食物、舞蹈互動。
- 查看實際 Game 畫面與引擎截圖，確認警察短鏡頭、退開全景和後續劇情沒有電影式上下黑邊。
- 測試後退出 Play Mode，臨時 `Assets/Editor/Chapter1TransitionProbe.cs` 及其 meta 已移除。原檔備份、測試工具副本、狀態紀錄及截圖保留在 `_CodexBackups/police_transition_20260924/`；正式 P 鍵與自動觸發截圖分別在 `p-key-verified/`、`auto-completion-verified/`。
- 尚未使用實體 VR 頭戴裝置驗證；本次也未改動或重測門口選項之後的兩條分支。

## 補上遺漏的外圍任務 NPC（2026-09-24 後續修正）

- 使用者指出警察說話鏡頭的後方仍留有族人。前一輪轉場與驗證只涵蓋 14 名舞圈／劇情族人，遺漏了固定站立、接收酒與食物的 4 名 NPC。
- 實際遺漏角色為 `部落男性`、`部落女姓2`、`賽德克帥哥`、`原住民小孩(2)`；截图中留在警察背後的兩人是 `部落女姓2`、`賽德克帥哥`。
- 修改 `Chapter1PerformanceController.PoliceEntrance.cs`，將送酒、食物目標的完整角色根物件也納入退開名單，排除重複及父子層級重複。婚禮任務階段仍保持原本的固定位置，進入警察劇情後才一起走開。
- 站位增加到 18 個，較矮的任務 NPC 優先使用火堆旁的前排位置，避免被烤架遮住；另加站位不足時自動補足的處理，避免新增人物再次留在原地。
- 本次在 Unity 6000.0.58f1 Editor Play Mode 重新按 P 驗證兩輪；最終版本的 18 名族人均有有效動作骨架且到達群眾側。特別核對使用者截圖的「這種野蠻婚禮，竟然還敢辦得這麼熱鬧？」鏡頭，警察身後已無落單族人，滿版画面維持不變，測試沒有 Error／Exception。
- 這次測試記錄改為同時收集舞圈成員與送酒／食物目標，不再只檢查舞圈名單。備份、完整名單及對照截圖放在 `_CodexBackups/police_receiver_retreat_20260924/`，其中 `police-insult.png` 對應使用者截圖，`crowd-retreat-complete.png` 顯示 18 人的新站位。
- 測試後退出 Play Mode並移除臨時 Editor 測試腳本與 meta；未使用實體 VR 裝置，未逐項重玩婚禮任務或重測選項分支。

## 修正任務成人 NPC 過小的比例（2026-09-24 後續修正）

- 使用者指出退開全景中有兩名族人異常矮小。確認 `部落女姓2`、`賽德克帥哥` 都是成年人；先前只把較矮的任務 NPC 排到前排，沒有修正兩人被排除在身高統一處理之外的問題。
- 修改 `Chapter1PerformanceController.cs`：身高統一處理也涵蓋送酒／食物 NPC，仍按成人與小孩分別比對原有舞圈角色的參考身高。維持任務 NPC 的固定站位，不將其加入舞圈。
- 在遊戲啟動時修正比例，並同步更新任務 NPC 的還原用縮放紀錄，避免 `PrepareDeliveryTaskNPCs` 在轉入劇情前把身高恢復成原先過小的比例。
- 修改 `Chapter1PerformanceController.PoliceEntrance.cs`：前排優先站位依角色是否為小孩判定，不再把偏小的成人當成小孩安排。
- 本次實際在 Unity 6000.0.58f1 Editor Play Mode 按 P 驗證一輪，從婚禮任務階段記錄到族人退開、警察對話與門口選項。兩名成人的骨架量測高度約 12.77、12.76 場景單位，與其他成人相近；`原住民小孩(2)` 維持約 9.13，縮放未變。
- 核對任務階段與退開完成紀錄，四名任務 NPC 的縮放均保持一致；18 名族人都有有效劇情骨架，警察對話鏡頭背後無遺留族人。引擎截圖保持滿版，測試期間無 Error／Exception。未使用實體 VR，未重玩送物互動或門口選項分支。
- 原檔備份、測試工具副本、前後身高／縮放紀錄與實際畫面保存在 `_CodexBackups/police_guest_scale_20260924/`；`crowd-retreat-complete.png` 為修正後全景，`before-incident.json` 與 `crowd-retreat-complete.json` 可對照比例。
- 測試後已退出 Play Mode，移除臨時 `Assets/Editor/Chapter1TransitionProbe.cs` 與 meta；本次沒有更動場景序列化內容。

## 「沉默觀望」警察離去情境圖（2026-09-25）

- 使用者要求生成兩名警察離開、族人目送背影的圖片，背景延續遊戲場景。
- 以 `_CodexBackups/police_guest_scale_20260924/` 中的 `crowd-retreat-complete.png`、`police-entrance-short.png`、`police-insult.png` 實際遊戲截圖作為參考，使用內建 image_gen 生成一張橫式情境圖。
- 圖片存於 `output/imagegen/沉默觀望_警察離去_v1.png`；完整提示詞存於同目錄的 `沉默觀望_警察離去_v1.prompt.txt`。
- 已目視確認兩名黑制服警察背對鏡頭走遠、族人在前景目送，背景延續木造屋舍、碎石地與黃綠樹林，沒有字幕或遊戲 UI。此為依截圖生成的圖片，並非 Unity 引擎直接渲染的新鏡頭。
- 本次只產出圖片，未修改程式、場景或接入選項分支，未執行 Unity／VR 測試；「沉默觀望」離去演出仍未實作。

## 完成「沉默觀望」屋內事件與警察離去（2026-09-25，接續中斷任務）

- 使用者在「生成警察離開的背影圖」（`01a0d6f5-b5b8-74b3-b078-011a4c6be776`）接著要求直接修改 Unity：警察拉女性入屋、關門後只傳出慘叫、關門五秒後拖出女性、兩名警察離去，族人在前景目送。該次工作在最終驗證前中斷；本次接續既有修改完成驗證與收尾。
- 新增 `Assets/腳本/第一章演出/Chapter1PerformanceController.Watch.cs`，並由 `Chapter1PerformanceController.Doorway.cs` 的沉默分支呼叫。整段保持屋外視角；關門期間隱藏屋內兩人的渲染，沒有屋內動作或屋內鏡頭。女性被帶出後留在屋外，兩名警察回到廣場並慢步離開，最後構圖停留六秒。
- 新增 `Chapter1HutDoor.cs`、更新 `Chapter1DoorwayAuthoring.cs` 並新增 `Chapter1HutOpeningAuthoring.cs`，為現有木框加入旋轉木門、地板與原屋舍的局部门洞，原始匯入模型保留。場景的整屋 BoxCollider 分割為門洞周围的碰撞區塊，避免用整屋碰撞封住門口。
- 新門洞模型為 `Assets/Models/Chapter1Doorway/IncidentHutOpening.asset`。本次接手時場景仍引用它，但檔案缺失；已修正建立工具可使用保留的 `originalFacadeMesh` 重建，重新產生並儲存場景。保留未切割部分的原始頂點索引，目前約 124 萬頂點、163 MB 文字序列化檔，沒有改動原始屋舍資產。
- 新增 `Assets/Resources/Chapter1Voice/10_hut_female_scream.ogg` 與同名 `.LICENSE.txt`。素材來源與 CC0 授權紀錄保存在該檔；`GetHutCryClip()` 載入這個音效，從屋內位置播放並套用低通濾波。原先 `10_hut_cry.wav` 仍保留，這段改用新增音效。
- `Chapter1PerformanceController.cs` 在停用時停止這段 coroutine 並清理屋內音效、恢復角色可見性與環境音量。演出使用既有角色、場景和滿版鏡頭，沒有用生成圖片取代遊戲畫面。

### 本次實際驗證

- Unity 6000.0.58f1 Editor Play Mode，實際按 P 觸發、按 2 選「沉默觀望」，執行到 `watch-complete`。關門至重新開門實測 5.024 秒；音效在播放且有非零輸出，關門期間 `Police02_Model` 與 `部落女性1` 的可見角色渲染數均為 0。
- 檢查進屋、關門、拖出與離場的引擎截圖，最後畫面能看見前景族人與兩名警察背影。女性釋放後至結尾水平位移為 0；兩名警察正常可見並停止於離場終點。這輪 Error／Exception／Assert 為 0。
- 儲存後重新載入場景，透過臨時 Editor 測試工具觸發既有劇情與「上前阻止」。實際執行警棍擊中、倒地構圖，約三秒後自動退出 Play Mode；退出記錄 `completed=true`、`knockedOut=true`，沒有 Error／Exception／Assert。
- 另以場景原有的自動結束設定再跑一次「沉默觀望」：關門等待 5.043 秒，背影結尾停留約六秒後自動退出 Play Mode；退出記錄為 `beat=watch-complete`、`completed=true`、`knockedOut=false`，沒有 Error／Exception／Assert。這輪用程式呼叫與按鍵相同的劇情／選項入口，沒有再重玩婚禮任務。
- 門口射線檢查：門關閉時會碰到木門；門打開時入口通道不再碰到原屋舍實心碰撞，只在暗處後牆及其後方碰撞終止。
- 備份及先前測試在 `_CodexBackups/silent_watch_20260925/`；本次測試在其 `final-verification/` 下，`watch-manual/` 保留 P／2 實際按鍵的一輪，`intervene/` 保留另一分支的退出狀態與倒地畫面。
- `watch-auto-exit/` 保留自動結束的一輪。所有測試均暫時關閉 PlayerPrefs 結果寫入；未更動使用者的進度紀錄。測試後已退出 Play Mode，臨時 `Assets/Editor/Chapter1WatchProbe.cs` 與 meta 已移出 Assets 並存於驗證資料夾，避免之後 Play Mode 自動觸發測試。
- 移除測試工具後完成最後一次 Unity 腳本重編譯，Console 顯示 0 Error、0 Warning；編輯器留在此場景的 Scene／Edit Mode。
- 本次尚未使用實體 VR 頭戴裝置，也沒有逐項重玩婚禮送酒、食物與舞蹈互動。

## 修正 GitHub 單檔超過 100 MB（2026-09-26）

- 接續「繼續生成警察離去背影圖」（`01a0d84e-08f4-7673-844f-0eafd889d03d`）最後的 GitHub Desktop 上傳問題。使用者明確要求待提交的單一檔案不得大於 100 MB；以更嚴格的 100,000,000 bytes 作為檢查上限。
- GitHub Desktop 實際使用上一層儲存庫 `C:\Users\jimmy\畢專_霧社事件\wwww`，分支 `main`，遠端 `41117b034-commits/wwww`。Unity 根目錄內另有 Git 登錄，不能以內層全部未追蹤的狀態代替真正待上傳清單。本次開始時，上一層儲存庫僅有 `wwww/Assets/Models/Chapter1Doorway/IncidentHutOpening.asset` 尚未追蹤。
- 門洞模型原為 163,381,756 bytes 的文字資產；現在為 81,700,300 bytes（81.70 MB）的二進位資產。保留全部 1,236,488 個頂點，沒有減面或降低精度。
- 新增 `Assets/腳本/第一章演出/Chapter1HutMeshAsset.cs`，用 `[PreferBinarySerialization]` 的主資產容器保留二進位格式，原 Mesh 留在相同資產中。模型 GUID `df746058c5f5aa34db82cde97416f821` 與 Mesh fileID `4300000` 均未改變；`.asset.meta` 的主物件改為容器，場景仍引用原 Mesh。
- `Chapter1HutOpeningAuthoring.cs` 在重建門洞後會維持上述格式，另提供 `Tools/Chapter 1/Store Hut Opening as Binary`，且檢查輸出必須小於 100,000,000 bytes。Unity 全域設定保持 `ForceText`，不切換整個專案的儲存格式。提交時需包含容器腳本及其 meta、模型及其 meta、建立工具和本交接紀錄。
- 初次嘗試全域格式切換時，Unity 額外重存了其他資產；已依工作開始時的乾淨 Git 狀態、完整備份與雜湊比對還原，額外產生的範例 Lighting 檔已移至備份。Git 索引內容亦核對維持不變。最終正式修改不包含其他場景、材質、Prefab 或 ProjectSettings。

### 本次實際驗證

- Unity 6000.0.58f1 成功編譯。透過獨立反序列化讀回二進位檔，逐位元比對頂點資料、索引資料、屬性排列、子網格與 Bounds 的 SHA-256，與轉存前一致；再次匯入、一般 SaveAssets 及重新載入第一章場景後引用均正常。
- 轉存前後網格資料 SHA-256：`20945D4AD5024610643ADCE4771EE6F99191553F37C4D2631AAE9B2A77C7785E`。第一章場景檔與工作開始前備份的 SHA-256 完全相同。
- 在 Editor Play Mode 以臨時驗證工具呼叫既有警察劇情與「沉默觀望」入口，跑到 `watch-complete` 並自動退出。關門等待 5.026 秒，屋內兩名角色渲染均隱藏、慘叫音效有非零輸出，女性釋放後水平位移為 0，結尾可見兩名警察離去背影；這輪 Error／Exception／Assert 為 0。
- 備份、資產資料比對、復原清單與本輪截圖存於 `_CodexBackups/hut_mesh_size_20260926/`；該資料夾以自己的 `.gitignore` 排除，不會把原本 163 MB 的備份加入提交。
- 重複執行單檔轉存後仍為 81,700,300 bytes，資料雜湊與引用不變。測試後退出 Play Mode，將兩份臨時 Editor 驗證腳本及 meta 移至上述備份的 `verification-tools/`；正式待提交清單共 6 個檔案，最大為 81.70 MB，全部小於 100,000,000 bytes，`git diff --check` 通過。
- 本次未實際執行 Git commit／push，也未使用實體 VR 頭戴裝置；沒有重玩婚禮任務或重測「上前阻止」。先前兩分支驗證仍屬 2026-09-25 的紀錄。

## 入場隱藏、選項掙扎與離場步態（2026-09-26）

- 依使用者三張截圖調整 `第一章新版警察` 的 Play → P 演出。正式變更僅五份 C# 腳本及本紀錄，不修改場景、模型或全域序列化設定。
- `Chapter1PerformanceController.PoliceEntrance.cs` 在建立退開名單前隱藏 `部落女姓2`、`賽德克帥哥`，避免由警察後方穿到前方。兩人只在警察劇情中停用，婚禮任務階段保留；控制器停用時恢復原先啟用的角色。後續觀望群眾名單也會排除兩人。
- `Chapter1PerformanceController.Doorway.cs` 在門口選項期間固定角色位置，但不凍結女性骨架。`Chapter1IncidentRig.cs` 新增原地掙扎：軀幹後拉、扭動，空出的手上下拉扯，雙腳交替向後抬起；被抓住的手與警察共用可達的固定抓手點。選擇任一分支後清除原地掙扎及固定抓手點，接回原本演出。
- 觀望結尾使用專用離場步態：略縮短步幅、降低骨盆使膝蓋保有彎曲空間、降低抬腳高度，加入小幅重心與肩部轉動、反向擺臂及持棍手擺動；警棍較靠近垂直。兩名警察的步伐相位錯開，移動前先轉向，起步與停步加入短暫加減速。

### 本次實際驗證

- Unity 6000.0.58f1 Editor Play Mode：第一輪以實際 P、2 按鍵完整播放。入場截圖與資料均確認兩名指定 NPC 已停用、可見渲染數為 0；直到觀望結束仍隱藏。離開 Play Mode 後再次確認兩人啟用且可見。
- 比對選項期間連續引擎截圖，女性的空手與雙腳均持續改變姿勢。最終版本 33 筆選項樣本的角色根位置 XYZ 範圍均為 0；雙腳垂直活動範圍各約 0.83 場景單位，抓手最大間距約身高的 0.000073，沒有因身體晃動而分開。
- 最終版本另以臨時 Editor 工具呼叫既有劇情入口，完整跑過「沉默觀望」及「上前阻止」。前者到 `watch-complete`、`completed=true` 並自動退出；後者到警棍擊中與倒地構圖，約三秒後自動退出，`completed=true`、`knockedOut=true`。兩輪 Error／Exception／Assert 均為 0，分支開始後掙扎旗標均已清除。
- 查看離場連續畫面，確認警察交替踏步、膝蓋彎曲、持棍手擺動，以及兩人步伐錯開；查看阻止分支的倒地引擎截圖，確認仍能完成原本結局。
- 原檔備份、逐段引擎截圖與骨架資料保存在 `_CodexBackups/police_motion_20260926/`，以其 `.gitignore` 排除提交。`watch-validation/` 為實際按鍵的一輪；`final-watch/`、`final-intervene/` 為最終版本的兩分支驗證，`final-watch/choice-metrics.json` 為選項動作量測。
- 測試暫時關閉 PlayerPrefs 寫入；未變更使用者進度。測試後已退出 Play Mode，臨時驗證腳本及 meta 移出 Assets，存於上述備份的 `verification-tools/`。
- 未使用實體 VR 頭戴裝置，未逐項重玩婚禮送酒、食物及舞蹈互動；本次未執行 Git commit／push。仍以單檔 100,000,000 bytes 為上限，原門洞模型保持 81,700,300 bytes。
- 移除測試工具後完成最後腳本編譯並回到 Edit Mode。真正的上一層 Git 儲存庫待提交共 6 檔，最大為 `Chapter1PerformanceController.cs` 的 586,000 bytes（0.586 MB），全部小於限制；`git diff --check` 通過，大小清單存於 `file-size-audit.json`。

## 恢復踢酒與人物動作（2026-10-02）

- 使用者表示在 GitHub 操作時誤刪修改，要求依五張截圖重做。實際確認目前演出已退回舊版，但 `_CodexBackups/motion_second_pass_20261002/` 保留今天的修正副本。本次先備份現況，再只恢復相關差異；保留現有的第二章轉場、VR 字幕橋接與字型修改，未重設 Git 或整個專案。
- 在 Unity Editor 內重新擺放並儲存 `酒杯`、`酒杯 (1)`、`無蓋酒甕 (1)`、`無蓋酒甕 (2)`。四件道具預設就在火堆旁、警察前方，底部貼地；`incidentGroundWineProps` 引用已存進 `第一章新版警察.unity`。沿用原有酒杯取物來源，沒有另造大型模型。
- `Chapter1PerformanceController.Confrontation.cs` 恢復走近、右腳踢擊與酒倒地流程。字幕精確改成「警察踢倒擺在地上的酒。」；鏡頭拉寬以容納警察全身及酒。翻倒與聲音等待腳尖進入接觸範圍後才觸發，恢復後另將觸發容差由身高的 3.5% 收緊為 0.8%。
- `Chapter1IncidentRig.cs` 恢復掌心、鞋底與骨架校正；警棍從掌心握持，無手指骨頭的警察模型使用執行時網格握拳，結束後清理，沒有把網格寫成新資產。警察及女性 FBX 的 meta 僅開啟 Read/Write，原 FBX 未改写。
- 女性空手改成靠近被抓手腕的保護動作，手掌跟隨前臂；進門前清除舊指向、對話及推擠姿勢，進屋、出屋與選項掙扎使用同一套可達抓腕位置。降低手腳擺動，保留原地小幅掙扎。
- 警察離場恢復左右腿長及髖骨水平校正、平滑踏地與抬腳曲線、較慢的行走速度及擺臂；`Chapter1PerformanceController.cs` 排除舊貼地元件覆蓋 IncidentRig 鞋底高度的情況。該主檔只新增這項判斷，保留目前第二章轉場程式。
- `Chapter1PerformanceController.Doorway.cs` 補上門口專用 OnGUI 的 fallback 字幕繪製，使屋內慘叫旁白、離場字幕與結尾旁白在沒有 DialogueUI 的目前場景仍可見；選項期間保留原本選單。

### 本次實際驗證與檔案大小

- Unity 6000.0.58f1 成功編譯。`watch-manual/` 為實際 P、2 按鍵完整重播；之後調整踢擊容差、鏡頭與離場字幕，再以同一劇情／選項入口執行最終回歸。
- 最終 `regression-watch/` 完成四件酒翻倒、關門等待 5.006 秒、屋內音效、拖出女性與離去構圖，結果 `beat=watch-complete`、`completed=true`，Error／Exception／Assert 為 0。查看連續引擎截圖確認新字幕、女性手腳與警察交替踏步；離場字幕及結尾旁白均實際可見。
- 最終離場每位警察各 47 筆樣本，左右整腿長度最大差約 0.00102 場景單位，左右髖骨高度差不超過 0.00049。女性選項期間與釋放後的根位置最大位移均為 0；詳細結果在 `regression-watch/motion-metrics.json`。
- 初輪曾出現兩次 Unity 編輯器 TextCore 字型 Assert，呼叫堆疊來自 `UnityEditor.AppStatusBar.DrawStatusText` 的字型圖集建立；後續兩輪觀望未再出現。編譯仍有專案原有 obsolete API／未使用欄位警告，不宣稱整個專案零警告。
- 備份、當次測試工具、事件紀錄、量測與截圖放在 `_CodexBackups/recovered_motion_20261002/`，由資料夾自身 `.gitignore` 排除提交。測試關閉章節結果 PlayerPrefs 寫入。
- 以較嚴格的 100,000,000 bytes 檢查正式資產及待提交檔案。門洞模型仍為 81,700,300 bytes（81.70 MB），所有 `Assets`／`Packages`／`ProjectSettings` 檔案均低於上限；本次場景及程式修改均小於 1 MB。未做 Git commit／push。
- 本次未使用實體 VR 頭戴裝置，未逐項重玩送酒、食物與舞蹈任務；未驗證第二章內容或整段跨章轉場。
- 最終 `regression-intervene/` 以既有劇情／選項入口跑到揮棍命中與倒地，倒地約三秒後得到 `completed=true`、`knockedOut=true`，Error／Exception／Assert 為 0。驗證工具在取得完成結果後退出 Play Mode；目前主程式會提出第二章轉場要求，這次沒有把跨章載入當作驗證目標，也沒有還原成舊版退出程式。
- 測試後已退出 Play Mode。把原本留在 `Assets/Editor`、每次播放都會寫入舊驗證目錄的臨時 `Chapter1WatchProbe.cs` 及 meta 移出 Assets，最新驗證版本存於本次備份的 `verification-tools/`，開始前版本也有備份；正式播放不再自動產生這些測試 JSON／截圖。
- 收尾再次 `git diff --check` 通過；上一層實際儲存庫的待提交現存檔案最大為既有字型變更 2,279,112 bytes，本次場景 783,279 bytes。已確認新備份目錄由 Git ignore 排除。
- 移出工具後完成最後編譯、重新載入已儲存的第一章場景；Unity 留在 Scene／Edit Mode，該次收尾 Console 畫面為 0 Error、0 Warning。

## 踢酒與行走修正收尾核對（2026-10-03）

- 使用者要求查看並繼續「修正警察踢酒與行走姿勢」（`01a0fc85-2e7e-7572-91f8-259304290859`）。讀取原對話後確認：前次已完成修正、兩條分支測試與臨時工具移出，只在最終回覆前因用量限制中斷，沒有新的姿勢要求待實作。
- 本次唯讀核對程式及場景：四件 `incidentGroundWineProps` 引用已儲存，字幕為「警察踢倒擺在地上的酒。」，踢擊以腳尖接觸觸發翻倒；掌心握棍校正、女性抓腕姿勢、離場步態與 fallback 字幕程式仍在。未重新覆寫已完成的程式或場景。
- 實際上一層 Git 儲存庫已有本機提交 `3d63c23`（2026-10-02 20:35:32 +08:00，訊息 `123`）；開始核對時工作目錄乾淨。此次只更新本交接檔，未執行 commit／push，也未確認遠端上傳狀態。
- 重新讀取先前 `regression-watch/verified-result.json` 與 `regression-intervene/verified-result.json`：前者 `watch-complete`、`completed=true`，後者 `completed=true`、`knockedOut=true`，兩者 `errors=[]`。踢擊紀錄包含四次腳尖接觸；本次目視核對保存的踢酒、進屋與離場引擎截圖。以上 Play Mode 結果屬 2026-10-02 的測試，本次沒有重新播放或宣稱新一輪動態驗證。
- 本次透過 Unity 視窗實際確認目前為 `第一章新版警察` Scene／Edit Mode，Console 畫面顯示 0 Error、0 Warning；`Assets/Editor/Chapter1WatchProbe.cs` 及其 meta 均已不存在，不會自動啟動舊驗證工具。
- 本次重新掃描 `Assets`、`Packages`、`ProjectSettings` 共 2,958 個檔案，沒有任何檔案達到 100,000,000 bytes。最大為既有石盤圖片 87,096,360 bytes（87.10 MB），門洞模型仍為 81,700,300 bytes（81.70 MB）。
- 踢酒與人物姿勢這次需求已完成。實體 VR、婚禮任務逐項重玩、第二章內容與跨章轉場仍未於本次驗證。

## 單次踹酒與手腳修正最終驗證（2026-10-03）

- 接續「修改」（`01a0fd99-e0c5-7682-8187-2a1d173f1a73`）最後一輪，使用者要求修正握棍手勢、一次大力踹倒四件酒、女性歪腳及兩名警察的手腳與離場步態。前一節的完成核對早於這些新要求，不能代表這一版已測完。
- 讀取原對話與目前程式後確認：最新 `Chapter1IncidentRig.cs`、`Chapter1PerformanceController.Confrontation.cs` 已存在本機提交 `586a4ad`（2026-10-03 01:59:08 +08:00）。前次中斷前又降低持棍手、調整警棍朝向、限制指向時手腕折角並修正握拳網格法線；該最後版本尚未完整回歸。本次保留此版本，未重寫已完成的場景與動作。
- 最終演出使用一次蓄力抬膝與快速前踹，在腳尖接觸同一幀啟動四件酒的翻倒；腳掌根據實際網格鞋底和腳趾方向校正。握棍沿用掌心定位與執行時握拳；空手沿前臂方向自然垂下；離場步伐的支撐段依角色位移推進相位，減少腳底滑動。
- 本次在 Unity 6000.0.58f1 的 `第一章新版警察` 實際 Play → P → 2 跑完觀望，`resumed-watch/verified-result.json` 為 `beat=watch-complete`、`completed=true`、`errors=[]`。單次接觸紀錄為 `Single impact: 4 props; toe distance=0.000`；四件酒在同一筆樣本開始旋轉，最終各翻倒約 90 度。關門等待實測 5.069 秒。
- 本次目視核對引擎截圖的握棍、抬膝踹酒、門口掙扎、進出屋、女性放手站姿與警察離去連續畫面。觀望離場兩位警察各 47 筆樣本，左右腿長最大差小於 0.001 場景單位，腳尖相對行進方向偏角小於 6 度；女性釋放後 156 筆樣本根位置位移為 0、腳底保持水平，空手手腕與前臂對齊。詳細量測在 `resumed-watch/motion-metrics.json`。
- 本次另外用同一劇情及選項入口重播「上前阻止」，紀錄揮棍命中與倒地畫面；`resumed-intervene/verified-result.json` 為 `completed=true`、`knockedOut=true`、`errors=[]`。這些無錯誤結果僅涵蓋第一章分支完成前，不代表第二章或整個專案沒有問題。
- 第一章完成後，既有轉場自動載入 `第二章`；隨後記錄到錯誤：`[Chapter1] Could not replace wedding dancer 賽德克青年 because Humanoid donor 賽德克中年(2) was not available. The original actor was kept.` 原演員保留，第二章內容尚未驗證；此問題在本次動作修改範圍外，另列待辦。收尾前 Console 計數為 1 Error、24 Warnings（含跨章錯誤與原有警告），不宣稱全專案零錯誤或零警告。
- 原始檔備份及本輪測試來源 SHA-256 存在 `_CodexBackups/motion_refine_20261003/resumed-20261003/`；本輪資料、截圖與量測為 `_CodexBackups/motion_refine_20261003/resumed-watch/`、`resumed-intervene/`。這些路徑均由 `motion_refine_20261003/.gitignore` 排除提交。
- 已退出 Play Mode 並恢復第一章。將已被前次提交帶入的臨時 `Assets/Editor/Chapter1WatchProbe.cs` 與 meta 移至 `_CodexBackups/motion_refine_20261003/verification-tools/`，正式播放不再安裝測試指令輪詢與截圖／JSON 採樣。兩輪測試停用第一章結果 PlayerPrefs 寫入。
- 本次掃描正式資產，所有檔案均低於 100,000,000 bytes；最大仍為石盤圖片 87,096,360 bytes，門洞模型 81,700,300 bytes。未執行 Git commit／push；未使用實體 VR 頭戴裝置，未逐項重玩婚禮任務。第二章缺少角色的替換錯誤仍待後續處理。
- 移出工具後已執行 Unity Refresh，腳本編譯 ExitCode 0、程序集成功重新載入；收尾日誌存於 `resumed-20261003/final-compile-log.txt`。兩份正式動作腳本的 SHA-256 與測試開始前完全一致；`git diff --check` 通過。本次待提交僅刪除臨時 Probe 及 meta、更新本交接檔，動作修正仍在既有提交中。

## 踢酒、酒桶與黑幕退出接續收尾（2026-10-04）

- 使用者要求接續兩張截圖的未完工作：踢酒腳角度自然、兩個酒桶落地且遠離火堆、警察離場右手離開身體，以及兩個選項都在全黑後結束 Play Mode。
- 接手時正式修改已在目前檔案中，且四份相關程式與場景的 SHA-256 均符合 `_CodexBackups/kick_grounding_20261003/verified-source-hashes.json`。該資料夾實際還有 10 月 3 日晚間的兩分支退出紀錄，早期交接尚未涵蓋。本次保留這些正式修改，重新播放驗證、移出殘留 Probe 並補齊交接；沒有重寫已完成的動作。
- `Chapter1IncidentRig.cs` 使用較低的向前踢擊，抬腳蓄力與腳掌角度降低，接觸時保留小腿向前伸展空間；離場空手以獨立手臂目標和擺動計算，右手不再貼著腰側。
- `Chapter1PerformanceController.Confrontation.cs` 沿火堆外側並略向外踢倒四件酒，以模型表面支撐點對地形計算高度，避免旋轉後的包圍盒空角造成酒桶懸空。場景內的四件道具亦已往火堆外移；保留原角色、背景及「警察踢倒擺在地上的酒。」字幕。
- `Chapter1ToChapter2Transition.cs` 在 Editor 中完成淡黑及短暫黑幕停留後設定 `EditorApplication.isPlaying=false`；建置版保留第二章載入分支，本次未測建置版。

### 本次實際驗證

- Unity 6000.0.58f1，場景 `Assets/Scenes/第一章新版警察.unity`。先完成腳本重新載入，再以實際鍵盤 P → 2 跑完觀望、另開一輪以 P → 1 跑完阻止。第二輪另在踢中瞬間暫停目視腿部角度，再恢復播放。
- 觀望：`watch-complete`、`completed=true`、`knockedOut=false`；阻止：完成揮棍與倒地，`completed=true`、`knockedOut=true`。兩輪均記錄 `black-screen.json` 的 `endingAlpha=1`、實際全黑截圖、正式程式的 `Black screen complete; stopping Play Mode.`，以及 `returned-to-edit.json` 的 `playing=false`，場景仍是第一章。Probe 未代為停止這兩輪播放。
- 兩輪 Error／Exception／Assert 均為 0；測試後 Console 計數為 0 Error、14 Warnings，不能稱全專案零警告。測試期間暫停章節結果 PlayerPrefs 寫入；未重玩婚禮送酒、食物與舞蹈任務，未使用實體 VR。
- 本輪觀望單次碰撞同時踢動 4 件酒，腳尖到接觸點誤差約 0.000245 場景單位。核對踢擊、倒下後、離場連續引擎截圖與第二輪接觸停格。
- 倒地後完整模型所有頂點的地形最小間隙：兩個酒杯約 0.0201、0.0198，兩個酒甕約 0.0184、0.0162 場景單位。兩酒甕中心到火堆／舞圈中心的水平距離分別由 21.856、21.753 增為 25.726、25.199；畫面中已在火堆外側地面。
- 離場兩名警察各 46 筆樣本，右手相對右肩沿角色外側的距離至少約 0.869 場景單位；前後擺幅約 1.738、1.715。目視可見手與身體之間的空隙及自然擺動。
- 本輪截圖、事件、完整頂點量測與來源雜湊存於 `_CodexBackups/kick_grounding_20261004/`，其中 `watch-manual/`、`intervene-manual/` 為兩輪實測；`watch-motion-summary.json` 為摘要。測試前後五個正式檔案雜湊一致。
- 已自動退出 Play Mode，將 `Assets/Editor/Chapter1WatchProbe.cs` 和 meta 移至該備份下的 `verification-tools/`；正式遊戲不再輪詢測試命令或輸出測試截圖。備份由自身 `.gitignore` 排除提交。
- 正式資產大小掃描未發現達到 100,000,000 bytes 的檔案；最大石盤圖片 87,096,360 bytes，門洞模型 81,700,300 bytes。本次未 commit／push，沒有修改 Git 的 safe.directory 設定。
- 此次指定的第一章要求已完成；先前第二章角色 donor 缺失問題未處理或重測，Editor 現在於第一章黑幕後結束，這兩輪沒有進入第二章。

- 移出 Probe 後 Unity 已完成最後程序集重編譯（Assembly-CSharp-Editor.dll 已更新），Console 畫面為 0 Error、0 Warning、0 Log；回到 Scene／Edit Mode。這是清理後的編譯狀態，不抹除上述播放期間的 14 項警告。
