# 第三章改為駐在所開場

2026-10-13 使用者新要求優先於舊部落開場／先領刀再領槍的設計。已參考固定劇情摘錄第 6–9 頁；本次台詞是遊戲編寫，並非歷史原話。

## 場景與流程

- 正式入口仍是 `Assets/Scenes/第三章.unity`，Play 直接進入馬赫坡駐在所，巴萬在玩家面前、建築在後方。原部落區域已從主場景移出。
- `Assets/Scenes/ch3部落與山林.unity` 保留原部落、山林、同伴、刀桌、兩輪開場及短山路。可直接從 Editor 開啟播放，走到原銜接點顯示本段結束卡。未加入正式章節選單或 Build Settings。
- 主場景兩輪玩家輸入與巴萬 LLM 回答；第二輪保留閱讀時間後自動帶路，不再另按出發按鈕。空白、重複送出與第三次輸入不計數。中文 IME、逾時備援與跳段取消均保留。
- 抵達莫那後，莫那說明已攻打駐在所日本警察，接著前往霧社與達多會合，請玩家選一把襯手的武器。
- 使用第二章砍樹選擇框的比例／配色，兩個選項為「1 刀 · 獵刀」與「2 槍 · 村田十八年式步槍」，支援滑鼠與數字鍵。
- 原部落刀、桌面和四根桌腳移到駐在所右側（面朝建築），槍桌在左側。刀桌有實體碰撞。
- 選擇後黃色小箭頭只指向對應武器；走近後 E 拿取。未選、選錯武器、距離太遠或重複拿取都無效。
- 拿取的同一流程立即開啟 LLM「準備好了嗎」，不需回巴萬身邊再按 E。巴萬在問答已開啟時走到玩家附近，攝影機持續對著他。
- 肯定回答接出發與短交戰；不明確回答留在問答；否定沿用莫那斥責／收回武器／停止 Editor Play，台詞依刀或槍調整。獨立程式沿用結束卡。

## 單一武器規則

`Chapter3Flow.CarriedWeapon` 是本次遊玩的唯一持有狀態，不寫入通關或第四章結果。領取後另一把仍在桌上，靠近即提示「只能持有一種武器」。提示在 LateUpdate 套用，避免帶路協程覆蓋。

山路、學校兩任務與檢查點重試沿用選定武器；1／2 不能切到未持有武器。刀不顯示補彈提示，也不接受補彈。快速跳 F7／F8 若尚未領武器，僅配一把槍供製作測試；F6 重新開始兩輪駐在所開場與選擇。

持刀時學校守衛移到可近身的站位：突破線在各道橫木前，掩護線在當前可走區域內；避免守衛在路障／進度邊界後，導致刀無法攻擊而卡關。持刀的學校守衛交戰距離縮為 12m，保留原 25 點傷害、瞄準預告與 4.5 秒射擊間隔，讓玩家有近身空間；拿槍時沿用原站位及 32m 交戰距離。任務仍分別是清除守衛後長按 E 拆橫木，以及 E 指揮同伴穿越火線。

## 實作檔案

- `Chapter3OpeningConversation`：駐在所兩輪自由文字問答，沿用既有 Chapter2LocalDialogue 傳輸，不更動模型服務或金鑰。
- `Chapter3StationController`：開場、帶路、莫那說明、選武器、領取、即時準備確認與單一武器提示。
- `Chapter3WeaponChoiceUI`：同 Canvas 的雙選項框。
- `Chapter3Flow`、`Chapter3Weapons`、`Chapter3Combat`、`Chapter3SchoolBattle`、`Chapter3Enemy`：持有狀態、重試保留、動態操作提示與學校近戰相容。
- `Chapter3ReadinessConversation`：武器名稱與 LLM 背景依持有選擇變動。
- `Chapter3Controller`：保留場景的結束卡與 Editor 重玩入口。
- `Chapter3StationOpeningAuthoring`：先另存保留場景，再搬原刀桌並移出部落；重複執行不覆蓋保留場景。

## 本輪驗證

證據與修改前備份：`_CodexBackups/chapter3_station_opening_20261013/`。以下 10 組報告均在原專案 Editor 執行，`completed=true`、`errors=[]`、`warnings=[]`，共 189 項布林判定通過。

| 報告 | 通過判定 | 實際涵蓋 |
| --- | ---: | --- |
| `test-knife-final.json` | 31 | 兩輪問答、自動帶路、莫那台詞、滑鼠選刀、箭頭、錯誤／過遠領取、立即準備問答、單一武器警告、不明確／肯定回答、短交戰、學校及重試保留 |
| `test-rifle-final.json` | 31 | 相同主要流程，以數字2選槍、步槍攻擊與重試 |
| `test-school-knife-bawan.json` | 7 | 實際持刀擊倒兩名守衛、長按E拆兩道橫木、三人匯合；另記錄剩餘體力50 |
| `test-school-knife-pihu.json` | 7 | 實際持刀擊倒兩名守衛、E指揮兩次穿越、三人匯合；另記錄剩餘體力50 |
| `test-refuse.json` | 24 | 持刀否定回答、莫那靠近斥責、收刀台詞、拒絕跳段、遊戲主動停止Play |
| `test-refuse-rifle.json` | 24 | 同上，持槍分支及對應台詞 |
| `test-cancel-opening.json` | 7 | 開場問答送出後F8取消，延遲回覆不推進舊流程，F6重新開始 |
| `test-cancel-ready.json` | 23 | 領取後準備問答送出時F8取消，F6重設持有狀態 |
| `test-live.json` | 30 | 使用正常25秒逾時設定重走槍流程，開場2次與準備確認1次均為服務實際回覆，沒有使用備援 |
| `test-archive.json` | 5 | 保留場景兩輪問答、拿原刀、短山路、結束卡，未加入Build Settings仍可在Editor重播 |

刀／槍31項報告早於最後學校近戰相容及提示文字微調；學校相容改完後另跑兩條持刀任務，最後提示文字改完後跑否定、取消與真實LLM流程。`runtime-validation-hashes.json` 與 `scene-validation-hashes.json` 記錄最終驗證版本，收尾比對一致。未把早期31項報告稱為最後一版重新跑過的全測試。

已實際檢視本輪引擎PNG：駐在所開場取景、雙選項框、兩側武器與黃色箭頭、拿取後立即問答、另一桌的單一武器限制、莫那否定表演、持刀兩任務的匯合。`test-live-opening.png` 與 `test-live-choice.png` 是最終開場及選擇框畫面。

測試使用 Input System 事件、UI callback、武器入口與 CharacterController，部分重試測試注入傷害；持刀兩條任務全線沒有注入傷害或重試，使用實際近戰攻擊及警察射擊。這不等於真人全段鍵鼠、實體VR或獨立建置測試；也沒有測得即代表所有未來LLM生成台詞皆符合要求。除 `test-live` 外，多數問答測試刻意縮短逾時以驗證備援；沒有保存測試用逾時／字幕加速值。

早期 `diagnostic-hint-order.json` 抓到提示被帶路協程覆蓋，已改LateUpdate。`diagnostic-probe-gate-distance.json` 與 `diagnostic-probe-melee-position.json` 是測試驅動的站位問題：前者未走進互動距離，後者追逐被同伴占住的精確座標；調整實際移動與近戰距離後重跑通過。這些診斷與 `test-knife.json` 不作通過依據。

收尾 `audit.txt`：正式第三章、Play已停、dirty=false、compileFailed=false、missingScripts=0、missingMeshes=0。臨時 `Chapter3OpeningProbe.cs` 與 meta 已移出Assets，保留在 `verification-tools/`；移除後 Editor assembly 成功重新編譯與 reload，`cleanup-import.log` 沒有新編譯錯誤。第一章、第二章和Build Settings的SHA256與修改前一致。報告零錯誤是各測試採集範圍內的結果，未宣稱整份歷史Editor/OpenXR日誌清空。
