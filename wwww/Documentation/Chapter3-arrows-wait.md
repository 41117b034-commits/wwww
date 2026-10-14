# 第三章等候回頭與黃色箭頭

2026-10-14，依使用者兩張截圖修正：山路短交戰後兩名同伴都要回頭等玩家；學校入口到達多的路上加黃色箭頭；第三章所有遊戲指引箭頭統一第一章樣式。

## 本次修改

- `Chapter3Combat` 戰後帶路超過 5.5 公尺時，巴萬與比胡一起停止並持續平滑朝向玩家；玩家走近到 4.6 公尺內才恢復，避免在停止距離邊緣反覆轉身。朝向更新在 LateUpdate 執行，避免共用角色 Update 的最後移動方向覆蓋回頭。
- `Chapter3YellowArrow` 依第一章 `Chapter1PerformanceController.CreateDeliveryGroundArrow` 與場景序列化值建立箭桿及雙翼；黃色 `(1, .82, .02, 1)`、發光倍率 2.4、大小 1.05、離地 .08 公尺、間距 2.2 公尺、脈動幅度 .1、速度 4.5。地面箭頭沒有碰撞。
- `Chapter3RouteGuide` 原黃色短線改成上述箭頭，保留駐在所的既有路線與同伴取樣；整支箭頭含最大脈動尺寸只出現在玩家與帶路者之間，不伸到同伴前方。
- `Chapter3PickupArrow` 用相同箭桿／雙翼輪廓、黃色與脈動替換原黑框實心箭頭；保留刀／槍目標與畫面邊緣指引。
- `Chapter3SchoolController.ArrivalGuide` 沿學校出生點到校門左側達多顯示地面箭頭，走過的箭頭收起；交談、跳段與重入清理，F8 重進恢復。

第一、第二章腳本與場景不修改；第三章仍用原單一場景，空白鍵進運動會與第五批內容保留。場景內木牌屬實體場景標示，不是遊戲指引物件。

## 驗證

原檔、保護雜湊、臨時驗證工具與本次引擎圖片置於 `_CodexBackups/chapter3_arrows_wait_20261014/`。

原專案 Unity 6000.0.58f1 Play Mode 四份報告均 `completed=true`、`errors=[]`、`warnings=[]`，共 81 項：

| 報告 | 項數 | 範圍 |
| --- | ---: | --- |
| `test-all.json` | 42 | 三發實際步槍擊倒巡警、雙人停下回頭／側移追視、5.5／4.6m 等候門檻、恢復帶路、自然接入學校、箭頭完整範圍／朝向／無碰撞、接近及交談收起、F8 重進及 F6 清理 |
| `test-school.json` | 17 | 空白鍵直接進運動會、入口穩定畫面、沿路移動與箭頭收起、交談及重入清理 |
| `test-station-knife.json` | 11 | 兩輪備援問答、駐在所帶路箭頭、選刀、領刀箭頭輪廓與黃色、領取及跳段清理 |
| `test-station-rifle.json` | 11 | 同上，改選步槍 |

巴萬／比胡的實際 `Rig.Forward` 與玩家方向夾角均低於 .03°；側移後維持等候位置。已檢視雙人正面、側移、學校入口／中途、駐在所帶路與刀／槍箭頭引擎畫面。

測試以 Unity Input System 事件、CharacterController、UI callback 與實際武器入口驅動；短交戰初始武器由 driver 指定，台詞時間縮短。駐在所問答只縮短該次 Play 執行個體逾時以用備援文字，不代表真實 LLM 品質／延遲回歸。沒有真人整段鍵鼠、原生遊戲按鍵、實體 VR 或獨立建置驗證；第五批及兩條學校任務未重播。

最早入口圖片在生成箭頭的同幀立即呼叫 Camera.Render，部分呈綠色，另存 `diagnostic-first-frame-entry.png`。相同 runtime 稍後與重新進入的穩定畫面均為黃色；本地 URP 有載入用替代 shader，但未斷言根因。補測入口保留原視角、等待 .6 秒及略過開場字幕再拍，見最終 `school-entry-yellow-arrows.png`；沒有以改材質顏色隱藏診斷。測試工具的早期 CanvasRenderer.GetMesh API 編譯錯誤已修正後重編，屬工具而非正式腳本。

收尾以 `audit.txt`、`final-summary.json`、`cleanup-import.log` 為準。場景及第一章腳本保護雜湊、五個 runtime 驗證版本雜湊均核對；臨時 probe 與 meta 移出 Assets。報告零錯誤不等同清除既有 Editor／OpenXR 啟動診斷。
