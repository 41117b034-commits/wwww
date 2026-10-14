# 第三章地面、標線、聊天捲動與校門跟隨修正

日期：2026-10-14。接續「調整霧社事件場景與對話」最後六張截圖的要求，沿用正式 `Assets/Scenes/第三章.unity`。

- 黃色地面短線只顯示在玩家到巴萬之間。整條短線都必須在玩家前方 0.3 m、巴萬前方保留 0.7 m 的區間內；靠得很近或超越帶路者時隱藏，不再延伸到人物後方。
- 駐在所庭院與道路原本同高度的方塊表面互相重疊。現在以原表面聯集產生單一不重疊網格，停用原方塊 Renderer，保留道路邊界與原碰撞體。
- 問答保留完整顯示紀錄、滾輪及拖曳操作，移除右側顯眼的捲軸。
- 23 顆庭院石緣新增實體 BoxCollider。低矮石頭可被 CharacterController 踏上，並非不可跨越的牆；玩家不再直接穿過石頭體積。
- 新增碰撞後，完整出發測試發現一個巴萬停靠點與兩處比胡側向路徑碰到石頭。三顆石頭向路邊移位，保留高度、尺寸、旋轉及碰撞，不更動帶路路點或共用避障邏輯。
- 校門旁的跟隨加入停止／再起步距離差。抵達最後一個玩家路徑點後持續走向停靠位置，避免下一幀又折回舊點；巴萬、比胡使用既有步行姿勢平滑設定。達多交談及觀看運動會流程保留。

程式：`Chapter3RouteGuide.cs`、`Chapter3ConversationHistory.cs`、`Chapter3SchoolController.cs`、`Chapter3GroundGateAuthoring.cs`；場景及新地面網格 `Assets/Chapter3/Station/Continuous courtyard and road.asset`。

## 證據與驗證範圍

本輪原檔、診斷、測試及引擎渲染圖在 `_CodexBackups/chapter3_ground_gate_20261014/`。前一對話的地面／校門報告已另存 `test-surface-before-resume.json`、`test-gate-before-resume.json`，與本次重跑分開。

本次原專案 Editor Play Mode 四份最終報告均 `completed=true`、`errors=[]`、`warnings=[]`，共 **89 項**布林判定通過：

| 報告 | 項數 | 實際範圍 |
| --- | ---: | --- |
| `test-final-knife.json` | 32 | 兩輪問答、滾輪／拖曳、超越帶路者、刀選擇與領取、準備問答、全段出發、等候、短交戰銜接、F6 重入 |
| `test-final-rifle.json` | 32 | 同上，改選步槍；兩條流程比胡後退里程皆 0 |
| `test-surface.json` | 5 | 23,265 個取樣驗證連續表面恰好覆蓋原聯集且不重疊、23 顆石緣碰撞體、兩處實際 CharacterController 接觸／踏上 |
| `test-gate.json` | 20 | 校門停靠、交談後觀看運動會、任務選擇與 F8 重入；三名角色根節點逐幀位置／朝向紀錄 |

已查看本次引擎渲染的兩個地面視角、無捲軸聊天紀錄、標線及校門畫面。移動採 CharacterController 驅動，按鍵採 Input System 事件，聊天採 UI／ScrollRect callback；超越測點使用 Warp。這些結果不是真人完整鍵鼠驗收，校門測試亦未重新跑完兩條戰鬥任務。

`diagnostic-stone-blocked-departure.json` 是續做時重現新增碰撞擋住出發路線的失敗報告，不列入通過數。修正後 `route-audit.txt` 的十個路點皆可到達，並沿巴萬中心線及比胡側向路線每 0.2 m 檢查碰撞與道路範圍。

正式 LLM 模型、服務與 15 秒逾時設定未改。問答流程測試僅將測試執行個體逾時縮短，使用備援文字檢查捲動、顯示與流程；不代表重新驗證真實模型延遲或回答品質。沒有實體 VR 或獨立建置測試。

第一／第二章場景及 Build Settings 與本輪原始保護雜湊相同。正式第三章停止 Play，場景未 dirty，編譯失敗旗標為 false，遺失腳本／網格皆為 0；測試工具與 meta 已移出 Assets，封存在 `verification-tools/`。最終場景、網格與四個正式程式的雜湊記錄為 `final-validation-hashes.json`。

移除測試工具後，Editor assembly 再次編譯與 domain reload 成功，見 `cleanup-import.log`。報告的零錯誤不等於清除既有 OpenXR 診斷或早期診斷日誌；未新增 commit／push。
