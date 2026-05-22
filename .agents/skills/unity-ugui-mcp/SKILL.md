---
name: unity-ugui-mcp
description: Use this skill for any Unity UI task, including creating, modifying, refactoring, or fixing UI, menus, panels, HUDs, dialogs, buttons, lists, popups, overlays, layout, or visual interface elements. This skill requires Unity UGUI and a connected MCP server before making any changes.
---

# Unity UGUI + MCP UI Workflow

Use this skill whenever the task involves Unity UI creation, modification, refactoring, layout adjustment, visual interface work, or UI-related scripts/prefabs/scenes.

## Mandatory preflight: MCP must be connected

Before modifying any Unity scene, prefab, UI object, script, or asset:

1. Connect to the configured MCP server.
2. Use MCP to inspect the relevant Unity project state when applicable, including scenes, prefabs, hierarchy, assets, and existing UI structure.
3. If MCP is unavailable, disconnected, misconfigured, unreachable, or the required Unity MCP tools cannot be used, stop immediately.

If MCP is not connected, do not modify files and do not guess the project structure. Respond exactly:

> MCP Server is not connected. Please start or reconnect the MCP Server before I continue.

Only continue after MCP is available.

## UI implementation rules

All UI must be implemented using Unity UGUI.

Use UGUI components such as:

- `Canvas`
- `RectTransform`
- `Image`
- `Button`
- `TextMeshProUGUI`
- `LayoutGroup`
- `HorizontalLayoutGroup`
- `VerticalLayoutGroup`
- `GridLayoutGroup`
- `ContentSizeFitter`
- `ScrollRect`
- `CanvasGroup`
- Other UGUI-compatible components as needed

Do not use:

- UI Toolkit
- IMGUI
- Runtime-only custom drawing
- Non-UGUI UI systems

unless the user explicitly requests it.

## Hierarchy visibility requirement

All important UI objects and child nodes must be visible and editable in the Unity Editor outside Play Mode.

When creating or modifying UI:

1. UI elements must exist in the scene hierarchy or prefab hierarchy.
2. All important child nodes must be visible in the Unity Hierarchy before entering Play Mode.
3. The user must be able to manually inspect and adjust the UI in Unity without running the game.
4. Use clear, meaningful GameObject names.
5. Prefer serialized references over runtime object lookup.
6. Prefer prefabs for reusable UI.
7. Do not hide important UI structure inside runtime-only generated objects.

Avoid creating important UI only through runtime code such as:

```csharp
new GameObject(...)
Instantiate(...)