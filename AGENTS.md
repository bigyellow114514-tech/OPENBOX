# Codex Project Instructions

## Unity UI Development Rules

When working on this Unity project, Codex must follow these rules for any task related to UI.

### 1. Use UGUI for all UI

For any UI creation, modification, refactor, layout adjustment, popup, menu, HUD, panel, button, list, dialog, tooltip, overlay, or visual interface work:

- Always use Unity UGUI.
- Use `Canvas`, `RectTransform`, `Image`, `Button`, `TextMeshProUGUI`, `LayoutGroup`, `ScrollRect`, `ContentSizeFitter`, and other UGUI-based components as appropriate.
- Do not use UI Toolkit, IMGUI, custom runtime drawing, or non-UGUI solutions unless the user explicitly requests it.

### 2. UI must be visible in the Unity Hierarchy outside Play Mode

All UI objects and their child nodes must be visible and editable in the Unity Editor while the project is not running.

Codex must not create important UI only at runtime through code such as:

```csharp
new GameObject(...)
Instantiate(...)
```

# Project Rules

## Sprite Sequence Production

生产序列帧资源时，如果有参考角色图，输出帧画布应接近原图尺寸。

When producing animation frame sequences from a reference character image, keep the output frame canvas close to the original reference image size. Do not shrink frames to a small default canvas such as 128x128 unless the import settings or runtime scaling are explicitly adjusted to preserve the original in-game display size.
