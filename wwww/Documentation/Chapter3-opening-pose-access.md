# 第三章：固定開場、達多蹲伏手勢與校內通行

2026-11-01，依使用者三張實玩截圖修改正式 `Assets/Scenes/第三章.unity` 使用的五份 runtime 腳本。參考固定劇情文字第 8–10 頁；以下台詞及站位為遊戲安排。

## 修改

1. 駐在所開場不再呼叫兩輪 `Chapter3OpeningConversation.Begin`，也不再預備該開場 LLM。地點／清晨的開場後，巴萬直接說「我們到馬赫坡駐在所了。莫那在前面，我帶你過去，聽他安排。跟我來。」；說完立即接原 `ApproachMona` 帶路。沒有輸入框或等待玩家回答。莫那說明、武器選擇、領取後的準備問答保留。舊開場類別仍留作相容介面，但正式流程不再啟動它。
2. 達多岩石後的姿勢改用 `Chapter3SchoolCompanionPose.SetSheltered`：保留原蹲伏、雙腳接地及身體位置，雙手置於各自膝腿附近，掌心向下。與比胡交火時遮護頭胸的姿勢分開；選定任務後仍清除蹲伏姿勢並正常行走。
3. 移除達多路線 `ConstrainAssault` 的校內 z=24／43 隱形限制。四組交戰既然同時發生，玩家便能走近任一組。仍保留場景外圍、建築、器材、人物的實體碰撞；巴萬／比胡側路既有進度限制未改。四組擊倒、達多與玩家集合、吹哨及全員實際到齊條件照舊。

## 本輪證據

證據目錄：`_CodexBackups/chapter3_opening_pose_access_20261101/`。

- `report-before.json`：修改前重現，6 項通過。空地起點 (5,23) 用 W 輸入兩秒，位置精確停在 z=24；`Constrain` 對期望 z=30 回傳 z=24。`tado-before.png` 同時重現使用者第二張圖的托掌姿勢。
- `report-opening.json`：最終版 11 項通過，`completed=true`、`errors=[]`。固定介紹、無開場問答、自動實走至莫那、武器選擇、領刀後保留準備問答、F6 重進及介紹中跳至學校取消均通過。
- `report-access.json`：最終版 28 項通過，`completed=true`、`errors=[]`。自然觸發達多分工、新手勢、選任務解除蹲伏、從岩石旁經校門實走到後方第四組、W/S/A/D、實際獵刀擊倒第四名警察、未清第一組不跳進度、未全清不能吹哨、倒下重試及 F8 重置均通過。跨越原界線的測點由 (4.87,22.52) 到 (4.91,28.04)。

最終兩份驗收共 39 項，不把修改前重現混入。引擎完成幀圖包括 `opening-introduction.png`、`automatic-escort.png`、`tado-access.png`、`last-group-reached.png`，已實際檢視。

測試層級：正式場景 Editor Play Mode，WASD 為 Input System 合成事件，行走採實際 `Chapter3Player`／`CharacterController`，進第四組後走正式武器攻擊入口。學校測試開頭使用 F8 等效入口及達多旁定位，之後到第四組沒有傳送或關閉碰撞；開場領取使用桌旁定位；倒下重試注入 100 點傷害。未測真人全程鍵鼠、實體 VR、獨立 exe、真實 LLM 回答或全章末。

`diagnostics/` 的首次測點被可見器材桌擋住、編譯重載與啟動重疊造成的暫態錯誤，以及暫時工具首次編譯命名衝突，均不是最終通過報告。修正測點至空地、完成編譯後重新從 Edit Mode 開始，才得到上述結果。

收尾：playing=false、dirty=false、compileFailed=false；第一／第二／第三章場景與 Build Settings 四份 SHA256 不變。臨時工具及 meta 移至本目錄的 `verification-tools/`，移除後再確認 Unity 編譯與重載。未 commit／push。
