# Unity 霧社事件專案交接

更新日期：2026-10-10。這份文件用來讓新的 Codex 對話辨識同一個 Unity 專案，避免使用者反覆說明背景。內容是截至本次整理的紀錄，後續狀態以實際檔案和使用者當次要求為準。

最新顯示修復：2026-10-10 Unity Game 預覽水平5倍／垂直1倍的異常縮放已透過介面恢復等比例置中，放大分頁也確認正常；遊戲程式及場景未改。

最新外觀：2026-10-10 人物問候氣泡已改為深灰綠底、米金色文字及橄欖綠回覆按鈕；Unity 編譯及實際遊戲預覽確認完成。配色驗證見文末。

先前功能狀態：2026-10-10 第二章已接上第一章分支記憶，新增森林行走邊界及人物問候氣泡（早安／你好、回覆 E），移除中央綠色交談框。第一章上前阻止結尾漏存結果已補齊。最終原專案 Editor 測試九人交談、四邊四角阻擋／走回、異常位置恢復、三次真實雲端記憶問答及到期帶路至 TreeChoice 通過，errors=0、warnings=1（既有停用音源）。正式探索設定仍180秒；詳細範圍見文末。

目前雲端狀態：使用者已自行完成免費金鑰設定，本轮實際問答 Provider 為 Free cloud Gemma: gemma-4-26b-a4b-it，收到三次成功回答。下方舊「免費雲端 Gemma 接線」段落的「尚未提供 key」只代表當時接線驗證，已被後續實測取代。本輪沒有改金鑰、開啟計費或更換模型；雲端失敗仍不自動切本機。

前次模型狀態：2026-10-10 第二章自由探索 NPC 已切換到免費本機 Gemma 4 E2B IT Q4_0（D:/WusheLocalLLM，約2.84 GB），Qwen 檔案保留可切回。補強玩家／NPC 姓名指涉及警察、小孩身分限制。Unity 七次真實問答完成、errors=0；但同時跑 Unity 的首問較慢（初次約56秒，換NPC約30–34秒，接續問答約2–6秒）。詳細實測、操作及限制見文末「2026-10-10 Gemma 本機試用」。

前次狀態：2026-10-10 完成第二章人名與近距迎接：全九位白天人物可交談，帶路者阿威·比胡介紹後巡走，督工警察中村正雄加入LLM對話；探索及當次回答／閱讀結束後，阿威先移到玩家附近再短程走來。巨木旁命令與回話採雙人構圖，阿威向警察走近再面向他說話。夜間四位原有角色與身分字幕保留，其他配角補上姓名。詳細驗證與限制見文末「2026-10-10 人名與近距迎接」。

前次帶路狀態：2026-10-10 更新第二章探索後單人帶路。三分鐘到期並等当次回答／閱讀結束，原帶路族人先尋路走到玩家當時視線前方，再面向玩家邀請跟隨，獨自帶路到原巨木。另兩位原隊員移至巨木左右，各自約0.9公尺內巡走，進入巨木對話前停下，保留後續救援與受驚演出。原五位路邊人物加這兩位，共七位可在3公尺內交談；底部「靠近路邊人物……」常駐字串已移除。驗證證據在 _CodexBackups/chapter2_single_escort_20261010/，詳細限制見文末。

前次 LLM 功能：2026-10-09 完成影片後三分鐘探索及免費本機文字對話；模型已下載並驗證，安裝在 D:/WusheLocalLLM，沒有付費 API 或外網備援。該輪 final-local-v2 與 expiry-pending 為新增帶路調整前的驗證，不能代替本輪路徑與同伴演出檢查。操作見 Documentation/Chapter2-local-dialogue.md。

前次場景佈置：2026-10-09 已將第二章 Play → P 的四棟會議房屋校正水平並貼地，修復傳統建築的遺失網格引用；開場九位成年人全部坐著，左側原站立兩人補上座椅，右前方加入坐矮椅的原住民小孩，共十人。小孩全程坐著聆聽；原劇本成人起身仍保留。該輪 final-support／final-refuse 均 checks.passed=true、errors=0、結尾自行停止 Play Mode，第一章場景檔案未改。證據在 _CodexBackups/council_seats_houses_20261009/，細節與驗證限制見文末。

前次畫面修正：2026-10-09 已完成達多握刀時手腕順著前臂、所有發言者抬頭對鏡頭、莫那起身宣告時右手握緊拳頭；四位字幕人名加上使用者指定身分。保留原營火／人物／倒木場景，外圍沿用第一章模型與材質的房屋。本輪座位調整保留這些演出與字幕。

上次劇本更新：2026-10-09 已將第二章 Play → P 的夜間會議換成使用者提供的新劇本：莫那·魯道、達多·莫那、巴萬·拿威、瓦旦為具名發言者，依指定分行逐行字幕。同意分支插刀／全員起身／拉遠與決戰字卡；拒絕分支玩家走近莫那、斥責與指向側面出口後直接淡黑。final-support 35 行、final-refuse 34 行已經正式輸入路徑與引擎畫面檢查，兩輪 checks.passed=true、errors=0、退出後 playing=false。人物為既有模型與程序肢體動作，未新增配音及專用細部臉部動畫；其他限制與資產序列化處理見文末。

前次快捷功能：2026-10-08 已新增第二章 P 鍵快速進入夜晚秘密會議。Play 後點一下 Game 視窗，片頭或白天流程中按 P 即進入營火全景，再接原本三秒開場與對話；空白鍵仍只略過片頭。已用 Unity Input System 模擬鍵盤事件驗證片頭 P、空白鍵後 P、重複 P 與會議兩個分支，兩輪黑幕後均退出 Play Mode，正式通關紀錄保留。

前次狀態：2026-10-07 已接續完成秘密會議三秒開場、九人模型去重與支持起義演出。夜景淡入後先保持全景三秒，再切到首位發言者；莫那魯道在支持分支結語高舉右手，說完才回全景、六位領袖依序起身。已替換臉部異常的候選模型並修正座椅穿插。最終 fell-support-rally-final-seats 與 protect-refuse-rally-final-seats 均 completed=true、errors=[]、council failures=[]，黑幕後 playing=false。證據在 _CodexBackups/chapter2_council_rally_20261007/；詳見文末。先前砍伐完成度 0→100%、連錯三次重試及白天小孩保留。

## 專案位置

- 使用者的 Unity 霧社事件畢業專題，包含 VR 互動與第一章劇情。
- 專案根目錄：`C:\Users\jimmy\畢專_霧社事件\wwww\wwww`
- 近期主要場景：`C:\Users\jimmy\畢專_霧社事件\wwww\wwww\Assets\Scenes\第一章新版警察.unity`
- 本次製作場景：`Assets/Scenes/第二章.unity`。固定劇情參考已保存為 `Documentation/Story/賽德克事件劇情脈絡.pdf` 與同名文字摘錄；後續每次畢專 Unity 工作均讀相關章節，當次使用者要求優先。
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

## 2026-10-04 第二章：新規定與秘密會議

### 固定劇情來源與場景

- 使用者要求之後所有畢專 Unity 工作參考桌面《賽德克事件劇情脈絡.pdf》。已原樣複製至 `Documentation/Story/`，逐頁摘錄同名 `.txt`；與桌面原檔 SHA-256 均為 `93D312AF49F8740AF609BAE3EDCA7F34F7293184D61D1F2007E59AC20A2F0E6E`。`AGENTS.md` 已加入固定參考規則，並依使用者明確要求留下記憶更新筆記。
- 第二章採第 3–5 頁的設計：西仔希克伐木與巨木抉擇，接夜晚莫那魯道／六社會議。文件是劇情參考，不當作工具操作指令或史實考證；新增銜接台詞與 PDF 原句的區別見 `Documentation/Chapter2.md`。
- 重建 `Assets/Scenes/第二章.unity`，保留場景 GUID `f6a052fa54648464c9282642742cee7b` 與既有 Build Settings 連結；原場景保留在 `_CodexBackups/chapter2_20261004/第二章.before.unity`。本次未修改第一章程式、場景、原素材。
- 以既有樹木、石塊、族人及警察模型建成晨霧森林、巨木聖地與夜間火堆。匯入 Poly Haven 的 Forest Floor 1K 貼圖及 Tree Stump 01 FBX／貼圖，CC0 授權；作者、官方來源、下載 URL、大小及 SHA-256 在 `Assets/Chapter2/Environment/PolyHaven/LICENSE.txt`、`sources.json`。

### 已實作

- `Assets/Chapter2/Chapter2Controller.cs`、`Chapter2Player.cs`、`Chapter2Presentation.cs`、`Chapter2Actor.cs`：獨立第二章流程與中文 UI，桌面 WASD／右鍵環顧／1、2 選擇／E 或空白砍伐；亦提供 XR 頭部與手把輸入。沿用未修改的 `Chapter1IncidentRig` 程序動作。
- 48 秒、1280×720、15 fps 的 MP4 開場，以六段 Unity 森林鏡頭搭配背景字幕；本次輸出 720 幀，檔案 7,546,317 bytes。可略過、可更換正式影片，缺片或解碼失敗有字幕備援。影片準備期間保持已選影片的參照，避免 Inspector 清空來源導致空參照。
- 跟隨三名族人至巨木，落後時引路者等待；保護／砍伐二選一。保護有警察威脅、黑幕槍聲及一名族人倒下；砍伐有距離／面向／冷卻檢查、節奏綠色區間、木材完整度、五次有效砍伐、錯誤受損、歸零重試及樹倒演出。
- 淡黑轉夜晚：莫那魯道與六位領袖發言；支持有自由生活的期望，拒絕有保守派爭執離場。兩路均有莫那魯道站起、關鍵演說、桌面近景／拉遠和「決戰的時刻，將至。」黑幕。VR 保留頭部視角。
- Editor 全黑後自行結束 Play Mode；建置版有返回章節選單按鈕。結果寫到獨立 `WusheEvent.Chapter2.Result`，尚未接第三章。
- `Assets/Editor/Chapter2SceneAuthoring.cs`、`Chapter2FilmAuthoring.cs` 為正式維護工具，選單 `Tools > Chapter 2`。重建工具會重建第二章，未來手工調整前請先另存場景副本。詳細玩法及來源見 `Documentation/Chapter2.md`。

### 本次驗證與證據

- 在 Unity 6000.0.58f1 原專案實際進入 Play Mode，以臨時 Editor driver 移動玩家、觸發 UI 按鈕及砍伐，流程加速為 3 倍；影片／淡黑保留原本不縮放的計時。不是實體 VR 或人工完整輸入驗收。
- 本次四種組合均有 `completed=true`、`errors=[]`、全黑截圖，以及正式流程自行回 Edit Mode 的 `playing=false`，driver 未代為停止成功的測試。記錄在 `_CodexBackups/chapter2_20261004/`：

| 組合／驗證 | 成功紀錄前綴 | 額外檢查 |
| --- | --- | --- |
| 保護＋支持 | `protect-support-skip-v2` | 略過片頭、重複選擇被阻擋 |
| 砍伐＋支持 | `fell-support-skip-retry-v2` | 5 次錯誤至歸零、重試後 5 次有效砍伐、遠距砍伐被阻擋 |
| 保護＋拒絕 | `protect-refuse-skip-v3` | 拒絕台詞與保守派離場、黑幕退出 |
| 砍伐＋拒絕 | `fell-refuse-full-final` | 實際 MP4 播完，`videoPlayed=true`、`videoFinished=true`，5 次有效砍伐 |
| 缺片備援＋保護支持 | `protect-support-fallback-verified` | Start 前移除影片來源，字幕備援接遊戲並完成，未播放影片 |

- 前三組測於最終美術小修前；之後處理樹冠多餘樹幹、營火黑球及材質，重新輸出影片。人工檢視引擎渲染的片頭、森林、砍伐 UI、會議與黑幕截圖；角色仍屬既有模型與程序姿勢的初版，不代表精修動畫已驗收。
- 過程中曾有警察未啟用、引路等待及測試期間清空片頭來源造成的失敗，均有後續修正與成功重測。舊失敗輸出保留作診斷，不能混作成功證據。
- Play Mode 啟動曾有 13 筆 `The referenced script (Unknown) on this Behaviour is missing!` 警告；第二章階層逐物件檢查未找到 Missing Script，警告來源尚未定位。另有 WindowsMediaFoundation 色彩資訊回退警告與 URP 點光陰影圖縮小訊息。不能稱全專案零警告。

### 尚未驗證與後續製作

- 此版本是第二章可玩初版。影片是場景鏡頭加字幕，尚無正式配音、完整搬木表演；莫那魯道／六社領袖暫用既有族人模型，角色、斧頭與火焰仍需後續美術精修。
- 未使用實體頭戴裝置或控制器；未驗證 VR 手勢舒適度、效能、頭部高度與畫面配置，亦未做獨立建置版／Android 實機測試。
- 不聲稱森林為西仔希克的實地復原；一般素材用來呈現 PDF 的森林、巨木與火堆敘事。
- 測試暫停結果 PlayerPrefs 寫入，正式結果持久化與第三章讀取仍需整合驗證；本次未重測第一章全部任務，未 commit／push。

### 最後回歸與清理（2026-10-04 22:11）

- 最後一輪 `fell-support-full-source-change-retry-verified` 在 22:10:50 完成：片頭開始準備後故意清空公開影片來源欄位，仍播完原影片；`videoPlayed=true`、`videoFinished=true`、`completed=true`、`errors=[]`。5 次錯誤砍伐後成功重試，再完成 5 次有效砍伐；距離限制及重複選擇檢查通過，正式結尾自行退出，`playing=false`。
- `protect-support-fallback-verified` 在 22:07:14 完成；這是 Start 前真的未指定影片的備援測試，與準備期間更動欄位的測試分開記錄。
- 最終場景仍指定正式 MP4、`saveResult=1`、`stopEditorAfterEnding=1`。`verified-source-hashes.json` 保存本次驗證的正式程式、場景與影片雜湊；`Documentation/Chapter2-forest-preview.png`、`Chapter2-night-preview.png` 為本次 Play Mode 引擎截圖。
- 已將 `Chapter2WorkProbe.cs`、`Chapter2Verification.cs` 及其 meta 移至備份內 `verification-tools/`。Unity 於 22:12:02 完成 Editor 程式集重新編譯；清理後 Console 畫面為 0 Error／0 Warning／0 Log，Play 未啟動，第二章場景已儲存。這個編譯狀態不抹除前述播放／匯入期間的警告。
- 22:12 最終大小掃描涵蓋 Assets、Documentation、Packages、ProjectSettings 共 3,057 個檔案，沒有檔案達 100,000,000 bytes；最大 87,096,360 bytes，詳見 `asset-size-audit.json`。生成目錄 Library／Temp 與診斷備份不納入發佈資產掃描。
- 警察步槍目前是布景掛載，尚無正式握槍／開槍動畫；角色手部接觸、步態與斧頭外觀可在下一輪美術調整時逐鏡精修。

## 2026-10-04 第二章跟隨引導、巨木操作位置與伐木斧調整

### 使用者回饋與修改

- 依本次截圖與回饋調整第二章：需要第一章式黃色引導，族人應走到巨木而不是石頭旁，砍伐要能看見斧頭與實際揮砍。開始前重新讀取固定劇情參考第 3–5 頁；第一章原有程式與場景未改。
- 新增 `Assets/Chapter2/Chapter2RouteGuide.cs`：沿用第一章三段式黃色地面箭頭造型，引路者頭上有黃色菱形。先指向引路者，隊伍抵達後改指樹幹前方的操作點。跟隨時橫向範圍約 x=-3.2..3.2 公尺，不能搶跑超過引路者 1.1 公尺；離隊過遠時族人等候，畫面提示回到隊伍，仍可自由環顧。
- 族人終點改到巨木前側兩旁；原聖地地標石移至左側，清開隊伍行走走廊中的散落石塊。操作點改為 (0,0,9.3)，相對巨木 (0,0,12)；觸發範圍從 5.5 公尺縮為平面距離 0.7 公尺，站在原石頭位置不能進入抉擇或砍伐。
- 新增 `Chapter2Axe.cs` 與自製 `Geometry/ForgedAxeHead.asset`、獨立斧頭材質。木柄與鐵刃有可辨識的輪廓；抬斧、揮入、短暫接觸、收回一輪約 0.8 秒。採樹幹 MeshCollider 實際接觸點，斧刃接觸才計算砍伐與播放敲擊音、木屑、斧痕；同一揮砍不能重複輸入。
- 節奏與完整度移到右上角，避免擋住樹幹與斧頭；桌面選擇砍伐時將視角對準落斧位置，仍可繼續環顧。XR 頭部不強制轉向，未做實體 VR 驗證。
- 視覺驗證發現既有第二章樹皮材質的透明裁切設定會讓自製巨木樹幹與部分木製物件不顯示。已只在第二章材質副本關閉 AlphaClip／AlphaToMask 並使用 Opaque；修正後引擎截圖能清楚看見巨木和斧刃接觸處。原始樹皮素材未修改。
- `Assets/Editor/Chapter2InteractionAuthoring.cs` 提供保存場景後的局部更新；`Chapter2SceneAuthoring` 完整重建也套用相同配置。原第二章本輪備份在 `_CodexBackups/chapter2_guidance_20261004/第二章.before.unity`。

### 伐木工具參考

林業保育署嘉義分署〈阿里山的歷史影像（三）〉記錄日治初期以人力使用伐木斧等工具，來源 https://chiayi.forest.gov.tw/0000664 。據此選擇木柄鐵刃伐木斧作遊戲道具，不能據此證明西仔希克特定工具的形制。另查臺史博鉞斧藏品說明，該類主要用於倒木後的角材削製，因此未直接當成砍立木的工具復刻。詳細來源見 `Documentation/Chapter2.md`。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Play Mode：以驗證 driver 透過正式 `Chapter2Player.Move` 移動、點擊 UI 選項、呼叫正式砍伐輸入；跟隨及砍伐約 2 倍速度、夜間段 3 倍速度。未使用實體 VR，亦非全程人工鍵盤驗收。
- `protect-refuse-final`（23:15:30 完成）與 `fell-support-retry-final`（23:20:35 完成）均 `completed=true`、`errors=[]`，有黑幕截圖，正式結尾自行退出，`returned-to-edit.json` 均 `playing=false`。本輪沒有重跑其餘兩種會議組合，不把前輪四組證據算成這次全組回歸。
- 兩輪均通過左／右邊界、避免搶跑、原石頭位置不觸發、操作點近距離才觸發，以及三名族人到達新終點檢查。選項出現時與操作點距離約 0.376／0.365 公尺；跟隨截圖記錄到 4 支可見黃色箭頭。
- 砍伐輪另通過遠處、背對、重複輸入的拒絕檢查；5 次錯誤使完整度歸零，再重試並完成 5 次有效砍伐，共 10 次接觸。斧刃尖端到 MeshCollider 命中點最大誤差約 0.000000203 公尺；這是程式幾何量測，另有人工檢視抬斧、接觸、收回及樹皮斧痕的引擎截圖。
- 視覺接受以 `fell-support-retry-final-*` 與 `protect-refuse-final-*` 為準；v1 的測試工具在對話時把玩家移回石頭旁而停住，v2 的樹幹尚未完成材質修復，均保留診斷但不作最終畫面證據。v3 為接觸視角微調前版本。
- 證據根目錄 `_CodexBackups/chapter2_guidance_20261004/`。最新可看 `Documentation/Chapter2-follow-preview.png` 與 `Documentation/Chapter2-axe-preview.png`。劇情選擇結果在測試中停用 PlayerPrefs 寫入；正式場景仍保留 `saveResult=1`。
- 播放仍有 URP 點光陰影圖縮小提示；先前的 Missing Script／影片色彩資訊警告問題未作為本輪修復範圍，不宣稱全專案零警告。
- 已重錄 48 秒開場影片（720 幀、匯入成功，7,509,642 bytes），讓修正後巨木外觀出現在片頭；本輪確認輸出／匯入，未把先前完整影片播放證據當作此新影片的重新播放驗證。
- 臨時驗證程式及 meta 已移到本輪備份的 `verification-tools/`，正式遊戲不再輪詢命令或輸出驗證截圖。清理前場景檢查為 `dirty=false`、`playing=false`，階層沒有 Missing Script。
- 最後正式資產掃描包含 Assets／Documentation／Packages／ProjectSettings 共 3,077 檔，無檔案達 100,000,000 bytes；最大仍為 87,096,360 bytes。來源雜湊與 `asset-size-audit.json` 保存在本輪備份。
- 清理後 Unity Editor 程式集於 23:35:03 完成重新編譯，Console 畫面為 0 Error／0 Warning／0 Log，Play 未啟動；這是最後編譯狀態，不能抹除播放／影片匯入期間已有的警告。

## 2026-10-05 第二章六項畫面回饋：接續中斷工作並收尾

### 範圍與正式修改

- 接續對話「規劃第二章場景與玩法」（`01a1063c-2ddd-78c1-933c-6501273751bd`）最後六張截圖的要求。重新讀取 AGENTS、此交接檔及劇情參考第 3–5 頁；沿用原專案與既有第二章修改。
- 右側跟隨界線收至 x=0.65，左側仍為 -3.2；保留不超前及落後等待。黃色菱形與地面箭頭旁增加即時公尺文字，抵達後改顯示前往巨木的距離。
- 引路族人先面向玩家、站定介紹，約 1.2 秒轉身指向巨木，邀請跟隨後才開始行走。介紹時暫停移動，結束後交還操作。
- 警察與族人說話時各有容納全身的鏡頭，再回到對峙構圖；新增 `Chapter2Controller.Performance.cs` 管理這些鏡頭及伐木視角還原。
- 巨木操作點從 (0,0,9.3) 後移到 (0,0,8.6)，拉遠 0.7 公尺；操作半徑為 0.5。玩家本體與攝影機一起還原到操作點，斧頭仍以 MeshCollider 實際接觸點揮砍。
- 保護分支中，族人走到警察前面阻擋，鏡頭保持兩人同框；警察舉槍、瞄準、開槍，短暫槍口閃光／槍聲／後座後族人後仰倒地，之後才淡黑轉夜間。`Chapter2Rifle.cs` 管理持槍及開槍，`Chapter2Actor.cs` 加入約 1.9 秒倒地與蒙皮最低點接地校正。
- 上次 v1 已跑過流程，但步槍前後方向仍有問題；這次在 Unity 套用並儲存 `Chapter2RifleAuthoring` 的 modelVersion=2，再用最終場景重測。舊 v1 畫面不作最終握槍驗收依據。
- 正式第二章場景為 `Assets/Scenes/第二章.unity`，仍有 `saveResult=1`、`stopEditorAfterEnding=1`。第一章場景與共用 `Chapter1IncidentRig.cs` 雜湊均與本輪開始前的 `chapter1-before.json` 相同。

### 本次實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode。臨時 driver 呼叫正式移動、UI 選項與砍伐入口，白天為 2 倍速、會議為 3 倍速；本輪略過片頭。不是全程人工鍵盤驗收、不是實體 VR 測試，也未重新播放驗證 48 秒 MP4。
- `protect-refuse-resume-v2` 與 `fell-support-retry-resume-v2` 均 `completed=true`、`errors=[]`，各自有黑幕截圖和 `returned-to-edit.json` 的 `playing=false`。成功測試由正式章節結尾自行退出，driver 未代停。
- 兩輪均檢查介紹時面向玩家且站定、左／右／前方界線、右界線仍看得到引路者、公尺文字及警察／族人全身入鏡，結果皆通過。另實際查看引擎截圖核對介紹、轉身、跟隨與兩段全身對話。
- 保護輪：警察與受擊族人均在鏡頭內，1 次開槍、倒地完成；槍口方向誤差約 0.205 度，左右握點距離約 0.048／0.032 公尺，倒地皮膚最低點 y 約 0.025。這些是程序量測；另逐張核對瞄準、開槍、倒下中途和躺地畫面，不宣稱為精細手指動畫。
- 砍伐輪：遠距、背對與揮砍中重複輸入均被拒絕；5 次失誤使完整度歸零，重試後完成 5 次有效砍伐，共 10 次接觸。操作點到樹幹接觸距離約 1.735 公尺，斧刃最大接觸誤差約 0.000000359 公尺。查看抬斧、接觸、收回畫面，確認拉遠後仍有木屑與斧痕。
- 本輪沒有重跑其餘兩種會議組合；沿用先前分支設計，不能把前一輪四組證據列為這次全組回歸。測試暫停 PlayerPrefs 寫入，正式場景設定保持啟用。
- 播放仍有既有 Missing Script 警告及 URP 點光陰影圖縮小訊息。場景逐物件檢查沒有 Missing Script，`scene-inspection.txt` 為 `dirty=False playing=False`；警告來源仍未定位，不能宣稱全專案零警告。
- 本輪啟動額外 Unity 程序出現應用程式錯誤；已關閉失敗訊息，恢復並確認既有編輯器可正常執行完整兩輪。此啟動問題與章節 runtime 的 `errors=[]` 分開記錄。
- 證據存於 `_CodexBackups/chapter2_performance_20261005/`。更新預覽：`Documentation/Chapter2-follow-preview.png`、`Chapter2-axe-preview.png`；新增 `Chapter2-introduction-preview.png`、`Chapter2-shooting-preview.png`、`Chapter2-fall-preview.png`。均為 Unity 引擎輸出。

### 後續限制

- 這六項回饋已完成場景套用與本輪桌面流程／畫面驗證。既有美術、程序人物動作與替代音效仍沿用初版；未新增正式配音或搬木影片表演。
- 尚未使用實體 VR 頭戴裝置與手把；未做獨立建置／Android 實機測試。XR 鏡頭採淡黑移動原點、保留頭部追蹤，舒適度仍需實機確認。
- 第三章結果讀取、正式 PlayerPrefs 整合，以及既有 Missing Script 警告仍不在此次六項修改範圍。未 commit／push。

### 清理及大小稽核

- 已將 `Assets/Editor/Chapter2WorkProbe.cs`、`Chapter2Verification.cs` 及其 meta 移入本輪備份的 `verification-tools/`，正式 Assets 不再包含命令輪詢或驗證 driver。來源雜湊保存於 `verified-source-hashes.json`。
- 本次正式資產大小掃描含 Assets／Documentation／Packages／ProjectSettings，共 3,128 檔；沒有單檔達 100,000,000 bytes，最大為 87,096,360 bytes。詳見 `asset-size-audit.json`；生成目錄與歷史診斷備份不列入發佈資產統計。
- 清理後於 2026-10-05 09:18 完成 Editor 程式集重新編譯；實際查看編輯器 Console 為 0 Error／0 Warning／0 Log，Play 未啟動、場景標題沒有未儲存標記。此為最後編譯狀態，不抹除前述播放期間的警告。


## 2026-10-05 第二章槍聲後兩位族人的壓抑悲憤反應

### 本次正式修改

- 使用者要求：保護巨樹、槍聲與一名族人倒地後，另外兩位族人轉身望向他、握拳，呈現生氣卻無能為力。已先讀 AGENTS、本交接檔及固定劇情參考第 3–5 頁。
- `Chapter2Controller.Protect()` 僅為 workers[1]、workers[2] 準備 `Chapter2GriefReaction`，在實際 `rifle.Fire()`／`BeginFall()` 後觸發。兩人相差 0.18 秒受驚，約 1.65 秒完成轉向、約 2.45 秒收緊雙拳；肩背緊繃、一次欲上前又忍住的前傾、低頭望向受擊者，持續到白天淡黑。原場景、鏡頭、角色位置、槍聲及台詞保留。
- `Chapter2GriefReaction.cs` 在共用骨架和 Chapter2Actor 之後施加姿势，避免動作每幀被重設。`Chapter2GriefHandPose.cs` 與 `Assets/Chapter2/Resources/GriefHands/` 保存兩個專用握拳 blend shape 與手掌方向；處理模型缺少完整手指骨架的情況，沒有新增面部表情骨架。
- 握拳網格是第二章專用壓縮二進位副本，分別 33,269,200、36,541,964 bytes；不要將它們轉為大型文字資產。模型 FBX 與原匯入 meta 最終已還原；原第一章、第二章場景及共用 `Chapter1IncidentRig.cs` SHA-256 與本輪開始時相同。沒有改動 Chapter2Actor 或第一章程式。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode，臨時 driver 走正式移動、選項與砍伐入口。略過片頭；跟隨 2 倍、槍聲後 1 倍、夜間 3 倍。未使用實體 VR，亦非全程人工鍵盤驗收。
- 最終 `protect-refuse-grief-v3`：兩位族人的 turn=1、fists=1、面向倒地者的水平 dot=1；各自 `Chapter2 clenched fists=100`，只有兩個反應元件。實際檢查轉身、雙人全景、兩位各別近景與槍放下後仍維持姿勢的引擎截圖；1 次開槍、倒地完成，夜間拒絕分支及黑幕正常，`completed=true`、`errors=[]`，正式結尾自行退出，`playing=false`。
- 最終 `fell-support-grief-regression`：5 次有效砍伐、遠距／背向／揮砍中重複輸入拒絕檢查通過；反應元件數為 0。支持分支及黑幕正常，`completed=true`、`errors=[]`、`playing=false`。本輪未重做完整度歸零重試、另兩個會議組合、完整 MP4 播放或獨立建置測試。
- v1 的近景仍張手；v2 資產引用失效，均是診斷版本，不能作最終握拳證據。最終以 v3 及砍伐回歸為準。證據在 `_CodexBackups/chapter2_grief_20261005/`，正式预覽為 `Documentation/Chapter2-grief-preview.png`、`Chapter2-grief-male-preview.png`、`Chapter2-grief-female-preview.png`。
- 資產製作途中曾切換序列化格式而觸發全資產重存，已切回原設定並改用單檔二進位匯出；第一章／第二章場景和共用骨架核對原雜湊一致。掃描時顯示舊示例場景 Missing Prefab 錯誤，不能與最終兩輪 runtime 的 `errors=[]` 混為同一結論。正式播放仍出現先前已記錄的 Missing Script 警告及 URP 陰影圖縮小訊息；未宣稱全專案零警告。
- 測試停用 PlayerPrefs 結果寫入，原場景 `saveResult=1`、`stopEditorAfterEnding=1` 保持原值。未 commit／push。


### 本輪最後清理

- `Chapter2WorkProbe.cs`、`Chapter2Verification.cs` 與其 meta 已移至證據資料夾的 `verification-tools/`；正式 Assets 不保留測試命令輪詢。握拳資產製作用程式副本保存在 `Chapter2GriefReaction.baking.cs`，正式 runtime 只讀取已產生的資產。
- 12:46:51 完成清理後 Editor 程式集編譯；12:47 實際查看 Unity Console 為 0 Error／0 Warning／0 Log，Play 未啟動、場景沒有未儲存標記。此編譯狀態不抹除前述資產製作與播放中的警告／診斷錯誤。
- 編輯器序列化模式的執行中值確認為 ForceText，磁碟 `ProjectSettings/EditorSettings.asset` 已恢復文字格式及 `m_SerializationMode: 2`。兩份握拳 mesh 仍保留單檔二進位壓縮格式。
- 最後掃描 Assets／Documentation／Packages／ProjectSettings 共 3,185 個檔案，沒有檔案達 100,000,000 bytes；最大 87,096,360 bytes。`asset-size-audit.json`、`verified-source-hashes.json`、`unchanged-originals.json` 保存大小與本輪來源核對。未將生成目錄或歷史備份計入發佈資產。


## 2026-10-05 第二章族人奔向倒地同伴與白天森林畫質提升

### 本次正式修改

- 依使用者本次要求，將上一輪微微前傾改成明顯的慌張奔跑；先讀取本交接檔、AGENTS 與固定劇情參考第 3–5 頁，直接修改原專案。
- `Chapter2GriefReaction.cs`：兩人槍響後錯開受驚，沿不同弧線跑向倒地同伴；增加明顯抬腿、擺臂、身體前傾與上下起伏，抵達後減速、俯身、伸手與低頭查看。保留第二章專用手部資產；奔跑時握拳、抵達時放鬆。沒有修改共用 `Chapter1IncidentRig.cs`。
- `Chapter2Controller.Protect()` 在真正開槍及開始倒地後啟動兩人的演出，落點分開放在傷者旁，保留玩家看見倒地者的空間；轉夜間前的等待延長一秒。
- `Chapter2ForestDetailAuthoring.cs` 的 Tools / Chapter 2 / Improve Morning Forest Detail 已套用並儲存第二章場景。地面沿用原森林落葉圖樣，升級為 4096×4096 的 diffuse／normal／AO；主要樹皮改為 2048×2048 pine bark 的三張貼圖。樹葉與胡桃木紋使用第二章專用副本，改善匯入品質及可用的法線貼圖，啟用 Trilinear／16 倍異向性過濾；沒有改動原始素材。
- 地面及巨木網格補上切線以正確呈現法線。道路兩側新增 100 株小型既有植物、無碰撞體，隨白天群組隱藏；保持原樹木、石頭、人物與路線布局。第二章共用樹材質也會顯示於夜間，夜間燈光與流程保留。
- `Chapter2ForestDetail.cs` 在白天啟用專用地面與暫時 URP 副本：桌面主光陰影 4096、4 層 cascade、渲染比例至少 1、High SMAA；切夜間／退出時還原地面、攝影機設定及原管線。XR 分支使用較低陰影解析度並保留其抗鋸齒設定，但尚未實機驗證。
- Unity Game View 從會放大低解析度的 Aspect 預覽改為 Full HD (1920×1080)，採符合視窗大小的縮放。新增高解析素材來源為 Poly Haven CC0：[forest_floor](https://polyhaven.com/a/forest_floor)、[pine_bark](https://polyhaven.com/a/pine_bark)；來源、大小與校驗值保存在 `Assets/Chapter2/Environment/PolyHaven/detail-sources.json`。
- 最新預覽為 `Documentation/Chapter2-follow-preview.png`、`Chapter2-rescue-running-preview.png`、`Chapter2-rescue-preview.png`。既有 grief 預覽屬上一輪原地反應，不代表最新動作。片頭影片未重錄。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode；臨時 driver 經正式移動、UI 選項與砍伐入口執行。略過片頭，跟隨 2 倍、槍聲後 1 倍、會議 3 倍速度；另人工檢視引擎截圖及 Unity 畫面，不宣稱全程人工鍵盤或實體 VR 驗收。
- 最終 `protect-refuse-rescue-v2`：`completed=true`、`errors=[]`、`rescueArrived=true`，1 次槍響、倒地完成、2 名反應者。兩人位移約 3.58／2.21 公尺，取樣最高速度約 3.91／3.27 公尺每秒；最小間距約 1.61 公尺，最後面向傷者。查看奔跑中與抵達後畫面，未見兩人互相穿身，傷者保持可見。資料量測與畫面檢查分開記錄。
- 白天 runtime 確認 `renderScale=1`、主光陰影 4096、SMAA、postProcessing=True、地面貼圖 4096、地面切線 14641、路旁植物 100。v1 為畫質套用前動作診斷，最終畫質與流程以 v2 為準。
- `fell-support-rescue-regression`：`completed=true`、`errors=[]`，5 次有效砍伐與 5 次接觸；遠距、背向與揮砍中重複輸入均被拒絕。此分支沒有開槍或啟動傷者反應。斧刃最大接觸誤差約 0.000000359 公尺，另查看接觸畫面。
- 兩輪都通過介紹時面向玩家／站定、跟隨界線、引路者可見及全身對話構圖檢查；`nightPipelineRestored=true`。均抵達各自夜間結尾、黑幕後由正式流程自行退出，`returned-to-edit.json` 均 `playing=false`、`completed=true`，driver 未代停。
- 測試暫停 PlayerPrefs 寫入；正式場景仍為 `saveResult=1`、`stopEditorAfterEnding=1`、`interactionDistance=0.5`、`rightBoundary=0.65`。未重做完整度歸零重試、另外兩個會議组合、完整 MP4 播放、独立建置或實體 VR／效能測試。
- 播放期間仍見既有 Missing Script 警告及 URP 點光陰影圖縮小訊息；沒有將 `errors=[]` 解讀成全專案零警告。新增高解析貼圖與陰影會增加渲染負擔，VR 實機的幀率仍待確認。

### 最後清理與保存

- 證據及原始備份：`_CodexBackups/chapter2_rescue_forest_20261005/`。臨時 `Chapter2WorkProbe.cs`、`Chapter2Verification.cs` 和 meta 均已移到其 `verification-tools/`，正式 Assets 不保留命令輪詢或測試 driver。
- 清理前編輯器檢查為 `playing=False dirty=False pipeline=PC_PipelineAsset`。清理後已完成重新編譯，20:35 實際查看 Console 為 0 Error／0 Warning／0 Log，Play 未啟動、場景無未儲存標記；此最終狀態不抹除播放期間的既有警告。
- 第一章場景、共用 `Chapter1IncidentRig.cs` 與 `ProjectSettings/QualitySettings.asset` SHA-256 均與本輪開始前一致，見 `unchanged-originals.json`；修改來源校驗值存於 `verified-source-hashes.json`。
- 正式檔案掃描含 Assets／Documentation／Packages／ProjectSettings 共 3,226 檔，無檔案達 100,000,000 bytes；最大 87,096,360 bytes。`asset-size-audit.json` 保留結果，生成目錄及歷史備份不計入發佈資產。未 commit／push。


## 2026-10-06 第二章兩位族人的低蹲姿勢修正

- 使用者指出上一版蹲姿像坐馬桶，希望蹲低且自然。依本次截圖調整 `Assets/Chapter2/Chapter2GriefReaction.cs` 的抵達後姿勢；先讀 AGENTS、本檔與劇情參考第 3–5 頁。
- 骨盆下降量從身高的 10.5% 加深至 29%，兩人比舊版低約 27～28 公分，重心略後移；兩腳小幅前後錯開、膝蓋向前外側彎曲、腳尖微外開。上身前傾至約 34 度，抵達後約 1.05 秒平順沉下，保留原奔跑演出。
- 手部改為一手扶膝、另一手低伸向同伴方向，掌面朝下並保留手肘彎曲，避免兩手同高懸在胸前。沒有改動角色模型或共用第一章骨架。
- Unity 6000.0.58f1 原專案 Editor Play Mode：最終 `protect-refuse-crouch-v3` 跑完保護＋拒絕，`completed=true`、`errors=[]`、`rescueArrived=true`，1 次槍響、2 名反應者、倒地完成；夜间還原原管線，黑幕後正式流程自行退出，`returned-to-edit.json` 為 `playing=false, completed=true`。
- 臨時 driver 透過正式移動和 UI 選項執行，略過片頭，跟隨 2 倍、槍後 1 倍、會議 3 倍。實際查看跑停後沉下的分時截圖、兩名族人近景、最後全景及 Unity 播放畫面。骨盆高度約 y=0.44／0.33，腳骨高度皆約 y=0.06；角色更低、雙腳保持地面接觸、傷者可見。量測只支持骨架位置，視覺自然度另以引擎畫面檢查。
- 預覽：更新 `Documentation/Chapter2-rescue-preview.png`（正式遊戲鏡頭），新增 `Documentation/Chapter2-crouch-preview.png`（驗證時移近攝影機、暫時隱藏 UI 的引擎近景；已還原攝影機與 UI，沒有改正式構圖）。v1／v2 為手部高度調整前的診斷版本，最終以 v3 為準。
- 本輪只修改保護分支專用反應元件，沒有重跑砍伐分支、另一個會議結尾、48 秒片頭影片、獨立建置或實體 VR。上輪砍伐測試仍屬歷史證據。播放有既有 Missing Script 警告及 URP 點光陰影圖縮小訊息，未宣稱全專案零警告。
- 原始程式、v3 測試輸出及來源雜湊保存於 `_CodexBackups/chapter2_crouch_20261006/`；兩章場景、Chapter2Controller.cs 與 Chapter1IncidentRig.cs 核對未變。清理前檢查 `playing=False dirty=False pipeline=PC_PipelineAsset`。
- 臨時 Chapter2WorkProbe.cs／Chapter2Verification.cs 與 meta 已移至本輪備份的 verification-tools；正式 Assets 不保留測試輪詢。正式檔案大小掃描含 Assets／Documentation／Packages／ProjectSettings 共 3,235 檔，沒有檔案達 100,000,000 bytes，最大 87,096,360 bytes。未 commit／push。
- 21:05 清理後已完成重新編譯，實際查看 Console 為 0 Error／0 Warning／0 Log，Play 未啟動、場景沒有未儲存標記。這是最後編譯狀態，不抹除播放中的既有警告。


## 2026-10-06 接續第二章四張截圖修改並完成驗證

- 接續對話「查明訊息重新連線失敗原因」（01a10bf5-645f-7c73-94bd-6cf248041603）最後尚未完成的 Unity 修改；本次範圍為四張截圖，沒有重新處理較早的 Codex 連線診斷。已讀 AGENTS、本檔與劇情參考第 3–5 頁。
- `Chapter2Controller.cs`：警察字幕為「把這些樹都砍了。」；族人說「別碰它！這是我們的聖地。」時，用 2.1 秒走到警察前約 1.7 公尺，再對峙與被射擊。兩名同伴的新目的地跟隨阻擋者位置，沿用既有奔跑及低蹲查看動作。
- 槍後敘述從原約 5.3 秒延後顯示改為等待 0.35 秒即顯示，與倒地過程重疊；本輪有截圖負載的 driver 實測延遲約 0.67 秒，不將設定秒數誤稱為量測結果。
- `Chapter2Controller.Performance.cs`、新增 `Chapter2StartleReaction.cs`：砍伐完成後，警告字幕開始時三人依序縮身、抬手、仰頭，交錯後退三步，約 1.15 公尺；腳部使用落腳點及抬腳軌跡。依鏡頭切換前的玩家實際位置計算倒樹方向，倒下後再接族人台詞。
- 新增三個 `Resources/FearFaces/` 專用表情網格，抬眉眼、嘴部下沉，配合驚嚇動作；没有修改原模型 FBX、原匯入設定或共用骨架。新增 `Assets/Editor/Chapter2FearFaceAuthoring.cs`，Tools / Chapter 2 / Bake Fear Faces 在 Edit Mode 製作二進位資產，正式 runtime 只載入資產，不在播放時製作。三檔大小分別為 35,986,544、37,152,576、41,549,076 bytes，使用 Low mesh compression，勿整批重存成大型文字資產。
- 發現巨木改向玩家倒後會延伸進夜間會議區，已在 `SetNight()` 的淡黑期間隱藏砍倒的巨木與殘樁；保護分支保留站立巨木。夜間人物、火堆與字幕均經截圖確認不受遮擋。

### 本輪實際驗證與限制

- Unity 6000.0.58f1 原專案 Editor Play Mode，臨時 driver 經正式移動、UI 選項、揮砍入口，略過片頭；白天 2 倍、槍響後／砍樹後 1 倍、會議 3 倍。另逐張人工檢查引擎截圖與 Unity 畫面；不是全程人工鍵盤或實體 VR 驗收。
- 最終 `protect-refuse-four-final`：`completed=true`、`errors=[]`，新字幕正確；阻擋距離 1.7066 公尺、槍後字幕延遲約 0.67 秒、1 次槍響、倒地完成、兩位同伴抵達、兩人最小間距約 1.6077 公尺。實際核對近距對峙、早出字幕、跑向傷者與低蹲畫面。
- 最終 `fell-support-four-final`：`completed=true`、`errors=[]`，5 次有效砍伐／接觸，遠距、背向、揮砍中重複輸入皆被拒絕；三人各 3 步、位移約 1.15 公尺。倒向與事先保留的玩家方向 dot=1，倒樹軸與該方向 dot=1；已查看三步分時畫面、倒下中途與夜間畫面，`nightTreeCleared=true`。
- 兩輪皆通過開場面向／站定、左右與前界線、引路者可見、全身說話構圖；夜間還原原管線；各自抵達黑幕後由正式流程自行退出，兩份 returned-to-edit.json 皆 `playing=false, completed=true`，driver 未代停。
- v1 砍樹輪因 runtime 讀取不可讀模型而產生錯誤；v2 雖無 runtime 錯誤，但高壓縮表面粗糙，且同幀表情截圖重用了渲染快取，均不是最終表情驗收。v3 改用 Low compression，暫停動作並跨幀拍攝各角色 0／100 左右權重近景，確認三人眉眼／嘴部實際變化；原姿勢、攝影機與 UI 均還原。正式 final 兩輪沒有這段表情診斷暫停。
- 播放仍有既有 Missing Script 警告與 URP 點光陰影圖缩小訊息；資產製作中有已修正的編譯／模型讀取診斷錯誤，不能與最終 `errors=[]` 混為全專案零警告。本輪未測完整度歸零重試、另外兩個會議組合、完整 MP4、獨立建置或實體 VR。
- 測試關閉 PlayerPrefs 寫入；磁碟場景仍 `saveResult=1`、`stopEditorAfterEnding=1`、`interactionDistance=0.5`、`rightBoundary=0.65`。兩章場景、`Chapter1IncidentRig.cs`、`Chapter2GriefReaction.cs` 均與上次中斷前原始雜湊一致。
- 新預覽：`Documentation/Chapter2-close-confrontation-preview.png`、`Chapter2-early-caption-preview.png`、`Chapter2-startle-preview.png`、`Chapter2-fall-toward-player-preview.png`、`Chapter2-fear-face-preview.png`；更新 rescue／crouch 預覽。表情與蹲姿近景是驗證攝影機畫面，未替換正式構圖。
- 臨時 Chapter2WorkProbe.cs／Chapter2Verification.cs 及 meta 已移至本輪備份的 verification-tools；正式 Assets 不保留輪詢／測試 driver。原始檔、各輪診斷、最終結果、來源差異、雜湊保存在本輪備份。清理前 `playing=False dirty=False pipeline=PC_PipelineAsset`。未 commit／push。
- 最後正式檔案大小掃描 Assets／Documentation／Packages／ProjectSettings 共 3251 檔，達 100,000,000 bytes 的檔案共 0 個，最大 87,096,360 bytes；生成目錄與歷史備份不列入發佈資產。
- 2026-10-06 23:05 清理後重新編譯完成，實際查看 Unity Console 為 0 Error／0 Warning／0 Log，Play 未啟動，場景標題無未儲存標記。這是最後編譯狀態，不抹除前述播放警告與診斷版本錯誤。

## 2026-10-07 第二章抵達距離、雙手阻擋與槍後強迫伐木

- 本輪由 10/06 晚間開始，依使用者三張截圖直接修改原專案。已讀 AGENTS、本檔與劇情參考第 3–5 頁；當次要求優先於舊版「巨木保住了」的字幕敘述。
- `Chapter2Controller.cs` 新增 `treeArrivalDistance=1.5` 與平面距離判斷，場景已儲存此值。隊伍抵達後，玩家到黃色標記小於 1.5 公尺便自動進入對話；提示同步說明範圍。實際砍伐仍沿用 `interactionDistance=0.5`、面向檢查及樹幹接觸檢查。
- 新增 `Chapter2BlockingPose.cs`：族人走到警察前約 1.7 公尺後，雙手在約 0.65 秒內抬起，張開掌心朝警察；雙手高度略錯開，使側面鏡頭中兩手都可分辨。倒地時停止套用阻擋姿勢。從共用骨架完成的手部朝向取得校正，不新增大型網格或修改原模型。
- `Chapter2Rifle.cs` 新增槍口向下的收槍姿勢與動態瞄準點。槍響後立即開始約 0.42 秒收槍；倒地完成後才讓兩位族人奔向同伴，保留既有低蹲查看動作。
- `Chapter2GriefReaction.cs` 增加查看完成與站起狀態；兩人完全蹲下後留 2.2 秒查看，警察再舉槍，依序指向兩人的胸部高度，字幕為「日本警察：給我去砍樹」。之後字幕清空，兩人略微錯開，以約 1.4 秒平順站起、轉向，再沿分開的弧線走到巨木兩側。
- 新增 `Chapter2Controller.ProtectAftermath.cs` 管理上述命令、起身、回到樹邊的段落；兩人抵達後停留 1.2 秒，才使用正式流程的淡出轉夜間。保留原角色、森林、倒地者與對峙鏡頭；沒有新增實際砍伐演出或額外槍響。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode。臨時 driver 經正式移動、UI 選項、砍伐入口執行，略過片頭；跟隨 2 倍、槍後／倒樹 1 倍、會議 3 倍。另逐張查看引擎截圖及 Unity 播放畫面，並非全程人工鍵盤或實體 VR 驗收。
- 最終 `protect-refuse-sequence-final`：`completed=true`、`errors=[]`。1.51／1.50 公尺均不算抵達，從 1.51 前進 0.02 公尺後，在實測 1.49000025 公尺自動进入正式對話。最終砍伐回歸也通過相同邊界檢查。
- 保護分支確認雙手抬起、1 次槍響、倒地完成後才開始救援、兩人查看後才收到命令、完整站起後才行走、兩人抵達後才淡出。槍口收下的取樣時間約為槍響後 0.67 秒，含截圖負載；0.42 秒是設定時長，並非此輪實測。收下時槍方向 y 約 -0.68。最小兩人間距約 1.608 公尺。
- 最終畫面已核對雙手阻擋、瞄準、立即收槍、奔向傷者、低蹲查看、命令字幕、走回樹木及抵達後構圖。v1 是雙手重疊較多的診斷版本，正式以 final 為準。
- `fell-support-sequence-regression`：`completed=true`、`errors=[]`，5 次有效砍伐與 5 次接觸，遠距／背向／重複揮砍均被拒絕。三人後退各 3 步，倒樹方向與玩家方向 dot=1，夜間倒木已隱藏，另查看斧刃接觸及夜間會議畫面。
- 兩輪夜間都還原原渲染管線，正式結尾自行退出 Play Mode，兩份 returned-to-edit.json 皆 `playing=false, completed=true`，driver 未代停。測試關閉 PlayerPrefs 寫入；正式場景保留 `saveResult=1`、`stopEditorAfterEnding=1`。
- 播放仍見既有 Missing Script 警告與 URP 點光陰影圖縮小訊息。本輪未測實體 VR、獨立建置、完整片頭影片、完整度歸零重試或其餘兩個夜間組合；不將先前測試算成本輪驗證。

### 保存與清理

- 備份、兩輪最終報告、診斷 v1、逐段截圖、序列 CSV 及來源雜湊：`_CodexBackups/chapter2_protect_sequence_20261006/`。臨時 Chapter2WorkProbe／Chapter2Verification 與 meta 已移至其 verification-tools；正式 Assets 不保留輪詢及 driver。
- 第一章場景與共用 `Chapter1IncidentRig.cs` 雜湊核對未變，見 unchanged-originals.json。清理前場景 `playing=False dirty=False pipeline=PC_PipelineAsset`。第二章場景序列化差異僅新增抵達半徑及步槍兩個預設欄位。未 commit／push。
- 更新 `Documentation/Chapter2-close-confrontation-preview.png`、`Chapter2-rescue-preview.png`；新增 `Chapter2-arrival-distance-preview.png`、`Chapter2-forced-logging-order-preview.png`、`Chapter2-survivors-return-preview.png`，均為本輪正式遊戲鏡頭的引擎截圖。
- 清理後已重新編譯，2026-10-07 實際查看 Console 為 0 Error／0 Warning／0 Log，Play 未啟動且場景標題無未儲存標記；這是清理後狀態，不抹除播放期間的既有警告。
- 正式 Assets／Documentation／Packages／ProjectSettings 共 3,258 檔，達 100,000,000 bytes 的檔案為 0，最大 87,096,360 bytes；詳細結果在 asset-size-audit.json。測試後所有正式來源雜湊仍與驗證時相同。

## 2026-10-07 護樹張臂阻擋、起身短步後漸隱（接續中斷修改）

- 使用者指出上一版雙手靠近頭部像投降，且兩人走回樹邊的動作像機器人；要求改成保護樹木的阻擋姿勢，並在兩人從蹲姿站起、走兩三步時就漸隱。已讀本專案指引與固定劇情參考第 3–5 頁，沿用原場景、人物及攝影機。
- 接手時 `Chapter2BlockingPose.cs`、`Chapter2Controller.ProtectAftermath.cs` 的修改及新增 `Chapter2ReluctantWalk.cs` 已在磁碟，尚無本版播放證據。本輪接續完成步伐銜接與驗證，未重新套回上一輪長距離回樹路線。
- `Chapter2BlockingPose.cs`：雙臂在肩膀附近向兩側展開並略向前，雙手低於頭部；軀幹微轉、前傾，雙腳拉開並錯開前後位置，頭部保持朝警察。實際鏡頭能讀到張臂護樹的輪廓，不再把雙掌並排舉在頭旁。
- `Chapter2ReluctantWalk.cs`：兩人完整站起後，稍微錯開轉身與起步；以交替支撐腳、低幅抬腳、擺臂及重心起伏完成三個短步。身體平移採步與步之間速度連續的曲線，修掉每次落腳都停頓的問題，手臂也漸進混合入步行姿勢。
- `Chapter2Controller.ProtectAftermath.cs`：保留「給我去砍樹」及原本蹲下查看、1.4 秒起身；兩人各完成兩步便交給原流程做 1.6 秒漸隱，第三步接在漸隱中。總位移約 0.8 公尺，不再走到舊終點、停住 1.2 秒才淡黑。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode，以臨時 driver 經正式移動／UI 選項／砍伐入口測試。略過片頭，跟隨 2 倍、最終版保護與倒樹後果 1 倍、夜間會議 3 倍；另外查看 Unity 播放畫面與逐段引擎截圖。這不是全程人工操作、實體 VR 或獨立建置驗證。
- `protect-refuse-shield-v1` 為診斷輪，完成但步間重心尚有短停；最後修改後重新執行 `protect-refuse-shield-final`，completed=true、errors=[]。雙手相距約 1.093 公尺；檢查了阻擋、警察瞄準、蹲下查看、站起中段、短步行與淡出中的實際畫面。
- 最終保護輪：checkedBeforeOrder=true、orderWhileCrouched=true、stoodBeforeWalking=true；淡出開始取樣時兩人的 StepsCompleted 均為 2，位移約 0.689／0.590 公尺；漸隱中各完成第 3 步，最後位移各約 0.8 公尺。取樣含截圖負載，不能把取樣時間當成固定動畫時長。結果檔中的 returnedToTrees／fadedAfterReturn 是未使用的舊欄位，不作為此版驗收條件。
- `fell-support-shield-regression`：completed=true、errors=[]，5 次有效砍伐與接觸；遠距／背向／重複揮砍防護通過；三名族人各後退 3 步，倒樹朝玩家方向 dot=1；已查看倒木與夜間會議截圖。
- 兩輪夜間均還原 PC_PipelineAsset，正式結尾黑幕後自行退出，returned-to-edit.json 均 playing=false、completed=true，driver 未代停；PlayerPrefs 寫入在測試中關閉。播放仍有既有 Missing Script 警告及 URP 陰影圖縮小訊息，不宣稱全專案零警告。
- 未重測完整片頭、完整度歸零重試、另兩個會議組合、第一章、實體 VR 或獨立建置；本次兩項要求已完成 Editor 層級驗證。

### 保存與清理

- 原始備份、最終兩輪與診斷輪結果、逐段截圖、sequence／short-walk CSV、驗證來源雜湊位於 `_CodexBackups/chapter2_shield_shortwalk_20261007/`。臨時 Chapter2WorkProbe／Chapter2Verification 及 meta 已移入其 verification-tools；正式 Assets 不保留 driver。
- 更新 `Documentation/Chapter2-close-confrontation-preview.png`、`Chapter2-survivors-return-preview.png`（現在顯示走兩步即開始淡出），新增 `Chapter2-short-walk-fade-preview.png`。
- 兩章場景、共用 Chapter1IncidentRig、Chapter2Controller、Chapter2Rifle、Chapter2GriefReaction 均與接手前原始雜湊一致；最後播放後再次核對三個修改／新增的正式腳本仍為已驗證版本。清理前 playing=False、dirty=False、pipeline=PC_PipelineAsset。未 commit／push。
- 清理後重新編譯完成，2026-10-07 13:34 實際查看 Console 為 0 Error／0 Warning／0 Log，Play 未啟動，場景標題無未儲存標記；這是清理後狀態，不抹除播放期間的既有警告。
- 最後正式檔案大小掃描 Assets／Documentation／Packages／ProjectSettings 共 3,261 檔，達 100,000,000 bytes 的檔案為 0，最大 87,096,360 bytes；詳見本輪 asset-size-audit.json。

## 2026-10-07 第二章森林、寫實地面與樹旁活動 NPC

- 依使用者開場「聆聽族人介紹聖地」截圖直接修改原專案。已讀 AGENTS、本檔與固定劇情參考第 3–5 頁；本輪只處理場景豐富化，不重做既有劇情動作。
- `Assets/Scenes/第二章.unity` 新增 `Forest enrichment · layered woodland`：38 棵不同高度樹木、327 叢細草、368 叢低矮植物、46 叢白色野花、35 叢灌木、61 組小石、8 組樹根、3 段枯木。中央跟隨通道、巨木演出區及營火會議區留空；樹根依地面碰撞高度放置。原森林、巨木、主要人物與攝影機保留。
- 新增 `Chapter2ForestRichnessAuthoring.cs`，選單 Tools / Chapter 2 / Enrich Forest and Background NPCs 可重建本輪具名群組；不重建整個章節。新材質與貼圖在 `Assets/Chapter2/Environment/RichForest/`，使用 scoped 複本，不改第一章共用來源匯入設定。
- `Shaders/ForestGroundBlend.shader` 與 `Layered woodland earth.mat`：泥土小徑、落葉、苔草依世界座標柔和混合，使用法線、AO 與粗糙度；新泥土採 Poly Haven Brown Mud 2K CC0 材質，來源與授權保存在 `RichForest/ASSET_SOURCES.txt`。原有 4K 落葉貼圖保留 4K 匯入；不是單純提高解析度。此地面也用於夜間，會正常接收營火光。
- `Chapter2AmbientNPC.cs`：用現有角色增加 3 位族人、2 位警察，具名群組 `Background people · local patrols` 位於白天群組內。每人停留、轉身、沿約 1.275 公尺短路線往返，活動相位錯開；進入主線選擇後停下朝向巨木，隨白天群組在夜間隱藏。背景警察不複製主線步槍及槍口特效，不加入劇情 workers／leaders 陣列。
- `Chapter2UnderstoryInstances.cs`：播放時將重複草葉與植物改成 GPU instancing 批次繪製，Edit Mode 仍保留可直接編輯的個別物件；停用時恢復原 renderer。背景高面數人物取消多層陰影重複繪製，主線人物陰影不變。此為降低額外負載，未做 VR 幀率或獨立建置效能承諾。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode。先拍攝修改前 baseline，再檢查 v1／v2 診斷版本；v1 泥土偏黑，v2 草葉 GPU 網格快取未更新，均已修正，正式以 forest-final 與 v3 畫面為準。曾修正臨時 Metrics 工具存取 internal 骨架 helper 的編譯錯誤；不能把此診斷版本混入最後通過結果。
- 正式 driver 經既有移動、UI 選項與砍伐入口執行；略過片頭、跟隨 2 倍、槍後及倒樹 1 倍、夜間 3 倍。另逐張查看引擎截圖與 Unity 播放畫面，並非全程人工鍵盤或實體 VR 驗收。
- `protect-refuse-forest-final`：completed=true、errors=[]；開場面向／站定、左右與前界線、主要說話人物全身可見均通過。1 次槍響，查看了對峙、救援、短步離開與夜間會議畫面。夜間原管線還原，正式流程自行退出，returned-to-edit 為 playing=false、completed=true。
- `fell-support-forest-final`：completed=true、errors=[]；5 次有效砍伐／5 次接觸，倒木方向與玩家方向 dot=1、nightTreeCleared=true、nightPipelineRestored=true。已查看倒木中段及夜間會議畫面，正式流程自行退出，returned-to-edit 為 playing=false、completed=true。
- 兩輪環境 Metrics：5 位 NPC 均完成至少 4 段短程行走；最大距離起點約 1.275–1.280 公尺，與跟隨中的玩家最小平面距離約 6.10 公尺。夜間 hiddenAtNight=true，shaderErrors=[]。腳部記錄是骨骼取樣，沒有把它稱為網格鞋底精密接地量測。UnityStats 含額外截圖渲染，不作為穩定 FPS 或正式效能測試。
- 清理未引用的新素材後，重新載入磁碟場景並另開一次 Play Mode 查看開場／等待跟隨畫面；透過臨時檢查攝影機拍攝泥土、野花、樹旁 NPC 近景，拍後還原正式攝影機與 UI。這些近景是驗證視角，未改正式鏡頭。此輪最後由檢查工具手動停止；與兩輪正式結尾自停分開記錄。
- 播放仍有既有 Missing Script 警告與 URP 點光陰影圖縮小訊息；編譯可能顯示既有第一章未使用欄位警告。未測實體 VR、獨立建置、另外兩個會議組合、完整度歸零重試；原片頭 MP4 未重新錄製。

### 保存與清理

- 原始場景／交接檔備份、修改前後截圖、兩輪結果、NPC／shader Metrics、來源雜湊及大小稽核在 `_CodexBackups/chapter2_forest_richness_20261007/`。未引用的本輪新增素材移入其中 `unused-created-assets`，保留可還原副本。
- 新預覽在 Documentation：`Chapter2-enriched-forest-preview.png` 為正式開場鏡頭；`Chapter2-forest-ground-preview.png`、`Chapter2-forest-flowers-preview.png`、`Chapter2-background-npcs-preview.png` 為本輪引擎近景。
- 第一章場景、Chapter2Controller、Performance、ProtectAftermath 及共用 Chapter1IncidentRig 與接手前雜湊一致。第二章場景及本輪正式程式與兩條最終驗證時雜湊一致，見 final-source-comparison.json。未 commit／push。
- 臨時 Chapter2WorkProbe、Chapter2Verification、Chapter2ForestMetrics、Chapter2ForestReview 及 meta 已全部移入本輪備份的 verification-tools，正式 Assets 不保留測試／輪詢工具。清理前狀態 playing=False、dirty=False、pipeline=PC_PipelineAsset。
- 清理後重新編譯完成，2026-10-07 實際查看 Unity Console 為 0 Error／0 Warning／0 Log，Play 未啟動，場景無未儲存標記；這是清理後狀態，不抹除播放期間與診斷版的既有警告／已修正錯誤。
- 最後正式 Assets／Documentation／Packages／ProjectSettings 共 3,354 檔，達 100,000,000 bytes 的檔案為 0，最大 87,096,360 bytes；詳見 asset-size-audit.json。新環境檔案雜湊另存 final-environment-hashes.json。

## 2026-10-07 砍伐完成度倒數與左側原住民小孩

- 依使用者兩張截圖直接修改原專案；已讀 AGENTS、本檔與固定劇情參考第 3–5 頁。本次使用者明確指定的 100→80→60→40→20→0 倒數優先於「完成度」通常遞增的語意。
- `Chapter2Presentation.SetMeter` 改顯示「砍伐完成度」，百分比只依有效砍伐次數計算，每次減 20%。黃色條同步縮短；原 Image 沒有 sprite，僅設 fillAmount 不會縮短，已改以 RectTransform 寬度呈現。節奏綠區與白色游標不變。
- `Chapter2Controller` 在真正斧刃接觸與重試時立即更新 UI。原 Integrity 仍作內部木材受損及重試判斷，失誤不會扣畫面的完成度；達 5 次有效砍伐正常進入倒樹演出，0% 不會觸發受損重試。
- 第二章白天背景群組的 `林間族人 1`（起點 x=-6.3、z=-5.5）替換為 `林間原住民小孩`，來源是第一章既有 `原住民小孩(2)`，不是縮小成人。新增 `Assets/Chapter2/Characters/原住民小孩.prefab`（46,659 bytes），沿用模型與材質，authoring 網格高度 1.22 公尺；保留原活動位置、路徑與相位。
- 新增 `Chapter2ChildNPCAuthoring.cs` 的 Tools / Chapter 2 / Replace Left Background NPC with Child，可針對此 NPC 替換。森林重建工具 `Chapter2ForestRichnessAuthoring` 也沿用該小孩，避免重建後退回重複成人。
- 場景既有物件的差異僅為背景父物件的子引用、兩個 HUD 名稱及初始標籤；其餘為原 NPC 骨架移除與小孩骨架加入。第一章場景、共用 Chapter1IncidentRig 的雜湊確認未變。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode，以臨時 driver 經正式移動、UI 選項、TryChop 與斧刃接觸執行；略過片頭，白天 2 倍、倒樹 1 倍、夜間 3 倍，另逐張檢視引擎截圖。不是全程人工鍵盤或實體 VR 驗收。
- `fell-support-retry-mixed-final` 是第一輪診斷：文字與計數正確，但截圖發現黃色條未縮短。修正條長後重播 `fell-support-retry-mixed-bars-final`，completed=true、errors=[]；有效 1–5 下分別為 80／60／40／20／0%，實際查看 60% 與 0% 畫面確認條長正確。
- 最終輪先做 5 次失誤觸發重試，之後有效 2 下再失誤一次，顯示維持 60%，最後完成 5 下，共 11 次接觸。遠距、背向、重複揮砍拒絕檢查皆通過；重試與倒樹流程正常。CSV 保存每次有效／失誤後的文字、fillAmount 與條長 anchor。
- 實際查看跟隨畫面，小孩外觀與其他成人可區分，骨架有效；完成 4 段短程往返，最大起點距離約 1.275 公尺。夜間 hiddenAtNight=True；夜間原渲染管線恢復，倒木隱藏。最終 returned-to-edit 為 playing=false、completed=true，由正式結尾自行退出，driver 未代停。
- 測試關閉 PlayerPrefs 寫入，正式場景 saveResult／stopEditorAfterEnding 保持原值。播放仍見既有 Missing Script 警告及 URP 陰影圖縮小訊息；不宣稱全專案零警告。未重測保護分支、完整片頭、實體 VR 或獨立建置。

### 保存與清理

- 原始檔、診斷與最終結果、截圖、CSV、大小稽核、來源雜湊放在 `_CodexBackups/chapter2_meter_child_20261007/`；最後核對 6 個正式修改／新增檔案與最終測試時雜湊一致。
- 新預覽：`Documentation/Chapter2-chopping-progress-preview.png`（有效 2 下、60%）、`Documentation/Chapter2-left-child-preview.png`。`Documentation/Chapter2.md` 已更新玩法說明。
- 臨時 Chapter2WorkProbe、Chapter2Verification、Chapter2MeterChildReview 與 meta 已移到本輪備份的 verification-tools；正式 Assets 不保留測試 driver。清理前 playing=False、dirty=False、pipeline=PC_PipelineAsset。未 commit／push。
- 清理後已完成重新編譯，實際查看 Unity Console 為 0 Error／0 Warning／0 Log，Play 未啟動且場景無未儲存標記；這是清理後狀態，不抹除播放期間既有警告。
- 最後 Assets／Documentation／Packages／ProjectSettings 共 3,361 檔，沒有檔案達 100,000,000 bytes；最大 87,096,360 bytes，详見本輪 asset-size-audit.json。

## 2026-10-07 完成度改為遞增、連續三次失誤才重試

- 使用者更正上一輪倒數需求；本段取代前段的百分比與重試規則，先前倒數截圖／測試僅作歷史紀錄。本輪已重讀 AGENTS、本檔與固定劇情參考第 3–5 頁。
- `Chapter2Presentation.SetMeter`：0 次有效砍伐為 0%，1–5 次依序 20／40／60／80／100%，黃色條同步增加，失誤不增加完成度。場景初始標籤、條長及名稱亦已儲存為新規則。
- `Chapter2Controller` 新增 ConsecutiveFailedCuts；真正斧刃接觸判定失誤才累加，成功命中立即歸零。達 3 才觸發既有「木材不能再受損！放慢動作，重新找準落點。」及重試；重試後有效次數、連错次數和完成度均歸零。FailedCuts 仍保留總失誤統計，Integrity 仍保留原內部損傷紀錄，但不再用其歸零判斷重試。
- 原住民小孩 prefab 與兩個 NPC／森林 authoring 腳本雜湊未變；本輪只改 Controller、Presentation 與第二章 HUD 的 4 行序列化設定。
- Unity 6000.0.58f1 Editor Play Mode 最終 `fell-support-consecutive-three-final`：completed=true、errors=[]，夜間還原管線且倒木隱藏，正式結尾自行退出，returned-to-edit 為 playing=false、completed=true。driver 經正式移動、UI 選項、TryChop 與實際斧刃接觸；略過片頭，白天 2 倍、倒樹 1 倍、夜間 3 倍，並逐張查看引擎截圖，不是全程人工鍵盤或實體 VR。
- 專項實測先「錯、錯、對、錯、錯、對、錯、錯、錯」：每次砍中即清除連錯，總失誤到第 5／6 次均不警告；第 9 次接觸為連錯第三次，才出現一次警告。重試歸零後再「錯、錯、對、錯、錯、對、對、對、對」，正常完成 5 次有效砍伐。全輪 18 次接觸、11 次總失誤、恰好 1 次警告，專項 streak-result.json 為 initialZero=true、resetAfterWarning=true、completed=true、failures=[]。
- 已查看初始 0%、砍中一次 20%、連錯第三次警告、完成 100% 與黃色條畫面；完整 CSV 保留每次接觸的有效數／總失誤／連錯數／標籤／條長。遠距、背向、重複揮砍的拒絕檢查也通過。測試關閉 PlayerPrefs 寫入；未重測保護分支、完整片頭、獨立建置或實體 VR。播放保留既有 Missing Script 警告與 URP 陰影圖縮小訊息。
- 備份、測試結果、截圖、CSV、來源比對在 `_CodexBackups/chapter2_progress_streak_20261007/`；三個正式變更檔案与測試時雜湊一致。更新 Documentation/Chapter2.md 與 Chapter2-chopping-progress-preview.png（有效 4 下、80%），新增 Chapter2-three-miss-warning-preview.png。
- 臨時 Chapter2WorkProbe、Chapter2Verification、Chapter2StreakReview 與 meta 已移到備份的 verification-tools。清理前 playing=False、dirty=False、pipeline=PC_PipelineAsset。未 commit／push。
- 清理後重新編譯完成，實際查看 Console 為 0 Error／0 Warning／0 Log，Play 未啟動且場景無未儲存標記；此為清理後狀態。正式 Assets／Documentation／Packages／ProjectSettings 共 3,362 檔，超過或等於 100,000,000 bytes 為 0，最大 87,096,360 bytes。

## 2026-10-07 夜間倒木、木材堆與發言人物鏡頭

- 依使用者夜間會議截圖直接修改原專案；已讀 AGENTS、本檔與固定劇情參考第 3–5 頁。夜間景觀依本次要求呈現全數倒木，保護與砍伐兩條路線皆套用；不改白天護樹的當下演出與選擇结果。
- 新增 `Chapter2LoggedForest.cs`，在轉夜淡黑時將原森林 130 棵、補植 38 棵與巨木共 169 棵橫放，沿營火外圍錯開倒向；巨木橫置會場後方，隱藏不自然的程序根部。夜間樹木不阻擋鏡頭／離場路徑，白天仍保留原位置及站立狀態；不複製大型樹木 mesh。
- 場景夜間群組新增六堆、共 42 根切好角材，沿用既有 Carried timber 材質。新增 `Chapter2CouncilAuthoring.cs` 與 Tools / Chapter 2 / Prepare Logged Night Council；森林重新建立後需再執行此命令更新樹木引用。既有營火、人物、地面、石頭與座位保留。
- 新增 `Chapter2Controller.Council.cs`：每段夜間 Say 前清除上一句字幕，沿火堆外圍平滑移至說話者正面，人物朝向鏡頭；坐姿與站姿使用不同高度。保守派兩人使用斜向構圖，避免後一人被前一人遮住。六社講完、選擇立場與保守派離場時回全景，最終站立演說後保留拉遠／黑幕結尾。
- XR 沿用既有短淡黑原點移動路徑，保留頭部追蹤；未以實體頭戴装置測試，不能把桌面畫面等同 VR 結果。`Chapter2Controller.Performance.cs` 與接手前一致。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode；臨時 driver 經正式移動、UI 選項及砍伐入口，略過片頭、跟隨 2 倍、後果與夜間 1 倍。逐張查看引擎正式鏡頭截圖，另查看 Unity 播放後狀態；不是全程人工鍵盤、獨立建置或實體 VR 驗收。
- `fell-support-council-v1` 為診斷輪，發現莫那背向及巨木根部過於顯眼後修正；`protect-refuse-council-final` 發現後一名保守派被遮擋，後續針對兩名保守派改成斜向鏡頭。不能把這兩輪當最終視覺驗收。
- `fell-support-council-final`：completed=true、errors=[]，5 次有效砍伐；夜間全部 169 棵橫倒、六堆木材，10 段發言均有對應鏡頭，council failures=[]。已逐張檢視六位領袖及支持分支人物。此輪在保守派專用角度修正之前；該最後修正只對 conservatives 陣列成員生效，支持分支不使用它。
- `protect-refuse-council-clear-speakers`：最終來源實測 completed=true、errors=[]；11 段發言，包括兩名保守派與最後站立演說，council failures=[]。已查看兩位保守派新構圖，臉部不再互相遮擋。人物頭部在可讀視框、面朝鏡頭，並以截圖人工檢查遮擋，未單憑視框數值宣稱畫面正確。
- 兩輪均 daytimeUpright=true、allNightTreesHorizontal=true，最大樹幹 up 與世界 up 的絕對內積約 2.68e-7；nightPipelineRestored=true，正式結尾自行停止，returned-to-edit playing=false、completed=true。測試關閉 PlayerPrefs 寫入；此輪未重測連错三次規則（先前專項證據保留）、完整片頭、另外兩個組合或第一章。
- 播放仍有既有 Missing Script 警告與 URP 點光陰影縮小訊息；不宣稱全專案播放零警告。最終來源雜湊保存在 clear-speakers-source-hashes.json，與播放後 final-source-comparison.json 一致。

### 保存

- 備份、診斷與最終結果、逐人鏡頭截圖、speakers CSV、來源雜湊在 `_CodexBackups/chapter2_logged_council_20261007/`。第一章場景、共用 Chapter1IncidentRig、Chapter2Presentation 與小孩 prefab 雜湊確認未變。
- 新預覽：`Documentation/Chapter2-logged-council-preview.png`、`Chapter2-council-speaker-preview.png`、`Chapter2-council-dissent-preview.png`；玩法與維護說明同步更新於 `Documentation/Chapter2.md`。未 commit／push。
- 臨時 Chapter2WorkProbe、Chapter2Verification、Chapter2CouncilReview 與 meta 已移入備份的 verification-tools，Assets 不保留測試輪詢工具。清理前 playing=False、dirty=False、pipeline=PC_PipelineAsset；清理後重新編譯完成，實際查看 Unity Console 為 0 Error／0 Warning／0 Log，Play 未啟動，場景無未儲存標記。這是清理後狀態，不抹除播放期間既有警告。
- 最後 Assets／Documentation／Packages／ProjectSettings 共 3,371 檔，達 100,000,000 bytes 的檔案為 0，最大 87,096,360 bytes；詳見 asset-size-audit.json。

## 2026-10-07 接續完成會議開場、不同人物與起義響應

- 接續「修改木材砍伐进度与NPC」（01a11540-bc83-7382-bd12-fc55a35a0006）中斷的最新要求；先閱讀原對話、現行檔案與固定劇情參考第 3–5 頁。保留已做好的倒木、木材堆、砍伐完成度與連错規則。
- 夜景完全淡入後維持營火全景三秒，此時沒有發言字幕；接著才沿火堆外圍移向莫那魯道並開始第一句。其餘發言維持逐人正面鏡頭。
- 原九人重複使用三種模型；現在莫那魯道、六位領袖與兩位保守派共使用九種不同模型。莫那使用專案既有長老；新增 Chapter2CouncilCastAuthoring，保存 Tools / Chapter 2 / Use Distinct Council Characters 方便重建配置。原 FBX、匯入設定與第一章人物保留。
- 前次中斷所用的賽德克青年模型，本次實播仍有臉部扭曲，靜止與還原 bind pose 也沒有消除，最終改用未在會議中使用的「日治時期長輩女」既有模型。已重新檢查發言近景，該臉部問題消除。
- Chapter2RallyGesture 在一般骨架／坐姿層之後套用右手高舉姿勢，另一手保持低處。僅支持起義時，莫那在「我們的血，不該再白白流淌。霧社，該覺醒了！」結語使用此動作，鏡頭拉開以容納手掌。
- 整句六秒結束後清字幕、鏡頭回全景，再啟動六位領袖依序起身（間隔 0.13 秒、每人 1.25 秒），向前移動 0.46 公尺離開木椅。兩名原本站立的旁聽者留在會場；拒絕分支保留保守派離場，不啟動舉手或集體起身。
- Chapter2Actor 的坐姿骨盆高度改成配合 0.44 公尺高的木椅，不再對不同身形一律下移 0.38 公尺。第三位領袖的椅子縮窄並後移 0.25 公尺，消除近景裙襬穿過木椅的問題；站起後不穿椅。

### 本次最終驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode。臨時 driver 經正式移動、UI 選項與斧刃接觸流程，略過片頭、跟隨使用 2 倍，後果和夜間 1 倍；逐張檢查引擎正式鏡頭截圖，另查看實際 Unity Game 視窗。不是全程人工鍵盤驗收、獨立建置或實體 VR 測試。
- protect-refuse-rally-final-seats：completed=true、errors=[]，九個不同人物模型、11 個發言鏡頭；全景淡入後至攝影機開始位移實測約 3.177 秒，六位領袖至結尾仍 seated=true；保守派兩人的新模型發言構圖均已目視檢查。council failures=[]，正式黑幕結尾自行退出，returned-to-edit playing=false。
- fell-support-rally-final-seats：completed=true、errors=[]，五次有效砍伐、九個不同人物模型、10 個發言鏡頭；全景至攝影機開始位移約 3.227 秒。莫那右手高於頭部約 0.351 公尺，台詞期間六位領袖保持坐姿，結束後全部完成起身；角色根位置到最近椅子中心的最小水平距離 0.46 公尺，並另外目視檢查起身中途及完成畫面。council failures=[]，黑幕後自動退出，playing=false。
- 兩輪皆確認白天樹木直立、夜間 169 棵倒地與六堆木材，夜間還原正式渲染管線。最終來源 SHA-256 與測試前一致，見 final-source-hashes.json / final-source-comparison.json。
- fell-support-rally-v1、resumed-v2、fell-support-rally-final 及 pose-* 是診斷／較早版本；不能取代名稱含 final-seats 的最終兩輪。前兩輪仍有臉部問題，較早 final 尚未完成第三位領袖木椅調整。
- 播放仍有既有 Missing Script 警告與 URP 點光陰影圖縮小訊息；不宣稱全專案播放零警告。未重测完整片頭、連錯三次專項、第一章或另外兩個選擇組合。
- 第一章場景、共用 Chapter1IncidentRig、Chapter2Presentation 及 Chapter2LoggedForest 與接手前雜湊一致，見 unchanged-after.json。未 commit / push。

### 保存與交接

- 證據、原始備份、最終兩輪截圖與資料：_CodexBackups/chapter2_council_rally_20261007/。原有交接文件與程式備份保留，resumed-originals 保存此次接手時仍未完成的程式。
- Documentation/Chapter2.md 已更新玩法與維護說明。預覽更新為 Chapter2-logged-council-preview.png、Chapter2-council-speaker-preview.png、Chapter2-council-dissent-preview.png，新增 Chapter2-council-rally-preview.png 與 Chapter2-council-standing-preview.png。
- 測試結束後檢查 playing=False、dirty=False、pipeline=PC_PipelineAsset。臨時 Chapter2WorkProbe、Chapter2Verification、Chapter2CouncilReview、Chapter2CouncilPoseReview 與 meta 已移至本次備份目錄的 verification-tools；正式保留角色配置 authoring 和演出程式。
- 清理後重新整理並完成 Unity 編譯，實際開啟 Console 檢查為 0 Error / 0 Warning / 0 Log；Play 未啟動，場景無未儲存標記。這是清理後狀態，播放期間的既有警告仍記錄在前述證據。
- 最終 Assets / Documentation / Packages / ProjectSettings 共 3,377 檔；達 100,000,000 bytes 的檔案為 0，最大 87,096,360 bytes，詳見 asset-size-audit.json。本次要求已完成；尚未做實體 VR 或獨立建置驗收。

## 2026-10-08 第二章 P 鍵快速預覽秘密會議

- 使用者希望保留空白鍵略過影片，另外按 P 直接到截圖中的「第二章 / 夜晚的秘密會議」營火全景，方便後續修改與預覽。已閱讀根目錄指引及固定劇情文字摘錄第 3–5 頁。
- Chapter2Player.Key 新增 P；Chapter2Controller.Update 呼叫 SkipToMeeting。片頭及白天階段可跳轉，Meeting／Vote／Ending／Complete 不重開；同次跳轉重複按鍵亦忽略。
- 跳轉停止控制器所有 coroutine（含獨立揮斧）、影片與音效，釋放影片 RenderTexture，收起斧頭、選項、砍伐條、箭頭和舊字幕，沿用 SetNight、營火全景、淡入與三秒停留。未改場景、角色、光線與對話內容。
- 原會議至結尾抽為 RunMeeting，正常白天流程與快捷入口共用。已比對備份，會議內容除「快捷預覽不保存結果」條件外完全一致；快捷預覽不捏造白天選擇，也不覆寫 WusheEvent.Chapter2.Result。
- Documentation/Chapter2.md 已補充操作方式。原始備份、輸入測試工具、引擎截圖、狀態及來源雜湊在 _CodexBackups/chapter2_meeting_shortcut_20261008/。

### 本次實際驗證及限制

- Unity 6000.0.58f1 原專案編譯成功。透過臨時 Editor probe 將 KeyboardState 事件送入 Unity Input System，正式 Update 讀取 P／Space／1／2；不是直接呼叫跳轉方法。Windows computer-use 送入的按鍵未觸發遊戲，故不宣稱已完成原生人工鍵盤驗收。
- 片頭 P：Intro → Meeting，截圖 meeting-overview-210426.png 與使用者參考一致；video=false、day=false、night=true、axe=false、meter=false、choices=false、fade=0。會議途中再送 P 未重開；接續 Vote → Ending（拒絕）→ Complete，退出後 playing=false、dirty=false。
- 空白鍵只進入 Follow，尚未進入夜景，見 space-follow.txt；白天再送 P → Meeting，截圖 meeting-overview-210749.png。支持選項也到 Complete，退出後 playing=false、dirty=false。兩輪 PlayerPrefs 與測試前完整字串一致。
- 已目視檢查 1600×900 引擎全景截圖，也查看 Unity Game 視窗；電腦操作工具取得的 Game 畫面有顯示失真，視覺驗收以引擎截圖為準。會議三秒全景與發言依正式一倍速播放。
- 本次未重跑完整白天保護／砍伐，亦未專項測試揮斧或槍擊中途的 P；該路徑的 coroutine 中止与場景／UI 清理由程式檢查確認。未進行獨立建置、實體 VR 或第一章回歸。
- 播放記錄包含 Missing Script 警告、URP 點光陰影圖調整訊息；第一次播放另外有一筆 Unity Editor DontSaveInEditor 斷言，後續兩輪未再記錄該斷言。沒有 C# 編譯錯誤或新功能例外，不宣稱全專案零警告／零斷言。
- 測試後移出臨時 Chapter2ShortcutProbe 與 meta，保留副本於備份目錄；未 commit／push。Assets / Documentation / Packages / ProjectSettings 最大檔案 87,096,360 bytes，無檔案達 100,000,000 bytes。
- 清理後再次編譯成功並完成 domain reload；實際開啟 Unity Console 顯示 0 Error／0 Warning／0 Log，Play 停止、場景名稱無未儲存星號。這是清理後狀態，播放期間的警告與斷言仍保留於上述記錄。最終正式來源雜湊與兩輪測試時一致，見 final-source-comparison.json。

## 2026-10-09 接續完成會議手腕、身材比例與握拳起身

- 接續使用者三張截圖的未完成工作，直接修改原專案；已讀 AGENTS、本檔及固定劇情文字第 3–5 頁。沿用前次已加入的 CouncilHands／CouncilPoseAuthoring 初稿，再修正握拳外形並完成當前版本驗證。
- 第三位女性領袖：Chapter2CouncilHands 使用 authoring 校正的左右手掌座標，在共用骨架／坐姿之後對齊手腕，說話時右掌自然朝上，消除原本拇指朝下、手掌像反接的外觀。手掌資料預先保存到場景，無須在 Play Mode 讀取不可讀的原 FBX 網格。
- 第五位領袖：保留同一長輩女性模型，在原等比配置上調成寬 1.22、高 0.90、厚 1.10，修正細長外形；不修改原模型匯入設定。發言近景與集體站立畫面已查看。
- 支持起義：最後一句前先對準坐著的莫那，台詞開始時以 0.8 秒前傾起身，同時收緊右拳、彎肘抬起；右肘穩定角度約 102.43 度，手臂不是伸直舉掌。手腕轉向分攤至前臂，避免扭折接縫。六位領袖維持坐姿聽完，鏡頭回全景後才依序起身；拒絕分支保留一般站起及保守派離場。
- 原莫那模型沒有手指骨骼，新增第二章專用網格 `Assets/Chapter2/Poses/MonaRallyFist.asset`。作者工具從原網格建立四指收緊、併攏與拇指扣住的 blend shape，並重算法線。此二進位資產 36,490,444 bytes；原 FBX 保留。Chapter2CouncilCastAuthoring 重建角色後也會重新套用手腕與比例校正。

### 本次實際驗證

- Unity 6000.0.58f1 原專案編譯完成。透過臨時 Chapter2CouncilRevisionProbe 送入 Unity Input System 的 P／1／2，正式 Update 讀取後進入會議；最終兩輪均使用正式台詞時間及一倍速，沒有直接跳 Stage。這是 Editor 輸入模擬與引擎畫面檢查，不是全程人工鍵盤、獨立建置或實體 VR 驗收。
- `final-support`：completed=true、assertionsPassed=true、errors=0、warnings=0；起身握拳、右肘彎曲、莫那說話期間六位領袖仍坐著、之後全部起身等斷言通過。逐張查看女性手腕、第五位領袖、正式握拳鏡頭、診斷近照與站立全景。正式黑幕結尾自行停止，playing=false、dirty=false。
- `final-refuse`：completed=true、assertionsPassed=true、errors=0、warnings=13（既有 Missing Script）；沒有觸發 CouncilRally 或集體起身，六位領袖維持坐姿；查看保守派發言畫面。正式結尾自行停止，playing=false、dirty=false。
- 兩輪 PlayerPrefs 的 WusheEvent.Chapter2.Result 前後字串完全相同；快捷預覽未覆寫正式通關紀錄。女性領袖的手指主軸與前臂偏差最高約 0.03 度；這項數值是手腕方向驗證，握拳外形另外以渲染近照判讀，不能只用 blend shape=100 當作外觀通過。
- resumed*、revision*、v4、v6、v7 圖片與資料均為較早診斷；中途曾出現手指空隙、局部折痕或拇指未扣緊，不能取代 final-*。v6-deformation-orientation.json 僅屬 v6 的數值檢查，不是最終版本的證據。
- 本輪未重跑白天保護／砍伐、第一章、完整片頭或 VR。第一章場景、共用 Chapter1IncidentRig、Chapter2Actor 與接手時 SHA-256 一致；最終九個正式來源／場景／網格檔案與兩輪測試版本一致。

### 保存與清理

- 證據、原始備份與最終兩輪在 `_CodexBackups/chapter2_council_pose_20261008/`；final-continuation-originals 為此次續作接手時備份。final-source-hashes／final-source-comparison 與 unchanged-comparison 保存來源比對。
- Documentation/Chapter2.md 已更新。新增 Chapter2-council-wrist-preview.png、Chapter2-council-proportions-preview.png、Chapter2-council-fist-close-preview.png，更新 Chapter2-council-rally-preview.png 與 Chapter2-council-standing-preview.png。
- 測試後將臨時 Chapter2CouncilRevisionProbe 與 meta 移至 verification-tools；正式 Assets 不保留自動播放／輪詢測試工具。清理後重新編譯完成，實際開啟 Unity Console 顯示 0 Error／0 Warning／0 Log；Play 已停止，場景無未儲存星號。這是清理後狀態，播放期間既有 Missing Script 警告仍保存在測試記錄。
- 最終 Assets／Documentation／Packages／ProjectSettings 共 3,387 檔，沒有檔案達 100,000,000 bytes，最大 87,096,360 bytes；詳見 final-size-audit.json。未 commit／push。

## 2026-10-09 夜間會議改為四位具名角色的新劇本

- 依本次完整劇本直接修改原專案第二章；已讀 AGENTS、交接與固定劇情摘錄第 3–5 頁。最新使用者劇本取代舊的六社輪流發言、保守派離場及舊結語；白天正常流程和 P 快捷共用新 RunMeeting。
- 人物與場景保留既有模型。原長老為「莫那·魯道」；原第二位男性領袖為「達多·莫那」；原第四位男性領袖為「巴萬·拿威」；原第一位灰髮領袖為「瓦旦」。四位的 GameObject 名稱與 controller 引用已保存到第二章場景，字幕直接顯示這些姓名。其他五人改用描述性會議群眾名稱，集體台詞顯示「巴萬與眾戰士」，新劇情沒有族人 1／2／3 或六社領袖編號字幕。
- 新增 Chapter2Controller.CouncilStory.cs，按原文保存共通 26 行、同意 9 行、拒絕 8 行。每次只顯示一行，不將整段合併；Gaya、Sediq Bale 與括號文字保留。短句至少 2.15 秒，長句按字數延長。開場仍是夜景淡入及三秒營火全景。
- 新增 Chapter2CouncilDrama.cs：按刀柄、憂慮伸手、凝視火堆、握刀、低頭、莫那起身、達多拔刀插入泥土、眾人起身拔刀、莫那點頭與側指出口等程序肢體演出。瓦旦「閉上眼」段落目前以低头／停頓呈現，沒有專用眼皮形變；冷笑、咆哮沒有新增配音或口型，臉部仍沿用既有模型。不得將這些細部臉部演出寫成已完整製作。
- 同意分支：達多先起身插刀，兩行誓言後再轉全景；其餘人物站起、持刀響應三行。莫那說完四行囑咐後拉遠，展示營火與九人，再淡黑、顯示原有「決戰的時刻，將至。」並停止 Editor Play Mode。
- 拒絕分支：視角先回到玩家站位，再沿營火外圍走到莫那面前。莫那轉向接近中的玩家；玩家台詞後由莫那斥責四行，再指向側面出口斥責三行，直接淡黑退出，不進入支持分支的起身／拉遠／決戰字卡。
- 新增 Chapter2CouncilStoryAuthoring、Chapter2KnifeGripAuthoring 與 Props/ 內小型獵刀／刀鞘／材質及七份右手握柄網格，修正原先刀柄浮在張開手掌旁的診斷版本。Tools / Chapter 2 / Apply Named Council Story 可重套名字、引用與刀具，Use Distinct Council Characters 之後也會套用。網格必須保留二進位；作者工具使用名稱與檔名一致的非 persistent 副本，直接以 SaveToSerializedFileAndForget 儲存，避免匯入器因名稱不符改写為文字。

### 本次實際驗證

- Unity 6000.0.58f1 原專案 Editor Play Mode，以臨時 Chapter2StoryProbe 送入 Input System 的 P／1／2，由正式 Update 處理。全程正式台詞時間與 Time.timeScale=1，另查看引擎 1600×900 截圖。這是 Editor 輸入模擬與視覺檢查，不是全程人工鍵盤或實體 VR 驗收。
- final-support：35 行字幕與姓名逐項對照使用者原文，所有 Text 實際 visualLines=1；checks.passed=true、completed=true、errors=0、warnings=0。插刀／全員起身／決戰字卡皆出現；刀尖 y=-0.05967，插入地面。完成約 131.91 遊戲秒，正式流程自行停止，playingAfterExit=false、dirty=false。
- final-refuse：34 行字幕／姓名與單行顯示全部通過；checks.passed=true、completed=true、errors=0。玩家路徑至營火中心的最小水平距離約 1.516 公尺，沒有穿過火圈；沒有插刀、集體起身或決戰字卡。完成約 127.78 遊戲秒，playingAfterExit=false、dirty=false。已查看玩家對話、莫那側指出口及 Sediq Bale 長句畫面。
- 兩輪 WusheEvent.Chapter2.Result 字串前後相同，P 預覽沒有覆寫原本通關紀錄。final-refuse 有 13 筆既有 Missing Script 警告；播放另有 URP 點光陰影圖調整訊息，不宣稱全專案播放零警告。
- diagnostic-support 是未校正握柄／腿部接地的較早畫面，review-refuse 是尚未修正莫那朝向與側指方向的版本，不能代替 final-*。
- 未重跑完整白天護樹／砍伐、第一章、獨立建置或實體 VR。XR 保留頭部追蹤；拒絕路線移動 XR origin，未做頭戴裝置舒適度驗證。

### 資產處理、保存與限制

- 作者工具診斷期間曾短暫切換 EditorSettings.serializationMode，觸發 Unity 對既有場景及資產重新序列化；已還原 ForceText（m_SerializationMode: 2），最終工具已移除此做法。大型既有 MonaRallyFist、GriefHands 及 IncidentHutOpening 已重存二進位。其他場景／資產可能存在此次重新序列化的檔案差異；沒有用較舊備份覆蓋使用者既有修改，不宣稱第一章檔案雜湊完全不變。
- 本輪所有正式檔案均小於 100,000,000 bytes，最大為 Grip_會議戰士 · 長裙女族人.asset，89,742,488 bytes。詳細最後稽核保存在 asset-size-audit.json。前面的 169–271 MB 握刀網格是已修正的文字序列化診斷狀態。
- 原始備份、expected-lines.json、check-results.ps1、兩分支 JSON、截圖、紀錄及雜湊在 _CodexBackups/chapter2_script_20261009/。Documentation/Chapter2.md 已更新；新增四張 Chapter2-new-council-*.png 預覽。沒有 commit／push。
- 清理前 playing=false、dirty=false；臨時 Chapter2StoryProbe 與 meta 已移到本輪 verification-tools，正式 Assets 不保留自動播放測試工具。已核對 8 個正式來源／場景檔案與最終播放時 SHA-256 相同。
- 測試工具移出後重新編譯完成；清理後實際查看 Console 為 0 Error／0 Warning／0 Log，Play 停止，場景無未儲存星號。此為清理後狀態，不抹除 final-refuse 的既有 Missing Script 警告。最後大小稽核 3,420 檔，超過或等於 100,000,000 bytes 的檔案為 0。

## 夜間會議手腕、視線、握拳與第一章房屋（2026-10-09）

使用者依三張 Play → P 截圖要求修正達多握刀手腕、發言者低頭、莫那宣告時仍張掌，以及字幕身分；並澄清場景是保留截圖中的按 P 後環境，只補上第一章房屋。

- `Chapter2CouncilDrama.cs`：持刀手腕沿前臂，刀身改跟隨掌中握持軸；前臂分攤旋轉。發言時最後一層頭部姿勢對準實際鏡頭，覆蓋原低頭角度。莫那 Declare 使用既有 MonaRallyFist 形狀，右肘彎曲、拳头收緊舉起。拒絕斥責時仍看向玩家，驅逐右手指向出口。
- `Chapter2Controller.Council.cs`：坐姿近景高度根據角色頭部位置，起身鏡頭調整構圖，不再高角度俯視臉部。
- `Chapter2Controller.CouncilStory.cs`：字幕人名依序顯示「達多·莫那（莫那·魯道之子）」、「巴萬·拿威（年輕戰士）」、「瓦旦（長老代表）」、「莫那·魯道（賽德克社頭目）」。Hierarchy 保留前次具名角色名稱。台詞與原分行未變。
- 新增 `Chapter2CouncilVillageAuthoring.cs` 與已儲存第二章場景群組 `會議外圍房屋 · 第一章模型`：四棟房屋沿用第一章的茅屋1、茅屋2、傳統建築及場景材質，夜間才顯示；保留營火、九人、木材堆及倒木。唯讀 Preview Scene 取得房屋，僅保存第二章；本輪第一章場景 SHA-256 前後一致（8901760AF24CABC1049CC98B2EB031F34771496A93FC912B21316D0BD578CAF2）。

### 本輪實際驗證與限制

- Unity 6000.0.58f1，透過暫時 Editor probe 注入 Input System 的 P、1／2 事件，執行正式 Update 路徑；使用原速度與完整字幕等待。不是實體鍵盤逐鍵操作，也未測實體 VR。
- 最終支持證據 `review3-support`：35 行與身分逐一符合，台詞及人名視覺行數皆為 1；宣告七行拳頭權重1；插刀接地、全員站起、拉遠、決戰字卡及淡黑退出均完成。errors=0、warnings=0，elapsed 約132.97秒。
- 最終拒絕證據 `final-refuse`：34 行與身分逐一符合、單行顯示；玩家繞火走近後莫那對視斥責、指向側面，淡黑後退出，沒有支持結尾字卡。errors=0，13則既有 Missing Script 警告；最小繞火距離約1.56公尺，elapsed 約128.30秒。
- 兩輪 `checks.passed=true`、`completed=true`、`playingAfterExit=false`、`dirty=false`、`prefsUnchanged=true`。已目視引擎1600×900截圖：達多手腕／四位說話視線／莫那閉拳宣告／房屋全景／拒絕對視及指向出口。5個正式來源與場景的 SHA-256 均與測試時相同。
- 診斷輪 `review-support` 因驗證工具尚在重新編譯時進入 Play，Domain Reload 使既有 rig 暫態陣列失效，已停止並完整重啟；不計為最終驗證。`review2-support` 房屋 FBX 軸向尚未保留，修正為保留 donor 旋轉後才取得 `review3-support` 最終畫面。沒有修改共用第一章 Rig 來掩蓋診斷錯誤。
- 資料與原檔備份在 `_CodexBackups/council_visual_revision_20261009/`。臨時 `Chapter2VisualProbe.cs` 與 meta 在完成後移至其中 `verification-tools/`，不保留於正式 Assets/Editor。
- 預覽圖：`Documentation/Chapter2-council-houses-preview.png`、`Chapter2-tado-wrist-preview.png`、`Chapter2-speaker-gaze-preview.png`、`Chapter2-declaration-fist-preview.png`。
- 新房屋只引用原模型與材質，未複製大型網格；本輪未切換專案序列化設定。正式資產大小稽核無單檔達100,000,000 bytes；最大仍為89,742,488 bytes的既有握刀網格。
- 本輪未重玩第一章、第二章白天分支與完整片頭，未新增配音或細部臉部表情。沒有提交或推送 Git。
- 清理後 Unity 已完成重新編譯；Console 顯示 0 Error／0 Warning／0 Log、Play 停止且場景無未存標記。這是清理後狀態，不抹除上面拒絕輪的13則既有警告。最終稽核3426個正式檔案，無超過100 MB。

## 2026-10-09 會議房屋校平、全員坐姿與小孩

- 使用者以新截圖要求檢查傾斜房屋、讓左側兩位站立族人也坐下，並加入坐著的小孩。直接修改原第二章場景；已讀專案指引及固定劇情參考，本次只處理夜間會議佈置。
- `Chapter2CouncilVillageAuthoring.LevelHouse` 保留原 FBX 軸向修正，移除從第一章实例繼承的俯仰與側傾，只保留水平朝向。四棟高度統一3.6公尺、底部貼地。原茅屋最大傾斜約10.6度；傳統建築原先 MeshFilter 引用遺失，已重接同一原 FBX 網格，現在可見於莫那後方。房屋並非改用新的模型。
- 新增 `Chapter2CouncilSeatingAuthoring`：九位成年人開場皆設為坐姿；左側捲髮青年與頭巾女族人移至左前弧線、各配0.44公尺高木椅。原七位成人、營火與森林位置保留。
- 沿用既有 `Assets/Chapter2/Characters/原住民小孩.prefab`，在夜間群組右前方新增 `會議原住民小孩`，配0.29公尺矮椅。`Chapter2Actor.seatedHipHeight` 預設仍為原成人0.54公尺，孩子使用0.38公尺，足部前伸距離相應縮短。小孩沒有加入戰士／演出名單，不持刀、兩分支均持續坐著。原劇本莫那、達多與成年群眾起身的時序保留。
- `Tools > Chapter 2 > Level Council Houses and Seat Listeners` 可重套以上佈置；場景已保存，使用者正常 Play → P 無須執行工具。Documentation/Chapter2.md 已記錄維護方式。

### 本輪實際驗證

- Unity 6000.0.58f1 原專案，臨時 Editor probe 透過 Input System 注入 P／1／2，由正式 Update 處理；一倍速完整播放兩分支，另目視1600×900引擎截圖。不是全程人工鍵盤或實體 VR 驗收。
- 最終 `final-support`：35行字幕、姓名與單行顯示通過，插刀、九位成人起身、拉遠、決戰字卡及黑幕退出均完成；elapsed約132.48秒、errors=0、warnings=0。
- 最終 `final-refuse`：34行字幕、姓名與單行顯示通過，玩家繞火走近、莫那斥責及淡黑退出完成；elapsed約128.15秒、errors=0、warnings=13，警告均為既有 Missing Script，詳見本輪log。未出現支持分支的集體起身或決戰字卡。
- 兩輪均 `checks.passed=true`、`completed=true`、`playingAfterExit=false`、`dirty=false`、`prefsUnchanged=true`。每轮開場 `initialActors=10`、`initialSeated=10`；`childAlwaysSeated=true`、`childHasKnife=false`；四棟房屋 `maxHouseTilt=0`、`maxHouseGroundGap=0`。水平檢查扣除 FBX 軸向轉換，不能將 Inspector 的 X=270度當作房屋歪斜。
- 已查看全景、左側兩位成人座椅近照、孩子坐姿近照、成人集體站立而孩子仍坐著的全景，以及拒絕玩家近景。座椅位置最終前移至臀部下方；`review-support` 與 `seats-only` 是座椅尚偏後的診斷版本，不能代替 final-*。
- 證據、原始備份、逐行比對、雜湊及截圖保存在 `_CodexBackups/council_seats_houses_20261009/`。預覽為 `Documentation/Chapter2-all-seated-child-preview.png` 與 `Documentation/Chapter2-seated-child-preview.png`。
- 本輪未重玩白天護樹／砍伐、第一章、完整片頭，亦未做獨立建置或實體 VR。成人坐姿預設數值不變，第一章場景 SHA-256 與本輪修改前相同；不代表第一章本輪已進行玩法驗收。未 commit／push。
- 臨時 `Chapter2SeatingProbe.cs` 與 meta 已移至本輪 `verification-tools/`，正式 Assets 不保留自動播放工具。移出後 Unity 編譯及 domain reload 完成，實際開啟 Console 顯示0 Error／0 Warning／0 Log，Play停止、場景無未儲存星號。清理後零警告不抹除拒絕輪的13則既有 Missing Script。
- 四個正式來源／場景檔案與最終兩輪測試時 SHA-256 一致，見 final-source-comparison.json。最終 Assets／Documentation／Packages／ProjectSettings 共3430檔，無單檔達100,000,000 bytes，最大89,742,488 bytes；本輪沿用原模型，沒有新增大型網格。未切換專案序列化設定。

## 2026-10-09 三分鐘自由探索與免費本機 NPC 對話

- 使用者指定路邊全部警察、族人與小孩、探索180秒、不能使用可能計費的雲端 API，並明確同意立即下載約1至2 GB免費模型。沿用固定第二章場景，已參照 AGENTS.md、交接與第二章劇情摘錄。沒有新增集合橋段，也不把帶路三人誤當自由交談人物。
- Chapter2Controller／Performance 在原 IntroduceForest 後串接 Chapter2Exploration.Explore(180)，接著 BeginForestEscort。保留原「前面就是西仔希克。這片森林，守護著我們的生活。」並補指定探索句。新句目前只有字幕等待，未錄配音。探索期間原隊伍等待、黃色箭頭與跟隨走廊限制關閉；時間到再播原帶路提示、開始跟隨。
- Chapter2Exploration 僅從 Background people · local patrols 取得現有五位路邊角色（2警察、2成年族人、1小孩），最近人物水平距離≤1.5m可按 E 或按鈕交談。超出範圍提示消失；桌面文字輸入／送出／Esc離開，交談暫停人物巡邏及玩家移動，人物面向玩家。打字中的 P 不會觸發夜間快捷。三分鐘不中斷計時，到期禁止新問題，等待既有請求並保留5至13秒閱讀。最後從當前位置回到原帶路，修正 Chapter2RouteGuide 避免將走廊外玩家瞬移回中央。
- Chapter2LocalDialogue 使用 llama.cpp b11526 CPU x64 與官方 Qwen2.5-1.5B-Instruct-GGUF Q4_K_M。模型1,117,320,736 bytes，已驗 SHA-256；執行檔下載包亦與 GitHub release digest 核對。均置於 D:/WusheLocalLLM，不放進 Assets/Git。資料見本輪 model-installation.json。無帳號、API金鑰或付費雲端備援，模型下載後無須外網；服務限定 HTTP loopback 127.0.0.1:18080。Unity隱藏啟動本機服務，離開場景／Play時停止自己啟動的程序。
- Assets/StreamingAssets/Chapter2LocalLLM.json 保存安裝路徑；Tools/LocalLLM/Install-LocalNPC.ps1 提供展示電腦的下載／校验／改路徑步驟。第一次可需約1.2GB流量，其後正常 Play。換電腦或打包時仍要部署外部模型並核對 StreamingAssets 路徑；本次未製作模型安裝包。
- 人物設定在 Chapter2LocalDialogue.Persona，背景文字在 Resources/Chapter2LocalDialogueContext.txt。每位人物各保存最近三輪成功問答，限本次遊玩，不需外部或向量資料庫。模型輸出只作文字顯示，不能控制主線或執行程式。仍可能答錯；設定不是史料原話，也不是模型已獲完整史料訓練。
- Windows原生繁體轉換已修正字串終止參數，250筆測試通過。舊 final-local 的亂碼畫面不作驗收；以修正後 final-local-v2 為準。ui-diagnostic 縮短且停用模型，只是 UI 診斷，review-local 是繁體轉換前診斷。

### 本輪自由探索驗證

- final-local-v2 使用場景正式180秒、真實本機模型，五人皆有非預寫回答；本機 i5-1235U／約16GB RAM 的五次回覆耗時約19.53／10.05／17.79／9.22／1.03秒。小孩知道玩家自介「阿山」，另一位警察回答不知道，確認人物近期紀錄未混用。總探索184.94秒含結尾閱讀時間。errors=0、warnings=1（既有停用音源），結束後playing=false、dirty=false。
- 距離1.501m拒絕、1.49m可開，五人提示與範圍外隱藏通過，聊天中P不跳章、隊伍等待與箭頭隱藏通過；回接時玩家位置跳動0，測試抵達 TreeChoice。回覆面板及人物畫面已檢查1600×900引擎截圖，預覽為 Documentation/Chapter2-local-police-preview.png 與 Chapter2-local-child-preview.png。
- 正式180秒輪最後的短回覆在到期前已完成，因此另做 expiry-pending：真實問題送出後將測試截止點設在0.1秒，確認仍在生成時到期會等到完整回答與閱讀後再走，並抵達 TreeChoice；errors=0、warnings=1。此為刻意強制造成到期的邊界測試，不能當作180秒時長證據。check-exploration.ps1 同時要求正式時長、五人真實回答、記憶隔離與待答邊界測試，final-local-v2-checks.json passed=true。
- 以上由臨時 Editor 工具注入 Input System 鍵盤事件、填入實際輸入欄並觸發送出；跟隨段以工具移動玩家沿隊伍至巨木。不是全程人工走圖或實體 VR 驗收。第一版未做語音輸入／合成語音、VR虛擬鍵盤、獨立執行檔驗收；未重跑完整白天護樹／砍伐分支或第一章。模型品質及展示機速度仍需試玩調整。
- 原始備份、測試報告與截圖在 _CodexBackups/chapter2_exploration_llm_20261009/。模型服務退出Play後已確認關閉；沒有 commit／push。

### 本輪原夜間流程回歸

- 同一本輪 council/ 下 final-support 與 final-refuse 均以正常一倍速 Play → P → 1／2 跑完，35／34行字幕逐行比對通過，checks.passed=true、errors=0、playingAfterExit=false、dirty=false、prefsUnchanged=true。兩輪各15則既有警告（13則 Missing Script、2則停用音源），未把清空後 Console 狀態當作無警告的遊玩紀錄。
- 支持分支插刀、成人起身、拉遠與決戰字卡通過；拒絕分支走近莫那、斥責及淡黑退出通過。兩輪十人開場坐姿、孩子全程坐姿與無刀、四棟房屋水平貼地檢查通過，且本輪已查看引擎全景。
- 九個正式來源／設定／場景檔案與 final-local-v2 測試時雜湊一致（final-source-comparison.json）；第一章場景 SHA-256 與本輪開始相同。本輪未提交／推送。

- 清理完成：兩個臨時 Editor probe 及 meta 已移至本輪 verification-tools/，正式 Assets 不保留自動測試工具。Unity 清理後重新編譯／domain reload 完成，實際開啟 Console 顯示0 Error／0 Warning／0 Log，Play停止、場景無未儲存星號；此狀態不取代上述遊玩輪警告記錄。正式 Assets／Documentation／Packages／ProjectSettings／Tools 共3481檔，無單檔達100,000,000 bytes，最大89,742,488 bytes；約1.1GB模型保留在專案外 D 槽。

## 2026-10-10 探索後迎接玩家、單人帶路與3公尺交談

- 依使用者兩張截圖：修正到期後族人在視線外說話，改由原 workers[0] 走到玩家視線前方再邀請跟隨；只保留他帶路。workers[1]／[2] 改放在巨木右／左側附近巡走，後續仍查看被槍殺的同伴。移除底部探索操作字串，交談半徑由1.5改3公尺。已讀固定第二章劇情參考，以本次要求為準。
- 新增 Chapter2ForestEscort，使用既有 Unity Navigation，從本場景森林地面與實體碰撞建立 runtime NavMesh；排除玩家／人物／夜间佈置，不新增下載套件。迎接點優先選玩家鏡頭水平前方2.4公尺，樹幹遮擋時選視線前方附近可達位置，尋路走到後才說原跟隨台詞。玩家位置不重置；非VR在極端俯仰或邊界情況可微調視線確保人物可見。迎接及邀請期間暫停桌面移動／轉頭，VR頭部追蹤保留。
- 邀請結束後一人按路徑走到原 WorkerDestination(0)，速度1.65m/s，玩家落後超過5.5m便等待；黃色箭頭沿可走路徑轉彎。舊 x=-3.2～0.65 的單向窄走廊限制取消，避免玩家在樹後／道路兩側無法跟上，CharacterController 實體碰撞仍保留。Chapter2Actor 增加默認0的 GroundHeight，只有引路者按地面高度更新，其他既有演出維持0。
- 場景保存唯一帶路者與原兩個同伴引用；後兩者改名「巨木右側族人」「巨木左側族人」，各加 Chapter2AmbientNPC，起點相對巨木為(3.8,0,-2.2)／(-3.8,0,-1.7)，來回位移(±0.8,0,-0.4)，最大水平離家約0.894m。兩人也納入自由交談，共七人；帶路者不加入。進入巨木對話前禁用兩人的巡走元件，避免與護樹救援、站起走兩步、或砍樹受驚程式搶姿勢。
- Chapter2Exploration 程式預設與場景序列化 interactionRadius 都為3，移除探索開始和每次關閉交談時的底部提示。附近按 E／點交談按鈕及對話面板內操作提示保留。模型、三分鐘、每人記憶與到期等待規則不變。Documentation/Chapter2.md 和 Chapter2-local-dialogue.md 已更新操作。

### 本輪驗證與已知範圍

- Unity6000.0.58f1 原專案，臨時 Editor probe 注入空白鍵略過片頭，用設定位移把玩家置於不同起點；迎接期間不替玩家转向，正式跟隨段使用實際 CharacterController.Move 沿路前進。檢查1600×900引擎輸出，未使用實體VR，也不是全程真人键鼠操作。
- review-east-v2：縮短探索15秒的右側林地測試，玩家(12,0.08,-8)面朝東；迎接者距離2.380m、水平視角差0.052°，面孔位於畫面中央；玩家身體及鏡頭僅有0.04m自然落地差，無位置傳送。能等待落後玩家，沿路抵達 TreeChoice，errors=0、warnings=1，checks.passed=true。此輪 exploreElapsed 舊測量欄含了迎接時間，不能当作計時證據。
- protect-full：場景正式180秒，和小孩交談，截止前0.4秒送出真實本機LLM問題。時間到仍在生成，回覆及閱讀完成後才迎接；exploreElapsed=207.47秒含生成與閱讀。帶路者距離2.377m、水平視角差0.686°，玩家及鏡頭位置差0。成功抵達巨木、選護樹，兩位同伴 Examining=true，完成後續命令／起身短走，進入 Meeting；errors=0、warnings=2，均為既有停用音源，checks.passed=true。
- 上述兩輪七人均在2.99m可開交談、3.01m拒絕，底部操作提示全程為空；樹旁兩人巡走最大0.894m。另用五處座標（道路兩側、巨木後方及較遠林地）分別驗證引路者到該處、該處回巨木的完整導航路徑，共每輪10條。這是可達性檢查，不代表五處都已逐步走完。
- 首輪 review-east 因 NavMeshPath 在 MonoBehaviour 欄位初始化時呼叫 Unity native API 失敗；已改為主執行緒第一次尋路時建立，該失敗輪不作驗收。後續編譯與遊玩結果另列；原始檔及所有診斷保留於本輪備份。
- final-west：縮短探索15秒，玩家(-14,0.08,5)面朝西。迎接距離2.4m、角差0°，沿路抵達巨木，五次有效砍伐、受驚退步、樹倒及轉入 Meeting 完成；errors=0、warnings=15（13則既有Missing Script及2則停用音源），checks.passed=true。
- final-behind-v2：縮短探索15秒，玩家(0,0.08,19)面朝北，原巨木在玩家後方。引路者繞樹到鏡頭前2.4m、角差0°，能反向繞回原目的地並觸發護樹；兩位同伴均查看倒地者，畫面顯示兩人蹲下，後續進入 Meeting。errors=0、warnings=2，checks.passed=true。其前一輪 final-behind 停在測試驅動的轉角0.2～0.4m死區，已只修正測試工具的前進門檻，不改正式路線或傳送玩家；以前一輪未完成資料作診斷，不作通關驗收。
- 四個通過輪均 playing=false、dirty=false。七個正式程式／場景檔案與上述測試雜湊一致（final-source-comparison.json）。第一章場景 SHA-256 與修改前一致；夜間會議演出檔案和佈置未改，本輪護樹／砍樹只驗證到 Meeting，未重新完整播放夜間兩分支或第一章。
- 預覽圖 Documentation/Chapter2-guide-meets-player-preview.png、Chapter2-tree-witnesses-rescue-preview.png。驗證記錄只代表 Editor、原場景現有碰撞與上述起點；未做獨立建置或實體VR頭戴裝置驗收。沒有提交／推送。

- 最後清理：Chapter2EscortProbe.cs 及 meta 已移至本輪 verification-tools/；正式Assets不留自動測試工具。清理後Unity重新編譯與domain reload成功，實際Console為0 Error／0 Warning／0 Log，Play停止、場景無未儲存星號。此清理後狀態不抹除上列測試輪的既有警告。七個正式來源與驗證雜湊再次一致；Assets／Documentation／Packages／ProjectSettings／Tools共3485檔，無單檔達100,000,000 bytes，最大89,742,488 bytes，本機模型仍在專案外D槽。退出Play後無llama-server殘留程序。

## 2026-10-10 人名與近距迎接

- 本輪只處理使用者五張截圖：帶路者介紹後巡走、漏掉的主警察交談、第二章全員姓名、探索後附近短程迎接、巨木前族人向警察走近並對話。備份及測試位於 `_CodexBackups/chapter2_named_roam_20261010/`。
- `Chapter2CastNames.cs` 與 `Chapter2Actor` 角色欄位統一 Hierarchy 名稱、字幕、交談標題及 LLM 自介。場景19個角色物件都有姓名，其中白天及夜間小孩同為都比·阿威。莫那·魯道、達多·莫那、巴萬·拿威、瓦旦保留，其他為根據原民會名譜創作的虛構配角；完整名單、命名原則及網路來源見 `Documentation/Chapter2-local-dialogue.md`。並非宣稱新增人物參與史實。
- 探索包含九位白天NPC：3警察、5成年族人、1小孩。阿威介紹後約2秒開始附近往返，最大巡走距離2.163m；主警察留在原職位但能交談。小孩判斷改為明確欄位，不依物件名是否含「小孩」。模型維持免費本機Qwen，無新下載或API變更。
- 等當次LLM回答／閱讀結束後，阿威移到玩家附近可行走位置（實測約3.2m），優先視線外，沿短路徑走到視線前；避開人物占用的迎接落點。原本已在很近的位置就直接走來。只瞬移NPC，不移動玩家；導航與單人帶路至原巨木、兩位同伴待命及後續查看倒地者保留。
- 巨木前雙人鏡頭同時顯示阿威與中村正雄。阿威走近約1.25m再向警察說出守護者台詞，雙方彼此朝向；保護分支兩句相互對話也指定對方為聽者。
- `review-east` 是第一輪診斷：主警察真實LLM回答「我叫中村正雄，是一名警察……」，九人2.99m可交談、3.01m不可，errors=0。發現迎接點可能與警察重疊後已增加占位檢查；此輪不能作為最終迎接畫面的證據。
- 最終 `protect-full`：正式180秒，在截止前送出真實LLM問題，expiryWaited=true；含生成／閱讀200.293秒。迎接1.501秒，路徑3.514m，玩家身體和鏡頭位置差0。九人範圍與命名、指南巡走、雙人畫面和朝向通過，護樹後兩位同伴Examining=true並進入Meeting；errors=0、warnings=2（既有停用音源），checks.passed=true。
- 最終 `final-west`：探索縮短25秒，玩家在西側面朝西；迎接1.774秒，附近路徑4.164m。保留等候落後玩家，實際以CharacterController走到巨木，砍倒巨木並進入Meeting；errors=0、warnings=15（13既有Missing Script、2停用音源），checks.passed=true。
- 最終 `final-behind`：探索縮短25秒，以實際Input System E事件開啟主警察對話，再把玩家測試起點移到巨木後方朝北；迎接1.821秒，路徑4.164m，反向返回巨木與護樹、兩同伴查看至Meeting通過；errors=0、warnings=2，checks.passed=true。各輪五處導航取樣均完整，不能解讀為已實際走遍所有地圖角落。
- `night-names`：正式Play→P事件路徑，四位既定人名正確、夜間全員具名，第一位達多的字幕仍為「達多·莫那（莫那·魯道之子）」。errors=0、warnings=15，檢查通過。本輪夜間只測開場至達多第一句，未重跑支持／拒絕完整結尾。
- 截圖已實際查看，包括警察LLM姓名答覆、E鍵介面、修正後近距迎接、雙人對話、兩同伴查看、砍樹及P夜间首句。精選預覽：`Documentation/Chapter2-named-tree-exchange-preview.png`、`Documentation/Chapter2-named-officer-dialogue-preview.png`。
- 驗證為Unity Editor加測試驅動的輸入／導航移動與引擎渲染，非真人完整走图、獨立建置或實體VR。未修改／測試第一章；未提交／推送GitHub。已有的遺失腳本與停用音源警告保留於各輪報告，並未宣稱修復。

- 本輪清理完成：兩個臨時Editor probe及meta已移到verification-tools，正式Assets已移除。Unity重新編譯／domain reload完成，實際Console為0 Error／0 Warning／0 Log，Play停止且無場景未儲存星號；不取代上列測試警告紀錄。10個正式程式／場景檔案與最終測試雜湊一致，第一章場景SHA-256未變。正式來源及文件共3489檔，無單檔達100,000,000 bytes，最大89,742,488 bytes；退出Play後無llama-server殘留。

## 2026-10-10 Gemma 本機試用

使用者想改試 Gemma，並提供 Qwen 把「你猜我叫甚麼名字」答成「我叫西仔西克，是個部落青年」的例子。本輪只處理本機對話模型與角色提示，保留既有劇情、人物、巡走及三分鐘探索。

- 模型為 `ggml-org/gemma-4-E2B-it-GGUF` revision `b4243c156154b6dca9324415f8c7ccc098b4aed1` 的 `gemma-4-E2B-it-Q4_0.gguf`，2,841,481,184 bytes；SHA-256 `8e30dff3ac4c8434c49a7036fa15564bdbb6044e42bf04550bf1a096ad7e6a52` 已驗證。放在 D:/WusheLocalLLM/models，沒有加入 Assets 或 Git。原 Qwen2.5-1.5B-Instruct Q4_K_M 保留。
- `Chapter2LocalDialogue` 預設模型與 StreamingAssets JSON 切為 Gemma；既有 CPU llama.cpp b11526 可直接載入，新增 `--reasoning off`，維持4執行緒、4096上下文、160輸出token、temperature .65。無付費 API、外網備援或金鑰。
- Persona 明確區分「你叫什麼」與「我叫什麼」，未知玩家名字要說不知道，西仔希克是地名；先給共同背景，再強調 NPC 個別身分。警察不能把族人的祖靈、家園、信仰說成自己的；小孩不自稱成年青年。仍是模型生成，沒有固定答案替代或關鍵字攔截。
- `Tools/LocalLLM/Install-LocalNPC.ps1` 預設 `-Model Gemma4E2B`，另可 `-Model Qwen25_15B` 切回，`-DownloadOnly` 只下載不改配置。先停 Play 再切換。兩種選項均核對 SHA；組員仍須在各自電腦安裝，不會因 GitHub 拉取專案就取得模型。文件已更新。

本輪實證在 `_CodexBackups/chapter2_gemma_trial_20261010/`：

1. Qwen/Gemma 各以原提示與更新提示測10個案例（seed42；三種角色的玩家姓名猜測、自介，另測地名、警察森林立場、不知道的文化細節、先前自介記憶）。這是小樣本、非正式準確率評測。Gemma 原提示仍會把警察當族人；更新後本輪例子正確。Qwen 更新提示仍有指涉／身分與虛構祭典問題。Gemma 更新提示在 Editor 未 Play 時約1.5–11.2秒，不能視為遊戲內速度。
2. 正式配置以 Unity Play → 空白鍵略過片頭 → 探索，透過實際輸入欄及送出按鈕連續7次真實 Gemma 問答：阿威未知玩家姓名、自介、接收阿山自介、回想阿山；中村未知玩家姓名、警察森林立場；小孩未知玩家姓名。全部收到非空回答，最後 errors=0、warnings=14（13個既有 missing script、1個停用音源），finished=true、playing=false、dirty=false。三種角色的 Unity 實際 Persona 與更新提示基準逐字核對一致。
3. 在 i5-1235U／16GB／Intel Iris Xe、Unity Play 與其他桌面程式同開的這輪實測，首問含啟動55.674秒，切警察首問29.700秒，切小孩首問34.071秒；同NPC後續2.324–6.172秒。記憶體壓力大（觀測最低剩餘約0.6GB），不能承諾每次即時。Qwen的速度和Gemma的回答品質須分開評估。
4. 引擎1600×900渲染已查看玩家記憶、警察立場畫面。為連續測7問，測試器僅在 Play 內把探索時間改600秒；正式場景仍180秒，場景檔未保存或改動。未重跑全部主線分支、三分鐘到期帶路、獨立建置或實體VR；先前場景驗證不能當成這轮模型的新證據。
5. 第一輪測試遭背景重編譯中斷，造成既有 rig 非序列化狀態遺失並反覆 IndexOutOfRange，記在 `hotreload-interrupted-*`，不算通過。停止該輪、清掉自己啟動而因重載失去追蹤的服務，編譯完成後全新 Play 的上述最終輪沒有錯誤，退出時服務正常自動結束。

臨時驗證腳本已移出 Assets，放至上述備份目錄。大型下載及部分檔案均在 D 槽，下載暫存已清理。後續可研究共享提示快取與提早暖機以改善首問等待，本輪未實作；模型仍可能產生錯誤歷史資訊，不能把以上少量問題通過解讀成零幻覺。

## 2026-10-10 免費雲端 Gemma 接線

使用者要求「先幫我試試看免費的」，授權嘗試雲端 NPC 推論，仍不可開啟付費 API。已依 Google 官方 Gemma API／價格頁查證，實作私人 Windows 試用接法；**還沒有使用者的免費 API key，因此尚未完成真實雲端問答**。

- 新增 `Assets/Chapter2/Chapter2CloudGemma.cs`：固定 Google HTTPS `generateContent`，只允許 `gemma-4-26b-a4b-it` 與 `gemma-4-31b-it`。沿用 Persona 與每位 NPC 的三輪記憶，systemInstruction 與 user/model 分開，關閉思考、256輸出token、30秒timeout；只讀非 thought 文字，不啟用搜尋／工具。429、401/403、404、斷線、空內容／封鎖內容有提示，不重試到付費模型。
- `Chapter2LocalDialogue` 支援本機／雲端兩種請求。雲端成功設定後不啟動本機模型；沒有設定時走既有本機。失敗不偷偷切換服務；可由設定視窗切回本機，下次 Play 生效。補齊取消 health request 的資源釋放，避免取消暖機後重複啟動自己尚在載入的程序。
- 新增 `Assets/Editor/Chapter2DialogueSettingsWindow.cs`，Unity 選單 `Tools → Chapter 2 → NPC 對話設定（免費雲端／本機）`。金鑰遮罩欄、Free Tier 確認、「測試並啟用」與本機切回。測試問「你猜我叫甚麼名字」，收到真實回答且儲存成功才启用雲端；此連線測試使用簡短阿威角色提示，正式遊戲仍用完整 Persona。此工具本身無法由 API key 判斷計費層級，使用者須先確認 AI Studio 專案為 Free Tier、未啟用計費。
- 金鑰以 Windows DPAPI 保存在 `%LOCALAPPDATA%/WusheNPC/cloud-gemma.json`，不放入專案、場景、EditorPrefs 或 Git。程式不列印金鑰／伺服器原始錯誤；header 傳 key，禁止重新導向。本轮沒有建立金鑰、開啟計費或對 Google 送出推論請求。組員可用各自免費金鑰，不需本機模型；尚未建立多人共用後端、公開發行的金鑰代理或 Quest／WebGL 適配。
- `Documentation/Chapter2-local-dialogue.md` 新增完整設定方式、資料傳送範圍、朋友電腦操作、免費额度與公開發行限制；Google 免費服务內容可能用於改善產品，已在設定視窗說明。

實際驗證，資料在 `_CodexBackups/chapter2_cloud_gemma_20261010/`：

1. 使用本專案 Unity 6000.0.58f1 Roslyn 與實際 rsp 參考，編譯整份 runtime 和 Editor 程式均 exit=0；主 Editor 重新編譯後 `scriptCompilationFailed=false`。既有棄用 API 警告未處理，不能宣稱整個專案零警告。
2. 隔離的小型 Unity 專案與主 Unity Editor 各執行12項接線檢查，均 passed=true：多輪格式、獨立 Persona、thought 排除、封鎖／空／損壞 JSON、額度／權限／網路錯誤提示、固定端點與禁止付費模型、Windows 加密往返、未改動個人設定。這些沒有真實 API key，也不是真實 Google 回應測試。
3. 主專案 Play → 空白鍵 → NPC 問答，本機回歸成功：阿威回答「我不知道你叫什麼名字。你先告訴我你的名字吧。」30.573秒；errors=0、warnings=1（既有停用音源），finished=true、playing=false、dirty=false。引擎截圖已查看。測試器暫將探索延長600秒，正式配置仍180秒。當時存在外部 llama-server，遊戲未關閉非自己啟動的服務。
4. 第二章場景與本機 JSON 雜湊前後一致。未改第一章、角色位置或主線。未重跑全部主線分支、獨立建置、實體VR，也未驗證雲端遊戲內速度或品質。
5. 原生電腦操作工具無法還原最小化的 Unity（`activate_window` timeout）；仍透過 Unity Editor 腳本完成上述編譯和回歸。設定視窗已由選單方法要求開啟，但未取得原生視窗畫面驗收。已請使用者自行開啟 Unity、用 Google 帳號取得 Free Tier 金鑰，且明確請勿把金鑰貼進聊天；尚未收到回覆。

待使用者操作：在 Unity 上述設定視窗貼入免費金鑰並按「測試並啟用」，再重跑實際遊戲內三種角色的未知玩家姓名、玩家名字記憶、警察森林立場與回應耗時。金鑰不得進聊天或 Git；未收到金鑰不能把目前接線檢查報成雲端已測通。

清理：兩個臨時主專案 Editor 驗證器及 meta 已移至本輪 verification-tools；正式 Assets 只保留對話程式與設定視窗。沒有提交或推送 GitHub。


## 2026-10-10 第一章記憶、森林邊界與人物問候氣泡

使用者要求把前一輪建議的第一章事件摘要／實際分支／人物知情範圍接入第二章，修正自由探索走出地形後落入空景，並以頭上氣泡取代綠色交談提示。本輪直接修改原專案；已讀根目錄指引及固定劇情第2–5頁，現行第一章程式優先於較舊設計文件。

- `Chapter2StoryMemory.cs` 與 Resources/Chapter1NpcMemory.json 保存可編輯的遊戲事件摘要、九人的知情範圍。第一章開始重置本輪標記，完成後寫入選擇、演出版本、送酒／食物次數及舞蹈完成狀態。補齊 Chapter1PerformanceController.Doorway 的上前阻止結尾 SaveChapterResult 呼叫；既有沉默觀望儲存也產生新記錄。未更動第一章動畫／字幕／場景。
- NPC 每次詢問均讀取當前存檔，兩個分支只選其一；沒有完成記錄時不猜玩家經歷。舊版只有選擇的存檔僅承接選擇，不推測詳細演出。需使用此版重玩完成第一章才有完整任務與結果。NPC 不能把第二章未發生的巨木事件／密議當記憶，模型回答不能改寫遊戲記錄。
- 阿威、帖木為外場見證者；達奇斯、拉娃、伊婉聽同伴轉述；第二章三名警察聽同僚轉述，並非第一章那兩位肇事者；都比只知婚禮被打斷，不提供成人衝突細節。這些是明確標示的本作虛構人物設定，不是新增歷史人物事實；JSON 可編輯。未建立雲端資料庫、文化史料庫或永久自由聊天記憶，聊天仍每 NPC 最近三輪。
- `Chapter2GreetingBubble` 以 UGUI 幾何繪製奶油白圓角氣泡、姓名、早安／你好及右下「回覆 E」。三公尺內可見人物顯示，離開／聊天／到期／P跳章收起；E選最近可見者，按鈕指定該人物，保留VR扳機入口。問候不呼叫模型，8秒冷卻避免範圍邊緣反覆換字；人物打招呼時短暫面向玩家後繼續巡走。原輸入與回答面板沿用。
- `Chapter2WorldBoundary` 附加於 Chapter2Player，從 Forest ground Collider 取得地面。預設日間可走範圍中心左右32m、前後36m，外圍地形仍作遠景；每次鍵鼠／搖桿 Move 都限制於安全區，可沿邊與返回。地圖外／地下異常位置恢復到地面，保留視角朝向。不可移動的劇情演出不受此限制。原無限制路徑引導未重新加回舊單向走廊。

### 實際驗證與限制

證據與原始檔保存在 `_CodexBackups/chapter2_memory_bounds_bubbles_20261010/`。

1. Unity6000.0.58f1 原專案編譯成功。memory-checks.json 全部通過：未玩／未完成不造記憶、兩分支互斥、已完成婚禮任務、九人知情設定、小孩過濾、舊存檔保守承接、重開不沿用舊結果。
2. chapter1-save-checks.json 通過：在實際第一章場景，以受控欄位呼叫真正 SaveChapterResult，Intervene／Watch 均保存完整新記錄；測試後恢復欄位、所有碰過的 PlayerPrefs 及原第二章場景，dirty=false。這是儲存路徑驗證，沒有重新完整播放第一章兩段演出。
3. 最終 play-result.json finished=true、passed=true、stage=TreeChoice、errors=0、warnings=1（既有停用音源）、playing=false、dirty=false。九位人物逐一驗證2.6m氣泡、3.04m隱藏／拒絕交談；阿威透過 Input System E，其餘透過 Unity UI PointerClick 事件開啟。四邊及四角反覆向外 Move 被擋住且可走回，正常邊界不觸發恢復傳送；額外地圖外低於地面的位置成功恢復，朝向不變。
4. 正式遊戲輸入／送出路徑發送三次真實已啟用免費雲端 Gemma 問題：阿威談上前阻止後被警棍擊倒，帖木談沉默觀望與木屋事件，小孩表示未目睹，只聽大人提及。三次非空回答已查看引擎截圖；這是少量功能案例，不代表歷史準確率或零幻覺。為比較兩種記憶，本輪在測試中切換模擬完成存檔，退出後恢復使用者原存檔。
5. 最後一問送出後刻意把截止點設為0.15秒，驗證到期等待正在生成的回答及閱讀，之後原單人迎接／導航成功到TreeChoice。為容納全部案例，測試期間探索暫設600秒；場景正式值仍180秒，沒有儲存場景。
6. diagnostic-approach-result／diagnostic-no-bubble-background 是底板缺CanvasRenderer、測試接近點與岩石碰撞的早期診斷；已修正底板並讓測試器挑空的站位。diagnostic-features-escort-driver 已通過九人、邊界、三次問答，但測試器停在引路者而非巨木互動點，最後逾時；已僅修正測試目標，最終完整輪成功。diagnostic-outer-terrain 圖為早期只擋地形邊緣的空景版本，後來將可走區內縮，map-edge.png 才是最終可見森林邊界。
7. 第一章與第二章場景、本機模型配置 SHA-256 前後一致（unchanged-after.json）。未改金鑰、計費、模型；沒有下載新套件。所有正式資產小於100,000,000bytes。未重跑砍樹／護樹／會議兩分支完整結尾、獨立建置或實體VR。

- 補充最終預覽：preview-result.json 使用正式180秒設定，Play→空白鍵進探索、氣泡實際畫面已查看；再送 P 至 Meeting 並等淡入完成，所有氣泡隱藏，errors=0、warnings=2（既有停用音源），playing=false、dirty=false。只確認夜間開場，不代表完整會議分支驗收。預覽保存於 Documentation/Chapter2-greeting-bubble-preview.png。
- 本輪驗證工具 Chapter2MemoryBubbleProbe.cs 及 meta 已移至備份 verification-tools，正式Assets不保留自動播放測試程式。11份正式程式／背景資料與最終遊玩時雜湊一致（final-source-comparison.json）；第一章／第二章場景與本機設定均未改。未 commit／push。
- 清理後 Unity 完成重新編譯及 assembly/domain reload，驗證工具與待處理命令檔均不存在；11份正式來源最後再次比對與通過輪完全一致。Play已停止，最終測試退出時場景dirty=false。未將舊Console斷言或既有音源警告解讀成已修復。

## 2026-10-10 問候氣泡配色

依使用者要求降低白色氣泡的突兀感，僅修改 Chapter2GreetingBubble.cs 配色：深灰綠底、柔和灰綠描邊、米金色姓名與問候、橄欖綠回覆按鈕。版面、三公尺距離、回覆 E 與對話流程不變。同步更新對話說明與 Documentation/Chapter2-greeting-bubble-preview.png。

實際驗證：原 Unity Editor 編譯成功（scriptCompilationFailed=false）。重用既有預覽工具進入 Play／探索，檢視引擎截圖確認新顏色與文字可讀；正式探索180秒保持。P跳會議後氣泡收起；preview-result.json passed=true、errors=0、warnings=2（既有停用音源），退出 playing=false、dirty=false。本次未發送 LLM 問題，未重跑記憶／邊界／全部故事分支或實體VR。證據在 _CodexBackups/chapter2_bubble_palette_20261010/；臨時預覽工具移回備份，不留在正式 Assets。未提交或推送。
## 2026-10-10 修復 Unity Game 預覽拉伸

使用者回報整個畫面及字幕橫向拉長。檢查 UserSettings/Layouts 中 GameView 的已儲存 ZoomArea，發現 m_Scale 約為 (5, 1)，且平移偏離中央。透過 Unity 原生介面重新設定 Game 視窗縮放滑桿到適合大小；一般面板恢復 (0.34837964, 0.34837964)，平移 (348.59998, 150.5)。再放大 Game 分頁確認仍維持正常16:9、完整構圖及正常文字；放大預覽顯示約0.78x。CurrentMaximizeLayout 的恢復用配置也已由 Unity 更新為等比例縮放。

本次只修正本機 Editor 預覽狀態，未修改遊戲程式、場景、攝影機或氣泡顏色。實際看過修復前後 Unity 原生視窗；未重播 Play／LLM／故事分支。結束時保留放大的 Game 分頁、Play停止。兩側黑邊是正常等比例留邊。導致先前水平／垂直倍率不同的具體操作尚未確認，不能推斷是玩家或遊戲程式造成。
