# 第三章第 2 批：單一場景、刀槍操作與山路短交戰

2026-10-12。依本次要求，第三章所有內容保存在 `Assets/Scenes/第三章.unity`。Hierarchy 的 `01 部落與山林`、`02 駐在所`、`03 山路短交戰` 是同一場景內輪流啟用的區域，共用玩家、攝影機、字幕與 EventSystem。保留原第三章 GUID，章節選單仍從部落開頭進入。

## 2026-10-13：六張截圖修正

- 開場隱藏「交談 0／3」數字，內部三輪交談條件不變。
- 木柵立柱及橫桿補上非 trigger 的 BoxCollider。出發路線繞到靠駐在所建築側的末端（z=3），再接原山路；土路與路標配合移位。`Chapter3PathMotor` 每步檢查膠囊碰撞，遇障礙物規劃短距離繞路，避免同伴在轉角直接穿柵；走不到就等待，不能穿越或瞬移。
- `Chapter3ViewArm` 使用原握持手模型，在 runtime 把前臂延續至鏡頭外的肩側，保留手指／持握，避免揮刀中段露出開口。没有改來源 FBX 或原手網格資產。
- `Chapter3CompanionReaction` 為兩名同伴提供攻擊判定與演出。刀擊會轉頭、後仰、抬手並以字幕回應「嘿，為什麼要攻擊我？你的敵人是日本警察！」，不扣同伴生命。步槍射線先遇到同伴時顯示「不能拿槍攻擊你的夥伴。」，阻止子彈、槍聲、槍口火光與耗彈；空膛朝同伴按攻擊也顯示同一提醒。兩種武器仍受掩體阻擋。
- 巡警倒地時，武器先跟隨骨架倒下，再平放於身旁，最低點固定在地表上約 3.5 公分；不再只停用瞄準腳本而把槍留在原地。
- 戰後移除原先「NPC 永遠跟在玩家後面」的邏輯。對話後朝道路前方看，巴萬與比胡先往前集合，再由巴萬以步行速度帶往出口；距離玩家超過 5.5 公尺時等待。兩位同伴與玩家都到達才進下一批銜接卡；檢查點重試會把同伴位置和攻擊反應一起重設。玩家已在出口附近時也不再把視角拉回後方。

本輪不是第 3 批製作；下一批仍是學校／運動會空間配置，全部繼續留在第三章單一場景。

### 本輪實際驗證與交付

證據：`_CodexBackups/chapter3_fixes_20261012/`。原 Unity 視窗在工具中無法恢復（程序仍存在），所以建立独立驗證副本，以 Unity 6000.0.58f1 的 batch Editor Play Mode 執行。F6/F7/E 使用 Input System 事件，行走使用既有 CharacterController；背景模式不保有 Game View 游標鎖定，戰鬥攻擊／換武器／裝填由臨時 driver 呼叫原公開方法。此區分不能寫成真人滑鼠或原 Editor 全段手動驗證。

| 報告 | 實際結果 |
| --- | --- |
| `test-station.json` | 約 69 秒；開場計數隱藏、F6、接令、E 領槍／出發、木柵內外雙向實體阻擋、完整繞路與同伴障礙物檢查、抵達交戰區；completed=true、errors=[] |
| `test-combat.json` | 約 42 秒；22 項檢查：兩人刀擊反應、裝彈／空膛友軍射擊攔截與不耗彈、掩體、單發裝填、正常敵人刀／槍傷害、三把槍落地、前方集合／等待／兩人同行至出口；completed=true、errors=[] |
| `test-escort-rear.json` | 最後視角修正後，從交戰起點清除敵人，巴萬在前方集合並完整帶路；completed=true、errors=[] |
| `test-escort-ahead.json` | 最後視角修正後，玩家已站在出口前方 z=29.5，視角仍向前、同伴抵達後正常銜接；completed=true、errors=[] |
| `audit.txt` | 驗證副本存檔後：dirty=false、playing=false、compileFailed=false、missingScripts=0、missingMeshes=0、sceneCount=1 |

已人工檢視引擎輸出的 `conversation-hidden.png`、`fence-route-around-end.png`、`knife-companion-reaction.png`、`rifle-companion-warning.png`、`knife-swing-peak.png`／`knife-swing-4x3.png`、`rifle-reload-arm.png`、`police-rifle-ground.png` 和 `escort-start-forward.png` 等畫面。槍最低點約 0.035 公尺、最高點約 0.175 公尺，沒有懸空。早期 probe 報告是背景輸入／驅動時序校正，不能當驗收；正式結果以上表四份為準。

`delivered-hashes.json` 確認同步回原專案的第三章場景與八份 runtime 原始碼皆與驗證版本相同；新腳本 meta 同步，原第三章 GUID 保留。`protected-scenes-after.json` 確認第一／第二章場景未變。第三章約 6.63 MB，最大資產仍為 89,742,488 bytes，低於 100 MB。臨時 `Chapter3FixesProbe` 已移至 `verification-tools/`，沒有留在 Assets。保留 `Chapter3CorrectionsAuthoring` 作可重跑的場景設定工具。

沒有實體 VR、獨立建置、真人完整鍵鼠、LLM 品質回歸；本輪沒有新增語音配音。原 Editor 顯示仍需重新載入磁碟上的第三章。Play 啟動仍有既有 OpenXR runtime 診斷，errors=[] 指 driver 開始後的執行時錯誤。沒有 commit 或 push。下方「本輪實際驗證」是原第 2 批歷史紀錄，不取代本段的新證據。

臨時驗證專案仍在 `D:/CodexValidation/chapter3_fixes_20261012`：測試完成後的清理指令被自動審核拒絕（只回覆 blocked by policy），沒有執行刪除。交付仍以 C 槽原專案為準，副本狀態記於證據目錄的 `validation-copy-status.txt`。

原獨立 `第三章_駐在所.unity` 及 meta 移到 `_CodexBackups/chapter3_batch2_20261012/archived-scenes/`，並移出 Build Settings。後續學校、運動會、救援及林間結尾仍做在這個第三章場景中，不另建章節子場景。舊建立工具保留作歷史來源；不要重跑舊駐在所場景建立。

## 操作

| 按鍵 | 功能 |
| --- | --- |
| F6 | 隨時從駐在所開頭試玩，取消正在執行的台詞／交談／戰鬥 |
| F7 | 隨時从短交戰開頭試玩，自動備妥獵刀、步槍與彈藥 |
| 空白鍵 | 略過當前預寫台詞，仍有最短 0.25 秒保護；不略過三輪 LLM 交談 |
| WASD | 移動 |
| E | 原本的領刀／領槍／確認出發；戰鬥時在後方木箱旁補備彈 |
| 1 / 2 | 切換獵刀／步槍 |
| 滑鼠左鍵 | 揮刀／射擊 |
| 按住右鍵 | 步槍瞄準；非戰鬥時環視 |
| R | 步槍單發裝填；倒下時從交戰前檢查點重試 |
| Esc | 交戰時放開滑鼠；再點一次遊戲畫面恢復控制，這次點擊不攻擊 |

快速跳轉使用功能鍵，不會把 P 等字母從中文交談欄攔走。F6/F7 是開發試玩捷徑，不記錄通關、任務路線或比胡的後續生死。

## 本批玩法

部落原三輪交談、領刀與山路 → 駐在所莫那交代任務、E 領槍、E 確認出發 → 山路遭遇三名巡警 → 清除巡警 → 走到路口 → 學校與運動會下一批銜接卡。

- 玩家持原有賽德克獵刀与村田十八年式簡化步槍。手部使用專案 `Hands_Rigged.fbx` 烘焙的握持網格；持刀有斬擊弧線，持槍有雙手位置、瞄準與後座，裝填有槍機後拉、退殼、放入一發、關閉槍機。這是桌面玩法動畫，不是實物槍械操作示範或 VR 雙手抓握。
- 步槍膛內最多 1 發、初始 8 發備彈；打空只能裝填，裝填約 2.6 秒。完成裝填才扣 1 發備彈，裝填中禁止射擊與切武器。後方補給箱可把備彈補回 8 發，避免打空後卡關。
- 獵刀範圍 2.2 公尺內、前方 42 度，一次斬擊最多命中一人；命中 60 傷害，兩刀擊倒。步槍命中 100 傷害；兩種攻擊都檢查掩體阻擋。
- 三名巡警一名前進、兩名守位。看見玩家後先抬槍約 1.45 秒，射擊時再次檢查視線，岩石與木箱可以擋住。射擊造成 25 傷害；玩家 100 體力，受傷短暫泛紅。敵人中刀會後仰、被擊倒會倒地，沒有血腥肢解。
- 玩家倒下可按 R 或畫面按鈕重試：重設體力、三名敵人、彈藥與位置；取消未完成裝填。巴萬與比胡留在身旁，這次遭遇沒有他們的生死分支。
- 完成後同伴一起往前走，靠近路口才淡黑。沒有學校人群、兩任務線、比胡救援或第四章記憶，這些仍按第 3–6 批製作。

## 資料與實作界線

本段沿用既定四章版本與故事 PDF 中遠程／近戰／支援的方向。山路三名巡警、具體對白和交战安排為遊戲虛構編排，不宣稱是史料所載的一次具體遭遇。槍聲沿用專案音效，沒有新增配音。原第一、第二章場景與模型資產保留；沒有更改雲端服務、金鑰、LLM 提示或計費。

主要程式：`Chapter3Flow` 負責單場景切段；`Chapter3Combat` 管理交戰及檢查點；`Chapter3Weapons` 管理輸入、刀槍與裝填；`Chapter3Enemy` 管理巡警反應、視線和攻擊。`Chapter3UnifiedAuthoring` 是一次性合併／製作工具，已合併的場景會拒絕再次 Build。後續直接修改已儲存的場景。

## 本輪實際驗證

證據集中在 `_CodexBackups/chapter3_batch2_20261012/`。使用原 Unity 6000.0.58f1 Editor 的 Play Mode；路線由臨時 Editor 驅動既有 CharacterController，鍵鼠由 Unity Input System 事件送入。已檢視引擎渲染圖與原生 Unity 編輯器畫面，並非真人從頭手動遊玩或 VR 實機測試。

- `test-full.json`：部落三輪交談、領刀、山路、駐在所接令／領槍／確認出發、山路交戰／倒下重試／通過出口，全段約 163 秒，`completed=true`、`errors=[]`；場景 handle 自始至終相同，`sceneCount=1`，只有一個啟用的攝影機與 AudioListener。此輪只在 runtime 用既有預寫備援及縮短字幕，沒有送出雲端問答或改正式服務設定。其後只調整手部網格／構圖、跳段恢復行走速度，未再聲稱全段重跑。
- `test-combat-release.json`：最終手部／構圖版本的完整交戰。24 項檢查通過，包括 F7、1／2、滑鼠斬擊、近／遠距刀擊、掩體擋敵方／玩家射擊、敵人傷害、R 重試、空膛擋連射、裝填中擋射擊／換武器、單發扣彈、右鍵瞄準、步槍命中／倒地、三人全部擊倒及出口。`completed=true`、`errors=[]`。最終視覺以 `test-combat-release-*.png` 為準。
- `test-shortcuts.json`：聊天開啟時 F6、空白略過預寫台詞、F7、重複 F7、裝填中 F6、再次 F7、E 補給均通過；一個場景、三名啟用敵人，`completed=true`、`errors=[]`。後續補上跳段重設步速的單行修正，不改捷徑狀態流程。
- `preserved-scenes.json`：第一章新版警察、第二章場景 SHA256 與開始前相同。場景檔第三章约 6.6 MB、手部網格約 8.9 MB；`largest-assets.json` 最大資產 89,742,488 bytes，均低於 100 MB。
- `audit.txt`／`scale.txt`：交接時編譯、遺失引用、停止播放與一般／放大 Game View 等比例檢查。臨時 `Chapter3Batch2Probe` 與 meta 移到 `verification-tools/`，沒有留在正式 Assets。

前期 `test-combat`、`test-full` 與 `test-combat-final` 的手模型畫面不是最後驗收：曾修正烘焙手網格的重複 100 倍倍率、暫存來源物件與舊網格渲染資料，再換新的保存網格和持握構圖。早期臨時 probe 的語法錯誤已修正，最後測試在成功編譯後執行。

未做獨立建置、實體 VR、真人鍵盤全段操作、LLM 品質回歸或第一／第二章整段重播。Editor 進 Play 仍有本機 OpenXR runtime 未配置的既有啟動診斷，`errors=[]` 指測試開始後蒐集到的執行時錯誤，不代表整個專案 Console 從未有錯誤／警告。未 commit 或 push。

交接完成：audit.txt 確認 playing=False、dirty=False、compileFailed=False、missingScripts=0、missingMeshes=0、sceneCount=1。scale.txt 一般 x/y 同為約0.32、放大同為約0.81，已回一般版面。移出測試工具後 cleanup-import.log 完成組件重載、沒有 C# 編譯錯誤；cleanup-files-unchanged.json 的五個正式檔案雜湊保持一致。
