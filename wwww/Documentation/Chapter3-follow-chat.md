# 第三章駐在所問答與同行修正

日期：2026-10-14。沿用正式 `Assets/Scenes/第三章.unity`，本次修改集中在駐在所控制器與交談顯示；未擴增劇情批次。

後續修正：同日的 `Chapter3-ground-gate.md` 已取代下方「右側捲軸」與標線顯示範圍：現在隱藏捲軸但保留滾輪／拖曳，標線只顯示在玩家與帶路者之間。本文件的 56 項是前一輪歷史驗證。

- 開場淡入與原 3 秒停留期間，下方字幕也顯示「馬赫坡駐在所 · 清晨」，開始問答時收起。
- 問答顯示歷史與模型的短上下文分開。送出立即加入玩家原文，答覆追加到紀錄；輸入第二題時仍保留第一題與答覆。支援滾輪、拖曳與右側捲軸。開場與拿武器後的準備問答都適用，各自開始時清空該段舊紀錄。
- 刪除第二輪回答後硬加的「莫那在前面。跟我來。」；接續單一帶路字幕「先去見莫那，聽他安排。跟我來。」保留玩家實際問題與回答。
- Escort 和 Gather 等候都讓巴萬、比胡持續看向玩家；停下交談的非發言同伴也自然轉向玩家。轉向有速度限制，不瞬間扭轉。
- 帶路階段沿既有路線生成黃色地面短線，具有世界深度遮擋。集合交談、武器選擇、離開駐在所或重入時清除。灰色道路邊界及場景地形未改。
- 領取武器後兩位同伴留在現有站位交談；移除巴萬先迎向桌邊、出發又返回原路點的折返。比胡依固定路徑里程跟隨，等巴萬通過既有站位再出發，轉角使用連續的側向路徑。

程式：`Chapter3StationController.cs`、`Chapter3OpeningConversation.cs`、`Chapter3ReadinessConversation.cs`；新增 `Chapter3ConversationHistory.cs`、`Chapter3RouteGuide.cs`。保留原共用 PathMotor，避免影響學校與短交戰的移動。模型、服務與逾時設定未改。

驗證與修改前備份：`_CodexBackups/chapter3_follow_chat_20261014/`。最終 `test-knife.json` 與 `test-rifle.json` 各 28 項通過，兩份皆 completed=true、errors=[]、warnings=[]。涵蓋開場字幕、兩輪問答保留、ScrollRect 滾輪 callback、各段等候朝向、刀槍領取後原地站位、全段帶路、轉角、入短交戰與 F6 重入清理；兩次比胡路徑里程的後退累積皆 0。已檢視引擎渲染圖。

本輪測試為原專案 Editor Play Mode，使用 CharacterController 行走、Input System 事件與 UI callback；刻意把測試執行個體的 LLM 逾時縮到 .01 秒，以穩定驗證畫面與流程，未重測真實模型的生成品質／延遲，正式設定仍為 15 秒。未測真人完整鍵鼠操作、實體 VR 或獨立建置。

診斷 `diagnostic-corner-reversal.json` 是修正連續轉角前的失敗報告；`diagnostic-turn-settle.json` 在自然轉身僅 .25 秒時檢查過早，最終檢查等待 1.5 秒並保留巴萬發言時間，沒有以比胡開始說話的瞬間轉身冒充非發言時朝向。診斷報告不當作通過證據。第一／第二／第三章場景及 Build Settings 雜湊皆未改。

收尾：正式第三章停止 Play、場景未 dirty，compileFailed=false、missingScripts=0、missingMeshes=0。暫時 probe 及 meta 封存到本輪 `verification-tools`；移出後 Editor assembly 編譯與 reload 成功，正式五個程式雜湊與通過測試版本一致。
