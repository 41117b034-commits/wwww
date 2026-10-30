# 本輪證據索引

工作 2026-10-29 開始、2026-10-30 完成。正式場景：Assets/Scenes/第三章.unity。

最終報告僅 `final-reports.json` 所列四份：link 8、school-quick（步槍）58、school-knife 53、bypass 3，共 122 項（含重複觀測），全部 completed=true、errors=[]。

`before/` 是本輪修改前 runtime。`tested-runtime-hashes.json` 是最終驗證版本。`protected-before.json` 保存正式場景、Build Settings、default layout 的原始雜湊。`verification-tools/` 是已移出 Assets 的 Editor 驅動工具，保留其定位、生命維持、直接清場等 fixture 以便審核。

根目錄 PNG 是最終驗證的完成幀截圖。`diagnostics/` 保留早期失敗、舊位置混戰畫面、初始場景盤點；不能用來宣稱最終通過。早期 link 報告有截圖後跨幀位移時間取樣錯誤，後續使用相應移動時間重新驗證。

第六張圖原本卡住未精確重現，bypass 新舊版本均可抵達。最終兩種武器路線均全員會合，獵刀繼續到比胡受傷／救援選擇；未驗完整結局、真人全程鍵鼠、實體 VR 或獨立 exe。

詳細改動與驗證邊界：Documentation/Chapter3-continuous-campus.md。
