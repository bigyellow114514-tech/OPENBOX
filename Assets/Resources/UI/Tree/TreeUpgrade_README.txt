仙树升级 UI 素材包

所有素材为 PNG/RGBA，按 1024x768 界面实际拼装尺寸导出。素材外部为透明区域（如按钮、图标、标题框），未加白底/黑底。

建议在 Unity 中：
- 以 layout/xianshu_upgrade_layout_1024x768.json 的 rect 作为初始摆放参考。
- 文字、概率数值、金币数量、倒计时建议使用 TextMeshPro 动态渲染，不要烘焙在图片里。
- item_slot_* 与 item_* 图标分离，便于后续替换或扩展掉落物。
- preview/reference_effect_1024x768.png 是效果图参考；preview/preview_composed_from_assets_1024x768.png 是素材拼装预览。
