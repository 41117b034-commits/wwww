# 第一章舞蹈互動操作

## 目標效果

玩家靠近舞圈後，畫面提示「按 E / 手把觸發 加入舞蹈」。按下後玩家視角會繞著火堆慢慢旋轉，並有輕微上下踏步感。舞蹈結束後會接到警察登場段落。

## 一、檢查 DanceTrigger

1. 在左邊「階層 / Hierarchy」點 `DanceTrigger`
2. 右邊「檢查器 / Inspector」確認：
   - `Mesh Renderer / 網格渲染器`：取消勾選
   - `Box Collider / 盒型碰撞器`：保留
   - `Is Trigger / 是觸發器`：勾選
   - `Chapter 1 Interactable / Chapter1Interactable`：保留

## 二、設定 Chapter1Interactable

在 `DanceTrigger` 的 `Chapter 1 Interactable / Chapter1Interactable` 裡：

- `Controller / 控制器`：拖入左邊的 `Chapter1_PerformanceController`
- `Player Root / 玩家根物件`：拖入左邊的 `XR Origin (VR)`
- `Interaction Type / 互動類型`：選 `Join Dance / 加入舞蹈`
- `Also Use E Key / 也使用 E 鍵`：勾選
- `Show Prompt / 顯示提示`：勾選
- `Prompt Text / 提示文字`：可以填 `按 E / 手把觸發 加入舞蹈`
- `Use Distance Check / 使用距離判定`：勾選
- `Interact Range / 互動距離`：建議 `4` 到 `6`

如果 `Interaction Type / 互動類型` 還停在 `Start Police Sequence / 開始警察段落`，只要物件名稱還叫 `DanceTrigger`，目前也會先跑舞蹈。

## 三、設定 Chapter1_PerformanceController

在左邊「階層 / Hierarchy」點 `Chapter1_PerformanceController`，右邊找到 `Immersive Dance / 沉浸式舞蹈`：

- `Player Root / 玩家根物件`：拖入 `XR Origin (VR)`
- `Dance Center / 舞蹈中心`：拖入 `DanceTrigger`
- `Dance Duration / 舞蹈秒數`：建議 `9`
- `Dance Radius / 舞蹈半徑`：建議 `3.2`
- `Dance Orbit Degrees / 旋轉角度`：建議 `330`
- `Dance Step Height / 踏步高度`：建議 `0.08` 到 `0.12`
- `Start Police After Dance / 舞蹈後開始警察段落`：勾選

## 四、讓族人圍著火堆跳舞

對每個要跳舞的族人角色做一次：

1. 在「階層 / Hierarchy」點族人角色
2. 右邊「檢查器 / Inspector」按 `Add Component / 添加元件 / 增加元件`
3. 搜尋 `Chapter1CircleDancer`
4. 加入後確認：
   - `Center / 中心`：拖入 `DanceTrigger`
   - `Play On Awake / 開始時播放`：勾選
   - `Orbit Speed Degrees / 繞圈速度`：建議 `12` 到 `22`
   - `Step Height / 踏步高度`：建議 `0.04` 到 `0.08`
   - `Face Center / 面向中心`：勾選

## 五、測試方式

1. 按上方 `Play / 播放`
2. 點 `Game / 遊戲` 分頁
3. 用玩家靠近火堆旁的 `DanceTrigger`
4. 看到提示後按 `E`
5. 玩家視角會開始繞火堆踏步，舞蹈結束後警察會出現

如果太暈，把 `Dance Orbit Degrees / 旋轉角度` 降到 `180` 到 `240`，或把 `Dance Step Height / 踏步高度` 降到 `0.04`。
