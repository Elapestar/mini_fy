# mini_fy 设计文档

**日期**: 2026-05-28
**版本**: 1.0
**状态**: 已确认

---

## 1. 项目概述

mini_fy 是一个轻量级 Windows 截图翻译工具。用户在任意场景下（游戏、网页、文档、视频）通过快捷键框选屏幕区域，程序自动 OCR 识别英文文字并调用百度翻译 API 翻译为中文，结果以悬浮窗展示。

**核心目标**: 启动快、占用低、操作短、翻译准、不卡顿。

---

## 2. 技术栈

| 层 | 选型 | 理由 |
|---|---|---|
| 桌面框架 | WPF (.NET 8) | 成熟稳定，Win32 互操作方便，系统级功能支持好 |
| OCR | Windows.Media.Ocr | Windows 10+ 内置，零额外依赖，英文识别准确率高 |
| 翻译 API | 百度大模型文本翻译 API | Bearer Token 鉴权，大模型翻译质量好 |
| HTTP | .NET HttpClient | 内置，异步，轻量 |
| 配置 | 本地 JSON 文件 | 零依赖，读写简单 |
| 日志 | 自定义轻量日志 | 不引入 Serilog 等大依赖 |

**无额外框架**: 不用 MVVM Toolkit、DI 容器、Prism。WPF 内置绑定对简单 UI 足够。

---

## 3. 架构：轻量事件驱动

```
App 入口 ──→ 协调各独立 Service 生命周期
              ├── HotkeyService      全局快捷键 (Ctrl+Alt+Q)
              ├── ScreenshotService  全屏截图 + 半透明遮罩 + 框选
              ├── OcrService         Windows.Media.Ocr 封装
              ├── TranslateService   百度翻译 API + 内存缓存
              ├── OverlayService     翻译结果悬浮窗
              ├── TrayService        系统托盘 + 右键菜单
              └── SettingsService    JSON 配置读写
```

每个 Service 实现对应接口，职责单一、独立可测。

---

## 4. 核心数据流

```
Ctrl+Alt+Q
    │
    ▼
ScreenshotService.Capture()
    ├── 全屏截图 (Graphics.CopyFromScreen)
    ├── 全屏透明遮罩 Window (冻结画面效果)
    ├── 用户拖拽框选 / Esc 取消
    └── 返回 Bitmap
    │
    ▼
OcrService.RecognizeAsync(Bitmap)     ← 后台线程
    └── 返回 string (英文原文)
    │
    ├── 为空? → Overlay 显示"未识别到文字"
    │
    ▼
TranslateService.TranslateAsync(text) ← 后台线程
    ├── 查缓存 (ConcurrentDictionary)
    └── 调用百度 API → 返回译文
    │
    ├── 失败? → Overlay 显示错误信息
    │
    ▼
OverlayService.Show(原文, 译文)       ← UI 线程
```

---

## 5. 模块设计

### 5.1 HotkeyService

- `RegisterHotKey` Win32 API（非低级键盘钩子）
- 快捷键：`Ctrl + Alt + Q`
- 注册失败时弹警告（可能被其他程序占用）

### 5.2 ScreenshotService

- `Graphics.CopyFromScreen` 全屏截图
- WPF 全屏无边框 Window（`Topmost`, `AllowsTransparency`）
- 半透明遮罩 + 选中区域原图显示（"冻结画面"效果）
- 拖拽框选（Canvas + MouseDown/Move/Up）
- Esc 取消，释放资源无副作用

### 5.3 OcrService

- `Windows.Media.Ocr.OcrEngine`，语言 `en`
- Bitmap → SoftwareBitmap → RecognizeAsync → 拼接 Lines
- 识别失败/无文字返回空字符串

### 5.4 TranslateService

- **API**: `POST https://fanyi-api.baidu.com/ait/api/aiTextTranslate`
- **鉴权**: `Authorization: Bearer {API_KEY}`
- **请求体**: `{"appid":"...", "from":"en", "to":"zh", "q":"..."}`
- **超时**: 10 秒
- **缓存**: `ConcurrentDictionary<string, string>` 内存缓存，程序退出清空

错误处理：

| 错误码 | 含义 | 用户提示 |
|--------|------|----------|
| 52001 | 请求超时 | 翻译超时，请重试 |
| 52002 | 系统错误 | 翻译服务异常，请重试 |
| 52003 | 未授权用户 | APPID 无效或服务未开通 |
| 54000 | 必填参数为空 | 请求参数错误 |
| 54001 | 签名/Token 错误 | API Key 配置错误 |
| 54003 | 访问频率受限 | 请求太频繁，请稍后 |
| 54004 | 账户余额不足 | 翻译额度已用完 |
| 59003 | q 超 6000 字符 | 文本过长，请缩减截图区域 |

### 5.5 OverlayService

- WPF Window（`Topmost`, `ShowInTaskbar="False"`, `WindowStyle="None"`）
- 布局：原文（灰色小字，可折叠）→ 译文（草绿色 `rgb(124,252,0)` 大字加粗）→ [复制译文] [关闭]
- 位置：截图区域右下角偏移 20px → 超出屏幕则居中
- 双击译文 → 复制到剪贴板
- 复制反馈：按钮变绿 0.5s
- 关闭：Esc / 点窗口外 / 点关闭按钮

### 5.6 TrayService

右键菜单：开始截图 | 设置 | 查看日志 | 退出
左键双击托盘 → 开始截图

### 5.7 SettingsService

- 配置文件：`settings.json`（程序同目录，不提交 git）
- 读：`JsonSerializer.Deserialize<AppSettings>`
- 写：`JsonSerializer.Serialize`（带缩进）

配置结构：
```json
{
  "hotkey": { "modifiers": "Ctrl+Alt", "key": "Q" },
  "baiduApi": { "appId": "", "apiKey": "" },
  "ocr": { "language": "en" },
  "screenshot": { "autoSave": false, "savePath": "%TEMP%/mini_fy/screenshots" },
  "general": { "autoCopyTranslation": true, "autoStartWithWindows": false }
}
```

---

## 6. 设置面板 UI

```
┌──────────────────────────────────────┐
│  mini_fy 设置                    [_] │
├──────────────────────────────────────┤
│  快捷键:  Ctrl + Alt + Q      [修改] │
│  ────────────────────────────────    │
│  百度翻译 API                        │
│    APPID:      [______________]      │
│    API Key:    [______________]      │  (密码框)
│  ────────────────────────────────    │
│  OCR 语言: ● 英文  ○ 自动检测(计划)  │
│  ────────────────────────────────    │
│  ☑ 截图后自动保存                   │
│  ☐ 开机自启                         │
│  ☑ 翻译结果自动复制到剪贴板         │
│  ────────────────────────────────    │
│  [清理缓存] [查看日志]              │
│  ────────────────────────────────    │
│                 [保存] [取消]        │
└──────────────────────────────────────┘
```

---

## 7. 项目结构

```
mini_fy/
├── mini_fy.sln
├── README.md
├── .gitignore
│
├── src/
│   ├── mini_fy.App/                    # WPF .NET 8 主项目
│   │   ├── mini_fy.App.csproj
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── Services/
│   │   │   ├── HotkeyService.cs
│   │   │   ├── ScreenshotService.cs
│   │   │   ├── OcrService.cs
│   │   │   ├── TranslateService.cs
│   │   │   ├── OverlayService.cs
│   │   │   ├── TrayService.cs
│   │   │   └── SettingsService.cs
│   │   ├── Models/
│   │   │   ├── AppSettings.cs
│   │   │   ├── TranslationResult.cs
│   │   │   └── HotkeyConfig.cs
│   │   ├── Views/
│   │   │   ├── SettingsWindow.xaml
│   │   │   └── TranslationOverlay.xaml
│   │   └── Helpers/
│   │       ├── Win32Api.cs
│   │       └── LogHelper.cs
│   │
│   └── mini_fy.Tests/                  # MSTest 单元测试
│       ├── mini_fy.Tests.csproj
│       └── Services/
│           ├── TranslateServiceTests.cs
│           ├── OcrServiceTests.cs
│           └── SettingsServiceTests.cs
│
├── config/
│   └── settings.example.json
│
└── docs/
    └── superpowers/
        └── specs/
            └── 2026-05-28-mini_fy-design.md
```

---

## 8. 安全与隐私

- 截图仅在本地处理，不上传图片
- 翻译请求只发送 OCR 提取的文本，不发送图片
- API 密钥从 `settings.json` 读取，不硬编码
- `settings.json` 不入 git
- 日志不记录完整密钥（仅记录后 4 位）
- 用户可手动清理缓存和日志

---

## 9. 性能要求

| 指标 | 目标 |
|------|------|
| 后台 CPU | ≈ 0% |
| 后台内存 | ≤ 100MB |
| 快捷键响应 | 即时（< 50ms 感知延迟） |
| 截图框选 | 流畅无延迟 |
| OCR + 翻译 | 异步，不阻塞 UI 线程 |
| API 请求超时 | 10s |

---

## 10. 扩展预留

核心闭环完成后可考虑：
- 更多语言支持（日文、韩文）
- 自动检测源语言（`from: "auto"`）
- 离线词典 / 本地翻译模型
- OCR 区域历史记录
- 截图后手动编辑文本再翻译
- 多显示器支持
- 剪贴板图片直接翻译
- Steam 游戏覆盖层支持
